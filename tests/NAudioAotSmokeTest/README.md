# NAudioAotSmokeTest

A console smoke that validates the source-generated COM bridging in
`NAudio.Wasapi`, and the winmm interop in `NAudio.WinMM`, survive the trim/AOT
compiler. Built as part of the regular solution build so a regression in the
`[GeneratedComInterface]` / `[GeneratedComClass]` analyzer story breaks CI
immediately (`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`).

## What it covers

The program runs in several phases against the default render endpoint:

1. **RCW direction** (Phase 2d / 2e). Enumerates active render endpoints via
   `MMDeviceEnumerator`, opens the property store on each, and reads
   VT_LPWSTR / VT_UI4 / VT_BLOB property values. This exercises raw
   `CoCreateInstance` + `StrategyBasedComWrappers.GetOrCreateObjectForComInstance`
   and the `IPropertyStore` / `PropVariant` projection.

2. **CCW direction** (Phase 2f). Registers an `IMMNotificationClient`,
   subscribes to `AudioEndpointVolume.OnVolumeNotification`, drives the master
   volume four times, and counts callbacks. This exercises
   `[GeneratedComClass]` CCW vtable generation and the QI-for-IID handoff to
   native (the latter being the bug fixed by Phase 2f's
   `Query<X>Interface` helpers).

3. **winmm WAVEHDR / WAVEFORMATEX** (issue #1425). `NAudio.WinMM` used to pass
   two native types to winmm as `[StructLayout]` classes by value. CoreCLR pins
   a blittable class argument in place; NativeAOT copies it into a per-call
   temporary and, for a class hierarchy, drops the base class fields entirely.
   So `WAVEHDR` never saw the driver's `WHDR_PREPARED` (every `waveInAddBuffer`
   returned `WAVERR_UNPREPARED`) and `WaveFormatExtensible` reached the driver
   with its SubFormat GUID written over the sample rate. This phase asserts the
   `WaveFormat.MarshalToPtr` blob layout byte by byte — which needs no audio
   hardware, so it is a real regression guard anywhere — and then drives
   `WaveOut` and `WaveIn` if the machine has devices.

4. **ACM stream header** (issue #1425). `ACMSTREAMHEADER` was the third native
   type passed as a `[StructLayout]` class by value, and the first fix missed it.
   The codec keeps private state in the header's reserved tail between
   `acmStreamPrepareHeader`, `acmStreamConvert` and `acmStreamUnprepareHeader`;
   NativeAOT's per-call copy round-trips the declared fields alone, so that tail
   came back zeroed and every conversion failed. This phase converts PCM through
   `AcmStream` directly, reads through a `WaveFormatConversionStream` (the path
   `Mp3FileReader` and `AudioFileReader` decode through), and suggests a PCM
   format for GSM 6.10. ACM codecs are software, so none of it needs a device.

## How CI uses it

CI covers this project in two ways.

**The `build` job** compiles it alongside the rest of the solution. Two settings
have to line up for that to mean anything. `<IsAotCompatible>true</IsAotCompatible>`
(or `<PublishAot>`) turns the ILLink analyzer on for a project, and
[.globalconfig](../../.globalconfig) promotes its `Trimming`, `SingleFile` and
`AOT` categories to warning. Without the second, the repo's catch-all
`dotnet_analyzer_diagnostic.severity = suggestion` swallows them: they show in
the IDE, never reach the build, and `TreatWarningsAsErrors` never sees them.
That was the state until the categories were named explicitly, and it is worth
knowing because the symptom is a clean build rather than an error — `NAudio.WinMM`
was marked `IsAotCompatible` and reported zero warnings while six `IL3050`s sat
in it. With both in place, a new `[RequiresUnreferencedCode]`- or
`[RequiresDynamicCode]`-annotated call from any of the AOT-compatible projects
(e.g. someone re-introducing `Marshal.GetObjectForIUnknown`-shaped reflection, or
`Marshal.SizeOf(Type)` in place of `Marshal.SizeOf<T>()`) fails the build.

**The `aot` job** publishes it with `PublishAot`, which runs ILC for real. This
exists because the analyzer only sees *annotated* APIs, and a lot of AOT breakage
carries no annotation at all — issue #1425 (marshalling a `[StructLayout]` class
by value) and Hazard H11 in `MODERNIZATION.md` (`Marshal.GetObjectForIUnknown`)
both produced a clean analyzer pass while being broken under AOT. Publishing
catches whole-program analysis and native link failures the analyzer cannot.
It runs as a separate job so the ILCompiler download and native link stay off
the critical path.

## Running the actual smoke locally

**`dotnet run` does not test AOT.** `PublishAot` only takes effect on
`dotnet publish`, so `dotnet run` executes under the JIT — where every path here
passed even while it was broken under AOT, which is how issue #1425 survived. The
program prints the runtime it is actually executing under, and labels its results
with it, so a green JIT run can't be mistaken for AOT validation.

To run the program end-to-end (which requires a real audio device with non-zero
default master volume that can be driven) use one of:

```bash
# Trim publish — fastest, no MSVC required
dotnet publish NAudioAotSmokeTest/NAudioAotSmokeTest.csproj -c Release -p:PublishAot=false -p:PublishTrimmed=true

# Native AOT publish — needs MSVC link.exe on PATH (a Visual Studio
# Developer Command Prompt is the easiest way to get this)
dotnet publish NAudioAotSmokeTest/NAudioAotSmokeTest.csproj -c Release
```

Then run the produced `NAudioAotSmokeTest.exe` from the publish directory.
Expect output ending with `ACM stream header under NativeAOT: OK` and exit
code 0. A `0xC0000005` access violation, a fast-fail message, or
`zero callbacks fired` indicates a regression in the Phase 2f migration; a
`FAIL` line in the winmm or ACM phase (or exit code 1) indicates a regression
in the issue #1425 marshalling fix.

The banner reports whether the run is under `JIT` or `NativeAOT`, because every
one of these paths passed under the JIT while broken under AOT. A `dotnet run`
of this project proves nothing about AOT.

The winmm blob assertions and the whole ACM phase run without audio hardware, so
they are worth running even on a machine with no devices — the `WaveOut`/`WaveIn`
drives report `SKIP` there rather than failing.

## Why the runtime test isn't in CI

CI publishes the app but never launches it. CI agents typically have no audio
hardware (or only a virtual device that doesn't fire
`IAudioEndpointVolumeCallback.OnNotify`), and the program drives a real render
endpoint and master volume before it reaches anything hardware-free — so it
would fault long before the winmm blob assertions and the ACM phase, which need
no device at all.

Making the run itself CI-viable means guarding each phase independently so
absent hardware is a skip rather than an unhandled exception. Worth doing: those
blob assertions are the only place the format checks execute under genuine
NativeAOT, which is the only place the #1425 corruption was observable — the
`WaveFormatMarshalTests` equivalents run on the JIT and passed throughout.
