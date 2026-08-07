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

## If you only read one section

For a typical NAudio 2 app, these five are what you will actually hit:

1. **Re-target to `net9.0`** (or later).
2. **`WaveOutEvent` is now called `WaveOut`**, and `WaveInEvent` is now `WaveIn`. The old
   names still work as `[Obsolete]` subclasses, so this is a warning, not a break.
3. **`WaveOut.DesiredLatency` is gone** — use `BufferMilliseconds`, which sizes each
   individual buffer rather than the total across all of them.
4. **Custom `IWaveProvider` / `ISampleProvider` implementations** need their `Read`
   override changed to take a single `Span<T>`.
5. **`WasapiOut` / `WasapiCapture` / `WasapiLoopbackCapture` are `[Obsolete]`** in favour of
   `WasapiPlayer` / `WasapiRecorder`. They still ship and still work.

Everything else is detailed below.

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
- **The `NAudio.Uap` package is removed.** The UWP/WinRT audio backend is gone; use
  `WasapiPlayerBuilder` / `WasapiRecorderBuilder` from `NAudio.Wasapi` instead.
- **`NAudio.WinForms` no longer supports `net472`, and `NAudio.WinMM` no longer supports
  `netstandard2.0`.** If you need .NET Framework, stay on the 2.x packages.

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

The abstract base classes changed to match: `WaveProvider32` now overrides
`Read(Span<float>)` and `WaveProvider16` overrides `Read(Span<short>)`.

One related overload was dropped: **`Init(IWavePlayer, ISampleProvider, bool convertTo16Bit)`
is removed**. Use `Init(IWavePlayer, ISampleProvider)`, which always initialises with IEEE
float, or convert upstream with `SampleToWaveProvider16` if you specifically need 16-bit:

```csharp
// before
player.Init(sampleProvider, convertTo16Bit: true);
// after
player.Init(new SampleToWaveProvider16(sampleProvider));
```

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
- **Device notifications are now event-based.** Implementing `IMMNotificationClient`
  and calling `MMDeviceEnumerator.RegisterEndpointNotificationCallback` /
  `UnregisterEndpointNotificationCallback` is no longer the way — the interface and
  those methods are now `internal`. Call `MMDeviceEnumerator.CreateNotificationClient()`
  and subscribe to the events on the returned `MMDeviceNotificationClient` instead. This
  removes the need to implement a COM interface (and, under NAudio 3, a `[GeneratedComClass]`
  and `<AllowUnsafeBlocks>`), and the enumerator manages the callback lifetime for you.
  Events marshal to the `SynchronizationContext` captured when the client is created (pass
  `useSynchronizationContext: false` to receive them on the audio worker thread instead).

  ```csharp
  // before
  class MyClient : IMMNotificationClient { /* implement all five methods */ }
  enumerator.RegisterEndpointNotificationCallback(new MyClient());

  // after
  var notifications = enumerator.CreateNotificationClient();
  notifications.DefaultDeviceChanged += (s, e) => Console.WriteLine(e.DeviceId);
  notifications.DeviceStateChanged   += (s, e) => Console.WriteLine($"{e.DeviceId} {e.NewState}");
  // ... dispose notifications (or the enumerator) to unsubscribe
  ```
- **The raw Core Audio COM interfaces are now `internal`** — `IAudioClient`,
  `IAudioClient2`, `IAudioSessionControl`, `IAudioSessionControl2`,
  `IAudioSessionNotification`, `IControlInterface` and friends. Use the wrapper classes
  (`AudioClient`, `AudioSessionControl`, …), which is what almost all NAudio 2 code
  already did.
- **The `AudioClient` constructor is `internal`.** Obtain one from
  `MMDevice.CreateAudioClient()` or `AudioClient.ActivateAsync()`.
- **Core Audio errors now throw `CoreAudioException`**, a subclass of `COMException`.
  Existing `catch (COMException)` still works; new code can catch the specific type.
- **`PropertyStoreProperty.Value` changed type from `PropVariant` to `object`.** It now
  exposes the resolved managed value (`string`, `uint`, `byte[]`, `Guid`, …) — cast to
  what you expect. The old `PropVariant` exposed pointer fields (LPWSTR/BLOB/CLSID) that
  were unsafe to read once the COM-allocated memory had been cleared. Relatedly,
  `PropVariant.DataType` now returns `NAudio.CoreAudioApi.Interfaces.VarType` rather than
  the deprecated `System.Runtime.InteropServices.VarEnum`; the numeric `VT_*` values are
  unchanged, so bitwise tests keep working.
- **`WasapiPlayer.Volume` is session volume, not device volume.** Unlike `WasapiOut`, it
  moves your application's slider in the Windows volume mixer (via `SimpleAudioVolume`)
  rather than the system-wide endpoint volume. For endpoint-wide control use
  `DeviceVolume.MasterVolumeLevelScalar`; for per-channel control of your own stream use
  `StreamVolume` (shared mode only).
- **`AudioEndpointVolume` notifications may arrive on a different thread.** If the object
  was constructed on the UI thread, notifications are posted back to it via the captured
  `SynchronizationContext`.

## Media Foundation

- **All the MF COM interfaces are now `internal`** — `IMFSourceReader`, `IMFSinkWriter`,
  `IMFTransform`, `IMFMediaType`, `IMFAttributes`, `IMFByteStream` and the rest — and so are
  the low-level `Mf*` wrappers around them (`MfSourceReader`, `MfSinkWriter`, `MfTransform`,
  `MfSample`, `MfMediaBuffer`, …). Of the `Mf*` types only `MfActivate` stays public. Work
  through the high-level classes instead: `MediaFoundationReader`,
  `StreamMediaFoundationReader`, `MediaFoundationEncoder`, `MediaFoundationResampler`,
  `MediaFoundationTransform`, `MediaFoundationApi` and `MediaType`.
- **`MediaFoundationInterop` is `internal`** — use `MediaFoundationApi` instead.
- **`MediaType` now implements `IDisposable`**, and its `IMFMediaType` constructor and
  `MediaFoundationObject` property are `internal`. Construct with `MediaType()` or
  `MediaType(WaveFormat)`, read via the properties (`SampleRate`, `SubType`, …), and
  dispose it (or use `using`).
- **Finalizers were removed from `MediaFoundationTransform` and `MediaFoundationEncoder`.**
  These no longer clean themselves up on the finalizer thread, so you must call `Dispose()`
  — a missed `using` is now a leak rather than a delayed release.
- **MF errors throw `MediaFoundationException`** (a subclass of `COMException`).
- **The underscore-prefixed enums and ALL_CAPS structs were renamed to PascalCase**, with
  PascalCase members. The ones you are most likely to have named:

  | NAudio 2 | NAudio 3 |
  | --- | --- |
  | `_MFT_ENUM_FLAG` | `MftEnumFlags` |
  | `MFT_MESSAGE_TYPE` | `MftMessageType` (`MFT_MESSAGE_COMMAND_FLUSH` → `Flush`) |
  | `MF_SOURCE_READER_FLAG` | `SourceReaderFlags` (`MF_SOURCE_READERF_ENDOFSTREAM` → `EndOfStream`) |
  | `MFT_INPUT_STREAM_INFO` | `MftInputStreamInfo` |
  | `MFT_OUTPUT_STREAM_INFO` | `MftOutputStreamInfo` |
  | `MFT_REGISTER_TYPE_INFO` | `MftRegisterTypeInfo` (now a class) |
  | `MF_SINK_WRITER_STATISTICS` | `SinkWriterStatistics` |

  The remaining `_MFT_*_FLAGS` enums follow the same pattern — `MftInputStatusFlags`,
  `MftInputStreamInfoFlags`, `MftOutputDataBufferFlags`, `MftOutputStatusFlags`,
  `MftOutputStreamInfoFlags`, `MftProcessOutputFlags`, `MftProcessOutputStatus` and
  `MftSetTypeFlags`.

- `MediaFoundationApi.EnumerateTransforms` now returns `MfActivate` wrappers, which expose
  `AttributeCount`, `GetAttributeByIndex`, `GetString`, `GetUInt32`, `GetGuid` and
  `ActivateTransform`.

## WaveOut / WaveIn

- **`WaveOutEvent` is renamed to `WaveOut`, and `WaveInEvent` to `WaveIn`.** In NAudio 2
  the plain `WaveOut` / `WaveIn` names belonged to the window-callback classes and
  `WaveOutEvent` / `WaveInEvent` were the recommended ones; in NAudio 3 the recommended
  classes get the plain names. `WaveOutEvent` and `WaveInEvent` still exist as `[Obsolete]`
  subclasses so existing code keeps compiling with a warning:

  ```csharp
  // before
  using var player = new WaveOutEvent();
  // after
  using var player = new WaveOut();
  ```

- **`WaveOut` and `WaveIn` now default to event-driven callbacks.** The legacy
  window-based variants are renamed `WaveOutWindow` / `WaveInWindow` and live in
  `NAudio.WinForms`. If you relied on the window-callback behaviour (for example
  pumping a UI message loop), reference `NAudio.WinForms` and use the `*Window`
  types.
- **`WaveCallbackInfo` and the `WaveCallbackStrategy` enum are removed.** The old
  three-way strategy is now expressed by picking a class and a constructor:

  | NAudio 2 | NAudio 3 |
  | --- | --- |
  | `new WaveOut(WaveCallbackInfo.NewWindow())` | `new WaveOutWindow()` (NAudio.WinForms) |
  | `new WaveOut(WaveCallbackInfo.ExistingWindow(hwnd))` | `new WaveOutWindow(hwnd)` (NAudio.WinForms) |
  | `new WaveOut(WaveCallbackInfo.FunctionCallback())` | no equivalent — function-callback mode was never reliable and is gone for good; use `new WaveOut()` |

  The same applies to `WaveIn` / `WaveInWindow`. `WaveWindow` and `WaveWindowNative` are no
  longer exposed — the message pump is an internal detail of the `*Window` classes.
- **`DesiredLatency` is replaced by `BufferMilliseconds`.** This is a compile break with no
  shim, and the meaning changed: `DesiredLatency` was the total across all buffers, whereas
  `BufferMilliseconds` (default 100) sizes each individual buffer. With the default
  `NumberOfBuffers = 2`, `DesiredLatency = 300` becomes `BufferMilliseconds = 150`.

  ```csharp
  // before
  var player = new WaveOutEvent { DesiredLatency = 300, NumberOfBuffers = 2 };
  // after
  var player = new WaveOut { BufferMilliseconds = 150, NumberOfBuffers = 2 };
  ```

- **`WaveIn`'s default record format changed** from 8 kHz 16-bit mono to 44.1 kHz 16-bit
  stereo. This is a silent behaviour change — if you relied on the old default, set
  `WaveFormat` explicitly:

  ```csharp
  var recorder = new WaveIn { WaveFormat = new WaveFormat(8000, 16, 1) };
  ```

- **The WinMM interop types are now `internal`**: `WaveInterop`, `WaveHeader`,
  `WaveHeaderFlags`, `MmTime`, `WaveOutBuffer`, `WaveInBuffer` and `WaveOutUtils`. These
  were never intended for direct use — go through `WaveOut` / `WaveIn`.
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
`WinRTMidiOut` in `NAudio.Midi` (Windows build), the backend-agnostic `IMidiInput` /
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
- **The DMO interop enums are now `internal`** (`DmoInputStatusFlags`, `DmoEnumFlags`,
  `MediaParamCurveType`, …). Go through `DmoEnumerator`, `MediaObject` and the
  `DmoEffectWaveProvider` wrappers.
- **`MediaBuffer`'s finalizer was removed** — call `Dispose()` (or `using`) rather than
  relying on finalization.
- **DMO errors throw `MediaFoundationException`** (a subclass of `COMException`), so
  existing `catch (COMException)` still works.
- `WindowsMediaMp3Decoder` has lost its old "DO NOT USE" label and is properly documented,
  but `DmoMp3FrameDecompressor` remains the class you want for high-level MP3 decoding.
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

## Reading and writing WAV chunks

The scattered "one subclass per chunk type" reader/writer pair has been replaced by a single
chunk model hanging off `WaveFileReader.Chunks` and `WaveFileWriter`, so cue points, BWF
`bext` and LIST/INFO metadata all work on an ordinary reader or writer.

### Reading

- **`WaveFileReader.ExtraChunks` was removed** — use `WaveFileReader.Chunks`, which returns
  a `WaveChunks` collection of the same `RiffChunk` elements plus `Find(id)`, `FindAll(id)`
  and `Contains(id)`.
- **`WaveFileReader.GetChunkData(RiffChunk)` was removed** — use
  `WaveFileReader.Chunks.GetData(chunk)`, with the same lazy-read semantics.
- **`WaveFileChunkReader` is now `internal`** (and moved to `NAudio.Wave`).
- **`CueWaveFileReader` was removed.** No subclass is needed any more:

  ```csharp
  // before
  var reader = new CueWaveFileReader("file.wav");
  CueList cues = reader.Cues;

  // after
  using var reader = new WaveFileReader("file.wav");
  CueList cues = reader.Chunks.ReadCueList();   // null if the file has no cues
  ```

- `IWaveChunkInterpreter<T>` is the extension point for chunk types NAudio doesn't know
  about; built-in interpreters cover cue lists, BWF `bext` (`BextInterpreter`) and
  LIST/INFO (`InfoListInterpreter` → `InfoMetadata`).

### Writing

- **`CueWaveFileWriter` was removed** — add cues to an ordinary `WaveFileWriter`:

  ```csharp
  // before
  var writer = new CueWaveFileWriter("out.wav", format);
  writer.AddCue(1000, "marker");

  // after
  using var writer = new WaveFileWriter("out.wav", format);
  writer.AddCue(1000, "marker");                 // or AddCue(position, label, length)
  // or, if you already have a populated CueList:
  writer.WriteCueList(cues);
  ```

- **`BwfWriter` was removed**, and RF64 promotion now belongs to `WaveFileWriter` rather
  than being tied to Broadcast Wave. Pass `WaveFileWriterOptions` and write the `bext` chunk
  as an extension:

  ```csharp
  // before
  var writer = new BwfWriter("out.wav", format, bextChunkInfo);

  // after
  using var writer = new WaveFileWriter("out.wav", format,
      new WaveFileWriterOptions { EnableRf64 = true });
  writer.WriteBroadcastExtension(broadcastExtension);
  ```

  `EnableRf64` reserves a `JUNK` placeholder up front and promotes the file to `RF64` +
  `ds64` on close once the data chunk exceeds 4 GB (tunable via `Rf64PromotionThreshold`).
- **`BextChunkInfo` was removed** — use `BroadcastExtension`, which is now the DTO for both
  reading and writing, adds BWF v2 loudness fields and a `ToChunkData()` serialiser. One
  field-level change: `OriginationDateTime` is replaced by separate `OriginationDate` and
  `OriginationTime` strings, with `BroadcastExtension.FormatOriginationDate(DateTime)` /
  `FormatOriginationTime(DateTime)` producing the BWF `yyyy-MM-dd` / `HH:mm:ss` forms.
- `WaveFileWriter.AddChunk(string, byte[], ChunkPosition)` and
  `AddChunk(IWaveChunkWriter)` are the low-level entry points for arbitrary RIFF chunks
  before or after the data chunk.

## Other type moves and API changes

- **`AudioMediaSubtypes` moved from the `NAudio.Dmo` namespace to `NAudio.Wave`.** It
  ships in `NAudio.Core` and always did, despite the DMO-sounding namespace — so
  cross-platform code had to write `using NAudio.Dmo;` to name the media subtype GUIDs
  even on Linux, and even without the `NAudio.Dmo` package installed. It now sits in
  `NAudio.Wave` alongside `WaveFormatExtensible`, its main consumer. If you compared a
  `WaveFormatExtensible.SubFormat` against `MEDIASUBTYPE_PCM` / `MEDIASUBTYPE_IEEE_FLOAT`,
  swap the `using`:

  ```csharp
  // before
  using NAudio.Dmo;
  if (fmt.SubFormat == AudioMediaSubtypes.MEDIASUBTYPE_PCM) { ... }

  // after — NAudio.Wave, which you almost certainly have already
  using NAudio.Wave;
  if (fmt.SubFormat == AudioMediaSubtypes.MEDIASUBTYPE_PCM) { ... }
  ```

  The type, its 70 GUID constants and `GetAudioSubtypeName` are otherwise unchanged. Real
  DMO code in the `NAudio.Dmo` package is unaffected.
- `AudioVolumeLevel` moved from `NAudio.Wasapi.CoreAudioApi` to
  `NAudio.CoreAudioApi` (alongside `MMDevice`, `Part`, `DeviceTopology`, …).
- `CaptureState` moved from `NAudio.CoreAudioApi` to `NAudio.Wave` (it is a
  backend-agnostic capture state used by `WaveIn`, `WasapiCapture` and
  `WasapiRecorder`). Code that named it via `using NAudio.CoreAudioApi;` now
  needs `using NAudio.Wave;`.
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
