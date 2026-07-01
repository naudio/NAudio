# Migrating from NAudio 2 to NAudio 3

NAudio 3 is a major release. The single `NAudio` assembly has been split into
focused packages, the minimum target framework is now `net9.0`, the core is
cross-platform and Native-AOT compatible, and several APIs have been modernised.
This guide walks through the breaking changes and how to update your code.

Most applications that reference the `NAudio` meta-package and use the common
playback/recording/file APIs will need only small changes — usually just
re-targeting to `net9.0` and adjusting any custom `IWaveProvider` /
`ISampleProvider` implementations to the new `Span<T>` `Read` signature.

> Tip: build with warnings visible. Removed members fail to compile, and almost
> everything that is *deprecated* rather than removed produces an `[Obsolete]`
> warning that points you at the replacement.

## Target framework and packages

- **Minimum target framework is now `net9.0`.** Legacy .NET Framework and .NET
  Standard 2.0 are no longer supported. Re-target your project to `net9.0` (or
  later) before upgrading the package.
- **`NAudio` is now a set of focused packages.** The shipping libraries are
  `NAudio.Core`, `NAudio.Midi`, `NAudio.WinMM`, `NAudio.Wasapi`, `NAudio.Asio`,
  `NAudio.WinForms` and `NAudio.Dmo`, alongside the new `NAudio.Effects` (in
  `NAudio.Core`), `NAudio.Sampler`, `NAudio.Vst3`, `NAudio.Alsa` and
  `NAudio.SoundFile`. The `NAudio` meta-package still pulls the Windows stack
  together, so if you reference `NAudio` you generally don't need to change your
  package references. If you reference individual packages, you may need to add
  one or two (see the type moves below). See
  [the assembly layout plan](https://github.com/naudio/NAudio/blob/main/Docs/Architecture/NAudio3AssemblyLayoutPlan.md).

## The `Read` signature change (`Span<T>`)

This is the change most likely to affect custom code.

- `IWaveProvider.Read(byte[] buffer, int offset, int count)` is now
  `Read(Span<byte> buffer)`.
- `ISampleProvider.Read(float[] buffer, int offset, int count)` is now
  `Read(Span<float> buffer)`.

**Calling** a provider:

```csharp
// before
int read = source.Read(buffer, offset, count);
// after
int read = source.Read(buffer.AsSpan(offset, count));
```

**Implementing** a provider — change the override and index from the start of
the span:

```csharp
// before
public int Read(byte[] buffer, int offset, int count) { ... buffer[offset + i] ... }
// after
public int Read(Span<byte> buffer) { ... buffer[i] ... }
```

The same pattern applies to the new `Span<T>` overloads added on
`BiQuadFilter.Transform`, `ALawDecoder.Decode`, `MuLawDecoder.Decode` and
`IMp3FrameDecompressor.DecompressFrame` (the last has a default interface method
so existing third-party decoders such as NLayer keep working).

## WASAPI

- **`WasapiOut`, `WasapiCapture` and `WasapiLoopbackCapture` are now
  `[Obsolete]`** in favour of the new `WasapiPlayer` / `WasapiRecorder` APIs
  (built via `WasapiPlayerBuilder` / `WasapiRecorderBuilder`). The legacy types
  still ship and continue to work, so this is a warning, not a break. See the
  [WasapiPlayer](WasapiPlayer.md) and [WasapiRecorder](WasapiRecorder.md)
  tutorials.
- **`WasapiOut`'s embedded DMO resampler was removed.** In exclusive mode, if
  your source format is not natively supported by the device you now get a
  `NotSupportedException` from `Init` instead of silent on-the-fly resampling.
  Resample upstream (for example with `MediaFoundationResampler`), use shared
  mode (which still auto-converts via `AutoConvertPcm`), or switch to
  `WasapiPlayerBuilder`.
- **`WaveInEventArgs` now fires one event per WASAPI packet** (previously
  batched). A new `BufferSpan` property exposes the data without copying through
  the `Buffer` byte array.
- **`MMDevice.AudioClient` is `[Obsolete]`** because it created a new instance
  per access — use `MMDevice.CreateAudioClient()`.
- **`PropertyStore`'s raw-`PropVariant` indexer is `[Obsolete]`.** The
  `PropertyStore[int]` indexer now resolves `PropVariant` values safely.
- Several `Mf*` Media Foundation wrapper types are now `internal`; only
  `MfActivate` and `MediaType` remain public.

## WaveOut / WaveIn

- **`WaveOut` and `WaveIn` now default to event-driven callbacks.** The legacy
  window-based variants are renamed `WaveOutWindow` / `WaveInWindow` and live in
  `NAudio.WinForms`. If you relied on the window-callback behaviour (for example
  pumping a UI message loop), reference `NAudio.WinForms` and use the `*Window`
  types.
- **`BufferedWaveProvider` buffer duration is now set in the constructor**
  (default 5 seconds); `BufferLength` and `BufferDuration` are read-only.

## MIDI and WinMM

- **`MidiIn`, `MidiOut`, `MidiInCapabilities` and `MidiOutCapabilities` moved
  from `NAudio.Midi` to `NAudio.WinMM`.** `NAudio.Midi` is now cross-platform —
  its `net9.0` target no longer P/Invokes `winmm.dll`. If you use the classic
  Windows MIDI I/O classes, add a reference to `NAudio.WinMM` (the `NAudio`
  meta-package already includes it).
- **`MmResult`, `MmException` and `Manufacturers` moved from `NAudio.Core` to
  `NAudio.WinMM`.**
- **`MidiInMessageEventArgs.Timestamp` / `MidiInSysexMessageEventArgs.Timestamp`
  are now `TimeSpan`** (previously `int` milliseconds), preserving the WinRT
  100 ns resolution.
- **`MidiIn.CreateSysexBuffers` was removed** — `MidiIn` now allocates sysex
  receive buffers automatically inside `Start()`.

New (non-breaking) additions worth knowing about: WinRT `WinRTMidiIn` /
`WinRTMidiOut` in `NAudio.Wasapi`, the backend-agnostic `IMidiInput` /
`IMidiOutput` interfaces, and the `IMidiInstrument` MIDI-file → audio pipeline.

## DMO and DirectSound

- **New `NAudio.Dmo` package.** The DMO effects, the DMO MP3 decoder
  (`DmoMp3FrameDecompressor`), the DMO resampler (`ResamplerDmoStream`) and
  `DirectSoundOut` have been carved out of `NAudio.Wasapi` / `NAudio.Core`.
  Namespaces are preserved (`NAudio.Dmo`, `NAudio.Dmo.Effect`, and `NAudio.Wave`
  for `DirectSoundOut`). Meta-package consumers see no change — `NAudio.Dmo`
  comes in transitively. **Direct `NAudio.Wasapi` consumers** who use the
  DMO/DirectSound types now need an explicit
  `<PackageReference Include="NAudio.Dmo" />`.
- `DmoMp3FrameDecompressor` moved from `NAudio.FileFormats.Mp3` to `NAudio.Dmo`
  (update your `using`).
- For new code, prefer `MediaFoundationResampler` over `ResamplerDmoStream`, and
  `WasapiPlayerBuilder` over `DirectSoundOut`.

## Effects (removed types and replacements)

The old ad-hoc effect types were removed in favour of the new
[`NAudio.Effects`](AudioEffects.md) framework:

- **`SimpleCompressorStream` (now `SimpleCompressorEffect`) was removed** along
  with the internal ChunkWare DSP — use the new `CompressorEffect` (and the
  wider dynamics suite: `LimiterEffect`, `GateEffect`, `MultibandCompressorEffect`,
  etc.).
- **`ImpulseResponseConvolution` was removed** (it was an unusable O(n²) stub) —
  use `ConvolutionReverbEffect` (partitioned FFT convolution).
- **`NAudio.Extras.Equalizer` and `NAudio.Extras.EqualizerBand` were removed** —
  use `NAudio.Effects.Equalizer` / `EqualizerBand` (in `NAudio.Core`). The new
  EQ is per-channel and click-free when retuned, and adds shelf/pass/notch/
  band-pass/all-pass shapes. The band API changed: `Bandwidth` / `Gain` became
  `Q` / `GainDb` (or `ShelfSlope`), and the equaliser is now an `IAudioEffect`
  (wrap it with `EffectSampleProvider` instead of passing a source to the
  constructor).

## Stream ownership in file writers (`WaveFileWriter` / `AiffFileWriter`)

`WaveFileWriter` and `AiffFileWriter` now follow the same stream-ownership rule the
readers (`WaveFileReader`, `AiffFileReader`, `Mp3FileReader`) already use, and which the
.NET BCL follows: **you dispose what you own.**

- The **filename** constructors (`new WaveFileWriter("out.wav", format)`) open the
  underlying `FileStream` themselves, so they still own and close it on `Dispose` —
  unchanged behaviour.
- The **stream** constructors (`new WaveFileWriter(stream, format)`) now treat the stream
  as caller-owned. Disposing the writer still **finalizes the header and flushes** so the
  file is valid, but it **no longer disposes the stream you passed in** — that is left for
  you to dispose.

Previously the stream constructor disposed the caller's stream unconditionally, which is
why `IgnoreDisposeStream` was needed to write to a `MemoryStream` you wanted to keep
(`new WaveFileWriter(new IgnoreDisposeStream(ms), format)`). That wrapper is no longer
necessary — passing the stream directly leaves it open. (`IgnoreDisposeStream` still
exists and existing code that uses it keeps working.)

**What to check when upgrading.** The one case that changes behaviour is passing a
*throwaway* stream you didn't keep a reference to and relying on the writer to close it,
classically:

```csharp
// before: the writer closed this FileStream for you
new WaveFileWriter(File.Create(path), format);   // <-- handle now leaks
```

After the upgrade that `FileStream` handle is left open. Either use the filename overload
(which owns the file), or dispose the stream yourself:

```csharp
// preferred - the writer owns the file
using var writer = new WaveFileWriter(path, format);

// or keep and dispose the stream yourself
using var stream = File.Create(path);
using var writer = new WaveFileWriter(stream, format);
```

The common `new WaveFileWriter(path, format)` filename usage is unaffected.

## Other type moves and API changes

- `AudioVolumeLevel` moved from `NAudio.Wasapi.CoreAudioApi` to
  `NAudio.CoreAudioApi` (alongside `MMDevice`, `Part`, `DeviceTopology`, …).
- `CaptureState` moved from `NAudio.CoreAudioApi` to `NAudio.Wave` (it is a
  backend-agnostic capture state used by `WaveIn`, `WasapiCapture` and
  `WasapiRecorder`). Code that named it via `using NAudio.CoreAudioApi;` now
  needs `using NAudio.Wave;`.
- **`WaveFileChunkReader` is now `internal`** (moved to `NAudio.Wave`). Read
  custom RIFF chunks via `WaveFileReader.Chunks` (`WaveChunks` / `RiffChunk` /
  `IWaveChunkInterpreter<T>`, with built-in interpreters for cue lists, BWF
  `bext` and LIST/INFO).
- **`CueWaveFileReader` was removed** — use
  `new WaveFileReader(...).Chunks.ReadCueList()` to get a `CueList`.
- `SoundFont.SampleHeader`'s public fields are now properties. This is
  source-compatible for normal reads/writes but binary-breaking for compiled
  consumers and source-breaking for `ref`/`out` access to the old fields.
- **`MixingWaveProvider32` was removed** — use `MixingSampleProvider` instead. It
  was an untested work-in-progress that accepted only 32-bit IEEE-float inputs, so
  it offered nothing over `MixingSampleProvider`, which mixes in float, converts
  PCM inputs for you (`waveProvider.ToSampleProvider()`), and adds dynamic
  add/remove, an input-ended event and `ReadFully`. If you need an `IWaveProvider`
  out of it, call `.ToWaveProvider()`:

  ```csharp
  // before
  var mixer = new MixingWaveProvider32();
  mixer.AddInputStream(floatWaveProvider);

  // after
  var mixer = new MixingSampleProvider(new[] { waveProvider.ToSampleProvider() });
  mixer.AddMixerInput(anotherProvider.ToSampleProvider());
  IWaveProvider output = mixer.ToWaveProvider();   // if you need IWaveProvider
  ```
- **`ImaAdpcmWaveFormat` was removed** — it was a non-functional "work in progress"
  stub (it left block align, average bytes per second and samples-per-block at zero
  and never serialized its `samplesPerBlock` extension field, so it produced an
  invalid header on every path) and was referenced nowhere. The
  `WaveFormatEncoding.ImaAdpcm` / `DviAdpcm` constants are unchanged; if you need an
  IMA/DVI ADPCM header, declare your own `WaveFormat` subclass that sets the fields
  and overrides `Serialize` (see `AdpcmWaveFormat` for the pattern).
- `WaveBuffer` is deprecated — use `MemoryMarshal.Cast` to reinterpret buffers.
- `StreamMediaFoundationReader` now throws `ArgumentException` for non-readable
  or non-seekable streams instead of failing later (#1288).
- `HResult.E_INVALIDARG` was corrected to `0x80070057` (it was the legacy
  `0x80000003`), and `HResult.MAKE_HRESULT` is deprecated in favour of
  `MakeHResult` (#1288).

## See also

- [Release notes](https://github.com/naudio/NAudio/blob/main/RELEASE_NOTES.md) — the full list of what's new in NAudio 3.
- [Migrating from `AsioOut` to `AsioDevice`](AsioMigration.md) — the ASIO API is
  redesigned; `AsioOut` is preserved as a facade, so this is optional.
- [Audio effects](AudioEffects.md), [the sampler](Sampler.md),
  [cross-platform audio files](CrossPlatformAudioFilesWithSoundFile.md) — guides
  to the major new subsystems.
