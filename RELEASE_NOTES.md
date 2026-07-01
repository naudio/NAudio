### Unreleased

<!--
Bullets land here as PRs merge. The maintainer renames this section to
"### 3.0.0 (date)" at release time. See CLAUDE.md and
Docs/Architecture/ReleaseStrategy.md for the release-notes process.

These notes are deliberately a curated summary — NAudio 3 landed a very
large amount of work and the full bullet-by-bullet history lives in the
git log and the merged PRs. Keep this section a high-level overview that
points at the per-feature tutorials and READMEs; the NuGet
`PackageReleaseNotes` field has a 35,000-character limit.
-->

NAudio 3 is a major release. The single `NAudio` assembly is now split into
focused, independently usable packages; the minimum target framework moves to
`net9.0`; the core is cross-platform and Native-AOT compatible; and several
large new subsystems — a cross-platform effects suite, a software sampler, VST 3
hosting, and ALSA and libsndfile backends — join the library. The headline
changes are below; see the linked tutorials and the per-package READMEs for the
detail.

#### Packages and platform

 * Minimum target framework is now `net9.0` — legacy .NET Framework and .NET Standard 2.0 support is dropped
 * `NAudio` is now a set of focused packages: `NAudio.Core`, `NAudio.Midi`, `NAudio.WinMM`, `NAudio.Wasapi`, `NAudio.Asio`, `NAudio.WinForms`, `NAudio.Dmo`, plus the new `NAudio.Effects` (in `NAudio.Core`), `NAudio.Sampler`, `NAudio.Vst3`, `NAudio.Alsa` and `NAudio.SoundFile`. The `NAudio` meta-package still pulls the Windows stack together so existing consumers see no change. See `Docs/Architecture/NAudio3AssemblyLayoutPlan.md`
 * `NAudio.Core`, `NAudio.Midi` and `NAudio.Wasapi` are Native-AOT compatible (`IsAotCompatible=true`), enforced in CI by `NAudioAotSmokeTest`. `NAudio.Midi`, `NAudio.Effects`, `NAudio.Sampler` and `NAudio.SoundFile` are cross-platform

#### New capabilities

Each new subsystem has its own tutorial/README; only the headline is listed here.

 * **Audio effects** — a cross-platform `NAudio.Effects` framework: `IAudioEffect` / `EffectSampleProvider` / `EffectChain` with click-free bypass, dry/wet mix and an optional parameter model, plus a broad effect set (EQ and filtering, dynamics, saturation/lo-fi, delay and modulation, reverb including FFT convolution, pitch shifting, and voice-comms AGC/noise-suppression). See [Docs/AudioEffects.md](Docs/AudioEffects.md)
 * **Software sampler** — new `NAudio.Sampler` package: polyphonic, cross-platform playback of SoundFont (`.sf2`) and SFZ instruments and single-sample instruments, rendered as an `ISampleProvider` (SF2 modulator engine, DAHDSR envelopes, LFOs, modulated filters, reverb/chorus sends, voice stealing, choke groups). See [Docs/Sampler.md](Docs/Sampler.md)
 * **VST 3 hosting (preview)** — new `NAudio.Vst3` package (Windows-only): discover, load and host VST 3 effects and instruments, with parameters, state and `.vstpreset` presets, native editor windows, program lists/units, latency compensation, and live/offline MIDI-file playback through the shared MIDI pipeline. See the `NAudio.Vst3` README and `Docs/Architecture/Vst3Hosting.md`. VST is a registered trademark of Steinberg Media Technologies GmbH
 * **Cross-platform audio files** — new `NAudio.SoundFile` package: read and write WAV/AIFF/FLAC/Ogg-Vorbis/Opus/MP3 via a system libsndfile on Windows, Linux and macOS (the first cross-platform FLAC/Vorbis/Opus *encoder* in NAudio). See [Docs/CrossPlatformAudioFilesWithSoundFile.md](Docs/CrossPlatformAudioFilesWithSoundFile.md) (#1289)
 * **Linux audio** — new `NAudio.Alsa` package: `AlsaOut` (`IWavePlayer`) and `AlsaIn` (`IWaveIn`) plus `AlsaDeviceEnumerator`, backed by `libasound`. See [Docs/PlayAudioFileLinuxAlsa.md](Docs/PlayAudioFileLinuxAlsa.md) and [Docs/RecordAudioFileLinuxAlsa.md](Docs/RecordAudioFileLinuxAlsa.md) (#1182)
 * **Sequencing** — a portable `NAudio.Sequencing` namespace in `NAudio.Core` (tempo and time-signature maps, transport, `EventTimeline`, swing, and a sample-accurate per-buffer dispatcher) underpinning MIDI-file playback and the sampler. See `Docs/Architecture/Sequencing.md`
 * **Modern WASAPI and ASIO** — high-level `WasapiPlayer` / `WasapiRecorder` (built via `WasapiPlayerBuilder` / `WasapiRecorderBuilder`: `IAudioClient3` low-latency, MMCSS, `IAsyncDisposable`, zero-copy buffers, process-loopback capture, default-device automatic stream routing via `WithDefaultDeviceStreamRouting()` + `BuildAsync()` so playback/capture follows the default endpoint and re-routes when it changes (Windows 10 1607+, #942), acoustic-echo-cancellation reference control via `WithEchoCancellationReferenceEndpoint` / `IAcousticEchoCancellationControl` (Windows 11 build 22621+, #1223) and a `WithCommunicationsMode` capture option that engages the system AEC/noise-suppression/AGC pipeline (also fixing `WasapiPlayerBuilder.WithCategory`, which previously did not apply the stream category), automatic resample-free format adaptation — bit depth and channels — in exclusive and low-latency modes, `WasapiPlayer.GetPlaybackCapability` to validate options up front, and `LowLatencyActive` / `WithLowLatency(required)` to control and inspect low-latency fallback on both `WasapiPlayer` and `WasapiRecorder` (recorder low-latency capture added in #1100)), and a new `AsioDevice` replacing `AsioOut` (explicit playback/recording/duplex modes, non-contiguous channels, per-channel `Span<float>` callbacks, driver-reset recovery, per-buffer timing). See [Docs/WasapiPlayer.md](Docs/WasapiPlayer.md), [Docs/WasapiRecorder.md](Docs/WasapiRecorder.md) and [Docs/AsioMigration.md](Docs/AsioMigration.md)
 * **MIDI** — `NAudio.Midi` is now cross-platform; new WinRT `WinRTMidiIn` / `WinRTMidiOut` (in `NAudio.Wasapi`) and backend-agnostic `IMidiInput` / `IMidiOutput`; a new `IMidiInstrument` seam with `MidiFileSequence` / `SequencedMidiPlayer` / `OfflineMidiRenderer` / `LiveMidiInstrument` gives an end-to-end MIDI-file → audio pipeline that drives the sampler or a hosted VST 3 instrument. `MidiFile` now also reads RIFF-RMID (`.rmi`) files (#1236); `MidiFile.Export` gains a `Stream` overload for writing to `MemoryStream` or any other seekable stream without going through a temp file, thanks to @MaKiPL (#499)
 * **WaveFormatExtensible** — new constructors let you set the SubFormat (or just choose PCM vs IEEE float via a `useIeeeFloat` flag), valid-bits-per-sample and channel mask explicitly (e.g. 32-bit integer PCM, or 24 valid bits in a 32-bit container), and `ValidBitsPerSample` / `ChannelMask` are now readable; a new `[Flags] Speakers` enum names the standard speaker positions for building channel masks (#1325)
 * **Reading audio from streams** — `AudioFileReader` and `CachedSound` now have `Stream` constructors, detecting WAV/AIFF from the stream contents and delegating any other format to Media Foundation, so audio held in an embedded resource or in-memory byte array can be played without writing a temporary file (#927, #963)
 * **WASAPI sessions** — `AudioSessionControl.SetDuckingPreference(bool)` exposes `IAudioSessionControl2::SetDuckingPreference`, so a communications session can opt out of the system's auto-ducking of other audio streams (#760)
 * **Sample providers / DSP** — new `ChannelMixerSampleProvider` with ready-made `ChannelMixMatrix` routings (mono↔stereo, stereo→5.1, …), thanks to @antiduh (#982); a new `FftProcessor`, an `IWaveChunkInterpreter<T>` extension point (cue lists, BWF `bext`, LIST/INFO), `Span<T>` overloads across the codec/DSP surface, and reusable building blocks (`EnvelopeFollower`, `DelayLine`, `Lfo`, `Oversampler`, `LinkwitzRileyCrossover`, `PartitionedConvolver`, …) underpinning the effects and sampler; `SmbPitchShiftingSampleProvider` now exposes its phase-vocoder latency (`Latency` / `LatencySamples` / `LatencySamplesPerChannel`) so shifted audio can be time-aligned with unshifted audio (#922); `AdsrSampleProvider` now exposes the full envelope (`DecaySeconds` and `SustainLevel` in addition to `AttackSeconds` / `ReleaseSeconds`), supports multi-channel sources, adds `Start()` / `State` and an `EnvelopeCompleted` event, and no longer throws on stereo input (#671); `FadeInOutSampleProvider` now raises `FadeInComplete` / `FadeOutComplete` events when a fade reaches its target (#1136)

#### Breaking changes

The full upgrade walkthrough — every breaking change with before/after code — is
in **[Migrating from NAudio 2 to NAudio 3](Docs/MigratingFromNAudio2.md)**. Most
apps need only re-target to `net9.0` and adjust custom providers to the new
`Span<T>` `Read` signature. The highest-impact changes:

 * Minimum target framework is now `net9.0` (legacy .NET Framework / .NET Standard 2.0 dropped)
 * `IWaveProvider.Read` / `ISampleProvider.Read` now take a single `Span<byte>` / `Span<float>` (was buffer/offset/count) — callers migrate via `source.Read(buffer.AsSpan(offset, count))`; implementations override the span method
 * `WasapiOut`, `WasapiCapture` and `WasapiLoopbackCapture` are `[Obsolete]` in favour of `WasapiPlayer` / `WasapiRecorder` (the legacy types still ship and work); `WasapiOut`'s embedded exclusive-mode resampler was removed, but it now adapts bit depth and channels (PCM↔float, mono↔stereo) in exclusive mode, so only a sample-rate mismatch requires upstream resampling
 * `WasapiRecorderBuilder.WithProcessLoopback(processId, mode)` now captures audio rendered by a specific process (and optionally its child processes) via `ActivateAudioInterfaceAsync` — build it with the new `BuildAsync()` (Windows 10 2004 / build 19041+)
 * `WasapiRecorder.DataAvailable` now also reports each packet's WASAPI device and QPC positions (`devicePosition` / `qpcPosition`), matching the timestamps already exposed by `CaptureAsync`, so audio captured via the zero-copy event path can be time-aligned with a wall clock (#948)
 * `WasapiPlayerBuilder.WithRawMode()` and `WasapiRecorderBuilder.WithRawMode()` open a raw stream (`AUDCLNT_STREAMOPTIONS_RAW`) that bypasses the system signal-processing pipeline (audio enhancements / APO effects such as downmixing toward mono), so samples reach the device unaltered; requires IAudioClient2 (#476)
 * `WaveOut` / `WaveIn` now default to event-driven callbacks; the window-based variants are renamed `WaveOutWindow` / `WaveInWindow` in `NAudio.WinForms`
 * Some types moved package/namespace as part of the split — classic Windows MIDI I/O and `winmm` types to `NAudio.WinMM`; the DMO/DirectSound types into the new `NAudio.Dmo` package; plus smaller moves (`AudioVolumeLevel`, `CaptureState`, `DmoMp3FrameDecompressor`). Meta-package consumers are unaffected
 * `SimpleCompressorStream`, `ImpulseResponseConvolution` and `NAudio.Extras.Equalizer` were removed — superseded by `NAudio.Effects` (`CompressorEffect`, `ConvolutionReverbEffect`, `Equalizer`)
 * `MixingWaveProvider32` was removed — it was an untested float-only mixer that offered nothing over `MixingSampleProvider` (use `waveProvider.ToSampleProvider()` for inputs and `.ToWaveProvider()` if you need an `IWaveProvider` out)
 * `WaveFileWriter` / `AiffFileWriter` no longer dispose a caller-supplied stream — the stream constructor now leaves the stream open (it still flushes/finalizes the header on `Dispose`), matching the readers' ownership rule; only the filename constructor owns and closes the file. The `IgnoreDisposeStream` wrapper is no longer needed when writing to a stream you want to keep. If you passed a throwaway stream and relied on the writer closing it, dispose it yourself or use the filename overload (#1040)

#### Notable bug fixes

In addition to the fixes below, the new sampler/SFZ/SoundFont subsystem saw
extensive correctness work during development — see [Docs/Sampler.md](Docs/Sampler.md).

 * `WaveOut`: fixed a race where stopping/disposing faster than the buffer latency could throw a `NullReferenceException` via `PlaybackStopped` (#804)
 * `DirectSoundOut`: fixed a startup race where the secondary buffer could begin LOOPING playback before it was primed (the priming `Feed` was gated on `PlaybackState`, which `Play()` flips to `Playing` on the calling thread), so playback could collapse immediately — most visibly when a caller spun on `PlaybackState` (#759)
 * `WaveFileWriter`: removed the finalizer that fired an unconditional `Debug.Assert` and could crash the process on the finalizer thread when construction had thrown
 * `WaveFileWriter.WriteSample` / `WriteSamples`: fixed 32-bit `WaveFormatExtensible` output writing near-silence or corrupt data — `WriteSample(float)` truncated the normalised float to an `Int32` before scaling (writing zero for almost every sample), and both paths ignored the format's SubFormat; they now write IEEE-float or integer-PCM samples according to the declared subformat (#651)
 * `WaveExtensionMethods.AsStandardWaveFormat`: now also resolves the SubFormat of a `WaveFormatExtraData` (how an extensible format read from a file/stream is materialised), not just a `WaveFormatExtensible`
 * `AudioClient.Dispose` is now idempotent and safe against concurrent/re-entrant disposal, fixing an intermittent interop `NullReferenceException` (#1183)
 * `WasapiRecorder`, `WasapiCapture` and `WasapiLoopbackCapture`: a capture device removed mid-recording (unplugged, or the default device changed) no longer crashes the process with an unhandled exception on the capture thread — the teardown `Stop`/`Reset` calls that themselves fail on the dead device are now best-effort, so `RecordingStopped` always fires with the originating exception and the recorder cleans up correctly (#672)
 * `AcmInterop`: serialised all `msacm32` P/Invokes process-wide, fixing process-killing access violations under concurrent ACM use
 * `WaveFileReader` / `AiffFileReader`: malformed headers declaring `BlockAlign=0` now throw `InvalidDataException` from the constructor instead of `DivideByZeroException` later (#1254); `WaveFileReader` no longer throws on an oversized `fmt` `cbSize` (#482)
 * `AiffFileReader` / `AiffFileWriter`: 8-bit PCM is now read and written as signed two's-complement per the AIFF spec, instead of being mishandled as unsigned (WAV-style) — fixing DC-shifted/garbled 8-bit AIFF playback (#1178)
 * `WaveFileReader`: a `data` chunk declaring a bogus/oversized length (e.g. FFmpeg's `0xFFFF1000` placeholder when writing to a non-seekable pipe) is now clamped to the bytes actually present, so reading from a `MemoryStream` no longer throws and the reported length reflects the available audio (#1090)
 * `WaveFormat.Serialize`: PCM formats now write the canonical 16-byte `fmt ` chunk (#934, #1098)
 * `WaveViewer`: fixed rendering upside-down (#801, #818) and now renders any source format correctly via `ToSampleProvider()` (#564)
 * `ToSampleProvider()` now handles `WAVE_FORMAT_EXTENSIBLE` PCM and IEEE float sources (e.g. multichannel / >16-bit WAV files) instead of throwing `Unsupported source encoding` (#639)
 * `AudioSessionControl`: now supports multiple registered event clients without leaking, and `UnRegisterEventClient` honours its argument (#1263)
 * `MMDevice.Dispose` now releases the device's property store deterministically (`PropertyStore` is now `IDisposable`), instead of leaving it to be reclaimed by COM finalization (#1145)
 * `AudioEndpointVolume.OnVolumeNotification`: fixed per-channel notifications all returning channel 0's volume (#351)
 * `CueListInterpreter`: WAV files with cue points but no labels now return cues with empty labels instead of null (#549)
 * Cue region lengths (`ltxt` sub-chunks) are now read and written by `CueListInterpreter` / `WaveFileWriter` — new `Cue.Length` / `CueList.CueLengths` and a `WaveFileWriter.AddCue(position, label, length)` overload (#1013)
 * `ResamplerDmoStream`: fixed an infinite loop on `Read` after seeking and the loss of the resampler tail at end-of-stream (#607, #608)
 * `LoopStream.Read`: no longer spins at 100% CPU when the wrapped source can't satisfy a read (#1338)
 * `RawSourceWaveStream`: the `byte[]` constructor now disposes the `MemoryStream` it creates internally when the stream is disposed (it previously leaked); a caller-supplied source stream is still left open
 * `Mp3FileReader`: fixed false sample-rate-change errors near end of file, and more robust MP3 frame parsing against album art / trailing metadata
 * `MidiFile`: preserve running-status across meta events (fixes "Read too far")
 * `BlockAlignReductionStream.Position`: validates the incoming value, so a block-aligned seek after an arbitrary read no longer wrongly throws (#368)
 * `BlockAlignReductionStream.Read`: a single read (or `Stream.CopyTo` chunk) larger than the 4-second internal buffer no longer silently discards the rest of the source and truncates the stream — e.g. converting a non-PCM WAV via `AudioFileReader` on .NET Core (#1022)
 * `MmException` messages now append a human-readable `MmResult` description (#1192); `Id3v2Tag.ReadTag` no longer throws/catches for tagless MP3 streams (#265)
 * ASIO: implemented missing `Asio64Bit` Int24LSB/Float32LSB conversions and fixed a byte-order bug in `GetSamplePosition`; `WdlResampler` backported three upstream Cockos WDL fixes
 * `WdlResampler`: reinterleave buffered samples when the channel count changes between calls, and flush denormals in the IIR feedback path (further upstream Cockos WDL backports) (#800)
 * Hardened Media Foundation and DMO interop against COM ref leaks on error paths (`MediaFoundationReader`, `MediaFoundationEncoder`, `MediaFoundationTransform`, `MediaBuffer`, `Mf*` wrappers) (#1293)
 * `MediaFoundationReader`: seeking is now sample-accurate — since `IMFSourceReader.SetCurrentPosition` only seeks to the nearest container keyframe, the reader now reads forward and skips into the decoded buffer to reach the exact requested position, instead of restarting playback from the keyframe (audible on audio extracted from video, e.g. Vorbis/AAC). `Position` now reflects the byte position actually reached (#628)
 * Removed dead `naudio.codeplex.com` links from the README, MixDiff and source comments (#985)
 * `MixingSampleProvider.AddMixerInput` now throws `ArgumentException` if the same source is added twice, instead of silently consuming it at 2× rate and aliasing it against itself (#662)

#### Performance

 * Vectorised mix-add and volume kernels via `System.Numerics.Tensors` (significantly faster on AVX2)
 * `WaveStream.Read(Span<byte>)` is overridden directly on every concrete reader (no intermediate byte-array copy); `WasapiCapture`'s path is now zero-copy via the native WASAPI buffer span
 * Eliminated per-`Read` allocations in `SmbPitchShiftingSampleProvider`, `ResamplerDmoStream` and `DmoMp3FrameDecompressor` (#971)
 * `Mp3FileReader` builds its table-of-contents lazily on first seek; the `Position` setter no longer blocks and rapid scrub seeks debounce
 * The sampler and sequencer render allocation-free and lock-free on the audio thread (control-rate envelope/LFO advancement, indexed region lookup, copy-on-write event timelines)

#### Tooling, packaging and modernisation

 * Most COM interop migrated from `[ComImport]` to `[GeneratedComInterface]` / `ComWrappers`, and many P/Invokes to `[LibraryImport]`, across the WASAPI/Core Audio activation chain, Media Foundation, the DMO interfaces, DirectSound and the `ComStream` CCW
 * Codebase-wide code-style sweep (file-scoped namespaces, target-typed `new`, string interpolation, …) now enforced at build time via `.editorconfig` / `.globalconfig`; added `.git-blame-ignore-revs` so the mechanical commits don't obscure `git blame`
 * Each NAudio package now ships its own README and embeds an SPDX 2.2 SBOM under `/_manifest/spdx_2.2/` in its `.nupkg`, and carries per-package `<Description>` metadata
 * Tests migrated from VSTest to `Microsoft.Testing.Platform`, and `NAudioTests` split into cross-platform `NAudio.Core.Tests` and Windows-only `NAudio.Windows.Tests`; migrated to the `.slnx` solution format; renamed `license.txt` to `LICENSE`
 * Added a DocFX documentation site (tutorials + API reference) published to GitHub Pages, built from `Docs/` and the source XML comments

#### Demos and test harnesses

 * New WPF demo modules for the new subsystems: Convolution Reverb, managed Realtime Effects, VST3 Realtime Effects, VST3 Realtime Instrument, VST3 MIDI File Player, a Live MIDI Sampler and a Single-Sample Editor
 * New `NAudioConsoleTest` CLI harness (`run-batch` for JSON test plans, `diagnose` for a structured host-audio snapshot) and `MfStressTest` for the new Media Foundation interop
 * WPF demos: spectrum analyser rewritten (correct dB/log-frequency/calibration), new `LiveWaveformControl`, loopback + multi-API device selection in the WAV recording demo, and a drum machine rebuilt on the new `NAudio.Sequencing` primitives
 * Replaced the deprecated vendored NSpeex with Opus (Concentus) in the network chat demo
 * Modernised the network chat demo: now UDP-only (dropped the unsuitable TCP transport), receivers bind to `IPAddress.Any` so it works across machines (not just loopback — #821), separate listen/remote ports for one-PC experiments, async cancellable sockets, a bounded jitter buffer, capture/playback moved to `WasapiRecorder`/`WasapiPlayer`, and a new [tutorial](Docs/NetworkChatDemo.md)

### 2.3.0 (12 Mar 2026)

 * Performance improvements for `PropertyStore` and Core Audio property access (#1206)
 * Improved multi-channel playback compatibility in WASAPI exclusive mode (#1234)
 * Fixed a bug that prevented `WasapiCapture` from using exclusive mode (#1122)
 * Fixed RF64 header parsing in `WaveFileChunkReader.ReadWaveHeader` (#1231)
 * `PropVariant` now supports `VT_EMPTY` by returning `null` (#1071)
 * Better exception when calling disposed `AcmStream.Convert` (#1108)
 * Fixed `AcmStreamHeader` finalizer crash with corrupted data (#1199)
 * Added `net6.0` targets for `NAudio.Asio` and `NAudio.WinMM` to remove registry dependency (#1139)
 * Updating TFMs, modernizing the UAP project to WinUI
 
### 2.2.1 (4 Sep 2023)

 * `WdlResampler` is now public
 * WASAPI uses background threads
 * `MmException` can return function name
 * ErrorCodes provides all the `AUDCLNT_E HRESULT` values from audioclient.h
 * `AiffFileWriter` chunk size bugfix
 * Support for Device Topology API (`IPart`, `IAudioAutoGainControl`, `IAudioMute`, `IAudioVolumeLevel`, `IControlChangeNotify`, `IControlInterface`, `IKsJackDescription`, `IPerChannelDbLevel`)
 * Add `ComImport` attribute to `CoreAudioApi` interfaces
 * Ability to set attribute on `MediaType`, and to specify `MediaFoundationEncode` buffer size
 * WASAPI stop improvements
 * FLAC and ALAC added to audio subtypes list
 * `MediaFoundationEncoder` bugfixes for null reference
 * Sysex dispose bugfix
 * Note: this replaces v2.2.0. Incorrectly versioned NAudio.Wasapi.dll (was 22.0) retired and replaced with 2.2.1

### 2.2 (22 Aug 2023)

 * `WdlResampler` is now public
 * WASAPI uses background threads
 * `MmException` can return function name
 * `ErrorCodes` provides all the AUDCLNT_E HRESULT values from audioclient.h
 * `AiffFileWriter` chunk size bugfix
 * Support for Device Topology API (`IPart`, `IAudioAutoGainControl`, `IAudioMute`, `IAudioVolumeLevel `, `IControlChangeNotify`, `IControlInterface`, `IKsJackDescription`, `IPerChannelDbLevel`)
 * Add `ComImport` attribute to CoreAudioApi interfaces
 * Ability to set attribute on `MediaType`, and to specify `MediaFoundationEncode` buffer size
 * WASAPI stop improvements
 * FLAC and ALAC added to audio subtypes list
 * `MediaFoundationEncoder` bugfixes for null reference
 * Sysex dispose bugfix

### 2.1 (29 Apr 2022)

 * `AudioFileReader` will use `MediaFoundationReader` as the default for MP3s
 * Minimum supported Win 10 version is now uap10.0.18362 (SDK version 1903)
 * `IWavePlayer` now has an `OuputWaveFormat` property
 * `WasapiCapture` and `WasapiLoopbackCapture` support sample rate conversion so you can capture at a sample rate of your choice
 * `WasapiOut` supports built-in sample rate conversion in shared mode
 * `MediaFoundationEncoder` allows you to encode to a `Stream`

### 1.9.0 (4 May 2019)

 * Switched to multi-targetting project type
 * Targets .NET 3.5, .NET Standard 2.0, and UWP
 * Better handling of `IAudioClient.IsFormatSupported`
 * `AsioOut` will no longer stop when it reaches the end

### 1.8.5 (3 Nov 2018)

- DMO Effect support via `DmoEffectWaveProvider` #413
- New Broadcast Wave File Writer `BwfWriter`
- Various bugfixes and enhancements:
  - Improvements to stopping recording in `WaveInEvent` #403
  - `WaveIn` and `WaveInEvent` support `GetPosition` #399
  - `CueWaveFileReader` support for `Stream` #409, #376
  - Fix reading wave files with odd chunk lengths #386
  - Fix some WASAPI exclusive /event mode issues #383
  - Fix 32 bit float ASIO sample converter #356
  - Fixing `IAudioCaptureClient` cast exception issue #350
  - `WaveOut` and `WaveOutEvent` read the actual volume #349
  - `PropVariant` support for `VT_FILETIME` #341
  - Added definitions of several media subtypes
  - Fixed offset bug in `StereoToMonoSampleProvider` #312
  - `KeySignatureEvent` reports flats properly as negative number #295
  - `WaveInProvider.Read` uses offset parameter #297
  - BREAKING - retired cakewalk drum map file format support
  - Retired Win 8 project in favour of UWP

### 1.8.4 (6 Dec 2017)

* Windows 10 Universal build now included in NuGet package
* adding a TotalTime property to WaveFileWriter
* adding a Broadcast Wave File Writer
* Various bugfixes and enhancements:
  * Prevent audio files from staying locked
  * additional constructor for MultiplexingWaveProvider
  * Faster SilenceWaveProvider implementation #257
  * fixing calling stoprecording without ever starting recording on WaveIn
  * improved reliability in WaveInEvent
  * make non-strict MIDI file checking tolerant of invalid CC values #250
  * Adding defaults for StereoToMonoProvider16 volumes #267

### 1.8.3 (5 Sep 2017)

* Allow access to property store of MMDevice
* Various bugfixes and enhancements:
  * Support unicode in MIDI TextEvent
  * Fixed noise issue on restart DirectSoundOut
  * improved support for mono AAC #223
  * fix NullReferenceException opening AsioOut by index #234


### 1.8.2 (6 Aug 2017)

* AudioFileReader supports filenames ending with .aif
* Various bugfixes and enhancements:
  * fixing problem with Mp3FileReader position advancing too rapidly #202
  * Implemented IDisposable in MMDevice
  * fix dispose of AudioSessionManager


### 1.8.1 (22 Jul 2017)

* AsioOut exposes FramesPerBuffer
* change WaveOut and WaveOutEvent default DeviceNumber to -1 (Mapper)
* Added MidiFile constructor overload that takes an input Stream object.
* Various bugfixes and enhancements:
  * desktop apps use MFCreateMFByteStreamOnStream instead of MFCreateMFByteStreamOnStream
  * Fix for propvariant marshalling #154
  * Soundfont should not require isng chunk #150
  * Fixed potential MFT memory leak
  * Mp3FileReader.ReadFrame advances Position #161
  * sfzfilereader class obsoleted
  * ensure DriverName property always set on AsioOut. #169
  * WaveFormatConversionProvider can throw an error in finalizer #188
  * Restore compatibility with .NET Portable. #189
  * improved error message for channel index out of range #208
  * Added Releasing of Com Object to AudioEndpointVolume Dispose

### 1.8.0 (27 Dec 2016)

* Windows 10 Universal project. Very similar feature set to the Win 8 one.
  * Added a Windows 10 Universal demo app with limited functionality  
* Windows 10 related bugfixes
  * WasapiOut fixed for Win 10
* WaveFileWriterRT for Win 8/10 (thanks to kamenlitchev)
* Improvements to Mp3FileReader seeking and position reporting (thanks to protyposis)
* updated NAudio build process to use FAKE, retiring the old MSBuild and IronPython scripts
* NAudio.Wma project is moved out into its own [GitHub repository](https://github.com/naudio/NAudio.Wma)
* ConcatenatingSampleProvider and FollowedBy extension method making it easy to concatenate Sample Providers
* MixingSampleProvider raises events as inputs are removed and allows access to list of inputs
* Improvements to MIDI event classes including clone support (thanks to Joseph Musser)
* SMBPitchShiftingSampleProvider (thanks to Freefall63)
* StreamMediaFoundationReader to allow using MediaFoundation with streams
* New Skip, ToMono, Take, ToStereo extension methods
* New SilenceProvider class
* OffsetSampleProvider fix for leadout following take
* Various bugfixes and enhancements. See commit log for full details
  * WasapiCapture buffer sizes can be specified 
  * MMDeviceEnumerator is disposable
  * MidiMessage better error reporting
  * More robust AIFF file handling
  * Fixed threading issue on WasapiCaptureRT
  * WasapiCaptureRT returns regular IEEE WaveFormat instead of WaveFormatExtensible   
  * RawSourceWaveStream allows you to read from part of input array
  * RawSourceStream handles end of stream better
  * PropVariant supports VT_BOOL
  * Better handling of exceptions in WaveFileReader constructor
  * WasapiOut default constructor (uses default device, shared mode)
  * WasapiCapture and WasapiLoopbackCapture can report capture state
  * BufferedWaveProvider can be configured to not fully read if no data is available
  * WasapiOut can report the default mix format for shared mode
  * AsioDriver and AsioDriver ext now public
  * Fix for Xing header writing
  * Fixed XING header creation bug
  * Fixed MIDI to type 1 converter bug
  
  
### 1.7.3 5 Mar 2015

* WaveFileWriter.Flush now updates the WAV headers, resulting in a playable file without having to call Dispose
* SampleToWaveProvider24 class added for conversion to 24 bit
* Audio Session APIs added to Core Audio API (thanks KvanTTT,  milligan22963)
* SimpleAudioVolume support in Core Audio API
* WasapiCapture can use events instead of Thread.Sleep like WasapiOut (thanks davidwood)
* NAudio has a logo! Can be found in the Assets folder of the Win 8 Demo
* WindowsRT assembly updated with support for additional core audio APIs (AudioSessionNotification, AudioStreamVolume, SessionCollection)
* Volume mixer demo added to NAudioDemo
* Various bugfixes and enhancements (see commit history for full log)
  * MMDeviceEnumerator.HasDefaultAudioEndpoint to determine if there is a default endpoint
  * AudioSessionControl no longer throws exceptions with Windows Vista
  * Expose IAudioStreamVolume from WsapiOut, and AudioClient.
  * Better handling 0 length Mp3 files
  * Word aligned Cue chunks
  * WaveOutEvent can set device volume
  * Better handling of WAVEFORMATEXTENSIBLE for WasapiIn

### 1.7.2 24 Nov 2014

* WaveFileReader and WaveFileWriter supporting data chunk > 2GB
* Working towards making WinRT build pass WACK
* WASAPI IAudioClock support
* MMDeviceEnumerator has Register and UnRegisterEndpointNotificationCallback
* TempoEvent can be modified
* Various bugfixes and enhancements (see commit history for full log)
  * BooleanMixerControl bugfix
  * DirectSoundOut fix for end of file
  * WasapiOut WinRT fixes
  * fix for stereo mu and a law
  * fix to MIDIHDR struct
  * WaveOutEvent dispose fix
  * Fixes for sync context issues in ASP.NET
  * Fixed WasapiOut could stop when playing resampled audio
  
### 1.7.1 10 Apr 2014

* WdlResampler - a fully managed resampler based on the one from CockosWDL
* AdsrSampleProvider for creating ADSR envelopes
* Improvements to demo apps 
  * MediaFoundationReader
  * 8 band graphic equalizer demo added
* More configurable BiQuad filter
* Various bugfixes and enhancements (see commit history for full log)
  * CurrentTime reporting fixed for mono files in AudioFileReader
  * WaveOut PlaybackState now gets correctly set to Stopped at end of file
  * MediaFoundationReader can raise WaveFormatChanged event
  * WaveOutEvent fixed to be restartafter reaching the end
  * OffsetSampleProvider bugfixes and TimeSpan helper methods
  * Cue markers RIFF chunk writing fixes
  * WaveIn and WaveOutEvent robustness fixes

### 1.7.0 29 Oct 2013

[Release announcement](http://markheath.net/post/naudio-17-release-notes)
* MediaFoundationReader allows you to play any audio files that Media Foundation can play, which on Windows 7 and above means playback of AAC, MP3, WMA, as well as playing the audio from video files.
* MediaFoundationEncoder allows you to easily encode audio using any Media Foundation Encoders installed on your machine. The WPF Demo application shows this in action, allowing you to encode AAC, MP3 and WMA files in Windows 8.
* MediaFoundationTransform is a low-level class designed to be inherited from, allowing you to get direct access to Media Foundation Transforms if that’s what you need.
* MediaFoundationResampler direct access to the Media Foundation Resampler MFT as an IWaveProvider, with the capability to set the quality level.
* NAudio is now built against .NET 3.5. This allows us to make use of language features such as extension methods, LINQ and Action/Func parameters.
* You can enumerate Media Foundation Transforms to see what’s installed. The WPF Demo Application shows how to do this.
* WasapiCapture supports exclusive mode, and a new WASAPI capture demo has been added to the WPF demo application, allowing you to experiment more easily to see what capture formats your soundcard will support.
* A new ToSampleProvider extension method on IWaveProvider now makes it trivially easy to to convert any PCM WaveProvider to an ISampleProvider. There is also another extension method allowing an ISampleProvider to be passed directly into any IWavePlayer implementation without the need for converting back to an IWaveProvider first.
* WaveFileWriter supports creating a 16 bit WAV file directly from an ISampleProvider with the new CreateWaveFile16static method.
* IWavePosition interface implemented by several IWavePlayer classes allows greater accuracy of determining exact position of playback. Contribution courtesy of ioctlLR
* AIFF File Writer (courtesy of Gaiwa)
* Added the ability to add a local ACM driver allowing you to use ACM codecs without installing them. Use AcmDriver.AddLocalDriver
* ReadFullyproperty allows you to create never-ending MixingSampleProvider, for use when dynamically adding and removing inputs.
* WasapiOut now allows setting the playback volume directly on the MMDevice.
* Support for sending MIDI Sysex messages, thanks to Marcel Schot
* A new BiQuadFilterfor easy creation of various filter types including high pass, low pass etc
* A new EnvelopeGeneratorclass for creating ADSR envelopes based on a blog post from Nigel Redmon.
* Lots of bugfixes (see the commit history for more details). Some highlights include…
  * Fixed a long-standing issue with MP3FileReader incorrectly interpreting some metadata as an MP3 frame then throwing an exception saying the sample rate has changed.
  * WaveFileReader.TryReadFloat works in stereo files
  * Fixed possible memory exception with large buffer sizes for WaveInBuffer and WaveOutBuffer
* Various code cleanups including removal of use of ApplicationException, and removal of all classes marked as obsolete.
* Preview Release of WinRT support.The NAudio nuget package now includes a WinRT version of NAudio for Windows 8 store apps. This currently supports basic recording and playback. This should still very much be thought of as a preview release. There are still several parts of NAudio (in particular several of the file readers and writers) that are not accessible, and we may need to replace the MFT Resampler used by WASAPI with a fully managed one, as it might mean that Windows Store certification testing fails.
  * Use WasapiOutRT for playback
  * Use WasapiCaptureRTfor record (thanks to Kassoul for some performance enhancement suggestions)
  * There is a demo application in the NAudio source code showing record and playback

### 1.6.0 26 Oct 2012

[Release Announcement](http://markheath.net/post/naudio-16-release-notes-10th)

* WASAPI Loopback Capture allowing you to record what your soundcard is playing (only works on Vista and above)
* ASIO Recording ASIO doesn’t quite fit with the IWaveIn model used elsewhere in NAudio, so this is implemented in its own special way, with direct access to buffers or easy access to converted samples for most common ASIO configurations. Read more about it here.
* MultiplexingWaveProvider and MultiplexingSampleProvider allowing easier handling of multi-channel audio. Read more about it here.
* FadeInOutSampleProvider simplifying the process of fading audio in and out
* WaveInEvent for more reliable recording on a background thread
* PlaybackStopped and RecordingStoppedevents now include an exception. This is very useful for cases when USB audio devices are removed during playback or record. Now there is no unhandled exception and you can detect this has happened by looking at the EventArgs. (n.b. I’m not sure if adding a property to an EventArgs is a breaking change – recompile your code against NAudio 1.6 to be safe).
* MixingWaveProvider32 for cases when you don’t need the overhead of WaveMixerStream. MixingSampleProvider should be preferred going forwards though.
* OffsetSampleProvider allows you to delay a stream, skip over part of it, truncate it, and append silence. Read about it here.
* Added a Readme file to recognise contributors to the project. I’ve tried to include everyone, but probably many are missing, so get in touch if you’re name’s not on the list.
* Some code tidyup(deleting old classes, some namespace changes. n.b. these are breaking changes if you used these parts of the library, but most users will not notice). This includes retiring WaveOutThreadSafe which was never finished anyway, and WaveOutEvent is preferred to using WaveOut with function callbacks in any case.
* NuGet package and CodePlex download now use the release build (No more Debug.Asserts if you forget to dispose stuff)
* Lots of bugfixes, including a concerted effort to close off as many issues in the CodePlex issue tracker as possible.
* Fix to GSM encoding
* ID3v2 Tag Creation
* ASIO multi-channel playback improvements
* MP3 decoder now flushes on reposition, fixing a potential issue with leftover sound playing when you stop, reposition and then play again.
* MP3FileReader allows pluggable frame decoders, allowing you to choose the DMO one, or use a fully managed decoder (hopefully more news on this in the near future)
* WMA Nuget Package (NAudio.Wma) for playing WMA files. Download here.
* RF64 read support

### 1.5.0 18 Dec 2011

[Release Announcement](http://markheath.net/post/naudio-15-released)

* Now available on NuGet!
* Numerous bugfixes mean we are now working fully in x64 as well as x86, so NAudio.dll is now marked as AnyCPU. (You can still force x86 by marking your own executable as x86 only.)
* WaveOutEvent – a new WaveOut mode with event callback, highly recommended instead of WaveOut with function callbacks
* 24 bit ASIO driver mode (LSB)
* Float LSB ASIO driver mode
* WaveFileWriter has had a general code review and API cleanup
* Preview of new ISampleProvider interface making it much easier to write custom 32 bit IEEE (float) audio pipeline components, without the need to convert to byte[]. Lots of examples in NAudioDemo of using this and more documentation will follow in future.
* Several ISampleProvider implementations to get you started. Expect plenty more in future NAudio versions:
  * PanningSampleProvider
  * MixingSampleProvider
  * MeteringSampleProvider
  * MonoToStereoSampleProvider
  * NotifyingSampleProvider
  * Pcm16BitToSampleProvider
  * Pcm8BitToSampleProvider
  * Pcm24BitToSampleProvider
  * SampleChannel
  * SampleToWaveProvider
  * VolumeSampleProvider
  * WaveToSampleProvider
* Added AiffFileReader courtesy of Giawa
* AudioFileReader to simplify opening any supported file, easy volume control, read/reposition locking
* BufferedWaveProvider uses CircularBuffer instead of queue (less memory allocations)
* CircularBuffer is now thread-safe
* MP3Frame code cleanup
* MP3FileReader throws less exceptions
* ASIOOut bugfixes for direct 16 bit playback
* Some Demos added to NAudioDemo to give simple examples of how to use the library
  * NAudioDemo has an ASIO Direct out form, mainly for testing the AsioOut class at different bit depths (still recommended to convert to float before you get there).
  * NAudioDemo has simple MP3 streaming form (play MP3s while they download)
  * NAudioDemo has simple network streaming chat application
  * NAudioDemo playback form uses MEF to make it much more modular and extensible (new output drivers, new file formats etc)
  * NAudioDemo can play aiff
* GSM 6.10 ACM codec support
* DSP Group TrueSpeech ACM codec support
* Fully managed G.711 a-law and mu-law codecs (encode & decode)
* Fully managed G.722 codec (encode & decode)
* Example of integration with NSpeex
* Fix to PlaybackStopped using SyncContext for thread safety
* Obsoleted IWavePlayer.Volume (can still set volume on WaveOut directly if you want)
* Improved FFT display in WPF demo
* WaveFileReader - tolerate junk after data chunk
* WaveOut constructor detects if no sync context & choose func callbacks
* WaveOut function mode callbacks hopefully chased out the last of the hanging bugs (if in a WaveOutWrite at same time as WaveOutReset, bad things happen - so need locks, but if WaveOutReset called during a previous func callback that is about to call waveOutWrite we deadlock)
* Now has an msbuild script allowing me to more easily create releases, run tests etc
* Now using Mercurial for source control, hopefully making bug fixing old releases and accepting user patches easier. n.b. this unfortunately means all old submitted patches are no longer available for download on the CodePlex page.
* WPF Demo enhancements:
  * WPF Demo is now .NET 4, allowing us to use MEF, and will be updated hopefully with more examples of using NAudio.
  * WPF Demo uses windowing before FFT for a more accurate spectrum plot
  * WPF Demo has visualization plugins, allowing me to trial different drawing mechanisms
  * WPF Demo has a (very basic) drum machine example

### 1.4.0 20 Apr 2011

[Release announcement](http://markheath.net/post/naudio-14-release-notes)

* Major interop improvements to support native x64. Please note that I have not in this release changed the dll’s target platform away from x86 only as I don’t personally have an x64 machine to test on. However, we are now in a state where around 95% of the interop should work fine in x64 mode so feel free to recompile for “any CPU”. You should also note that if you do run in native x64 mode, then you probably will find there are no ACM codecs available, so WaveFormatConversionStream might stop working – another reason to stay targetting x86 for now.
* There have also been major enhancements to MP3 File Reader, which is the main reason for pushing this new release out. Please read this post for more details as this is a breaking change – you no longer need to use a WaveFormatConversionStream or a BlockAlignReductionStream.
* More examples IWaveProvider implementers have been added, including the particularly useful BufferedWaveProvider which allows you to queue up buffers to be played on demand.
  * BufferedWaveProvider
  * Wave16toFloatProvider
  * WaveFloatTo16Provider
  * WaveInProvider
  * MonoToStereoProvider16
  * StereoToMonoProvider16
  * WaveRecorder
* The NAudioDemo project has been updated to attempt to show best practices (or at least good practices) of how you should be using these classes.
* The NAudioDemo project also now demonstrates how to select the output device for WaveOut, DirectSoundOut, WasapiOut and AsioOut.
* WaveChannel32 can now take inputs of more bit depths – 8, 16, 24 and IEEE float supported. NAudioDemo shows how to play back these files.
* A general spring clean removed a bunch of obsolete classes from the library.
* AsioOut more reliable, although I still think there are more issues to be teased out. Please report whether it works on your hardware.
* WaveFileReader and WaveFileWriter support for 24 and 32 bit samples
* Allow arbitrary chunks to appear before fmt chunk in a WAV file
* Reading and writing WAV files with Cues
* Obsoleted some old WaveFileWriter and WaveFileReader methods
* Fixed a longstanding issue with WaveOutReset hanging in function callbacks on certain chipsets
* Added sequencer specific MIDI event
* RawWaveSourceStream turns a raw audio data stream into a WaveStream with specified WaveFormat
* A DMO MP3 Frame Decoder as an alternative to the ACM one
* Easier selection of DirectSound output device
* WaveOut uses 2 buffers not 3 by default now (a leftover from the original days of NAudio when my PC had a 400MHz Pentium II processor!).
* Lots more minor bug fixes & patches applied – see the check-in history for full details

### 1.3.0 10 Oct 2009

[Release Announcement](http://markheath.net/post/naudio-13-release-notes)

* WaveOut has a new constructor (this is breaking change), which allows three options for waveOut callbacks. This is because there is no “one size fits all” solution, but if you are creating WaveOut on the GUI thread of a Winforms or WPF application, then the default constructor should work just fine. WaveOut also allows better flexibility over controlling the number of buffers and desired latency.
* Mp3FileReader and WaveFileReadercan have a stream as input, and WaveFileWritercan write to a stream. These features are useful to those wanting to stream data over networks.
* The new IWaveProvider interface is like a lightweight WaveStream. It doesn’t support repositioning or length and current position reporting, making the implementation of synthesizers much simpler. The IWavePlayer interface takes an IWaveProvider rather than WaveStream. WaveStream implements IWaveProvider, so existing code continues to work just fine.
* Added in LoopStream, WaveProvider32 and WaveProvider16 helper classes. Expect more to be built upon these in the future.
* I have also started using the WaveBuffer class. This clever idea from Alexandre Mutel allows us to trick the .NET type system into letting us cast from byte[] to float[] or short[]. This improves performance by eliminating unnecessary copying and converting of data.
* There have been many bugfixes including better support for VBR MP3 file playback.
* The mixer API has had a lot of bugs fixed and improvements, though differences between Vista and XP continue to prove frustrating.
* The demo project (NAudioDemo) has been improved and includes audio wave-form drawing sample code.
* There is now a WPF demo as well (NAudioWpfDemo), which also shows how to draw wave-forms in WPF, and even includes some preliminary FFT drawing code.
* The WaveIn support has been updated and enhanced. WaveInStream is now obsolete.
* WASAPI audio capture is now supported.
* NAudio should now work correctly on x64operating systems (accomplished this by setting Visual Studio to compile for x86).

### 1.2.0 26 Jun 2008

[Release Announcement](http://markheath.net/post/naudio-12-release-notes)

* WASAPI Output Model. We are now able to play audio using the new WASAPI output APIs in Windows Vista. We support shared mode and exclusive mode, and you can optionally use event callbacks for the buffer population. You may need to experiment to see what settings work best with your soundcard.
* ASIO Output Model. We can also play back audio using any ASIO output drivers on your system. It is not working yet with all soundcards, but its working great with the ever-popular ASIO4All.
* New DirectSound Output Model. We have moved away from using the old managed DirectX code for DirectSound output, and done the interop ourselves. This gives us a much more reliable way to use DirectSound.
* IWavePlayer simplifications. As part of our ongoing plans to improve the NAudio architecture, the IWavePlayer interface has gone on a diet and lost some unnecessary methods.
* ResamplerDMO stream. Some Windows Vista systems have a Resampler DirectX Media Object that can be used to convert PCM and IEEE audio samples between different sample rates and bit depths. We have provided a managed wrapper around this, and it is used internally by the WASAPI output stream to do sample rate conversion if required.
* ACM Enhancements - There have been a number of bugfixes and enhancements to the support for using the ACM codecs in your system.
* BlockAlignmentReductionStream - This WaveStream helps to alleviate the problem of dealing with compressed audio streams whose block alignment means that you can't position exactly where you want or read the amount you want. BlockAlignmentReductionStream uses buffering and read-ahead to allow readers full flexibility over positioning and read size.
* MP3 Playback - The MP3 File Reader Stream is now able to work with any wave output thanks to the BlockAlignmentReductionStream and playback MP3 files without stuttering. It uses any MP3 ACM decoder it can find on your system.
* Custom WaveFormat Marshaler - The WaveFormat structure presents an awkward problem for interop with unmanaged code. A custom marshaler has been created which will be extended in future versions to allow WaveFormat structures to present their extra data.
* NAudioDemo- One of the problems with NAudio has been that there are very few examples of how to use it. NAudioDemo has four mini-examples of using NAudio:
  * receiving MIDI input
  * playing WAV or MP3 files through any output
  * examining ACM codecs and converting files using them
  * recording audio using WaveIn
  * In addition the AudioFileInspector, MixDiff, MIDI File Splitter and MIDI File Mapper projects demonstrate other aspects of the NAudio framework.
* Unit Tests - NAudio now has a small collection of unit tests, which we intend to grow in future versions. This will help us to ensure that as the feature set grows, we don't inadvertently break old code.
* IWaveProvider Tech Preview - As discussed recently on my blog, we will be using a new interface called IWaveProvider in future versions of NAudio, which uses the WaveBuffer class. This code is available in the version 1.2 release, but you are not currently required to use it.
* Alexandre Mutel- Finally, this version welcomes a new contributor to the team. In fact, Alexandre is the first contributor I have added to this project. He has provided the new implementations of ASIO and DirectSoundOut, as well as helping out with WASAPI and the new IWaveProvider interface design. His enthusiasm for the project has also meant that I have been working on it a little more than I might have otherwise!

### 1.1.0 26 May 2008
 * Added some new NoteEvent and NoteOnEvent constructors    
 * WaveOffsetStream
 * WaveStream32 preparation for 24 bit inputs
 * WaveStream32 new default constructor
 * Made the decibels to linear conversion functions public
 * New constructor for ControlChangeEvent
 * New constructor for ChannelAfterTouchEvent
 * New constructor and property setting for PatchChangeEvent
 * New constructor for PitchWheelChangeEvent
 * Bugfix for sysex event writing
 * MidiEvent IsEndTrack and IsNoteOff are now static functions
 * New IsNoteOn function
 * NoteOnEvent now updates the NoteNumber and Channel of its OffEvent when they are modified
 * MIDI events are now sorted using a stable algorithm to allow batch file processing utilities to retain original ordering of events with the same start times.
 * New MidiEventCollection class to make converting MIDI file types more generic
 * Added an NUnit unit tests library
 * Fixed a bug in meta event constructor
 * MidiFile updated to use MidiEventCollection
 * Many enhancements to MIDI interop
 * New MidiIn, MidiInCapabilities classes
 * Added a new NAudioDemo for testing / demonstrating use of NAudio APIs
 * More MidiEventCollection automated tests
 * Test application can now send test MIDI out messages

### 1.0.0 19 Apr 2007
* Minor updates to support EZdrummer MIDI converter
* Beginnings of a new WaveOut class with its own thread
* Fixed a bug in WaveFileReader
* Fix to ensure track-view shows correct length
* An alternative thread-safe approach using locking
* Initial ASIO classes created
* Support for exporting MIDI type 0 files
* Can parse MIDI files with more than one end track marker per track
* Recognises some more rare MIDI meta event types
* Initial support for reading Cakewalk drum map files
* MIDI events report channel from 1 to 16 now rather than 0 to 15
* Got rid of the fader png image
* Cakewalk drum map enhancements
* ByteEncoding added
* MIDI Text events use byte encoding for reading and writing
* ProgressLog control and AboutForm added
* MIDI Text events can have their text modified
* ProgressLog control can report its text
* Initial support for file association modification
* Bug fixes to file associations    
* Support for modifying MIDI Control Change Event parameters
* After-touch pressure can be set
* Note number and velocity can be set
* Pitch wheel event modifications    
* Helper function for detecting note off events
* Updated some XML documentation
* Some checking for end of track markers in MIDI files
* WaveMixerStream32 updated ready to support dynamic adding of streams
* Some bugfixes to WaveOut to support auto stop mode again

### 0.9.0 6 Oct 2006
* ACM stream bug fixes
* Support for waveOut window message callbacks
* Wave In Recording bug fixes
* SimpleCompressor Wave Stream
* Optimisation to WaveViewer
* Minor bugfixes to Wave classes
* Created a new Pot control
* Real-time adjustment of SimpleCompressor Wave Stream
* Pot control drawing enhancements
* The beginnings of a track-view control
* The beginnings of a time-line control
* TimeLine control has a now cursor
* TimeLine control can zoom
* TimeLine supports changing colours
* TrackView can draw clips
* New trackheader control
* MIDI events now support being exported
* MIDI TrackSequenceNumber event
* MIDI KeySignature event
* Bugfix for exporting note-off
* Alternative constructors for MIDI events
* Bugfix for exporting MIDI variable length integers
* WaveFileReader can report information on non-standard chunks
* Bugfix MIDI export event sorting
* Bugfix MIDI export event sorting
* Some support for modifying MIDI event parameters
* Bugfix Time Signature Event and Control Change Event
* New SMPTE Offset event
* Patch and Bank name meta events added
* Meta events use VarInts for lengths now
* Allow non-strict reading of MIDI file
  
### 0.8.0 21 Feb 2006
* Minor bug fix to WaveMixer classes
* NICE specific code removed
* MP3 Reader can read ID3 tags and frames now
* Xing header support
* Reorganised class structures
* WaveIn recording support added
* More structural reorganisation
* Got rid of some compiler warnings
* Retired 16 bit mixing code
* Improved WaveViewer control
* Fader control uses an image for the slider
* Added some copyright messages to SoundFont source files
* Added BiQuad filters class
* Added envelope detector
* Added simple compressor
* Added simple gate
  
### 0.7.0 12 Dec 2005
* Made a 16 and 32 bit mixer stream
* Made a 32 bit WaveChannel stream
* A 32 to 16 bit conversion stream
* More MM error codes
* 32 bit audio path tested and working
* Initial support for an ACM MP3 decoder - not working yet    
* Basic working MP3 playback
* ADPCM Wave Format
* Wave Formats can serialize themselves
* WaveFileWriter can write non PCM formats
* WaveFileWriter writes a fact chunk - non-ideal though
* Improved support for playback of compressed formats
* Improvements to BlockAlign and GetReadSize
* Nice ADPCM converter
* Support for AGC codec
* Support for Speed codec
* WaveStream inherits Stream

### 0.6.0 16 Nov 2005
* Dual channel strip in WavePlayer
* Fixed bad calculation of offset seconds in WavePlayer
* Improved checking that we don't go beyond the end of streams
* SoundFont reading improvements for conversion to sfz
* IWavePlayer interface
* Initial DirectSoundOut class
* Major rework to return to 8 bit reads on all WaveStream, ready for inheriting Stream
* Cleaned up WaveFileReader
* WaveOut is an IWavePlayer
* WaveFormatStream
* Ability to select between WaveOut and DirectSound
* Initial playing back through DirectSound
* Retired StreamMixer project
* WavePlayer better switching between settings
* DirectSound feeds in on a timer now, (from MSDN Coding 4 Fun Drum Machine demo)
* DirectSoundOut fills buffer only twice per latency
* DirectSoundOut stops at end
* WavePlayer now has three channels
* Selectable latency in WavePlayer
* DirectSoundOut now only reads buffers of the right size, which solves GSM cutout issues
* WaveOut dispenses with an unnecessary delegate by passing WaveStream to WaveBuffer
* Fixed a crash in AdjustVolume on the MixerStream
* sfz loop_continuous fix
* Converted to .NET 2.0
* n.b. DirectSound has issues - needed to turn off the LoaderLock Managed Debug Assistant

### 0.5.0 31 Oct 2005
* WaveChannel can supply from stereo input
* Initial VST interfaces and enums
* VstLoader implements IVstEffect
* Began converting dispatcher opcodes to IVstEffect functions
* Finished IVstEffect functions
* IVstEffect function implementations for VstLoader
* Final consolidation of VST, prior to removal
* Wave Channel can convert mono to stereo now
* Wave Channel and Wave Mixer used for first time
* Volume and pan slider controls
* Channel strip and WavePlayer export to WAV
* WaveMixer doesn't go on indefinitely
* Some more LCD control characters
* Initial WaveViewer control

### 0.4.0 12 May 2005
* changes recommended by FxCop
* namespace changed to NAudio
* XML documentation, FxCop fixes, Namespace improvements
* WaveFormat constructor from a BinaryReader
* WaveChannel and WaveMixerStream
* More namespace improvements
* More XML documentation
* Ogg encoder improvements
* ACM driver enumeration
* Got test apps building again
* Retired the JavaLayer port - its a few versions out of date anyway
* WaveBuffer is now 16bit - experimental, needs optimising
* WaveStream::ReadInt16 optimisation
* Fixed bugs in 16 bit positioning code
* More XML documentation
* Initial Fader control implementation
* A very basic time domain convolution
* Improvements to wave-reader and writer for floating point audio

### 0.3.0 8 Mar 2005 
* Skip backwards and forwards in wav file
* WavPlayer trackBar indicates progress
* Allows trackBar repositioning
* WavePlayer show current time in hh:mm:ss
* Can start playing from any point in the file
* More ACM stream conversion interop
* More ACM interop improvements
* WaveFormatConversionStream class
* WaveStream no longer inherits from Stream
* AcmStream class
* waveOutDevCaps interop
* Improvements to WaveFileWriter
* AcmStream and AcmStreamHeader bug fixes
* Improvements to WaveFileReader and WaveFileWriter
* PCM to PCM offline conversion working		
* Very basic ability to play converted streams in realtime
* Initial version of Renaissance GSM conversion stream
* Fix to WaveFileWriter
* More disposable pattern
* WaveFileConversionStream can convert files offline
* WaveStreams can now recommend a read buffer size based on latency
* Offline Renaissance GSM stream conversion working
* WaveOut takes a latency now
* MmException improvement
* Greatly improved the ability to calculate appropriate buffer sizes
* Realtime GSM decoding is now working

### 0.2.0 25 Feb 2005
* Improvements to WaveStream class
* SoundFont library merged
* Converted to Visual Studio .NET
* Merged JavaLayer
* Merged newer SoundFontLib, MidiLib, Ogg, Acm
* Generic WaveStream class and WaveFileReader
* Improved class design trying to fix WaveOut bug (waveout callback was being GCed)

### 0.1.0 23 Dec 2002
* Added pause and stop for WaveOut
* Got wave playing working better
* Wave functions improved
* Mixer bugfixes and design improvements
* Added basic WaveOut interop &amp; classes
* Improvements to Mixer interop &amp; classes
* Added MIDI interop, MMException, more mixer classes

### 0.0.0 9 Dec 2002
* Initial version, basic mixer interop
