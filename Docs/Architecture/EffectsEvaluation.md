# NAudio 3 — Audio effects suite: evaluation & strategy

**Status:** Implemented on branch `naudio3-effects`. Phases 0–4 of the music suite and the
Phase 3 voice-comms core are done — the `NAudio.Effects` framework, ~27 effects, and the
`NAudio.Dsp` primitives, with unit tests. Remaining roadmap: Phase 5 (AEC — its own
milestone, §6) and the seekable-effects work (§3.8). Deliberately deferred quality tiers:
RNNoise ML noise suppression and a Signalsmith pitch/time mode. This document records the
*design*; per-wave landing notes and running test counts have been dropped — see the git
history and `RELEASE_NOTES.md` for the changelog.

**Goal:** Ship a high-quality, pure-C#, cross-platform effects suite as a first-class part of
NAudio 3. Two audiences:

- **Music production** — EQ, dynamics (compressor / limiter / gate / expander), saturation,
  delay, modulation (chorus / flanger / phaser / tremolo), reverb, stereo tools, time/pitch.
- **Voice comms** — DC/rumble removal, noise gate, noise suppression, automatic gain control,
  acoustic echo cancellation, voice activity detection.

The effects must also be **building blocks for the future NAudio sampler and synthesiser**, so
the DSP kernels have to be usable standalone (process your own buffer), not only as
pull-model stream wrappers.

The headline question the maintainer asked: **port permissively-licensed effects, or build
from scratch?** Short answer: **mostly build, selectively port.** The reasoning, the
per-effect breakdown, and a phased roadmap follow.

---

## 1. Executive summary

1. **Build the framework first.** The single highest-leverage decision is not any individual
   effect — it is a clean, consistent effect abstraction (block-based, `Span<float>`,
   channel-aware, `Reset()`, latency reporting, click-free parameter changes) sitting on a
   reusable per-sample DSP kernel layer. Today's effects are ad-hoc `ISampleProvider`
   wrappers over inconsistent primitives (some `internal`, some `double`-per-sample, some
   hard-wired to stereo). Fix this before adding anything.

2. **Build the small effects from scratch.** ~70% of the requested suite (EQ, dynamics,
   saturation, delay, modulation, stereo tools, DC blocker, gate) is a few dozen to a few
   hundred lines of well-understood textbook DSP. Hand-writing idiomatic, allocation-free,
   AOT-safe C# is *less* work than porting, adapting, and re-validating C++, and it produces
   code that doubles cleanly as synth/sampler building blocks with no third-party attribution
   sprawl.

3. **Port selectively, only where the algorithm is genuinely hard** and a high-quality
   permissively-licensed reference exists: algorithmic reverb (Freeverb / Dattorro as
   *reference*, not transliteration), partitioned-convolution reverb (reuses our existing
   `FftProcessor`), and the voice-comms DSP (Speex DSP / RNNoise / WebRTC APM). Acoustic
   echo cancellation specifically is a large standalone project, not a "port an afternoon".

4. **Licensing is not a blocker for the realistic candidates.** NAudio is MIT. The strong
   reference implementations are MIT / BSD / public-domain (DaisySP, Airwindows, Signalsmith,
   Freeverb, STK, Speex DSP, RNNoise, WebRTC APM). GPL/LGPL/proprietary options
   (Rubber Band, SoundTouch, Surge, Vital, TAL, JUCE DSP) are **out** and we should never
   copy from them. The repo *already* ports-with-attribution (`BiQuadFilter`,
   `SmbPitchShifter`, `WdlResampler`, the ChunkWare dynamics) — we formalise that convention
   with a `NOTICE`/`THIRD-PARTY-NOTICES.txt` file.

5. **Most of what we already ship is keepable** (`BiQuadFilter` — now hardened by #1259,
   `FftProcessor` / `FastFourierTransform`, `WdlResampler`, `SmbPitchShifter`,
   `EnvelopeGenerator`). The weak ChunkWare dynamics
   (`SimpleCompressor`/`SimpleGate`/`EnvelopeDetector`/`AttRelEnvelope`) and the dead
   `ImpulseResponseConvolution` are **deleted, not salvaged** (confirmed) — the new
   dynamics and convolution code is written clean from scratch. The Windows-only DMO
   effects are explicitly *not* the answer for a cross-platform suite (they stay as a
   Windows convenience).

---

## 2. What we have today (honest inventory)

### 2.1 Cross-platform DSP — `NAudio.Core/Dsp/` and sample providers

| Component | Verdict | Notes |
|---|---|---|
| `BiQuadFilter` | **Keep** | RBJ Audio-EQ-Cookbook, correct, `public`, `float` state / `double` coeffs, `Span<float>` block overload. **PR #1259 already did the robustness work** I'd scoped into Phase 0: parameter validation (positive sample rate, `0 < f < Nyquist`, positive Q/slope) on the instance setters *and* static factories, plus a NaN/Inf-latch fix (`SetCoefficients` now zeroes `x1/x2/y1/y2`), with `BiQuadFilterValidationTests`. Residual, now low-priority: (a) 3 stale `// TODO: should we square root this value?` comments remain — unfounded, `A = 10^(dBgain/40)` is the correct cookbook form, delete them; (b) factory-surface asymmetry — only LP/HP/peaking have in-place `Set*`; notch/bandpass/shelf are static-only, so they can't be retuned in place. **New consequence:** see §3 — `SetCoefficients` now *resets state by design*, so click-free retuning must be solved at the effect layer (A/B crossfade), not inside the filter. |
| `FastFourierTransform` + `FftProcessor` | **Keep** | `FftProcessor` is modern, real-input-specialised, zero-alloc steady state, windowed. This is the engine for spectral effects, partitioned-convolution reverb, FFT noise suppression, and pitch/time work. |
| `WdlResampler` | **Keep** | Cockos WDL, used with permission, high quality. Pitch/time and SRC building block. |
| `SmbPitchShifter` / `SmbPitchShiftingSampleProvider` | **Keep** | Bernsee STFT phase-vocoder pitch-shift with built-in limiter. Usable now; quality ceiling below Signalsmith (see §5) but fine as the default. |
| `EnvelopeGenerator` / `AdsrSampleProvider` | **Keep, make synth-facing** | EarLevel ADSR. Directly reusable by the future synth. `AdsrSampleProvider` is mono-only — generalise when the synth work starts. |
| `EnvelopeDetector` / `AttRelEnvelope` | **Delete** (confirmed) | `internal`, `double`, zero consumers once the ChunkWare dynamics go. We do *not* keep/refactor it — Phase 0 writes a clean `public float` attack/release follower from scratch. |
| `SimpleCompressor` / `SimpleGate` | **Delete** (confirmed) | ChunkWare SimpleComp/SimpleGate 2006. Both `internal`; `SimpleGate` has **no public surface at all**, `SimpleCompressor` is reachable only via `SimpleCompressorEffect`. `double`, `Process(ref double in1, ref double in2)` hard-wired to stereo, no soft knee/RMS/lookahead. Not worth salvaging — deleted, replaced by the new from-scratch dynamics. Internal removals → no RELEASE_NOTES entry. |
| `SimpleCompressorEffect` (was `SimpleCompressorStream`) | **Delete** (confirmed) | The only **public** dynamics entry point; hard-codes odd defaults, `lock` per `Read`. No consumers in repo. **Public breaking removal → RELEASE_NOTES `#### Breaking changes` bullet.** |
| `ImpulseResponseConvolution` | **Delete** (confirmed) | **Public**, but a toy: O(n·m) time-domain mono convolution that destructively normalises and allocates. No consumers in repo. Deleted now; partitioned/overlap-add FFT convolution (on `FftProcessor`) arrives in Phase 2. **Public breaking removal → RELEASE_NOTES bullet.** |
| `VolumeSampleProvider`, `FadeInOutSampleProvider`, `MeteringSampleProvider` | **Keep** | Already idiomatic (`VolumeSampleProvider` uses `TensorPrimitives`). Good pattern templates. |

### 2.2 `NAudio.Extras`

| Component | Verdict | Notes |
|---|---|---|
| `Equalizer` / `EqualizerBand` | **Move to Core, feature-rebuild** | Multi-band peaking EQ over `BiQuadFilter`. **PR #1259 already fixed the correctness/safety defect** (out-of-Nyquist or non-positive bands are now skipped instead of throwing/destabilising — with a demo bugfix). The remaining work is *features*, not crash-safety: move into the Core effects namespace, add shelving bands, per-channel state, and **click-free retuning**. The click problem is now *sharper*, not gone — even reusing a filter, `SetPeakingEq` zeroes its state (by #1259 design), so click-free EQ requires the §3 A/B-crossfade pattern, not in-place coefficient mutation. |

### 2.3 Windows-only — `NAudio.Wasapi/Dmo/Effect/`

`DmoCompressor`, `DmoEcho`, `DmoChorus`, `DmoFlanger`, `DmoDistortion`, `DmoWavesReverb`,
`DmoI3DL2Reverb`, `DmoParamEq`, `DmoGargle`, hosted by `DmoEffectWaveProvider<T>`.

**Verdict: out of scope for the cross-platform suite, keep as a Windows convenience.** These
are COM/DirectSound objects — Windows-only by construction, exactly what the maintainer wants
to move *away* from for the out-of-the-box experience. They remain useful on Windows and need
no changes; they just are not the answer to "pure C#, cross-platform".

### 2.4 Framework gap

There is **no effect abstraction**. Effects are individual classes implementing
`ISampleProvider` (pull model: each wraps a source). Consequences:

- No uniform parameter model, no `Reset()`, no latency reporting (a lookahead limiter or
  partitioned-convolution reverb *must* report latency for delay compensation).
- The reusable DSP and the stream wrapper are conflated. A synth/sampler voice wants to
  *push* its own buffer through a compressor, not pull from an `ISampleProvider`.
- No click-free parameter changes — the `Equalizer` pop bug is the canonical symptom and
  will recur in every effect unless solved once, centrally.

---

## 3. The framework (decided)

### 3.1 Confirmed design decisions

The maintainer has confirmed these — they are locked for Phase 0:

- **Home:** new effects live in a `NAudio.Effects` namespace inside **`NAudio.Core`**
  (cross-platform, AOT-safe). `NAudio.Dsp` stays the low-level primitive namespace.
- **Abstraction:** ship both `IAudioEffect` *and* a thin abstract `AudioEffect` base that
  provides click-free `Bypass` and dry/wet `Mix` for free; effects override `ProcessBlock`.
- **Buffer layout:** interleaved `Span<float>` is the contract (consistent with
  `ISampleProvider`); effects deinterleave internally only if they must.
- **Config:** `Configure(WaveFormat)` (reusable across format changes, chain-friendly),
  not sample-rate/channels baked into constructors. Effects are **channel-agnostic by
  design** — never hard-wire stereo (the exact mistake in the deleted ChunkWare code).
- **Explicitly deferred:** no automation / plugin / graph framework; no per-sample method
  on the *interface* (the synth uses concrete kernel types directly).

### 3.2 Two layers, cleanly separated

**Layer 1 — DSP kernels (the synth/sampler building blocks).** Plain classes, no source
reference. Sample-rate and channel aware. In-place block processing on the caller's buffer,
plus a single-sample path for the per-voice synth case:

```csharp
public interface IAudioEffect
{
    void Configure(WaveFormat format);          // sample rate + channel count
    void Process(Span<float> buffer);           // in-place, interleaved
    void Reset();                               // clear delay lines / state
    int LatencySamples { get; }                 // for delay compensation (0 for most)
}
```

- Parameters are plain typed properties: a small set with good defaults up front, advanced
  properties available but not required. A shared one-pole `ParameterSmoother` makes
  *scalar* parameter changes (gain, mix, drive) click-free by construction.
- **Biquad/coefficient retuning needs more than a smoother.** PR #1259 made
  `BiQuadFilter.SetCoefficients` *reset filter state on every change* (a deliberate
  NaN-recovery tradeoff). So a running filter cannot be retuned click-free in place, and
  smoothing the *parameter* doesn't help — the discontinuity is in the state reset. The
  framework therefore provides an **A/B filter crossfade** helper: keep two filter
  instances, retune the idle one, equal-power crossfade over a few ms. Every
  filter-based/automatable effect (parametric EQ, auto-wah, dynamic EQ, modulated
  filters) uses this. This is the corrected "central fix" for the `Equalizer` click,
  replacing the parameter-smoothing assumption in the original draft.
- Allocation-free steady state, AOT/trim-safe (no reflection — `NAudio.Core` is
  `IsAotCompatible`), `float` throughout, `Span<float>`, vectorise via `TensorPrimitives`
  where the algorithm allows (gain, mix, waveshaping) — matching the existing
  `VolumeSampleProvider` precedent.

**Layer 2 — streaming adapter.** One generic `EffectSampleProvider : ISampleProvider`
hosting an `IAudioEffect`, plus an `EffectChain` that runs an ordered list. Existing
pipelines keep working unchanged; the kernels stay reusable by the synth.

**Denormals:** cross-platform .NET has no portable FTZ/DAZ control. Standardise on the
proven mitigations — tiny DC offset into feedback paths (the existing ChunkWare code already
uses `DC_OFFSET = 1e-25`) or periodic flush — and bake it into the delay-line/feedback
helpers so every effect inherits it.

**This layer is the prerequisite for everything in §6 and is the cheapest high-value work.**

### 3.3 Thread model for parameter edits — block-boundary dispatch (decided)

A live effect is mutated on exactly one thread: the audio thread, at a block
boundary. Off-thread callers (the harness UI, later automation/presets) never
touch effect state directly while the effect is running.

- `EffectParameter` gains an optional dispatch sink. When a sink is installed,
  the `Value` setter clamps the value and **posts** `(parameter, value)` to a
  pre-allocated, lock-free single-producer/single-consumer ring
  (`ParameterDispatchQueue`) instead of invoking the effect's setter inline.
  With no sink (the `EffectChain`, offline render, unit tests — all
  single-threaded) it applies inline exactly as before, so the model stays
  opt-in and additive and `IAudioEffect` is unchanged.
- The realtime engine owns the queue, `Attach`es it to every parameter of the
  effects it adopts (and `Detach`es on stop/replace), and calls `Drain()` once
  at the top of each ASIO block, *before* DSP. Pending setters then run on the
  audio thread, so coefficient recomputation, delay-buffer resizes and the
  `CrossfadingBiQuadFilter` swap all execute where there is no concurrent
  reader. This is the ASIO buffer acting as the "parameter block" boundary.
- Consequence: `CrossfadingBiQuadFilter`'s unsynchronised swap and the
  delay-buffer-resize race are **safe by construction** — there is no
  cross-thread access left to harden per-primitive. The queue is bounded
  (drops the oldest pending write on overflow; the next edit of a parameter
  supersedes it) — a slider drag emits far fewer events per second than blocks
  are processed, so it never backs up in practice.
- `Bypass`/`Mix` stay direct writes: single `bool`/`float` fields (atomic per
  ECMA, no torn read) whose transitions the base class already ramps
  click-free, so deferring them buys nothing.

This is the recorded resolution of the "live parameter edit race" review
finding and the load-bearing decision for the future VST3 host (same model,
same queue).

### 3.4 Milestone 1 — realtime & correctness hardening (done)

Done before the first Windows clone-and-listen, on `naudio3-effects`:

1. **Parameter dispatch** (§3.3): `IParameterDispatch` + `ParameterDispatchQueue`,
   `EffectParameter` deferral, engine attach/detach/drain.
2. **No audio-thread allocation in the harness:** the engine pre-warms each
   newly added effect with one silent block off the audio thread, so
   `AudioEffect`'s one-time dry-buffer sizing never lands on the ASIO callback.
3. **`DelayLine` fractional-read boundary:** the line now keeps one extra
   internal sample so a fractional read at exactly `MaxDelaySamples`
   interpolates against the correct older neighbour instead of wrapping to the
   newest sample. Public contract (`MaxDelaySamples`, valid range) unchanged.
4. **Freeverb damping is sample-rate invariant:** the comb damping one-pole
   coefficient is re-mapped by `pow(a, 44100/sampleRate)` so the damping
   *cutoff frequency* (hence the high-frequency decay) no longer shifts with
   sample rate. Comb/all-pass lengths already scale, so the low-frequency
   decay time was already stable.
5. **Look-ahead limiter is now provably brick-wall:** the old design ran the
   peak detector's *release* at input time and applied that envelope to the
   delayed output, so an isolated short transient could release below the
   ceiling before its delayed copy emerged → overshoot. Replaced with a
   sliding-window minimum of the required gain over the look-ahead window
   (allocation-free monotonic deque) + click-free release; an
   isolated-transient unit test pins no-overshoot. *Known residual quality
   trait, documented not a bug:* a brief look-ahead "pre-dip" on the quiet
   audio preceding a transient — inherent to instant-attack look-ahead
   limiting.
6. **Harness file-vs-live mutual exclusion:** ASIO monitoring and file
   playback/render can no longer run the shared effect instances on two audio
   threads at once.

### 3.5 First-listen feedback follow-up (done)

From the first Windows monitoring session, addressed on `naudio3-effects`:

- **Optimistic parameter read (regression fix):** the §3.3 deferral made a
  two-way-bound UI control re-read the pre-edit value and snap back (toggles
  needed two clicks). `EffectParameter.Value` now returns the just-requested
  value while a dispatch is attached, applied for real on the next drain.
- **Delay tempo sync was opaque:** `Tempo`/`Division` are now parameters and a
  read-only `EffectiveDelayMilliseconds` shows what the chosen division
  resolves to. **Ping-pong** now sums to mono and injects one line so a
  centred/mono source actually bounces L↔R.
- **Tremolo Square / S&H clicked:** the modulator is edge-smoothed (~3 ms,
  inaudible on the smooth waveforms at LFO rates).
- **BitCrusher decimation re-parameterised:** target sample rate (Choice list)
  instead of an integer factor, with an optional `Smoothing` low-pass; the
  aliased sample-and-hold remains the default character.
- **ASIO input offset:** mono/stereo capture can start from any base channel
  (guitar on input 2, stereo pair on 5+6, …).
- **Expected, documented (no change):** stereo-width on a mono source is
  inaudible by definition (M/S side signal is zero) — a pseudo-stereo /
  dimension effect is the right tool and is on the missing-effects roadmap.

### 3.6 Milestone 2 — test-net hardening (done)

The review found the effects suite was mostly smoke ("runs, stays finite")
rather than correctness. Added, all in the fast per-build run:

- **`Reset()` determinism net** over every effect (run → `Reset()` → run same
  signal → assert identical, 0.5 s buffers). This caught two real bugs the
  ~30 untested `Reset()` overrides were hiding: `CrossfadingBiQuadFilter.Reset()`
  never cleared the wrapped biquad history (so `Equalizer`/`GraphicEqualizer`
  carried filter state across `Reset()`), and `ComfortNoiseEffect.Reset()`
  didn't re-seed its RNG. Fixed both (added `BiQuadFilter.ResetState()`).
- **No-allocation net:** representative effect per mechanism asserts zero
  managed bytes/`Process` after warm-up — pins the alloc-free claim now that
  Milestone 1's prewarm makes it true.
- **`DenormalGuard` net:** flush boundaries, plus a reverb-tail assertion that
  a decaying feedback path never emits a subnormal float.
- **Pitch accuracy:** autocorrelation + parabolic interpolation, asserting the
  shifted fundamental is within 50 cents for ±octave and a fifth (replaces the
  old zero-crossing/RMS proxies).
- Reverb/FDN decay ordering was already covered by existing tests; a precise
  cross-sample-rate spectral regression for the Milestone 1 Freeverb damping
  fix is deliberately *not* added to the fast suite (fragile/expensive) —
  Integration-only if ever needed.

**Runtime discipline outcome:** buffers capped at 0.5 s, pitch limited to 3
cases, no-alloc to 6 effects. Suite went 184 → 222 tests; per-run wall-time
variance on the same tests (≈3–6 s) already swamps the added cost (the 35
heaviest new tests execute in well under 1 s excluding fixed host startup), so
nothing needed `IntegrationTest`.

### 3.7 Demo-only composed effects (smoke test + composition example)

The four effects intentionally excluded from the harness catalogue (parametric
`Equalizer`, `GraphicEqualizer`, `MultibandCompressorEffect`,
`ConvolutionReverbEffect`) need bespoke UI because they have dynamic band lists
or an IR setup. To close the smoke-test gap *and* demonstrate the "compose your
own effect" pattern, three small wrappers live in
`NAudioWpfDemo/RealtimeEffectsDemo/CustomEffects/` (user code, not toolkit):

- **`SevenBandEqEffect`** — fixed BOSS GE-7 style 7-band peaking EQ
  (100/200/400/800/1.6k/3.2k/6.4k Hz) by containment on `Equalizer`. Setters
  write `band.GainDb` and call `Update()` for the click-free retune crossfade.
- **`FilterEffect`** — HPF + LPF with selectable 12 / 24 dB-per-octave slope,
  built directly from `BiQuadFilter` and `CrossfadingBiQuadFilter` (proper
  Butterworth Q values for the 24 dB/oct cascade). Shows the lower-level
  composition pattern.
- **`ThreeBandCompressorEffect`** — fixed 3-band compressor at 250 Hz / 2.5 kHz
  crossovers by containment on `MultibandCompressorEffect`, exposing per-band
  threshold/ratio plus gain-reduction meters (attack/release/make-up fixed at
  sensible per-band defaults).

Convolution reverb is covered by a separate, dedicated **Convolution Reverb**
WPF demo module — its IR-setup-as-input doesn't fit the fixed-parameter
facade. The module is an offline test bench: pick an input file and a folder
of impulse responses, render single-IR or batch-all, with IR auto-resampled to
the input rate (`WdlResamplingSampleProvider`) and peak-normalised to -3 dBFS.
Output is latency-compensated and tail-flushed; each render reports Nx
real-time and added tail length. Output WAVs land in a temp folder browsable
from the panel (matching the `WasapiCaptureDemo` Play/Delete/Open pattern).

### 3.8 Effects on positionable streams — position-preserving decorators (leading direction)

**The long-standing problem.** "Positionable `WaveStream` vs non-positional
`IWaveProvider` / `ISampleProvider`" is a recurring pain point: the seek-vs-decorator
dance — you reposition the file reader, but the player reads from the *decorated end* of
the chain, so seeks and the read head are on different objects. Worth resolving before
NAudio 3 ships.

**Leading direction (not 100% locked — expected to be delivered incrementally and refined
as the wrinkles below are met).** Keep the decorator model, but make the decorators
**position-preserving**: when the source is a `WaveStream`, return an adapter that is
*both* a `WaveStream` and an `ISampleProvider`. It forwards `Position`/`Length` down to
the seekable source while `Read` pulls through the effect chain — so the object the player
reads from *is* the object you seek. `AudioFileReader` (already a `WaveStream` that also
implements `ISampleProvider`) is the proven precedent for the dual-interface shape.

This is preferred over the previously-floated alternative of letting `WaveStream` host
effects directly (or a new base class to inherit), because it is **additive and opt-in and
keeps `IAudioEffect` / `EffectChain` out of Core's fundamental playback contract** — the
adapters depend on the effects layer, never the reverse. `WaveStream` is untouched; no
`IEffectsHost`, no lifting of the effect interfaces into `NAudio.Wave`.

**Entry points (chosen):** `waveStream.AddEffect(effect)` and
`waveStream.AddEffects(params IAudioEffect[])` — the effects are passed in and the
`EffectChain` is built *internally* on `source.ToSampleProvider()`. This sidesteps
`EffectChain`'s current source-at-construction coupling (passing a pre-bound chain would be
awkward). Mirror overloads on `ISampleProvider` return a plain `ISampleProvider` (you can't
synthesise positionability from a non-positionable source).

**Generalising to the other decorators.** The same idea extends to the existing
single-source, time-linear transforms (`Skip` = affine offset, `Take` = clamped `Length`,
`ToMono`/`ToStereo` = channel-ratio remap, resample = rate-ratio remap): when handed a
`WaveStream` they can return a positionable adapter instead of a bare `ISampleProvider`.
Rather than an adapter class per transform, collapse them into **one generic
`PositionPreservingSampleProvider : WaveStream, ISampleProvider`** parameterised by the
root `WaveStream`, the built pipeline, a position-mapping function, and an optional
`onSeek` reset hook. **Boundary:** position-preservation only makes sense for *single-source*
transforms — fan-in (`MixingSampleProvider`, `Concatenate`/`FollowedBy` of multiple
sources) has no single source position to forward and stays plain `ISampleProvider`.

**Doors this opens** (secondary motivations, not required for v1): `Skip`/`Take` can be
implemented more efficiently when the source is seekable (seek instead of read-and-discard),
and it gives a cleaner foundation for improving NAudio's currently-poor looping support
(a loop is a seek back to a mark on the same object the player reads from).

**Known wrinkles / limitations to handle as it matures (intrinsic to *any* seekable-effects
solution, not caused by this approach):**

- **Byte↔sample position mapping.** `WaveStream.Position`/`Length` are bytes in the source
  format; the pipeline is float. Trivial 1:1 for a float source like `AudioFileReader`;
  a bytes-per-frame ratio (and frame-aligned `set`) for a PCM source. A future
  sample-denominated `AudioFormat` would make this much cleaner.
- **Latency offset.** Forwarding `Position` reports the source read head, not the output
  position — off by `chain.LatencySamples` for pitch/convolution/look-ahead effects.
  Acceptable for a playback cursor; optionally subtract the latency.
- **Tails past EOF.** Reverb/decay output continues after the source hits `Length`, which a
  position-forwarder can't express. `Length`/`CurrentTime` are therefore approximate for
  latency/tail effects — the one genuinely unsolvable-in-general case, shared by every
  approach.
- **Seek resets effect state.** The `Position` setter must clear delay lines / filter
  history / reverb buffers or pre-seek audio bleeds through. Needs a small new
  `EffectChain.Reset()` (iterate `Effects`, call each `Reset()`).
- **Ownership/disposal** of the wrapped source (a `leaveOpen`-style flag or a clear
  convention).

**Follow-up items:** add `EffectChain.Reset()`; decide the `EffectChain` source-coupling
question (the `AddEffect(s)`-builds-internally entry points avoid it for this feature, but
it may still be worth decoupling the effect *list* from its bound source more broadly).

The plain decorator pattern (file → chain → player, or any non-`WaveStream` source like a
mic / synth / network) stays valid in every scenario; this is additive convenience for the
seekable case.

---

## 4. Port vs build — the decision framework

Decide per effect against five axes:

1. **Algorithmic complexity** — textbook one-liner vs research-grade.
2. **Quality ceiling of the from-scratch version** — can a careful C# implementation match
   the best reference, or is there irreducible know-how in a tuned reference?
3. **Benefit of being idiomatic C#** — `Span`, AOT, no attribution baggage, doubles as a
   synth block.
4. **Maintenance burden of a port** — transliterated C++ rots; we own the bugs forever.
5. **Licence** — MIT/BSD/Zlib/PD ingestible with attribution; GPL/LGPL/proprietary forbidden.

**Default: build.** The simple effects are *less* work to write than to port-and-revalidate,
and the result is cleaner. **Port only when (1) is high, (2) favours the reference, and a
permissive source exists** — reverb internals, partitioned convolution maths, and the
voice-comms stack. Even then, prefer "port as *reference/algorithm*, reimplement idiomatically"
over line-by-line transliteration, except where a reference is so battle-tuned (RNNoise model,
Speex AEC) that faithfulness matters more than idiom.

### 4.1 Licence reference for candidate sources

| Source | Licence | NAudio-compatible? | Best for |
|---|---|---|---|
| **DaisySP** (electro-smith) | MIT | ✅ | Reference for delay, chorus, flanger, phaser, overdrive, fold, decimate, limiter |
| **Airwindows** (Chris Johnson) | MIT | ✅ | Saturation/console/tape, small bespoke effects — algorithmically tiny |
| **Signalsmith DSP / Stretch** (Geraint Luff) | MIT | ✅ | Reference for high-quality FDN reverb and best-in-class time/pitch stretch |
| **Freeverb** (Jezar @ Dreampoint) | Public domain | ✅ | The classic Schroeder-Moorer reverb — ideal first algorithmic reverb |
| **STK** (Cook/Scavone) | MIT-style permissive | ✅ | Reverb (NRev/JCRev/PRCRev), chorus, pitch — synth-adjacent |
| **Mutable Instruments** (É. Gillet) | MIT | ✅ | Synth/sampler building blocks (relevant to the future synth) |
| **musicdsp.org** archive | mostly unlicensed/PD | ⚠️ caution | Idea source; do not copy verbatim without a clear licence |
| **WebRTC Audio Processing Module** (Google) | BSD-3 | ✅ | Gold-standard AEC3 / NS / AGC2 — but large, heavy C++ |
| **Speex DSP** (Xiph) | BSD-3 | ✅ | Pragmatic NS + AGC + VAD + (MDF) AEC — far smaller than WebRTC |
| **RNNoise** (Xiph/Mozilla) | BSD-3 | ✅ | Modern ML noise suppression, ~85 KB model, needs tiny NN inference |
| Rubber Band / SoundTouch | GPL / LGPL | ❌ | (use Signalsmith / our `SmbPitchShifter` instead) |
| Surge XT / Vital / TAL / JUCE DSP | GPL / proprietary | ❌ | never copy from these |

Compatible licences require: preserve the copyright/permission notice, add an entry to a
new top-level `THIRD-PARTY-NOTICES.txt`, and keep the existing in-file attribution-header
convention (`// based on … OPEN-SOURCE`, as `SmbPitchShifter`/`WdlResampler` already do).

---

## 5. Per-effect evaluation

LOC are rough, for the *kernel*, idiomatic C#. "Build" = write from scratch (reference an
algorithm/paper, not transliterate). "Port" = adapt a specific permissive implementation.

### Music production

| Effect | Complexity | Decision | Reference | Notes |
|---|---|---|---|---|
| Gain / trim / pan / width | trivial (~30) | **Build** | — | `VolumeSampleProvider` exists; add pan, stereo width, mono-maker, channel matrix. |
| Parametric / shelving EQ | low (~150) | **Build** | RBJ cookbook (have it) | Relocate `Equalizer` to Core; multi-band, shelves, per-channel. Crash/Nyquist-safety done by #1259; click-free retune via the §3.2 A/B crossfade. |
| Graphic EQ | low | **Build** | — | Fixed bands over the parametric core. |
| Compressor | low-med (~250) | **Build** | DaisySP/Airwindows as sanity ref | Soft knee, peak/RMS detector, optional lookahead, sidechain input, gain-reduction meter. Replaces ChunkWare. |
| Limiter (brick-wall) | medium (~300) | **Build** | DaisySP limiter | Lookahead + true-peak (oversampled) detection — the part that needs care. |
| Gate / expander | low (~150) | **Build** | — | Hysteresis, hold, range, sidechain HPF. Replaces `SimpleGate`. |
| Transient shaper | medium | **Build** | — | Dual-envelope (fast/slow) differencing. |
| De-esser | medium | **Build** | — | Split-band or sidechain-EQ'd compressor — composes from the above. |
| Saturation / waveshaper | trivial (~40) | **Build** | Airwindows | tanh/cubic/arctan + drive + tone + optional oversampling for alias control. |
| Bitcrush / decimate | trivial (~30) | **Build** | DaisySP decimate | Sample-rate + bit-depth reduction. |
| Delay (mono/stereo/ping-pong) | low (~150) | **Build** | DaisySP | Fractional delay line, feedback, damping, ping-pong, sync. |
| Tape/analogue delay | medium | **Build** | — | Delay + wow/flutter LFO + saturation in the loop — composes. |
| Chorus / flanger | low (~120) | **Build** | DaisySP/STK | Modulated fractional delay; flanger = short delay + feedback. |
| Phaser | low (~120) | **Build** | DaisySP | Cascade of `BiQuadFilter` all-pass (already in `BiQuadFilter`) + LFO. |
| Tremolo / auto-pan | trivial (~30) | **Build** | — | LFO × gain / pan. |
| Algorithmic reverb | **high** | **Port (as reference)** | Freeverb (PD) → then FDN à la Signalsmith | Phase 2. Start with Freeverb (small, public-domain, good baseline), then a tunable FDN for the quality tier. The hard part is tuning, not transcription. |
| Convolution reverb | **high** | **Build on `FftProcessor`** | partitioned-convolution literature | Replaces `ImpulseResponseConvolution`. Uniformly-partitioned overlap-add; we already have the FFT. |
| Pitch shift / time stretch | high | **Have + optional port** | `SmbPitchShifter` (have); Signalsmith (MIT) for quality tier | Ship `SmbPitchShifter` now; evaluate a Signalsmith-based high-quality mode later. |

### Voice comms

| Effect | Complexity | Decision | Reference | Notes |
|---|---|---|---|---|
| DC blocker / rumble HPF | trivial (~20) | **Build** | — | One-pole DC blocker + `BiQuadFilter` HPF. Do this first; everything else benefits. |
| Noise gate (voice) | low | **Build** | — | Same engine as the music gate, comms-tuned defaults + VAD-linked option. |
| Automatic gain control | low-med (~200) | **Build** | Speex/WebRTC AGC as ref | Slow loudness-targeting loop + fast limiter on top (composes from limiter). |
| Voice activity detection | medium | **Build** | Speex VAD; WebRTC GMM-VAD as ref | Energy + spectral-flatness; gates NS/AGC/transmit. |
| Noise suppression | medium→high | **Tiered** | (a) **build** spectral-subtraction/Wiener on `FftProcessor`; (b) **port RNNoise** (BSD) for the quality tier | (a) is a few hundred lines and decent; (b) is markedly better, ~85 KB model, needs a small GRU/dense inference path (no external ML runtime). |
| Acoustic echo cancellation | **very high** | **Port — standalone project** | Speex MDF (BSD) realistic; WebRTC AEC3 (BSD) gold-standard but huge | NLMS/MDF adaptive filter + double-talk detection + nonlinear residual suppression. The single biggest item in the suite — schedule as its own milestone, not a checklist line. |
| Comfort noise | trivial | **Build** | — | Shaped low-level noise during gated silence. |
| De-reverberation | research-grade | **Defer / non-goal** | — | WPE-class processing; out of scope for NAudio 3. |

---

## 6. Recommended phased roadmap

Pragmatic ordering: framework, then cheap-and-high-value, then the hard specialised work.
Each phase is independently shippable and independently back-out-able, mirroring the
"first round / subsequent phases" discipline used in `NAudio3AssemblyLayoutPlan.md`.

- **Phase 0 — Framework & cleanup (scope confirmed; *no new effects*).**
  1. **Delete** `SimpleGate`, `SimpleCompressor`, `SimpleCompressorEffect`,
     `ImpulseResponseConvolution`, and the now-orphaned `EnvelopeDetector` /
     `AttRelEnvelope`. Verified zero in-repo consumers. Add two
     `#### Breaking changes` bullets to `RELEASE_NOTES.md` for the public pair
     (`SimpleCompressorEffect`, `ImpulseResponseConvolution`); the four internal types
     need no entry.
  2. **`IAudioEffect`** + thin abstract **`AudioEffect`** base (click-free `Bypass` +
     dry/wet `Mix`), **`EffectSampleProvider`**, **`EffectChain`** — per the §3.1
     confirmed decisions (`NAudio.Effects` in `NAudio.Core`, interleaved `Span<float>`,
     `Configure(WaveFormat)`, channel-agnostic).
  3. **Shared helpers:** `ParameterSmoother` (scalar one-pole), **A/B filter-crossfade
     helper** (the corrected click-free-retune fix — see §3.2), denormal-safe
     delay-line / feedback primitive.
  4. **Clean `public float` attack/release envelope follower**, written from scratch
     (replacing the deleted internal one) — the shared dynamics + synth building block.
  5. **`BiQuadFilter` residual only:** delete the 3 stale `// TODO: should we square
     root` comments. The validation/NaN-latch hardening is **already done by #1259** —
     dropped from scope. The factory-surface asymmetry (no in-place `Set*` for
     notch/bandpass/shelf) is **deferred** — the just-merged, just-tested file isn't
     worth re-churning for cosmetics until an effect actually needs in-place shelf retune.

  Low-risk, unblocks everything else.

- **Phase 1 — Core music effects (all build-from-scratch).** EQ (relocated+fixed from
  `Extras`), compressor, limiter, gate/expander, saturation/waveshaper, bitcrush, delay
  (incl. ping-pong), chorus/flanger/phaser, tremolo/auto-pan, gain/pan/width/DC-blocker.
  This is the bulk of the music suite and almost all of it is small, well-understood DSP.

- **Phase 2 — Reverb.** Partitioned-convolution reverb on `FftProcessor` (replacing
  `ImpulseResponseConvolution`); algorithmic reverb starting from Freeverb, then an FDN
  quality tier referencing the Signalsmith design.

- **Phase 3 — Voice comms core.** DC/HPF, comms-tuned gate, VAD, AGC, and tier-(a)
  spectral noise suppression on `FftProcessor`. Evaluate the RNNoise port as the quality
  tier. **AEC is explicitly *not* in this phase** — it is Phase 5.

- **Phase 4 — Advanced music.** De-esser, transient shaper, multiband dynamics, true-peak
  lookahead limiter polish, optional Signalsmith-based high-quality pitch/time mode.

- **Phase 5 — Acoustic echo cancellation.** Its own milestone. Target a Speex-MDF-class
  port first; WebRTC AEC3 only if there is sustained demand and maintenance appetite.

### Real-time effects test harness — design (approved)

A cross-cutting deliverable: the primary *subjective* quality-evaluation tool, and the
seed for VST3-host testing. Design decided:

- **Host: WPF (`NAudioWpfDemo`), new `RealtimeEffectsDemo` module.** Chosen over WinForms
  because the MVVM/data-binding infra makes a dynamic, reorderable chain with
  auto-generated parameter panels far less code, and it aligns with the VST3-host /
  "parameters reused in other UIs" future. (WinForms has ready-made `Pot`/`Fader`/
  `VolumeMeter` widgets and would be faster *only* if we chose custom-UI-per-effect.)
- **Parameter model: opt-in, additive — `IAudioEffect` is unchanged.** A new optional
  `IParameterized` companion interface exposes `IReadOnlyList<EffectParameter>`;
  `EffectParameter` is a thin delegate-backed facade over each effect's existing typed
  properties (Continuous / Toggle / Choice / read-only Meter kinds), built via a small
  helper (~5–10 lines per effect). The realtime contract and the synth/sampler use of
  the kernels stay clean; the same model later drives a VST3-host generic UI, presets,
  serialization and automation. `Bypass`/`Mix` are rendered generically from the
  `AudioEffect` base, so per-effect parameter lists exclude them. Convolution IR is
  *not* forced into the model — it is a setup input handled specially by the harness.
- **Harness:** `AsioDevice.InitDuplex` (driver + 1/2 input-channel selection; mono
  duplicated to stereo; stereo out). Realtime callback is alloc-free; the ASIO engine
  holds the chain as an **atomic snapshot** (`IAudioEffect[]` swapped by reference). The
  file-playback path uses `EffectChain`, which is now itself safely mutable during `Read`
  (copy-on-write `Insert`/`RemoveAt`/`Move`), so add/remove/reorder is heard live there
  too. **Feedback safety:** starts muted, explicit
  Monitor toggle, runaway-level auto-mute, warn if input device == output device.
  **Sources:** live ASIO input *or* a WAV file → same chain → real-time monitor *or*
  offline render-to-WAV (offline path needs no ASIO; latency-compensated via
  `LatencySamples`). **Chain UI:** auto-generated panels from `IParameterized` +
  Bypass/Mix, add/remove, move up/down.
- **Build waves:** (A) parameter model + wire effects + tests [pure `NAudio.Core`,
  unit-tested]; (B) WPF module + ASIO duplex + safety; (C) chain UI + file source +
  offline render. Waves B/C are compile-validated in the Linux sandbox
  (`EnableWindowsTargeting`); runtime is CI/Windows.

**Status:** Complete. The `RealtimeEffectsDemo` module is both the subjective-quality
evaluation tool and the foundation for the future VST3-host UI; the realtime & correctness
hardening of §3.4 is in place. EQ/multiband (dynamic band lists) and convolution-reverb IR
are deliberately excluded from the generic catalog (they need bespoke UI).

Cross-cutting, every phase: allocation-free steady state, AOT/trim-safe, `Span<float>`,
numerical-correctness unit tests (the repo already does this — e.g. `BiQuadFilterTests`,
`FftProcessorTests`), and a demo entry in `NAudioWpfDemo`/`NAudioDemo`. Subjective audio
quality needs real listening tests, not just unit tests — call this out explicitly as a
non-automatable acceptance step.

---

## 7. Risks & non-goals

- **Quality is subjective.** "High quality" can't be unit-tested; budget for listening
  tests and reference-track comparison, especially for reverb, saturation, NS and pitch.
- **AEC, ML-NS and de-reverb are deep.** Treat AEC as a standalone project; treat
  de-reverberation as a non-goal for v3.
- **Don't over-abstract.** One small `IAudioEffect` + a smoother + a chain. Resist a
  plugin/graph/automation framework — that is a separate, later concern.
- **Licence discipline.** Never copy from GPL/proprietary code (Rubber Band, SoundTouch,
  Surge, Vital, TAL, JUCE DSP). Maintain `THIRD-PARTY-NOTICES.txt` and in-file attribution
  for every permissive port, extending the convention the repo already follows.
- **Denormals & cross-platform numerics.** No portable FTZ in .NET — solve once in the
  shared helpers, test on the headless CI runner.
- **DMO effects are not a migration target.** They stay Windows-only; the managed suite is
  a parallel, cross-platform offering, not a reimplementation obligation.
