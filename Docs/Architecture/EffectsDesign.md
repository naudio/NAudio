# NAudio 3 — Audio effects: design notes

User-facing documentation (how to apply effects, build chains, write your own,
the full catalogue) lives in [`Docs/AudioEffects.md`](../AudioEffects.md). This
document records the **architectural decisions** behind the `NAudio.Effects`
framework and the **NAudio 2 → 3 delta** — what got built, what got retired,
and what is deliberately deferred.

## 1. Goal

A high-quality, pure-C#, cross-platform effects suite as a first-class part of
NAudio 3, serving two audiences with one toolkit:

- **Music production** — EQ, dynamics, saturation, delay, modulation, reverb,
  stereo tools, pitch.
- **Voice comms** — DC/rumble removal, gating, noise suppression, AGC, VAD.

The effects also have to be **building blocks for the future NAudio sampler and
synthesiser**, so the DSP kernels must be usable standalone (process your own
buffer) — not only as pull-model stream wrappers.

## 2. Key design decisions

### 2.1 Two layers, cleanly separated

- **Layer 1 — DSP kernels.** Plain classes implementing `IAudioEffect`,
  configured with a `WaveFormat`, processing interleaved `Span<float>` in
  place. No source reference. This is the layer the future synth/sampler talks
  to.
- **Layer 2 — streaming adapter.** A single generic `EffectSampleProvider :
  ISampleProvider` hosts an `IAudioEffect`; an `EffectChain` runs an ordered
  list. The kernels stay reusable; existing `ISampleProvider` pipelines keep
  working unchanged.

### 2.2 The `IAudioEffect` contract

```csharp
public interface IAudioEffect
{
    void Configure(WaveFormat format);   // sample rate + channel count
    void Process(Span<float> buffer);    // in-place, interleaved
    void Reset();                        // clear delay lines / state
    int LatencySamples { get; }          // 0 for most; non-zero for look-ahead
}                                        // limiter, convolution reverb, etc.
```

- **`Configure(WaveFormat)`** rather than baking rate/channels into the
  constructor — effects are reusable across format changes and inside chains.
- **Channel-agnostic by design.** Never hard-wired to stereo (the exact
  mistake in the deleted ChunkWare code). Mono, stereo, surround all work.
- **Interleaved `Span<float>`** matches `ISampleProvider`. Effects deinterleave
  internally only if they must (e.g. spectral processing).
- **`Reset()`** is mandatory so any effect can be reused after a seek.
- **`LatencySamples`** is reported so a host can do delay compensation.

A thin abstract `AudioEffect` base wraps `Process` to provide **click-free
`Bypass` and dry/wet `Mix` for free** — concrete effects only override
`ProcessBlock`.

### 2.3 Live parameter edits — block-boundary dispatch

A live effect is mutated on exactly one thread: the audio thread, at a block
boundary.

- An optional `IParameterized` companion interface exposes
  `IReadOnlyList<EffectParameter>` (Continuous / Toggle / Choice / read-only
  Meter). It is **additive** — `IAudioEffect` itself is unchanged, and the
  parameter list is built from the effect's normal typed properties via a
  small helper.
- When a dispatch sink is attached (the realtime engine, eventually a VST3
  host), `EffectParameter.Value` setters **post** to a lock-free
  single-producer/single-consumer `ParameterDispatchQueue` instead of running
  inline. With no sink (offline render, chain hosted on a single thread, unit
  tests) edits apply inline exactly as before.
- The realtime engine `Drain`s the queue once at the top of each block, before
  DSP. Coefficient recomputation, delay-buffer resizes and the
  `CrossfadingBiQuadFilter` swap therefore all run where there is no
  concurrent reader.
- The queue is bounded; the next edit of a parameter supersedes a pending one
  (slider drags emit far fewer events than blocks per second, so it never
  backs up in practice).
- `Bypass`/`Mix` stay direct writes — they are single `bool`/`float` fields
  (atomic per ECMA, no torn read) that the base class already ramps click-free.

Same model later drives a VST3-host generic UI, presets, serialisation, and
automation.

### 2.4 Click-free filter retune — the A/B crossfade

PR #1259 made `BiQuadFilter.SetCoefficients` *reset filter state on every
change* — a deliberate NaN-recovery tradeoff. That means a running filter
cannot be retuned click-free in place, and smoothing the *parameter* doesn't
help (the discontinuity is in the state reset).

The framework therefore provides `CrossfadingBiQuadFilter`: two filter
instances, retune the idle one, equal-power crossfade over a few milliseconds.
Every filter-based / automatable effect (`Equalizer`, `GraphicEqualizer`, the
demo `FilterEffect`, future dynamic EQ / auto-wah / modulated filters) uses
this.

### 2.5 Mutable chain during playback

`EffectChain.Add` / `Insert` / `RemoveAt` / `Move` publish a new chain
atomically on the next block (copy-on-write — `Read` stays lock-free). Adding
or reordering effects mid-stream does **not** reset the other effects in the
chain. The realtime engine uses the same swap-by-reference pattern for its
ASIO callback.

### 2.6 Smaller, locked-in decisions

- **Home:** `NAudio.Effects` namespace, inside `NAudio.Core` (cross-platform,
  AOT-safe). `NAudio.Dsp` stays the low-level-primitive namespace.
- **Float throughout** (no per-sample `double`); allocation-free steady state;
  no reflection (`NAudio.Core` is `IsAotCompatible`). `TensorPrimitives` is
  used where the algorithm allows (gain, mix, waveshaping) — matching the
  existing `VolumeSampleProvider` precedent.
- **Denormals:** no portable FTZ/DAZ in .NET. Mitigated centrally — tiny DC
  offset in feedback paths and periodic flush — baked into the delay-line and
  reverb-tail helpers so every effect inherits the protection. A
  `DenormalGuard` test asserts no subnormal floats leak from decaying feedback
  paths.
- **Not in scope:** no automation framework, no plugin graph, no per-sample
  method on the *interface* (the synth uses concrete kernel types directly).

## 3. What got implemented

The framework (`IAudioEffect`, `AudioEffect`, `EffectSampleProvider`,
`EffectChain`), the optional parameter model (`IParameterized`,
`EffectParameter`, `ParameterDispatchQueue`), and ~27 effects across EQ /
filtering, level / pan / stereo, dynamics, saturation / lo-fi, delay /
modulation, reverb, pitch, and voice-comms. New DSP primitives underpin them:
`EnvelopeFollower`, `ParameterSmoother`, `DelayLine`, `CrossfadingBiQuadFilter`,
`Lfo`, `Oversampler`, `LinkwitzRileyCrossover`, `PartitionedConvolver`,
`VoiceActivityDetector`, plus `BiQuadFilter.ResetState()`.

The full catalogue, usage and parameter list per effect are in
[`Docs/AudioEffects.md`](../AudioEffects.md); the [`RELEASE_NOTES.md`](../../RELEASE_NOTES.md)
"New features" section is the canonical changelog.

The WPF **Realtime Effects** demo is the subjective-quality evaluation tool
and the seed for a future VST3 host: ASIO duplex monitoring + file
playback/render through an editable chain, with auto-generated parameter
panels driven by `IParameterized`. A separate **Convolution Reverb** demo
handles the IR-as-input workflow that doesn't fit the generic panel.

## 4. What got retired from NAudio 2

| NAudio 2 type | Disposition | Replacement |
|---|---|---|
| `SimpleCompressorEffect` (was `SimpleCompressorStream`) | **Deleted** (public, breaking) | `CompressorEffect` (soft knee, peak/RMS, channel-linked) |
| `SimpleCompressor`, `SimpleGate` | **Deleted** (internal) | `CompressorEffect`, `GateEffect` |
| `EnvelopeDetector`, `AttRelEnvelope` | **Deleted** (internal) | `EnvelopeFollower` (clean `public float` rewrite) |
| `ImpulseResponseConvolution` | **Deleted** (public, breaking) | `ConvolutionReverbEffect` (partitioned FFT) |
| `NAudio.Extras.Equalizer`, `EqualizerBand` | **Deleted, replaced** (public, breaking) | `NAudio.Effects.Equalizer` / `EqualizerBand` in `NAudio.Core` — per-channel, click-free retune, shelves/pass/notch/band-pass/all-pass added. Band API: `Bandwidth`/`Gain` → `Q`/`GainDb` (or `ShelfSlope`); equaliser is now an `IAudioEffect` (wrap with `EffectSampleProvider` instead of passing a source to the constructor). |
| DMO effects (`DmoCompressor`, `DmoEcho`, `DmoChorus`, …) | **Kept as a Windows convenience** | Out of scope for the cross-platform suite by construction — they are COM/DirectSound objects. The managed suite is a parallel, cross-platform offering, not a reimplementation obligation. |

Kept and built on (no behavioural change required for the effects work):
`BiQuadFilter` (hardened by #1259), `FastFourierTransform` / `FftProcessor`,
`WdlResampler`, `SmbPitchShifter`, `EnvelopeGenerator`,
`VolumeSampleProvider` / `FadeInOutSampleProvider` / `MeteringSampleProvider`.

## 5. Port vs build, and licence policy

**Default: build.** ~70% of the suite (EQ, dynamics, saturation, delay,
modulation, stereo tools, DC blocker, gate) is a few dozen to a few hundred
lines of well-understood textbook DSP. Hand-writing idiomatic, allocation-free,
AOT-safe C# is *less* work than porting, adapting, and re-validating C++, and
the result doubles cleanly as a synth/sampler building block with no
third-party attribution sprawl.

**Port selectively, only where the algorithm is genuinely hard** and a strong
permissively-licensed reference exists. In NAudio 3 this means:
algorithmic reverb (Freeverb / Dattorro as *reference*, not transliteration),
partitioned-convolution reverb (built on our existing `FftProcessor`), and the
voice-comms DSP. Even then, prefer "port as algorithm, reimplement
idiomatically" over line-by-line transliteration.

**Licence policy.** NAudio is MIT. Only MIT / BSD / Zlib / public-domain
sources may be ingested, with attribution preserved in-file (as
`SmbPitchShifter` / `WdlResampler` already do) and an entry in
[`THIRD-PARTY-NOTICES.txt`](../../THIRD-PARTY-NOTICES.txt). GPL / LGPL /
proprietary code (Rubber Band, SoundTouch, Surge, Vital, TAL, JUCE DSP) is
**out** and must never be copied from.

## 6. Deferred / open work

- **Acoustic echo cancellation.** Its own milestone — NLMS/MDF adaptive
  filter + double-talk detection + nonlinear residual suppression is the
  single biggest item in the voice-comms suite. Target a Speex-MDF-class port
  first; WebRTC AEC3 only if sustained demand and maintenance appetite
  materialise.
- **RNNoise ML noise suppression.** Quality tier above the
  `NoiseSuppressionEffect` STFT spectral suppressor. ~85 KB model, needs a
  small in-process GRU/dense inference path (no external ML runtime).
- **Signalsmith pitch / time mode.** Quality tier above the current
  `SmbPitchShifter` / `PitchShiftEffect`.
- **Seekable effects — position-preserving decorators.** Resolves the
  long-standing "positionable `WaveStream` vs non-positional `ISampleProvider`"
  pain. Leading direction: a generic
  `PositionPreservingSampleProvider : WaveStream, ISampleProvider` returned
  by `waveStream.AddEffect(...)` — forwards `Position`/`Length` to the
  seekable source while `Read` pulls through the effect chain, so the object
  the player reads from *is* the object you seek. Single-source transforms
  only (`Skip`/`Take`/channel remap/resample); fan-in stays plain
  `ISampleProvider`. Known wrinkles (byte↔sample mapping, latency offset,
  tails past EOF, seek-resets-state) are intrinsic to *any* seekable-effects
  solution. Requires an `EffectChain.Reset()` and a small ownership/disposal
  convention.

## 7. Non-goals for NAudio 3

- **De-reverberation** (WPE-class processing) — research-grade; out of scope.
- **Plugin / graph / automation framework** — keep `IAudioEffect` small. A
  later concern, separable.
- **DMO migration to cross-platform** — the DMO effects stay as a Windows
  convenience; the cross-platform suite is parallel, not a reimplementation
  obligation.
