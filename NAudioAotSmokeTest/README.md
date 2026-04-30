# NAudioAotSmokeTest

A console smoke that validates the source-generated COM bridging in
`NAudio.Wasapi` survives the trim/AOT compiler. Built as part of the regular
solution build so a regression in the `[GeneratedComInterface]` /
`[GeneratedComClass]` analyzer story breaks CI immediately
(`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`).

## What it covers

The program runs in two phases against the default render endpoint:

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

## How CI uses it

`dotnet build NAudio.slnx` builds this project alongside the rest. With
`<IsAotCompatible>true</IsAotCompatible>` and `<PublishAot>true</PublishAot>`
set, the trim/AOT analyzer runs on every build. Any new `[RequiresUnreferencedCode]`-annotated
call from `NAudio.Wasapi` or `NAudio.Core` (e.g. someone re-introducing
`Marshal.GetObjectForIUnknown`-shaped reflection paths) will surface as an
`IL2026` / `IL3050` warning, which is treated as an error here.

## Running the actual smoke locally

CI only validates the analyzer pass. To run the program end-to-end (which
requires a real audio device with non-zero default master volume that can
be driven) use one of:

```bash
# Trim publish — fastest, no MSVC required
dotnet publish NAudioAotSmokeTest/NAudioAotSmokeTest.csproj -c Release -p:PublishAot=false -p:PublishTrimmed=true

# Native AOT publish — needs MSVC link.exe on PATH (a Visual Studio
# Developer Command Prompt is the easiest way to get this)
dotnet publish NAudioAotSmokeTest/NAudioAotSmokeTest.csproj -c Release
```

Then run the produced `NAudioAotSmokeTest.exe` from the publish directory.
Expect output ending with `CCW dispatch under PublishAot: OK` and a clean
exit. A `0xC0000005` access violation, a fast-fail message, or
`zero callbacks fired` indicates a regression in the Phase 2f migration.

## Why the runtime test isn't in CI

CI agents typically have no audio hardware (or only a virtual device that
doesn't fire `IAudioEndpointVolumeCallback.OnNotify`). The build-time
analyzer pass is the part of the test that's reliable in CI; the runtime
smoke is a manual / local validation step.
