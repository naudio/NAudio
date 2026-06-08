### Unreleased

<!--
Bullets land here as PRs merge. The maintainer renames this section to
"### 3.0.0 (date)" at release time. See CLAUDE.md and
Docs/Architecture/ReleaseStrategy.md for the release-notes process.
-->

#### Breaking changes

 * `AudioVolumeLevel` moved from `NAudio.Wasapi.CoreAudioApi` to `NAudio.CoreAudioApi` — it now lives in the same namespace as the rest of the WASAPI/Core Audio API (`MMDevice`, `Part`, `DeviceTopology`, etc.) that it's returned from. The parallel `NAudio.Wasapi.CoreAudioApi` namespace (which otherwise held only internal COM-activation plumbing) is gone
 * `DmoMp3FrameDecompressor` moved from `NAudio.FileFormats.Mp3` to `NAudio.Dmo` — it's a DMO wrapper and now shares the namespace of the `NAudio.Dmo` assembly it ships in (alongside `DmoEffectWaveProvider`, `ResamplerDmoStream`, etc.)
 * `WaveFileChunkReader` is now `internal` (and moved from `NAudio.FileFormats.Wav` to `NAudio.Wave`) — it was internal plumbing for `WaveFileReader`. Read custom RIFF chunks via `WaveFileReader.Chunks` (`WaveChunks` / `RiffChunk` / `IWaveChunkInterpreter<T>`) instead
 * `CaptureState` enum moved from `NAudio.CoreAudioApi` to `NAudio.Wave` — it's a backend-agnostic capture-state type used by `WaveIn`, `WasapiCapture`, and `WasapiRecorder`, and was only in `NAudio.CoreAudioApi` for historical reasons. Code that named the type via `using NAudio.CoreAudioApi;` now needs `using NAudio.Wave;`
 * `IWaveProvider.Read` signature changed from `Read(byte[], int, int)` to `Read(Span<byte>)`. Existing callers with `byte[]` migrate via `source.Read(buffer.AsSpan(offset, count))`; implementations override `Read(Span<byte>)`
 * `ISampleProvider.Read` signature changed from `Read(float[], int, int)` to `Read(Span<float>)` (same migration pattern)
 * `MidiIn`, `MidiOut`, `MidiInCapabilities`, and `MidiOutCapabilities` moved from `NAudio.Midi` to `NAudio.WinMM` — all `winmm.dll` interop now lives in one assembly
 * `MmResult`, `MmException`, and `Manufacturers` moved from `NAudio.Core` to `NAudio.WinMM`
 * `DirectSoundOut` moved from `NAudio.Core` to `NAudio.Dmo` (DirectSound has always been Windows-only)
 * **New `NAudio.Dmo` package.** DMO effects (echo, chorus, reverb, etc.), the DMO MP3 decoder (`DmoMp3FrameDecompressor`), the DMO resampler (`ResamplerDmoStream`), and `DirectSoundOut` carved out of `NAudio.Wasapi`. Namespaces preserved (`NAudio.Dmo`, `NAudio.Dmo.Effect`, `NAudio.Wave` for `DirectSoundOut`). Meta-package consumers see no change — `NAudio.Dmo` comes in transitively. Direct `NAudio.Wasapi` consumers who use the DMO/DirectSound types now need an explicit `<PackageReference Include="NAudio.Dmo" />`.
 * `NAudio.Midi` is now cross-platform — its `net9.0` target no longer P/Invokes `winmm.dll`
 * `MidiInMessageEventArgs.Timestamp` and `MidiInSysexMessageEventArgs.Timestamp` are now `TimeSpan` (previously `int` milliseconds) — preserves full WinRT 100 ns resolution on the WinRT backend
 * `MidiIn.CreateSysexBuffers` removed — `MidiIn` now allocates sysex receive buffers automatically inside `Start()`
 * `WasapiOut`, `WasapiCapture`, and `WasapiLoopbackCapture` are now `[Obsolete]` in favour of the new `WasapiPlayer` / `WasapiRecorder` APIs (the legacy types still ship and continue to work)
 * `WasapiOut`'s embedded DMO resampler removed. Exclusive-mode callers whose source format is not natively supported now get a `NotSupportedException` from `Init` instead of silent on-the-fly resampling. Resample upstream (e.g. with `MediaFoundationResampler`), use shared mode (which still auto-converts via `AutoConvertPcm`), or switch to `WasapiPlayerBuilder`. Removes `NAudio.Wasapi`'s only intra-assembly dependency on DMO.
 * `WaveOut` and `WaveIn` now default to event-driven callbacks; the legacy window-based variants are renamed `WaveOutWindow` / `WaveInWindow` and live in `NAudio.WinForms`
 * `WaveInEventArgs` now fires one event per WASAPI packet (previously batched). A new `BufferSpan` property exposes the data without copying through the existing `Buffer` byte array
 * Several `Mf*` Media Foundation wrapper types are now `internal` — only `MfActivate` and `MediaType` remain public
 * `BufferedWaveProvider` buffer duration is now set in the constructor (default 5 seconds); `BufferLength` and `BufferDuration` are read-only
 * `WaveBuffer` is deprecated — use `MemoryMarshal.Cast` instead
 * `MMDevice.AudioClient` is `[Obsolete]` because it created a new instance per access; use `MMDevice.CreateAudioClient()`
 * `PropertyStore[int]` now resolves `PropVariant` values safely; the indexer that returned the raw `PropVariant` is `[Obsolete]`
 * Minimum target framework is now `net9.0` (previously supported legacy .NET Framework and .NET Standard 2.0)
 * `CueWaveFileReader` removed - use `new WaveFileReader(...).Chunks.ReadCueList()` to get a `CueList`
 * `StreamMediaFoundationReader` now throws `ArgumentException` for non-readable or non-seekable streams instead of failing later (#1288)
 * Corrected `HResult.E_INVALIDARG` to `0x80070057` (was the legacy `0x80000003`) and deprecated `HResult.MAKE_HRESULT` in favour of `MakeHResult` (#1288)
 * `SimpleCompressorEffect` (formerly `SimpleCompressorStream`) removed, along with the internal ChunkWare `SimpleCompressor` / `SimpleGate` / `EnvelopeDetector` — superseded by the new `NAudio.Effects` framework; high-quality dynamics effects follow in a later NAudio 3 phase
 * `ImpulseResponseConvolution` removed — it was an unusable O(n²) time-domain stub; FFT-based convolution will replace it in a later NAudio 3 phase
 * `NAudio.Extras.Equalizer` and `NAudio.Extras.EqualizerBand` removed — replaced by `NAudio.Effects.Equalizer` / `EqualizerBand` in `NAudio.Core`. The new EQ is per-channel, click-free when retuned, and adds shelves/pass/notch/band-pass/all-pass shapes. Band API changed: `Bandwidth`/`Gain` → `Q`/`GainDb` (or `ShelfSlope`), and the equaliser is an `IAudioEffect` (wrap with `EffectSampleProvider` instead of passing a source to the constructor)

#### New features

 * **Stereo samples in the sampler:** SFZ and single-sample instruments now play stereo samples with channel separation (the voice runs a second interpolating reader over the right channel in lockstep, filters each channel independently, and pans as a balance) rather than down-mixing to mono. `WaveSampleLoader` and `ISfzSampleLoader` now return left/right channels; `SingleSampleInstrument` takes an optional right channel
 * **SFZ Tier-2 note-on selection:** the sampler now honours keyswitches (`sw_lokey`/`sw_hikey`/`sw_last`/`sw_default`), round-robin (`seq_length`/`seq_position`), random layers (`lorand`/`hirand`) and CC gating (`loccN`/`hiccN`) — gating which regions sound on each note-on, with keyswitch presses making no sound
 * **SFZ Tier-1 finish:** the sampler now honours SFZ `trigger` (release samples play on note-off; `first`/`legato` select on held notes), `loop_mode=one_shot` (plays through note-off), directional `off_by` choke groups, and all four `fil_type` shapes — low-pass, high-pass, band-pass and band-reject. New state-preserving `BiQuadFilter.UpdateHighPassFilter`/`UpdateBandPassFilter`/`UpdateNotchFilter` (mirroring `UpdateLowPassFilter`) back the modulatable voice filters
 * **NAudio.SoundFile:** new cross-platform `SoundFileReader` / `SoundFileWriter` wrapping libsndfile — reads and writes WAV/AIFF/FLAC/Ogg-Vorbis/Opus/MP3 on Linux, macOS and Windows (the first cross-platform FLAC/Vorbis/Opus *encoder* in NAudio). `SoundFileReader` is a `WaveStream` and `ISampleProvider`; both reader and writer also work over a `System.IO.Stream`. Requires a system libsndfile; `SoundFileCapabilities` reports which codecs the build supports (#1289)
 * **Sampler DSP primitives (in `NAudio.Core`):** `SynthMath` (MIDI-note/frequency, cents, timecents and centibel conversions), `DahdsrEnvelope` (six-stage delay/attack/hold/decay/sustain/release envelope), `SampleSource` + `InterpolatingSampleReader` (variable-rate, looping sample playback with linear/Hermite interpolation), and a start-delay on `Lfo`. Shared building blocks for the forthcoming NAudio 3 sampler/synth (see `Docs/Architecture/SamplerDesign.md`)
 * **SoundFont 24-bit samples:** `SoundFont` now reads the optional `sm24` sub-chunk, exposing `SampleData24` (the low 8 bits of each sample) and `Has24BitSamples`
 * **SoundFont modulators:** `ModulatorType` now exposes its decoded fields (`Polarity`, `Direction`, `IsMidiContinuousController`, `ControllerSource`, `SourceType`, `MidiContinuousControllerNumber`) instead of keeping them private — needed by the forthcoming sampler modulator engine. `SampleHeader` fields are now properties
 * **SoundFont resolved instruments:** new `Preset.ResolveRegions()` / `SoundFontInstrumentResolver` flattens a preset into playable `SoundFontRegion`s, applying the SF2.04 generator model (instrument-absolute then preset-additive values over the spec defaults, global zones, key/velocity-range intersection). `SoundFontGenerators` exposes the accumulated generator values. First synthesis-facing step toward the sampler
 * **New `NAudio.Sampler` package:** `SoundFontSampler` is a polyphonic, cross-platform software sampler that plays a SoundFont through MIDI events and renders 32-bit float stereo as an `ISampleProvider`. Voice engine: pitch from root-key/tuning, the three SF2 loop modes, DAHDSR amplitude *and* modulation envelopes, two per-voice LFOs (modulation + vibrato) routed to pitch/filter/volume, a resonant low-pass filter modulated per block, velocity/attenuation gain, equal-power pan, voice stealing, exclusive-class (choke) groups, and channel state (program/bank select, pitch-bend, sustain pedal, volume/expression) (see `Docs/Architecture/SamplerDesign.md`)
 * **SoundFont modulator engine (SF2.04 §8.4):** the sampler now applies the implicit default modulators (velocity→attenuation/filter, CC1/channel-pressure→vibrato, CC7/CC11→attenuation, CC10→pan, CC91/CC93→sends) and any file-defined modulators, evaluating each against live MIDI controllers at control rate through the linear/concave/convex/switch source curves and combining levels per §9.5. Replaces the provisional velocity²-gain approximation. New supporting API: `SoundFontRegion.InstrumentModulators`/`PresetModulators`, the public `ModulatorType(ushort)` constructor and `ModulatorType.RawValue`, `Modulator.HasIdenticalRouting`, `TransformEnum.AbsoluteValue`, and `NAudio.Sampler.SoundFontModulatorMath` (the transform curves)
 * **MIDI-file playback/render:** `NAudio.Sampler.MidiFileSequence` loads a `MidiFile` onto a sequencing `EventTimeline` (canonical PPQ, tempo map from the file's tempo events); `SequencedMidiInstrument` plays it on any sampler (`SoundFontSampler`/`SfzSampler`/`SingleSampleSampler`) as an `ISampleProvider` with sample-accurate event dispatch; `OfflineMidiRenderer` renders a sequence to a float buffer or a WAV file offline. First end-to-end MIDI-file → audio path
 * **Single-sample instrument:** new `NAudio.Sampler.SingleSampleInstrument` maps one mono buffer (loaded WAV or a recording) across the keyboard at a chosen root key with editable start/end, loop points/mode, tuning, gain, pan, velocity tracking and amplitude envelope; `SingleSampleSampler` plays it through the shared engine (`SingleSampleSampler.FromWaveFile(path)`), rebuilding per note so edits are heard immediately. New shared `WaveSampleLoader` (WAV → mono float) backs it and the SFZ loader
 * **SFZ playback:** new `NAudio.Sampler.SfzSampler` plays an SFZ instrument as an `ISampleProvider` (`SfzSampler.FromFile(path)` or from a parsed `SfzInstrument` + `ISfzSampleLoader`), through the same voice engine as `SoundFontSampler`. SoundFont and SFZ now share a `SamplerEngine` base (voice pool, stealing, choke, sustain, channel state, reverb/chorus sends) and a format-neutral region the voice plays; SFZ samples load via `FileSfzSampleLoader` (WAV, mono-downmixed). Tier-1 opcode coverage — see `Docs/Architecture/SamplerDesign.md` §11.1 for the documented limits
 * **SFZ opcode semantics:** `NAudio.Sfz.SfzMappedRegion` interprets a parsed region's Tier-1 opcodes into typed, engine-ready values (key/velocity ranges with note-name parsing via `SfzNoteName`, `pitch_keycenter`/`tune`/`transpose`, `volume`/`pan`, the `ampeg_*` amplitude envelope, `cutoff`/`resonance`/`fil_type`, loop mode and offsets, `trigger`, `group`/`off_by`, `amp_veltrack`, `polyphony`); `SfzInstrument.MapRegions()` maps the whole instrument applying its note/octave offsets
 * **SFZ parser:** new `NAudio.Sfz.SfzParser` reads `.sfz` instrument files — `//` and `/* */` comments, the `#define`/`$variable` preprocessor, `#include` (pluggable `ISfzIncludeResolver`), the section grammar (sample paths with spaces, multiple opcodes per line) — and flattens the `<global>`/`<master>`/`<group>`/`<region>` hierarchy into `SfzRegion`s with merged opcodes, typed accessors and `default_path`-resolved sample paths. Text/structure layer; opcode semantics and sample loading follow (see `Docs/Architecture/SamplerDesign.md`)
 * **Sampler effect sends:** `SoundFontSampler` now routes each voice's SF2 `reverbEffectsSend`/`chorusEffectsSend` (and the CC91/CC93 default modulators) through shared `Reverb` (`ReverbEffect`) and `Chorus` (`ChorusEffect`) buses, mixing the wet return back into the output; both effects are exposed for tweaking or bypass. New generic `NAudio.Effects.SendBus` provides the reusable aux send/return plumbing
 * **DSP:** `BiQuadFilter.UpdateLowPassFilter` retunes a running filter without clearing its state, so a filter can be modulated per block/sample without the click that `SetLowPassFilter` causes by resetting state
 * **WASAPI:** new high-level `WasapiPlayer` and `WasapiRecorder` classes, built via `WasapiPlayerBuilder` / `WasapiRecorderBuilder`. Adds `IAudioClient3` low-latency support, MMCSS thread priority, `IAsyncDisposable`, zero-copy buffer access, and process-specific loopback via `WasapiRecorderBuilder.WithProcessLoopback()`
 * **ASIO:** new `AsioDevice` class replacing `AsioOut` as the primary ASIO interface. Adds explicit `InitPlayback` / `InitRecording` / `InitDuplex` modes, non-contiguous channel selection, per-channel `Span<float>` callbacks, `Reinitialize()` for driver-reset recovery, and per-buffer timing fields (`SamplePosition`, `SystemTimeNanoseconds`, `Speed`, SMPTE `TimeCode`)
 * **Sequencing:** new `NAudio.Sequencing` namespace in `NAudio.Core` with portable primitives for scheduling musical events — `ITempoMap` (with `LiveTempoMap` and `StaticTempoMap` implementations, plus `NextChangeAfter` for in-block tempo-split decisions), `TimeSignatureMap`, `Transport`, `EventTimeline<T>`, `SwingTransform`, the stateless `EventBufferQuery` per-buffer dispatcher, a `SequencedSampleProvider<T>` audio bridge that dispatches events with sample-accurate offsets, and a `MusicalTime.RescaleFromPpq` helper for the MIDI-file ingestion boundary. See `Docs/Architecture/Sequencing.md`
 * **ASIO events:** `LatenciesChanged` and `ResyncOccurred` surfaced separately; buffer-size changes routed through `DriverResetRequest`
 * **Media Foundation:** `MediaFoundationEncoder.EncodeToFlac` for lossless FLAC output. The FLAC/ALAC selector now falls back correctly on rate + channels
 * **WinForms:** `WaveOutWindow` and `WaveInWindow` available as window-callback variants of the modernised event-driven `WaveOut` / `WaveIn`
 * **DSP:** new `FftProcessor` with real-input specialisation and precomputed windowing
 * **WAV chunks:** new `IWaveChunkInterpreter<T>` extension point, with built-in interpreters for cue lists, BWF `bext` (v1 and v2), and LIST/INFO metadata. RF64 promotion is now an explicit `WaveFileWriterOption`
 * **`Span<T>` overloads:** added on `BiQuadFilter.Transform`, `ALawDecoder.Decode`, `MuLawDecoder.Decode`, and `IMp3FrameDecompressor.DecompressFrame` (default interface method preserves backward compatibility with `NLayer` and other third-party decoders)
 * **MIDI:** new `WinRTMidiIn` / `WinRTMidiOut` classes in `NAudio.Wasapi` backed by `Windows.Devices.Midi`, with `MidiMessageConverter` for interop with the WinRT MIDI types. New `IMidiInput` / `IMidiOutput` interfaces (with a `Send(MidiEvent)` extension) let callers write backend-agnostic code; legacy `MidiIn` / `MidiOut` also implement them
 * **MIDI:** `MidiFile` now reads RIFF-RMID (`.rmi`) files by unwrapping the RIFF container and parsing the embedded standard MIDI file (#1236)
 * **ALSA (Linux):** new `NAudio.Alsa` package — `AlsaOut` (`IWavePlayer`) and `AlsaIn` (`IWaveIn`) backed by `libasound`, plus `AlsaDeviceEnumerator`. Linux-only (`[SupportedOSPlatform("linux")]`, AOT-compatible `[LibraryImport]`); reference it explicitly, it is not part of the `NAudio` meta-package (#1182)
 * **Effects:** new cross-platform `NAudio.Effects` framework — `IAudioEffect`, an `AudioEffect` base with click-free bypass and dry/wet mix, `EffectSampleProvider`, and `EffectChain`. The chain is editable while it plays (`Add`/`Insert`/`RemoveAt`/`Move` publish a new chain atomically on the next block without resetting the other effects; `Read` stays lock-free) and exposes `Reset()` to clear all effect state (delay lines, filter history, reverb tails) for reuse after a seek
 * **Effects:** an optional `IParameterized` / `EffectParameter` model lets effects expose their controls (continuous/toggle/choice/meter) as a uniform list for generic UIs, presets and automation without changing `IAudioEffect`; live edits are marshalled to the audio thread by a lock-free `ParameterDispatchQueue` / `IParameterDispatch` so parameter writes never race the realtime callback
 * **Effects — EQ & filtering:** a per-channel multi-band `Equalizer` (peaking/shelf/pass/notch/band-pass/all-pass, click-free retune), a 10/31-band `GraphicEqualizer`, `MonoMakerEffect` (bass-mono) and `DcBlockerEffect`
 * **Effects — level & stereo:** `GainEffect`, `PanEffect` and `StereoWidthEffect`
 * **Effects — dynamics:** `CompressorEffect` (soft knee, peak/RMS detector, channel-linked), `LimiterEffect` (brick-wall with look-ahead and optional true-peak/inter-sample detection), `GateEffect` (gate/downward-expander with hysteresis and hold), `TransientShaperEffect` (dual-envelope attack/sustain shaping), split-band `DeEsserEffect`, and `MultibandCompressorEffect` (configurable LR4 bands, per-band threshold/ratio/attack/release/make-up); all expose live `GainReductionDb` for metering
 * **Effects — saturation & lo-fi:** `SaturationEffect` (tanh/cubic/arctan/hard-clip wave-shaper with drive, output trim and optional 2×/4× oversampling) and `BitCrusherEffect` (bit-depth plus target-sample-rate reduction, e.g. 22.05/16/8 kHz, with an optional smoothing low-pass)
 * **Effects — delay & modulation:** `DelayEffect` (tempo-syncable with read-only `EffectiveDelayMs`, feedback damping, ping-pong), `ChorusEffect`, `FlangerEffect`, `PhaserEffect` and `TremoloEffect` (with auto-pan)
 * **Effects — reverb:** `ConvolutionReverbEffect` (partitioned FFT convolution, mono or per-channel IR, reports latency, replacing the removed `ImpulseResponseConvolution`), `ReverbEffect` (lightweight Freeverb-style Schroeder–Moorer) and `FdnReverbEffect` (modulated feedback-delay network with RT60, size, damping and width)
 * **Effects — pitch:** `PitchShiftEffect` — framework-integrated per-channel phase-vocoder pitch shifting with semitone control and FFT latency reporting
 * **Effects — voice comms:** `AutomaticGainControlEffect` (VAD-gated leveller), `NoiseSuppressionEffect` (STFT spectral suppression) and `ComfortNoiseEffect`
 * **DSP:** new reusable building blocks underpinning the effects suite — `EnvelopeFollower`, `ParameterSmoother`, `DelayLine`, `CrossfadingBiQuadFilter`, `Lfo` (with `NoteDivision`/`TempoTime` tempo helpers), `Oversampler`, `LinkwitzRileyCrossover`, `PartitionedConvolver`, `VoiceActivityDetector`, plus `BiQuadFilter.ResetState()`
 * **Docs:** added an Audio Effects guide (`Docs/AudioEffects.md`) covering the suite, chains, dry/wet & bypass, the parameter model and writing your own effect
 * **Docs:** added `WasapiPlayer` and `WasapiRecorder` tutorials; the legacy `WasapiOut` and `WasapiLoopbackCapture` docs now point to them
 * **Core:** `NAudio.Utils.HResult` gained constants for common COM/storage HRESULTs plus an `IsError` helper (#1288)
 * **Sample providers:** new `ChannelMixerSampleProvider` remixes a source's channels through an arbitrary mixing matrix (downmix, upmix, weighted routing), with ready-made matrices in `ChannelMixMatrix` (mono↔stereo, stereo→5.1, etc.). Thanks to @antiduh (#982)

#### Demo apps and Test Harnesses

 * **WPF demo:** new **Convolution Reverb** module — offline test bench for `ConvolutionReverbEffect`: pick an input file and a folder of impulse responses, render single-IR or batch-all, with IR auto-resample to the input rate and peak-normalisation to -3 dBFS, latency-compensated output, full tail flush, and per-render Nx-real-time + tail-length reporting. Renders land in a temp folder with Play/Delete/Open-folder commands
 * **WPF demo:** new **Realtime Effects** module — full-duplex `AsioDevice` monitor (driver + mono/stereo input) with feedback safety (starts muted, headphone warning, runaway auto-mute), an add/remove/reorder effect-chain editor with auto-generated parameter panels (knobs/toggles/choices/meters + Bypass/Mix) driven by `IParameterized`, and WAV-file playback / offline render through the chain. Live parameter edits are marshalled to the audio thread via `ParameterDispatchQueue`, newly added effects are pre-warmed off the audio thread, and ASIO monitoring and file playback/render are mutually exclusive (the chain is shared). An input-channel offset selector allows mono/stereo capture from any base channel (e.g. a guitar on input 2, or a stereo pair on 5+6). Three demo-only effects (`SevenBandEqEffect`, `FilterEffect`, `ThreeBandCompressorEffect`) composed from the toolkit's `Equalizer` / `CrossfadingBiQuadFilter` / `MultibandCompressorEffect` primitives are wired into the harness — they smoke-test the underlying DSP and show how to build fixed-parameter effects on top of the dynamic-band primitives
 * **NAudioConsoleTest:** new CLI test harness for driving various NAudio features without the need for GUI. Includes `run-batch` for JSON-driven test plans and `diagnose` for capturing a structured host audio snapshot (OS, ASIO drivers, WASAPI/WinMM/DirectSound devices, NAudio assembly versions).
 * **WPF demos:** spectrum analyser rewritten with corrected dB formula (20·log₁₀), log-frequency mapping, real-input full-scale calibration, bars instead of polylines, peak-decay markers, and per-band smoothing. New `LiveWaveformControl` with configurable render styles, vertical scaling, and fill-between rendering
 * **WAV recording demo:** added loopback support and a multi-API device combo with provenance embedding
 * **Drum machine demo:** rebuilt on top of the new `NAudio.Sequencing` primitives. Adds a swing knob, a "Render to WAV" command exercising the offline sample-driven path, and a hi-hat choke group (closed and open hats cut each other with a 10 ms fade-out)
 * **MIDI In demo:** Refresh button for hot-plugged devices, device combos disabled while in use, test MIDI Out plays on channel 1 (was 2), Filter Auto-Sensing on by default, stopping test output now sends note-off so notes don't hang, and cleaner panel disposal
 * **MfStressTest:** Reliability tests for the new Media Foundation interop implementation in NAudio 3.
 * Replaced vendored NSpeex (deprecated) with Opus (Concentus) in the network chat demo; added round-trip unit tests
 
#### Performance

 * Vectorised mix-add and volume kernels via `System.Numerics.Tensors` — significantly faster on AVX2 hardware for typical buffer sizes
 * Eliminated per-`Read` allocations in `SmbPitchShiftingSampleProvider`
 * `WaveStream.Read(Span<byte>)` overridden directly on every concrete reader (no intermediate byte-array copy)
 * `WasapiCapture` capture path is now zero-copy via the native WASAPI buffer span
 * `BiQuadFilter` state and coefficient fields hoisted to locals in batch loops for register retention
 * `Mp3FileReader` now builds its table-of-contents lazily on first seek instead of eagerly during construction; the `Position` setter no longer blocks; rapid scrub seeks debounce and silence output
 * Eliminated per-`Read` allocations in `ResamplerDmoStream` and `DmoMp3FrameDecompressor` (cached input buffer and output-buffer array) (#971)

#### Reliability and bug fixes

 * `AudioSessionControl`: now supports multiple registered event clients. `RegisterEventClient` no longer leaks a prior registration, and `UnRegisterEventClient` now honours its `eventClient` argument instead of unregistering whichever handler happened to be stored (#1263)
 * `CueListInterpreter`: fixed returning null for WAV files with cue points but no labels (e.g. unnamed Wavosaur markers); cues are now returned with empty labels (#549)
 * `WaveViewer`: fixed waveform rendering upside-down (#801, #818)
 * `WaveViewer`: now renders correctly for any source format — the legacy renderer hard-coded a 16-bit PCM byte walk, so feeding it an `AudioFileReader` (or any non-16-bit `WaveStream`) produced a garbled waveform. Rendering now goes through `ToSampleProvider()` and operates on floats (#564)
 * `AcmInterop`: serialised all `msacm32` P/Invokes process-wide via a reentrant lock — fixes process-killing access violations under concurrent ACM access
 * `AcmStream`: fixed double-close in finalizer by zeroing the handle field before close
 * `MediaFoundationReader`: informational source-reader flags (`STREAMTICK`, `NEWSTREAM`, `NativeMediaTypeChanged`, `AllEffectsRemoved`) are now non-fatal instead of aborting reads
 * `MediaFoundationReader`: cleanup `finally` block on `Read` no longer leaks COM objects when `Unlock` fails — the hresult is captured and thrown only after both the buffer and the sample have been freed.
 * `MediaFoundationReader.Reposition`: fixed using a stale field instead of the parameter (seeks would default to stream start)
 * `MediaFoundationEncoder`: unselected `MediaType` instances are now disposed to prevent finalizer-thread COM ref leaks
 * `MediaFoundationEncoder`: In the `ConvertOneBuffer` method, there was a small possibility that if the sample creation was failed, the previously allocated buffer COM object would have been leaked.
 * `StreamMediaFoundationReader` and stream-based `MediaFoundationEncoder` encoding now use a direct managed `IMFByteStream` wrapper instead of the `IStream`→`IMFByteStream` shim, improving reliability of reading and encoding audio through .NET streams (#1288)
 * `Mp3FileReader`: fixed false sample-rate-change errors near end of file
 * `WaveFormat.Serialize`: PCM formats now write the canonical 16-byte `fmt ` chunk (no `cbSize` field) instead of 18 bytes, matching the `PCMWAVEFORMAT` layout (#934, #1098)
 * MP3 frame parsing: more robust against false frame detections from album art and trailing metadata
 * `MidiFile`: preserved running-status across meta events (fixes "Read too far" errors when meta events interrupt running-status sequences)
 * `WaveStream.CurrentTime` setter: now lands on a block boundary, preventing garbage audio on seek in custom readers
 * `BlockAlignReductionStream.Position` setter: now validates the incoming value instead of the stale current position, so a block-aligned seek after an arbitrary-length read no longer wrongly throws "Position must be block aligned" (#368)
 * `IconExtractor.Extract`: now guards against null icon handles from `ExtractIconEx`
 * `DirectSoundOut.InitializeDirectSound`: wrapped notification setup in try/finally to prevent COM ref leak on `SetNotificationPositions` failure
 * ASIO: implemented missing `Asio64Bit` conversions (Int24LSB and Float32LSB output sample types)
 * ASIO: fixed byte-order bug in `AsioDriver.GetSamplePosition` for `Asio64Bit` reassembly
 * `WdlResampler`: backported three upstream Cockos WDL bug fixes (latency calculation, `ResampleOut` clamping, Blackman-Harris window correction)
 * `MediaBufferLease`: hardened against out-of-order disposal
 * Added finalizers to DMO `MediaBuffer` and the `Mf*` wrappers that hold (RCW, IntPtr) pairs to prevent COM ref leaks
 * `WaveFileChunkReader`: fixed `ArgumentException` parsing WAV files whose odd-length chunks are followed by non-UTF-8 bytes — the word-alignment pad-byte check no longer decodes via `BinaryReader.PeekChar()`, and is now guarded against end-of-stream (#959)
 * Clarified `BiQuadFilter` `q` parameter docs (#1264)
 * Removed dead `naudio.codeplex.com` links from README, MixDiff Help menu, and source comments (CodePlex was shut down by Microsoft in 2017) (#985)
 * `AudioClient.Dispose`: made idempotent and safe against concurrent/re-entrant disposal — fixes an intermittent `NullReferenceException` from the COM interop layer when a WASAPI capture or playback wrapper is disposed more than once (#1183)
 * `WaveFileReader` / `AiffFileReader`: malformed headers that declared `BlockAlign=0` now throw `InvalidDataException` from the constructor instead of `DivideByZeroException` from the `Position` setter (#1254)
 * `AiffFileReader.Read`: truncated `SSND` chunks no longer trigger `IndexOutOfRangeException` in the byte-swap loop — the read count is rounded down to a whole block (#1254)
 * `AudioEndpointVolume.OnVolumeNotification`: fixed per-channel volume notification returning channel 0's volume for every channel — the read pointer was not advanced per channel (#351)
 * `MmException`: error messages now append a human-readable description of the `MmResult` via `waveOutGetErrorText`, e.g. `NoDriver calling waveOutSetVolume: No device driver is present` (#1192)
 * `Id3v2Tag.ReadTag`: no longer throws and catches a `FormatException` for MP3 streams without an ID3v2 tag — the header check now returns `null` directly (#265)
 * `WaveFileReader`: fixed `ArgumentException` reading WAV files whose `fmt` chunk declares more extra (`cbSize`) bytes than the fixed 100-byte buffer holds — the surplus is now discarded instead of throwing (#482)
 * `MediaFoundationTransform`: cleanup `finally` blocks no longer leak COM objects when `Unlock`/`RemoveAllBuffers` fails — hresults are captured and thrown only after every buffer/sample has been released (#1293)
 * `ResamplerDmoStream`: fixed infinite loop on `Read` after setting `Position`, and the loss of the resampler kernel's tail samples (~32 at the default quality of 30) when the input reaches end-of-stream. The DMO is now drained via `ProcessOutput` after `Discontinuity` — on seek the drained bytes are discarded so playback resumes from the new position, on EOS they're returned to the caller and subsequent reads return 0 cleanly (#607, #608)
 * Named the background threads created by `DirectSoundOut`, `WasapiOut`, `WasapiCapture`, `WasapiPlayer`, and `WasapiRecorder` so they show meaningful names in debuggers and profilers (#557)

#### Modernisation (Native AOT, source-generated COM)

 * `NAudio.Core`, `NAudio.Midi`, and `NAudio.Wasapi` are now `IsAotCompatible=true`. AOT compatibility is enforced at build-time by `NAudioAotSmokeTest`, which fails CI on any new trim or AOT analyzer warning
 * Most COM interop migrated from `[ComImport]` to `[GeneratedComInterface]` / `ComWrappers`. Affected interfaces include the WASAPI / Core Audio activation chain (`IActivateAudioInterfaceCompletionHandler`, `IMMNotificationClient`, `IAudioSessionNotification`, `IAudioSessionEvents`, `IAudioEndpointVolumeCallback`, `IAgileObject`, `IPropertyStore`), the Media Foundation cascade, the DMO interfaces, DirectSound, and the `ComStream` CCW (now source-generated `IStream`)
 * `Connector.ConnectTo`: fixed a source-generated COM leftover — it now projects the target connector via `ComWrappers` instead of `Marshal.GetComInterfaceForObject`, which is unsupported for `[GeneratedComInterface]` types and would fail at runtime (SYSLIB1099) (#1311)
 * DirectSound P/Invokes migrated to `[LibraryImport]` with `[UnmanagedCallersOnly]` thunks; `BufferDescription` and `BufferCaps` converted from class to struct
 * `AcmDriver` ported from legacy `NativeMethods` to `NativeLibrary`
 * Most `MediaFoundationInterop` blittable P/Invokes migrated to `[LibraryImport]`

#### Packaging and dependencies

 * Each NAudio package now ships its own README in the NuGet payload
 * Each NAudio package now embeds an SPDX 2.2 Software Bill of Materials (SBOM) under `/_manifest/spdx_2.2/` in its `.nupkg`, generated at pack time via `Microsoft.Sbom.Targets`
 * Test project migrated from VSTest to `Microsoft.Testing.Platform`
 * `NAudioTests` split into `NAudio.Core.Tests` (cross-platform, `net10.0`) and `NAudio.Windows.Tests` (Windows-only, `net10.0-windows`) — eliminates the dual-TFM double-run on Windows CI and lets non-Windows devs run just the cross-platform suite
 * `NAudio.Alsa.Tests` and `NAudio.SoundFile.Tests` now ignore MTP exit codes 8/9 so `dotnet test` succeeds on machines where the suite legitimately runs zero tests (ALSA off-Linux) or self-skips (libsndfile absent)
 * Migrated to the modern `.slnx` solution format
 * Renamed `license.txt` to `LICENSE` for GitHub license detection; refreshed copyright year to 2008–2026
 * Added per-package `<Description>` metadata to every shipping NAudio NuGet package so each clearly identifies itself as part of the NAudio family
 * Added a DocFX documentation site (tutorials + API reference) published to GitHub Pages, built automatically from `Docs/` and the source XML comments
 * Fixed the published API reference dropping the cross-platform namespaces (`NAudio.Effects`, `NAudio.Dsp`, `NAudio.Codecs`, `NAudio.SoundFont`, etc.) from the navigation — DocFX's two metadata blocks both wrote to the same destination, so the second overwrote the first's table of contents. The projects are now documented from a single metadata block

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
