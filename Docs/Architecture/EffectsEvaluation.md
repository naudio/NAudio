# NAudio 3 — Audio effects suite: evaluation & strategy

**Status:** Evaluation / proposal. No effect code yet — this document decides *what* we
build, *what* we port, and *in what order*, before any effect lands.

**Update (post PR #1259):** PR #1259 ("Fix BiQuadFilter NaN-latch and make Equalizer
Nyquist-aware", issue #190) has merged to `main` and is now on this branch. It hardens
`BiQuadFilter` (parameter validation + state reset on retune) and makes `Equalizer`
crash/Nyquist-safe — see §2.1/§2.2 for the re-evaluation and the new framework
consequence in §3. The maintainer has **confirmed** the Phase 0 design decisions (§3.1)
and **confirmed deletion** (not salvage) of the weak dynamics + dead convolution code (§2.1).

**Update (Phase 0 landed):** Phase 0 is implemented on branch `naudio3-effects`: the six
weak/dead types deleted, the `NAudio.Effects` framework (`IAudioEffect`, `AudioEffect`,
`EffectSampleProvider`, `EffectChain`) and the `NAudio.Dsp` primitives
(`ParameterSmoother`, `EnvelopeFollower`, `DelayLine`, `DenormalGuard`,
`CrossfadingBiQuadFilter`) added, the stale `BiQuadFilter` `TODO`s removed, RELEASE_NOTES
updated. `NAudio.Core` builds clean (0/0) and 19 new unit tests pass on `net10.0`.

**Update (Phase 1 wave a landed):** utility + EQ shipped — `GainEffect`, `PanEffect`,
`StereoWidthEffect`, `MonoMakerEffect`, `DcBlockerEffect`, a per-channel multi-band
`Equalizer` (peaking/shelf/pass/notch/band-pass/all-pass, click-free retune via
`CrossfadingBiQuadFilter.ReplaceStandby`), and `GraphicEqualizer` (10/31-band). The old
`NAudio.Extras` `Equalizer`/`EqualizerBand` are deleted and the WPF EqualizationDemo moved
to the new API. 37 Effects unit tests pass on `net10.0`; `NAudio.Core` and `NAudio.Extras`
build clean.

**Update (Phase 1 wave b landed):** dynamics shipped — `CompressorEffect`
(soft-knee, peak/RMS, channel-linked, GR metering), `LimiterEffect` (look-ahead
brick-wall, reports `LatencySamples`), `GateEffect` (gate/expander with hysteresis +
hold). 46 Effects unit tests pass on `net10.0`; `NAudio.Core` builds clean.

**Update (Phase 1 wave c landed):** `SaturationEffect` (4 curves, drive/trim, optional
2×/4× oversampling), `BitCrusherEffect` (bit-depth + sample-rate reduction), and the
reusable `NAudio.Dsp.Oversampler`. 55 Effects unit tests pass on `net10.0`.

**Update (Phase 1 wave d landed — Phase 1 complete):** `DelayEffect` (tempo-syncable,
feedback damping, ping-pong), `ChorusEffect`, `FlangerEffect`, `PhaserEffect`,
`TremoloEffect` (+ auto-pan), plus reusable `NAudio.Dsp.Lfo` and `NoteDivision`/
`TempoTime`. **70 Effects unit tests pass on `net10.0`; `NAudio.Core` builds clean.**
Phase 1 (the core music-effects suite) is done.

**Update (Phase 2 wave a landed):** `ConvolutionReverbEffect` + reusable
`NAudio.Dsp.PartitionedConvolver` (uniformly-partitioned overlap-save FFT convolution on
`FftProcessor`), replacing the removed `ImpulseResponseConvolution`. Numerically verified
against direct convolution. 77 Effects unit tests pass on `net10.0`; `NAudio.Core` builds
clean. Remaining Phase 2 waves: (b) Freeverb-inspired Schroeder–Moorer reverb,
(c) Signalsmith-referenced FDN reverb.

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

### Live monitoring demo (cross-cutting tooling — design TBD)

A planned deliverable, not tied to one phase: a GUI demo (WPF/NAudioDemo) that runs a
**live input → effect chain → output** path so effects can be auditioned on real guitar
or microphone input. This is the primary *subjective* quality-evaluation tool — the
"real listening test" the cross-cutting note below calls for — and should be slotted in
once there is a usable effect set (Phase 1 already qualifies; revisit after Phase 2).
**It needs its own design discussion before implementation.** Open questions: low-latency
backend choice per-OS (WASAPI exclusive / ASIO on Windows), buffer-size/latency controls,
feedback-safety (mute-on-start, output≠input device), per-effect parameter UI, and chain
ordering. **It may also be the foundation for testing the in-progress VST3 hosting
work (separate branch)** — the same live input→chain→output harness can host a VST3
plugin in place of (or alongside) a native `IAudioEffect`, so the host abstraction should
be designed with both in mind. Treat the VST3 linkage as an explicit design input when
that discussion happens.

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

---

## 8. Bottom line

Build the framework, build the ~70% that is small textbook DSP (it is genuinely less work
than porting and yields cleaner, AOT-safe, synth-reusable code), and reserve porting for the
few genuinely hard, well-served-by-permissive-references areas: reverb and the voice-comms
stack. Licensing does not constrain the realistic choices. The work is large but cleanly
phasable, and Phase 0 + Phase 1 alone would already give NAudio a credible, modern,
cross-platform effects suite out of the box.
