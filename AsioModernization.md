# NAudio 3.0 ASIO Modernization

This document records the architectural decisions, rationale, and final shape of NAudio 3.0's redesigned ASIO support. It complements [MODERNIZATION.md](MODERNIZATION.md) (WASAPI, target frameworks, Span migration).

The NAudio 2 entry point, `AsioOut`, single-handedly handled playback, recording, and duplex through an `IWavePlayer`-shaped API. That worked for simple stereo playback but created friction for multi-channel pro-audio work — recording was push, output was pull, duplex required `unsafe` IntPtr writes plus a hidden `WrittenToOutputBuffers` flag, channel selection was a contiguous offset, and `DriverResetRequest` had no supported recovery path. NAudio 3 keeps `AsioOut` for back-compat (now a facade) and ships a redesigned `AsioDevice` API alongside it.

---

## Goals

- A first-class API for the three real ASIO use cases: **playback only**, **recording only**, and **duplex processing** (record → process → play).
- Treat ASIO as it is — a low-latency, per-channel, non-interleaved interface with potentially many channels — not a stereo WAV pipe.
- Idiomatic C# by default (`Span<float>`, options objects, explicit three-state mode selection) with a zero-copy escape hatch for advanced users.
- Arbitrary input/output channel selection (not just contiguous offset+count).
- Preserve the existing `AsioOut : IWavePlayer` entry point bit-for-bit. Build the new API as `AsioDevice` alongside.

### Non-goals

- Rewriting the low-level ASIO COM interop (`AsioDriver`, `AsioDriverExt`). The internals stay; this is a public-API redesign.
- Supporting every `AsioSampleType` value. The four LSB variants in common use (`Int16LSB`, `Int24LSB`, `Int32LSB`, `Float32LSB`) are in scope.
- Cross-platform ASIO (ASIO is Windows/Steinberg).

---

## The new API: `AsioDevice`

A single class representing one opened ASIO driver, configured into exactly one of three mutually-exclusive modes before `Start()`:

```csharp
public sealed class AsioDevice : IDisposable
{
    public static string[] GetDriverNames();
    public static AsioDevice Open(string driverName);
    public static AsioDevice Open(int driverIndex);

    public AsioDriverCapability Capabilities { get; }
    public string DriverName { get; }
    public AsioDeviceState State { get; }                // Unconfigured | Configured | Running | Stopped | Disposed

    public int CurrentSampleRate { get; }
    public bool IsSampleRateSupported(int sampleRate);
    public void ShowControlPanel();

    public void InitPlayback(AsioPlaybackOptions options);
    public void InitRecording(AsioRecordingOptions options);
    public void InitDuplex(AsioDuplexOptions options);
    public void Reinitialize();

    public void Start();
    public void Stop();

    public int InputLatencySamples  { get; }
    public int OutputLatencySamples { get; }
    public int FramesPerBuffer      { get; }

    public event EventHandler<AsioAudioCapturedEventArgs> AudioCaptured;   // recording mode
    public event EventHandler<StoppedEventArgs>            Stopped;
    public event EventHandler                              DriverResetRequest;
}
```

`AsioDevice` deliberately does **not** implement `IWavePlayer` — the interface doesn't fit record-only or arbitrary-channel duplex. `AsioOut` keeps `IWavePlayer` and is retained for back-compatibility as a facade over `AsioDevice`.

### Mode 1 — Playback

`AsioPlaybackOptions` carries the source (`IWaveProvider`, with an `.From(ISampleProvider)` convenience), the output channel array, optional buffer size, and `AutoStopOnEndOfStream` (default true).

```csharp
using var device = AsioDevice.Open("Focusrite USB ASIO");
device.InitPlayback(new AsioPlaybackOptions
{
    Source = new AudioFileReader("music.wav"),
    OutputChannels = [2, 3]   // play through physical channels 3 and 4 (zero-based)
});
device.Start();
```

Source channel `n` is routed to physical output `OutputChannels[n]`. The number of entries in `OutputChannels` must equal `Source.WaveFormat.Channels`. Physical outputs not in the array receive silence.

### Mode 2 — Recording

`AsioRecordingOptions` carries the input channel array, optional sample rate (defaults to driver's current rate), and optional buffer size. Recording is event-driven: `AudioCaptured` fires once per ASIO buffer-switch with one `ReadOnlySpan<float>` per selected input.

```csharp
device.InitRecording(new AsioRecordingOptions
{
    InputChannels = [0, 1, 4, 5],
    SampleRate    = device.CurrentSampleRate
});
device.AudioCaptured += (s, e) =>
{
    var ch0 = e.GetChannel(0);   // physical input 0 (first entry in InputChannels)
    var ch1 = e.GetChannel(1);   // physical input 1
    // RMS, write to file, etc. Spans valid for the handler only.
};
device.Start();
```

`GetChannel(i)` is indexed into the **selected-channels array**, not the physical channel number. This matches JUCE/PortAudio conventions and keeps callback code portable across channel configurations.

`AsioAudioCapturedEventArgs` is a class (so handlers can close over it) but its spans point into library-owned buffers that are reused across callbacks — copy out anything the handler needs to keep.

### Mode 3 — Duplex processing

A single user-supplied `Processor` callback owns both directions. No `IWaveProvider`, no flag dance. The callback is the core low-latency DSP path.

```csharp
device.InitDuplex(new AsioDuplexOptions
{
    InputChannels  = [0, 1],
    OutputChannels = [0, 1],
    SampleRate     = 48000,
    Processor      = (in AsioProcessBuffers b) =>
    {
        var inL = b.GetInput(0); var outL = b.GetOutput(0);
        var inR = b.GetInput(1); var outR = b.GetOutput(1);
        for (int i = 0; i < b.Frames; i++)
        {
            outL[i] = inL[i] * 0.5f;
            outR[i] = inR[i] * 0.5f;
        }
    }
});
device.Start();
```

`AsioProcessBuffers` is a `ref struct` (stack-only, compiler-enforced) carrying `Frames`, `SampleRate`, `InputChannelCount`, `OutputChannelCount`, `SamplePosition`, `SystemTimeNanoseconds`, `Speed`, `TimeCode`, plus `GetInput(i)`/`GetOutput(i)` for float spans and `RawInput(i)`/`RawOutput(i)` for the zero-copy native-bytes escape hatch. Library-owned output float buffers are zeroed before each callback, so unwritten outputs are silent. See "Timing — sample position, host clock, varispeed, time code" below for what each timing field gives you.

Naming: `Duplex` is the technically correct term and unambiguous about meaning both directions. Alternatives considered (`InitProcessor`, `InitIO`, `InitCallback`, `InitRealtime`, `InitLive`) all overloaded existing terminology.

### Float-first with a raw escape hatch

Floats are the default because:

- `Span<float>` is what `ISampleProvider` already uses across NAudio 3, so the ASIO API is consistent.
- The conversion cost at typical ASIO buffer sizes (64–1024 frames) is negligible compared to user DSP work.
- `Int24LSB` in particular is painful to read correctly — having the library do it once removes a whole class of user bugs.

The raw hatch (`RawInput` / `RawOutput` returning `ReadOnlySpan<byte>`/`Span<byte>` plus `AsioSampleType`) exists for users who genuinely need zero-copy passthrough. Mixing the two in the same callback is allowed: a user may process some channels in float and memcpy others raw. Calling `RawOutput(i)` marks that channel as raw-handled and the library skips its float-to-native conversion.

Formats supported: `Int16LSB`, `Int24LSB`, `Int32LSB`, `Float32LSB`. Other native formats throw `NotSupportedException` at `Init*` time with both managed format and native `AsioSampleType` named, so the failure is loud and early.

### Channel selection — arrays of indices

The `AsioOut.ChannelOffset` / `InputChannelOffset` model is replaced by `InputChannels` / `OutputChannels` arrays of physical channel indices. Allows non-contiguous selection (`[0, 3, 5]`), makes "select all channels" a one-liner via `Capabilities.AllInputChannels` / `AllOutputChannels` (new properties on `AsioDriverCapability`), and matches JUCE/PortAudio expectations.

### Timing — sample position, host clock, varispeed, time code

Recording (`AsioAudioCapturedEventArgs`) and duplex (`AsioProcessBuffers`) both expose four driver-reported timing values per callback:

- **`SamplePosition`** (long) — frames since `Start()`, monotonically increasing. Lets a recording handler stamp every captured frame with an absolute position, splice/concatenate captures correctly after an xrun, or align multi-device recordings. Increments by `Frames` per callback under nominal operation.
- **`SystemTimeNanoseconds`** (long) — host system time at the moment the driver sampled `SamplePosition`. Pins the audio clock to the host clock at one precise instant per buffer, which is what enables A/V sync, drift correction against `Stopwatch`, and cross-device alignment. The deltas (not the absolute values) are what's portable, since the epoch the driver uses for "system time" is implementation-defined.
- **`Speed`** (double) — varispeed factor (1.0 = nominal). Non-1.0 values appear when the driver is doing pull-up/pull-down (e.g. 29.97 vs 30 fps film transfer) or external rate adaptation.
- **`TimeCode`** (`AsioTimeCodeInfo?`) — SMPTE/MTC time code from an external source (LTC input, MTC over MIDI). `null` in the common case; populated when the driver is receiving an external timecode stream and reports it as valid for the buffer. Carries `Samples`, `Speed`, and the `Running` / `Reverse` / `OnSpeed` / `Still` transport-state flags.

Two callback paths feed these. The host advertises `kAsioSupportsTimeInfo = 1` (and `kAsioSupportsTimeCode = 1`) from `AsioMessageCallBack`, which signals to the driver that it should prefer `bufferSwitchTimeInfo` over plain `bufferSwitch`:

- **Rich path** (`bufferSwitchTimeInfo`) — modern drivers call this once per buffer with a pointer to a driver-owned `AsioTime` struct. `AsioDriverExt.ReadTimingInfo` extracts the relevant fields via direct unsafe pointer reads at known offsets (Pack=4 layout) — no `Marshal.PtrToStructure<T>`, because that would marshal the `[ByValTStr]` reserved string fields into managed strings on every callback. Zero allocations in the realtime path. All four values are populated, gated on the `kSamplePositionValid` / `kSystemTimeValid` / `kSpeedValid` / `kTcValid` flag bits; missing fields fall back to `0` / `1.0` / `null` respectively.
- **Fallback path** (`bufferSwitch`) — for drivers that ignore the time-info opt-in, the plain `bufferSwitch` callback runs and we synthesise the timing info by calling `ASIOgetSamplePosition` from inside the callback. `Speed` defaults to `1.0` and `TimeCode` stays `null` — degraded but correct for the typical no-varispeed, no-external-source case.

In both paths the timing data is staged into `AsioDriverExt.LatestTimingInfo` (an `AsioBufferTimingInfo` struct) before invoking the user callback. `AsioDevice.OnBufferUpdateRecording` and `OnBufferUpdateDuplex` read it once and copy four fields onto the callback context — no per-callback driver call, no marshalling, no allocations.

The playback-only mode does not expose timing. Playback is `Read`-driven and has no per-callback context surface; if a future use case needs it, the playback path can grow a `BufferRendered` event mirroring `AudioCaptured`.

### Lifecycle

- `Init*` can only be called when `State == Unconfigured`. Reconfiguring requires a new instance.
- `Reinitialize()` is provided for `DriverResetRequest`: the device caches the last options and re-applies them. The recommended pattern is `Stop` → `Reinitialize` → `Start`.
- `Stopped` is dispatched on the captured `SynchronizationContext` (or thread pool if none) — never synchronously from the ASIO callback. Handlers may safely `Dispose` the device.
- All `Init*` paths throw synchronously for invalid configurations (channel index out of range, unsupported sample rate, format mismatch, missing processor).

---

## Back-compatibility — `AsioOut` as a facade

`AsioOut`'s public surface is preserved bit-for-bit. It is **not** marked `[Obsolete]` — many existing consumers use it for simple `IWavePlayer` playback and that pattern is still legitimate.

Internally, `AsioOut` delegates lifecycle (`Open`, capabilities, `Start`, `Stop`, `Dispose`, `Stopped` / `DriverResetRequest` events, F1/F2 protections) to a contained `AsioDevice`. For the buffer-switch hot path it keeps its raw `IntPtr`-based callback to preserve the legacy `AsioAudioAvailableEventArgs` contract (with its `WrittenToOutputBuffers` flag) for consumers wired up to it.

Two internal seams on `AsioDevice` make the facade work:
- `UnderlyingDriver` exposes the wrapped `AsioDriverExt` so `AsioOut` can drive `CreateBuffers` / `SetChannelOffset` / `SetSampleRate` directly.
- `ConfigureLegacyRawCallback(AsioFillBufferCallback)` registers a raw `IntPtr` callback that runs through the same drain / dispose-disposing / F1-guard infrastructure as the three documented modes.

The facade also fixes one historical hazard in legacy AsioOut: auto-stop on end-of-stream used to call `Stop()` from inside the buffer callback (the comment "this can cause hanging issues" sat on it for years). The facade now defers via `ThreadPool.QueueUserWorkItem`, so AutoStop is no longer a deadlock hazard for legacy consumers either.

A reflection-based snapshot test ([AsioOutPublicSurfaceTests.cs](NAudioTests/Asio/AsioOutPublicSurfaceTests.cs)) pins the surface: 3 constructors, 10 instance methods, 14 properties, 3 events, plus `IWavePlayer` implementation, `Volume` `[Obsolete]` attribute, and a defense-in-depth check for any unexpected new public methods. New code should target `AsioDevice`; the migration table in [Docs/AsioMigration.md](Docs/AsioMigration.md) shows side-by-side translations.

---

## Reliability — historical bug classes addressed

A walk of the GitHub issue tracker and `git log` for `NAudio.Asio` surfaced eight recurring bug classes. Each is mitigated in the new code; references below are to the file/symbol carrying the fix.

| Class | What it was | Mitigation |
| --- | --- | --- |
| **F1** | `Stop()` / `Dispose()` from buffer-callback thread self-deadlocks (`driver.Stop` blocks waiting for the callback to return) | `AsioDevice.Stop` throws `InvalidOperationException` when called from the captured callback thread. Auto-stop on end-of-stream defers to `ThreadPool.QueueUserWorkItem` in all three modes — and in the AsioOut facade. `Stopped` event always dispatched on captured `SynchronizationContext`, never from the callback. |
| **F2** | `AccessViolationException` when the driver is torn down while a callback is in flight | `AsioDevice.Dispose` sets a `disposing` flag (early-out for new callbacks), calls `StopInternal`, then waits on `callbackIdle` (`ManualResetEventSlim`) before releasing the COM driver. Each callback wraps its body in `Reset`/`Set` of the idle gate. Belt-and-braces on top of the ASIO `Stop()` contract. |
| **F3** | `NullReferenceException` from the buffer-update callback when the source format/native format combination has no convertor | Every `Init*` path validates the native format synchronously and throws `NotSupportedException` with both managed and native types named. Convertor selection happens at config time; the callback is provably non-null once `Init*` succeeds. |
| **F4** | Driver release leak on partial init failure | `AsioDevice` constructor wraps `AsioDriverExt` construction in try/catch; on failure, calls `DisposeBuffers()` + `ReleaseComAsioDriver()` (best-effort, exceptions swallowed) before rethrowing. |
| **F5** | Double-initialization of the same instance | State machine enforces `Unconfigured` precondition on every `Init*`; second call throws `InvalidOperationException`. |
| **F6** | `DriverResetRequest` fired but no supported recovery path | `Reinitialize()` caches the last applied options object on each successful `Init*` and re-applies after `Stop()`. Documented `Stop` → `Reinitialize` → `Start` pattern in [Docs/AsioDriverReset.md](Docs/AsioDriverReset.md). |
| **F7** | Choppy playback at high channel counts due to per-callback allocation | All per-channel float buffers and conversion staging are pre-allocated at `Init*` time. `AsioProcessBuffers` is `ref struct` to discourage user allocations. Gated by a BenchmarkDotNet job at 2/16 channels × 256/1024 frames × Int32LSB/Float32LSB asserting 0 B/op once warm. |
| **F8** | Borrowed-buffer lifetime footgun (driver-owned native pointers used after the callback returned) | `AsioProcessBuffers`, `AsioRawInputBuffer`, `AsioRawOutputBuffer` are all `ref struct` (compiler-enforced stack-only). `AsioAudioCapturedEventArgs` is a class but its spans point into library buffers that get invalidated by setting `Valid = false` after the handler returns; accessing properties throws if the event args is used outside its handler. |
| **F9** | `AsioDriver.GetSamplePosition` declared `out long samplePos` but the underlying `ASIOSamples` is `{uint hi; uint lo}` with `hi` at offset 0 — reading 8 bytes as a little-endian int64 produces `(lo << 32) \| hi`, backwards from the SDK layout. The bug was latent because no NAudio code called `GetSamplePosition` until the timing work landed. | Both the public method and the underlying P/Invoke delegate now take `out Asio64Bit` and the wrapper in `AsioDriverExt.TryGetSamplePosition` reassembles via the existing `Asio64Bit.ToInt64()` helper. The validation test in the console-test menu (records 10 s and checks audio-clock vs host-clock drift in milliseconds) catches any regression — wrong byte order would show drift in seconds-per-second, not milliseconds-per-ten-seconds. |
| **F10** | An ASIO driver whose `Init()` returns `false` and leaves `getErrorMessage()` empty surfaces in NAudio as `InvalidOperationException` with `Message == ""` — the user sees only the type name and has no idea what went wrong. Realtek's HDA-backed shim hits this path frequently. | `AsioDriverExt` now substitutes a diagnostic message when the driver provides nothing, naming the most common causes (x86/x64 bitness mismatch, endpoint held by another app, device disabled). The console-test menu's catch block also defensively renders `(no message)` for any other empty-message exception so the type name is never alone on screen. |

---

## Tests delivered

**Unit tests** (`NAudioTests/Asio/`) — all driver-free:

- `AsioPlaybackOptionsTests` — `From(ISampleProvider)` wrapping, default values.
- `AsioRecordingOptionsTests` — defaults and value preservation.
- `AsioDuplexOptionsTests` — defaults and value preservation.
- `AsioDriverCapabilityTests` — `AllInputChannels` / `AllOutputChannels` helpers.
- `AsioNativeToFloatConverterTests` — round-trip every supported native format, including 24-bit sign extension and full-scale points.
- `AsioFloatToNativeConverterTests` — round-trip every supported format with full-scale clamping (caught and motivated the Int32 staged-through-`long` fix for the `-1f * int.MaxValue → int.MinValue` precision trap).
- `AsioAudioAvailableEventArgsTests` — interleaved-sample helpers (legacy compatibility).
- `AsioSampleConvertorTests` — covers the legacy `IWaveProvider` → `AsioSampleType` selection used by the AsioOut facade.
- `AsioOutPublicSurfaceTests` — reflection-based snapshot of the NAudio 2.x AsioOut public surface.

**Manual / smoke tests** (`NAudioConsoleTest/Asio/AsioMenu.cs`):

- Info — list drivers, show capabilities.
- Playback — play audio file, play short test tone.
- Recording — record to WAV, show per-channel input levels.
- Duplex — passthrough with gain and per-channel peak meters.
- Lifecycle — Reinitialize round-trip.
- Timing — `AsioTimingTests.ValidateSamplePosition` records 10 s and checks four invariants: `SamplePosition` strictly increasing, Δsamples == `Frames` per callback, `SystemTimeNanoseconds` strictly increasing, and audio-clock vs host-clock drift under 50 ms over the full recording. The drift check is the killer test for byte-order regressions on the `Asio64Bit` → `long` conversion.
- Regression — Dispose from `Stopped` handler (F1 / F2 path), Stop from `AudioCaptured` callback (F1 guard).

**Benchmarks** (`NAudio.Benchmarks/AsioCallbackBenchmarks.cs`):

Reproduces the duplex callback's actual work (input convert → output zero-fill → trivial user processor → output convert) parameterized over Channels × Frames × Format. Asserts `Allocated = -` (0 B/op) via `MemoryDiagnoser`. Acts as the F7 zero-alloc gate; current results show 0 B/op across all 8 cells.

**Demo apps** (`NAudioDemo/`):

- `AsioRecordingDemo/AsioRecordingPanel` migrated to `AsioDevice.InitRecording`. Multi-select UI exercises non-contiguous `InputChannels`.
- `AudioPlaybackDemo/AsioDevicePlugin` + `AsioDeviceAdapter` + `AsioDeviceSettingsPanel` are the new playback plugin built on `AsioDevice`. The legacy `AsioOutPlugin` is kept alongside for back-compat validation (A/B testing the facade).

---

## Documentation

`Docs/`:

- `AsioPlayback.md` (rewritten) — `AsioDevice.InitPlayback`, output channel selection, sample rates, lifecycle, error handling, driver-reset hook.
- `AsioRecording.md` (rewritten) — `AsioDevice.InitRecording`, `AudioCaptured`, `GetChannel`, raw escape hatch, WAV-file patterns.
- `AsioDuplex.md` (new) — processor callback, `AsioProcessBuffers` reference, channel indexing, output zero-fill, real-time constraints, raw escape hatch.
- `AsioChannelMapping.md` (new) — per-mode patterns for contiguous / non-contiguous / multi-input-one-output / asymmetric-channel-count selection, validation guarantees.
- `AsioDriverReset.md` (new) — `Stop` → `Reinitialize` → `Start` recovery, state-machine matrix, error handling.
- `AsioMigration.md` (new) — when to migrate, what the facade gives you for free, full API translation table, side-by-side examples.

[README.md](README.md) tutorial index links all six articles.

---

## Resolved design decisions

These were settled during the design phase and confirmed in the implementation.

1. **Playback accepts both `IWaveProvider` and `ISampleProvider`.** `AsioPlaybackOptions.Source` is typed as `IWaveProvider`; the static `AsioPlaybackOptions.From(ISampleProvider, ...)` wraps via `.ToWaveProvider()`.
2. **`Reinitialize()` is manual, not automatic.** `DriverResetRequest` is raised as an event; user code decides when to recover.
3. **Library clears unwritten output buffers.** Before native-format conversion, any selected output channel the duplex callback didn't touch is zero. The raw `RawOutput` escape hatch bypasses this.
4. **`AsioDevice` is the chosen name.** Closest to ASIO vocabulary, no collision with WASAPI `AudioClient`.
5. **Duplex mode named `InitDuplex` / `AsioDuplexOptions`.** `Duplex` is unambiguous and avoids overloading "processor"/"callback" terms used elsewhere.

---

## What remains undone

### 1. Fake-driver seam for automated F2 / F1 regression tests

`AsioDriverExt` is a concrete class wrapping the COM `AsioDriver`. Constructing an `AsioDevice` requires a real installed ASIO driver, so the F1 (Stop-from-callback), F2 (dispose-during-callback), and F4 (init-failure release) regression scenarios are only testable manually via the `NAudioConsoleTest` smoke menu — they are not in the automated NUnit suite.

A fake-driver seam (extracting `IAsioDriverExt` and making `AsioDevice` constructible against it) would let unit tests drive synthetic buffer callbacks on a background thread and assert the drain / guard / release behavior automatically. The cost is meaningful (~150–250 lines of test infrastructure plus 6–10 unit tests). Skipping for now is defensible because the runtime guards are short, the patterns repeat across three callback paths so a regression that breaks one is unlikely to break all three identically, and the manual smoke tests cover the highest-value scenarios. Worth revisiting when the threading model is materially changed.

### 2. Cross-driver validation of the rich time-info path

`bufferSwitchTimeInfo` is wired up and tested on at least one modern driver. Drivers that ignore `kAsioSupportsTimeInfo` and fall back to plain `bufferSwitch` are still correct (Tier 1 fallback synthesises position and system time via `getSamplePosition`) but won't surface `Speed` or `TimeCode`. A multi-driver sweep across at least RME, Focusrite, MOTU, ASIO4ALL, and a Realtek shim would build confidence; better done as bug reports come in than upfront. If a particular driver needs investigation, temporarily reinstating per-path counters on `AsioDriverExt` (one increment per callback, exposed as a property) makes "which path is the driver actually using?" trivial to answer.

### 3. ASIO4ALL integration tests on CI

The plan called for integration tests gated by `NAUDIO_ASIO_INTEGRATION=1` that install ASIO4ALL on the runner, open it, run each of the three modes for ~1 second, and assert `State == Running` plus at least one buffer callback fired. This was never wired up. The manual demo apps and console-test menu cover the equivalent scenarios on developer machines, but there's no CI gate for "real driver still works after refactor".

---

## Relationship to the broader NAudio 3 direction

- `ISampleProvider` and `IWaveProvider` are both `Span`-based in NAudio 3. `AsioProcessBuffers` and `AsioAudioCapturedEventArgs` expose `Span<float>` the same way, so the ASIO API is consistent with the rest of the library.
- A platform-agnostic `AudioFormat` (replacing the Windows `WAVEFORMATEX`-based `WaveFormat`) is under consideration but not yet implemented. The ASIO options classes are deliberately named so they don't bake in "float" or "WaveFormat" — if `AudioFormat` lands before NAudio 3.0 release, the types carry over without renaming.
- No `IAudioSource` / `ISampleSource` rename. That direction was abandoned; `IWaveProvider` / `ISampleProvider` remain.
