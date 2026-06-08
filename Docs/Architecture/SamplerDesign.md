# NAudio 3 — Sampler: design notes

This document captures the **plan and architectural decisions** for the NAudio 3
sampler: a pure-C#, cross-platform instrument that plays MIDI through SoundFont
(`.sf2`) and SFZ (`.sfz`) files, plus a "drop a sample on the keyboard"
instrument built from the same engine.

It is a planning document — written before the bulk of the code exists — so it
records *intent* and *sequencing* as much as finished decisions. It will be
revised as PRs land. User-facing documentation (how to load an instrument, play
it, render a MIDI file) will live in `Docs/Sampler.md` once the API stabilises.

Related reading: [`EffectsDesign.md`](EffectsDesign.md) (the effects suite is an
explicit sampler building block), [`Sequencing.md`](Sequencing.md) (the
scheduling core the offline-render demo rides on — PR #1324),
[`NAudio3AssemblyLayoutPlan.md`](NAudio3AssemblyLayoutPlan.md) (packaging
principles).

## 1. Goal

A first-class software sampler for NAudio 3 that:

- **Plays SoundFont 2 and SFZ instruments** — from one-shot drum hits through
  multisampled melodic instruments to a full multi-timbral General MIDI bank.
- **Is driven by MIDI** — both live (`MidiIn`) and offline (a `MidiFile`
  rendered to WAV through the sequencing core).
- **Maps a single sample across the keyboard** — open or record a snippet,
  auto-map it, edit start/end/loop points against a waveform view, play it
  polyphonically. This is the sampler's "hello world" and a genuinely useful
  instrument in its own right.
- **Is pure managed, cross-platform, AOT-safe** — same bar as `NAudio.Core` and
  `NAudio.Effects`. No Windows dependency in the engine; live MIDI in / audio
  out are supplied by the host app via the existing backends.

The engine reuses the DSP and effects primitives NAudio 3 already shipped, and
deliberately shares its lower-level building blocks (envelopes, LFOs, filters,
interpolation) with the **future NAudio synthesiser**, so those pieces are
designed to be instrument-agnostic from day one.

## 2. Where things live

The guiding rule: **`NAudio.Sampler` contains only the pieces that are
sampler-specific.** Anything reusable in a wider context goes into
`NAudio.Core`, in the namespace that already owns its category.

| Piece | Home | Rationale |
|---|---|---|
| Variable-rate interpolating sample/wavetable reader (cubic/Hermite/sinc) | `NAudio.Core` → `NAudio.Dsp` | A general primitive — a wavetable synth, a varispeed player, and the sampler all want it. |
| DAHDSR envelope (6-stage, cB/timecent aware) | `NAudio.Core` → `NAudio.Dsp` | Shared with the future synth. Generalises the existing 4-stage `EnvelopeGenerator`. |
| LFO improvements (delay/fade-in, key-sync, additional shapes) | `NAudio.Core` → `NAudio.Dsp` (extend `Lfo`) | Shared with the synth; `Lfo` already exists and is close. |
| Resonant low-pass filter + correct cB-Q / cents-Fc parameter mapping | `NAudio.Core` → `NAudio.Dsp` (build on `BiQuadFilter` / `CrossfadingBiQuadFilter`) | Shared with the synth and any filter-based effect. |
| MIDI-note / cents / timecent / centibel math helpers | `NAudio.Core` → `NAudio.Dsp` (or a small `NAudio.Midi` math helper) | Useful anywhere MIDI meets DSP. |
| Reverb / chorus send-bus plumbing | `NAudio.Core` → `NAudio.Effects` if generic; otherwise `NAudio.Sampler` | The effects already exist; only the *send/return routing* is new. Put the generic bus in Effects, the SF2/SFZ send-amount wiring in the sampler. |
| SoundFont **parser** | stays in `NAudio.Core` → `NAudio.SoundFont` | Already there; it's format I/O. |
| SFZ **parser** | `NAudio.Core` → new `NAudio.SoundFont`-sibling (e.g. `FileFormats/Sfz`) | Consistent with SoundFont living in Core as format I/O. |
| Resolved instrument model, voice, voice manager, modulator engine, MIDI channel state, the `ISampleProvider` front-end | **`NAudio.Sampler`** | This *is* the sampler. |
| Single-sample auto-mapper / loop-point editor model | **`NAudio.Sampler`** | Sampler-specific instrument authoring. |

**`NAudio.Sampler`** is a new portable package: `net9.0`,
`<IsAotCompatible>true</IsAotCompatible>`, depending on `NAudio.Core` +
`NAudio.Midi` + the sequencing core. It follows the package conventions in the
assembly-layout plan and ships from the same `release.yml` as the rest. A future
`NAudio.Synth` package would depend on the same shared `NAudio.Core` primitives;
the two instruments are siblings over a common DSP floor, not one built on the
other.

## 3. Architecture — three layers

Mirrors the effects framework's "kernels vs streaming adapter" split.

- **Layer 1 — DSP kernels (`NAudio.Core`).** The interpolating reader, DAHDSR,
  LFO, resonant filter, envelope follower. Plain classes, process their own
  buffers, no MIDI/format awareness. Reused by sampler and synth.
- **Layer 2 — the synthesis engine (`NAudio.Sampler`).** A `Voice` wires the
  kernels together; a `VoiceManager` allocates/steals voices and tracks MIDI
  channel state; a format-neutral *resolved instrument model* feeds them. The
  engine exposes itself as a single `ISampleProvider` (one per MIDI channel set,
  or one multi-timbral provider) so it drops straight into the existing graph.
- **Layer 3 — hosts/demos.** Live-MIDI player, offline MIDI-file renderer,
  single-sample instrument editor. These compose Layer 2 with the existing
  output (`WasapiPlayer`, `WaveFileWriter`) and input (`MidiIn`) stack.

### 3.1 The format-neutral instrument model

The key design decision. Both SF2 and SFZ normalise onto **one internal model**
so the voice engine never sees a format:

- An **instrument** is a set of **regions**.
- A **region** has an absolute key range, velocity range, a sample reference
  (data + sample rate + root key + loop points + loop mode), and a fully
  resolved parameter set: tuning, volume/pan, filter cutoff/resonance, two
  envelopes (amp + mod), two LFOs (mod + vibrato), effect sends, exclusive/choke
  group, plus a list of **modulation routings** (source → destination → amount →
  curve).
- A single note may trigger **several regions at once** (layers/velocity
  splits), each becoming a voice.

SF2 generators and SFZ opcodes are two *front-ends* that both populate this
model. The voice engine is written once, against the model.

## 4. File-format support

### 4.1 SoundFont 2 — modernise, do not rewrite

The parser in `NAudio.Core/FileFormats/SoundFont/` (~24 files) is structurally
complete and correct: it reads INFO / sdta / pdta, the full
PHDR→PBAG→PGEN/PMOD and INST→IBAG→IGEN/IMOD hierarchies, SHDR sample headers,
all 61 generators (`GeneratorEnum`), and modulators. **A rewrite is not
warranted.** Targeted work only:

- **24-bit samples (`sm24`)** are silently skipped today
  (`SampleDataChunk.cs`). Add 24-bit support — common in modern banks.
- **Finish modulator-field parsing** (`ModulatorType.cs` carries a
  `// TODO: map this to fields`). The modulator engine cannot exist without
  this.
- **Opportunistic modernisation** to match NAudio 3 (`#nullable enable`, string
  interpolation, `record`/`init` data models, span-based reads). Low priority;
  not a blocker.
- **Writing** stays out of scope (`SoundFont.cs` — `// TODO: save`) — a separate
  feature, not needed to *play* anything.

The parser exposes the file's *structure*; on top of it sits a **resolved
instrument** layer (`SoundFontInstrumentResolver` / `SoundFontRegion` /
`SoundFontGenerators`) that flattens preset-zone + instrument-zone generators
(with correct absolute-vs-additive accumulation per SF2.04 §9.4, global zones,
default generator values, and key/velocity-range intersection) into a flat list
of playable regions. `Preset.ResolveRegions()` is the entry point.

**This resolution layer lives in `NAudio.Core` (`NAudio.SoundFont`), not
`NAudio.Sampler`** — an earlier draft placed it in the sampler package, but it
is pure SoundFont interpretation with no synthesis dependency, useful to anyone
reading/inspecting/converting SF2 files, so by the "generally-useful code goes
in Core" principle it belongs beside the parser. The format-neutral sampler
model (§3.1) becomes a thin projection of `SoundFontRegion`; modulator
*resolution* (the default + file modulator routings) stays with the synth engine
since it is synthesis-facing.

### 4.2 SFZ — build from scratch

No SFZ code exists and none is in git history. From-scratch build:

- **Tokenizer/parser** for `<global>` / `<master>` / `<group>` / `<region>` /
  `<control>` headers, `#define` / `#include`, `$variable` substitution,
  `default_path`.
- **External sample loading** — SFZ references WAV/FLAC/OGG by relative path.
  **`NAudio.SoundFile`** (libsndfile, #1291) already decodes FLAC/Ogg/Opus
  cross-platform, so the loader is mostly plumbing over existing readers.
- **Opcode coverage in tiers** (§6) — SFZ has hundreds of opcodes; we target a
  documented useful subset (SFZ v1 + common ARIA extensions) and state clearly
  what is unsupported.

### 4.3 Single sample / recorded snippet

The third instrument front-end: take one audio buffer (loaded WAV or a live
recording), synthesise a one-region instrument mapped across the whole keyboard
with a chosen root key, and let the user adjust start / end / loop-start /
loop-end and loop mode. This populates the *same* resolved instrument model —
it's the simplest possible producer for it. See §8.3 for the demo.

## 5. DSP building blocks — have vs. build

| Block | Status | Action |
|---|---|---|
| **Variable-rate interpolating sample reader** | ❌ missing — **critical path** | Build in `NAudio.Dsp`. Phase-accumulator reader at arbitrary, per-sample-varying rate; cubic/Hermite interpolation (sinc option); start/end offsets; the three loop modes. `WdlResampler` is fixed-ratio and block-based — wrong tool for per-voice pitch that moves under vibrato/bend/envelope. |
| **DAHDSR envelope** | ⚠️ partial | Build in `NAudio.Dsp`, generalising `EnvelopeGenerator`. SF2 wants 6 stages (Delay/Attack/Hold/Decay/Sustain/Release), convex attack, decay/release in **centibels**, plus key-to-hold/decay scaling. The current analog 4-stage ADSR doesn't match the spec. |
| **LFO** | ✅ close | Extend `Dsp/Lfo.cs` with delay/fade-in and key-sync if not already present. Two LFOs per voice (mod → pitch/filter/volume; vibrato → pitch). Tempo-sync (`NoteDivision`/`TempoTime`) already exists. |
| **Resonant low-pass filter** | ⚠️ adapt | `BiQuadFilter` is the right 2-pole shape; add correct **cB→Q** and **cents→Fc** mapping and per-sample coefficient updates for filter-envelope/LFO modulation. `CrossfadingBiQuadFilter` covers click-free retune. |
| **Note/cents/timecent/centibel math** | ❌ missing | Small helper set in `NAudio.Dsp` / `NAudio.Midi`. Pervasive. |
| **Polyphonic mix core** | ✅ reuse | `MixingSampleProvider` (vectorised via `TensorPrimitives`, 1024 inputs, lock-free read) is the voice-summing bus. |
| **Effects (reverb/chorus/EQ/dynamics…)** | ✅ reuse | `NAudio.Effects` (#1310) is far beyond what a sampler needs. Only the **send-bus routing** is new. |
| **Fixed-ratio resampler** | ✅ reuse | `WdlResampler` for output-rate matching and offline render, **not** per-voice pitch. |
| Phase-vocoder pitch shifter | ✅ (not used) | `SmbPitchShifter` is time-preserving — wrong tool here; sampler pitch comes from playback rate. |

## 6. The synthesis engine (`NAudio.Sampler`) — the real work

None of this exists anywhere in NAudio today; it is the heart of the feature.

1. **Voice** — one region playing one note: interpolating reader + resonant
   filter + amp envelope + mod envelope + two LFOs + amp/pan, all driven by the
   resolved region parameters and live modulation.
2. **Voice manager** — polyphony cap, allocation, **voice stealing**
   (oldest/quietest), note-on/note-off, **sustain pedal (CC64) hold**, and
   **exclusive/choke groups** (SF2 `exclusiveClass`, SFZ `group`/`off_by`). The
   sequencing PR's drum demo already prototypes a `ChokeableVoice` with deferred
   per-frame fade-out — a good reference for click-free choking.
3. **Generator accumulation (SF2)** — correct layering of instrument-zone
   (additive) and preset-zone generators, additive-vs-absolute distinction per
   generator, global zones.
4. **Modulator engine** — the part that makes SF2 sound *right*: the **10 SF2
   default modulators** (velocity→attenuation, velocity→filter, pitch-wheel→
   pitch, etc.) plus file-defined ones, with sources (CC/NRPN/velocity/key/
   pressure/pitch-wheel), transform curves (linear/concave/convex/switch),
   bipolar/unipolar polarity. SFZ's modulation (`*_oncc`, `*lfo_*`, EG-to-target)
   maps onto the same routing list. Depends on §4.1's modulator-field fix.
5. **MIDI channel state** — program/bank select, pitch-bend + RPN 0 bend range,
   CC1 mod, CC7 volume, CC10 pan, CC11 expression, CC64 sustain, all-notes-off,
   reset-all-controllers, NRPN.
6. **Multi-timbral routing** — 16 channels, **channel 10 = drums** (GM/GS),
   bank-select semantics distinguishing drum vs. melodic banks.
7. **`ISampleProvider` front-end** — the engine is a standard sample provider so
   it composes with the existing output stack and effect chains.

## 7. Complex features / format semantics to honour

**SF2 generators that must be *honoured*, not just parsed:**

- Loop modes: no-loop / continuous / **loop-until-release-then-tail**
  (`sampleModes`), with loop points plus the start/end/startloop/endloop address
  offset generators (coarse & fine).
- Pitch: `overridingRootKey`, `coarseTune`, `fineTune`, `scaleTuning`.
- Filter: `initialFilterFc`, `initialFilterQ`, mod-env→filter, mod-LFO→filter.
- Envelopes: full DAHDSR for amp & mod, plus `keynumTo{Vol,Mod}Env{Hold,Decay}`
  key tracking.
- Amp: `initialAttenuation` (cB), `pan`, velocity/keynum overrides,
  `chorusEffectsSend`, `reverbEffectsSend`.
- `exclusiveClass` (drum choke).

**SFZ opcodes — tiered scope:**

- **Tier 1 (must-have):** `sample`, `lokey`/`hikey`/`key`, `lovel`/`hivel`,
  `pitch_keycenter`, `tune`, `transpose`, `volume`, `pan`, `offset`/`end`,
  `loop_mode`/`loop_start`/`loop_end`, `trigger`
  (attack/release/first/legato), `group`/`off_by`/`off_mode`, `ampeg_*`,
  `pitch_keytrack`, `cutoff`/`resonance`/`fil_type`, `amp_veltrack`,
  `polyphony`.
- **Tier 2 (expected by real libraries):** keyswitches
  (`sw_lokey`/`sw_hikey`/`sw_last`/`sw_default`), round-robin
  (`seq_position`/`seq_length`), random layers (`lorand`/`hirand`), CC gating
  (`locc`/`hicc`, `on_loccN`/`on_hiccN`), `rt_decay`, key/velocity crossfades
  (`xfin_*`/`xfout_*`/`xf_keycurve`), `fileg_*`/`pitcheg_*`,
  `amplfo_*`/`fillfo_*`/`pitchlfo_*`, `eq1/2/3_*`, `effect1`/`effect2` sends,
  velocity curves.
- **Out of scope (document explicitly):** full ARIA/v2 flex EGs, `<curve>`
  tables, advanced loop-crossfades. Pick a line and state it in `Docs/Sampler.md`.

## 8. Demos (Layer 3)

### 8.1 Live MIDI player
`MidiIn` (from `NAudio.WinMM`) → sampler engine → `WasapiPlayer`. Load an SF2 or
SFZ, pick a program, play from an attached MIDI keyboard. Subjective-quality and
latency evaluation tool.

### 8.2 Offline MIDI-file → WAV render
`MidiFile` → `EventTimeline` → sampler engine → `WaveFileWriter`, driven
sample-accurately by the sequencing core. **Dependency:** the sequencing PR
(#1324) deliberately *defers MIDI-file ingestion* — so this needs a new
`MidiFile`→`EventTimeline` loader (using the `MusicalTime.RescaleFromPpq`
boundary helper they added for exactly this). Discrete, well-bounded task.

**Decision (scheduling): build this on the sequencer, not a standalone
tick-walk.** A self-contained offline renderer is tempting (it would be
independently mergeable and give audible output sooner), but offline render is
the one consumer that doesn't exercise the sequencer's real-time value
(`Transport`, `EventBufferQuery`, `SequencedSampleProvider`), so a standalone
version would duplicate the tick→time math only to be superseded. We instead
**wait for #1324 to merge to `main`**, then build the `MidiFile`→`EventTimeline`
ingestion (step 6) on top of it, giving offline render, live playback and the
VST3 host one canonical timing model. The cost is no audible output from the
sampler until the sequencer lands.

### 8.3 Single-sample / recording keyboard instrument
Open a WAV (or record a snippet via the capture stack), auto-map it across the
keyboard at a chosen root key, **display the waveform**, and adjust
**start / end / loop-start / loop-end** and loop mode interactively — hearing the
change immediately because edits flow into the live resolved instrument. The
simplest producer of the format-neutral model, and a useful instrument by
itself. WPF demo, consistent with the existing demo suite.

## 9. Port vs. build, and licence policy

Same policy as the effects suite (see [`EffectsDesign.md`](EffectsDesign.md) §5).
**Default: build.** Voice management, envelopes, interpolation, generator/opcode
resolution, and modulation routing are well-understood and best written as
idiomatic, allocation-free, AOT-safe C# — and double as synth building blocks.
**Port selectively** only where an algorithm is genuinely hard and a strong
permissive reference exists, "as algorithm, not transliteration." NAudio is
**MIT**: only MIT/BSD/Zlib/public-domain sources may be ingested, attributed
in-file and in `THIRD-PARTY-NOTICES.txt`. GPL/LGPL/proprietary synth/sampler
code (FluidSynth is **LGPL**, sfizz is **BSD-2** — usable as a *spec/behaviour
reference* only, never copied) stays out. The SF2.04 spec and the SFZ opcode
reference are the primary sources.

## 10. Suggested PR sequence (all on `feature/naudio3-sampler`)

Critical-path risks are concentrated in two places — the per-voice interpolating
oscillator (small but exacting) and the SF2 modulator engine (spec-heavy).
Sequenced cheapest-useful-first:

1. **DSP primitives → `NAudio.Core`:** interpolating sample reader, DAHDSR,
   LFO/filter extensions, note/cents/cB/timecent math. Pure, unit-testable.
   **DONE.**
2. **SF2 resolved-instrument model** + finish `sm24` and modulator-field parsing
   in the parser. **DONE** (resolved model in `NAudio.Core/FileFormats/SoundFont`).
3. **`NAudio.Sampler` engine v1:** voice, voice manager, generator accumulation,
   channel state, loop modes — *no modulators yet* (default attenuation/pan/
   filter only). Drum one-shots + GM melodic playable as an `ISampleProvider`.
   **DONE.** New `NAudio.Sampler` package: `SoundFontSampler` (`ISampleProvider`),
   internal `SamplerVoice` (pitch from root-key/tune, the three loop modes,
   DAHDSR amp envelope, static per-voice low-pass, velocity/attenuation gain,
   equal-power pan), `MidiChannelState` (program/bank, pitch-bend, sustain
   pedal, volume/expression), voice stealing and exclusive-class choke. Verified
   by deterministic offline-render tests (pitch ratio, looping, release decay,
   sustain pedal, choke, pan, key-range, polyphony).
4. **Modulator engine** (default + file modulators) — the "sounds correct" PR.
   **Part A DONE:** continuous *generator-driven* modulation — the two per-voice
   LFOs (modulation + vibrato, using `Lfo`'s start-delay) and a second
   `DahdsrEnvelope` as the modulation envelope, routed to pitch
   (`modLfoToPitch`, `vibLfoToPitch`, `modEnvToPitch`), filter cutoff
   (`modLfoToFilterFc`, `modEnvToFilterFc`) and volume (`modLfoToVolume`).
   Modulation runs at a control rate (64-sample sub-blocks) while the sources
   advance per sample; the filter is retuned per block via the new
   state-preserving `BiQuadFilter.UpdateLowPassFilter` (added to `NAudio.Core`
   so it serves any modulated filter). Verified by render tests (vibrato,
   tremolo, mod-env filter sweep). **Part B DONE:** the SF2 modulator *list* —
   the resolver now carries zone modulators onto `SoundFontRegion`
   (`InstrumentModulators`/`PresetModulators`, global+local concatenated for the
   §9.5 combination), the parser's `ModulatorType` ctor is public and exposes
   `RawValue`, and `NAudio.Sampler` adds the engine: the implicit default
   modulators (§8.4), file-defined modulators merged per §9.5 (local supersedes
   global by identical routing; instrument modulators are absolute and replace
   defaults, preset modulators are additive), the four source curves
   (`SoundFontModulatorMath`, linear/concave/convex/switch with direction +
   polarity) and the `Linear`/`AbsoluteValue` transforms. Modulators are
   evaluated at control rate against live MIDI controllers (velocity, key,
   CC1/7/10/11/91/93, channel pressure, pitch wheel) and summed per destination
   onto the generator values for attenuation, filter cutoff, pan and the
   pitch/filter/volume routings. The provisional `v*v` velocity curve is gone,
   replaced by the §8.4.1 velocity→attenuation default modulator (whose concave
   shape, by construction, reproduces the old near-square-law response).
   **Deferred within Part B:** the pitch-wheel default modulator (§8.4.10) is
   realised by the channel pitch-bend path rather than the modulator list (one
   source of truth for bend); reverb/chorus send destinations are evaluated but
   not yet rendered (await the §5 send-bus); poly-pressure and NRPN sources are
   not yet tracked.
5. **Effects send-bus** (reverb/chorus sends) reusing `NAudio.Effects`.
   **DONE.** New generic `NAudio.Effects.SendBus` (shared effect with a send
   buffer and wet return); `SoundFontSampler` owns one reverb (`ReverbEffect`)
   and one chorus (`ChorusEffect`) bus, each voice mixes a portion of its panned
   signal into them per the SF2 `reverbEffectsSend`/`chorusEffectsSend`
   generators (and the CC91/CC93 default modulators, now consumed), and the wet
   return is summed into the mix each block so tails outlast the notes. The
   effects are exposed (`SoundFontSampler.Reverb`/`Chorus`) for tuning or bypass.
   Verified by `SendBus` unit tests and reverb-send/tail render tests.
6. **MIDI-file → `EventTimeline` ingestion** (closes the sequencer gap) →
   enables the offline render demo. **DONE.** Now that the sequencer (#1324) is
   on `main`: `MidiFileSequence` loads a `MidiFile` onto an
   `EventTimeline<MidiEvent>` at the canonical PPQ (`MusicalTime.RescaleFromPpq`)
   and builds a `StaticTempoMap` from its `SetTempo` events;
   `SequencedMidiInstrument` drives a `SamplerEngine` from a `Transport`,
   dispatching each block's MIDI events to the sampler at their exact frame
   offset (rendering the sampler in segments between events, so timing is
   sample-accurate within the block — not block-quantised like the
   dispatcher-spawns-a-provider model). `OfflineMidiRenderer` renders a sequence
   to a float buffer or a WAV file, faster than real time and with no audio
   hardware. This is the first end-to-end "MIDI in → audio out", verified by a
   deterministic timing test (silence before the note, sound during).
7. **SFZ parser + mapping** (Tier 1, then Tier 2). **Tier 1 DONE.**
   **7a DONE — text/structure layer** (`NAudio.Core/FileFormats/Sfz`,
   namespace `NAudio.Sfz`): `SfzParser` handles `//` and `/* */` comments, the
   `#define`/`$variable` preprocessor, `#include` (via a pluggable
   `ISfzIncludeResolver`, default `FileSfzIncludeResolver`), the section grammar
   (including sample paths with spaces and multiple opcodes per line), and
   flattens the `<global>`/`<master>`/`<group>`/`<region>` hierarchy into
   `SfzRegion`s with merged opcodes and typed accessors. `<control>`
   `default_path` (applied in document order), `note_offset` and `octave_offset`
   are surfaced on `SfzInstrument`. Pure text, no engine dependency, unit-tested.
   **7b DONE — opcode semantics** (`SfzMappedRegion`, `SfzNoteName`): the
   Tier-1 opcodes interpreted into typed, engine-ready values — key/velocity
   ranges (note names like `c#4` resolved with the c4=60 convention and the
   `note_offset`/`octave_offset` shifts applied), `pitch_keycenter`/`tune`/
   `transpose`/`pitch_keytrack`, `volume` (dB), `pan` (±1), `ampeg_*` (seconds,
   sustain 0–1), `cutoff`/`resonance`/`fil_type`, `loop_mode`/offsets,
   `trigger`, `group`/`off_by`/`off_mode`, `amp_veltrack`, `polyphony`. Pure and
   unit-tested; the SFZ counterpart to the SoundFont generator model.
   **7c DONE — neutral model, sample loading, engine integration:**
   `SampleData` + `SamplerRegion` are the format-neutral region the voice plays
   (parameters carried as `SoundFontGenerators` in engine units); `SamplerVoice`
   reads only that, with an SFZ `amp_veltrack` velocity term. SoundFont and SFZ
   both project onto it: `SoundFontSampler` slices the shared sample pool;
   `SfzRegionProjector` + `ISfzSampleLoader`/`FileSfzSampleLoader` load each SFZ
   sample (WAV, mono-downmixed) and translate the opcodes to generators. The
   shared voice management (pool, stealing, choke, sustain, channel state,
   reverb/chorus sends, the `Read`/MIDI loop) lives in a `SamplerEngine` base;
   `SoundFontSampler` and the new `SfzSampler` subclass it and only supply the
   regions for a note. SFZ plays end-to-end as an `ISampleProvider`
   (`SfzSampler.FromFile`).
   **Remaining:** Tier-1 polish (release/first/legato triggers, true stereo
   samples, high/band-pass filters, cross-group `off_by`, one-shot ignoring
   note-off — see §11.1), FLAC/Ogg sample loading via `NAudio.SoundFile`, then
   Tier 2 opcodes (keyswitches, round-robin, CC gating, crossfades, extra
   LFOs/EGs).
8. **Single-sample instrument + auto-mapper** model. **DONE (model).**
   `SingleSampleInstrument` takes one mono buffer (loaded WAV or a recorded
   snippet), auto-maps it across the keyboard at a chosen root key, and exposes
   editable start/end, loop start/end and mode, tuning, gain, pan, velocity
   tracking and an amplitude envelope — projecting onto the same neutral
   `SamplerRegion`. `SingleSampleSampler` plays it through the shared
   `SamplerEngine` and rebuilds the region per note so edits are heard on the
   next note; `SingleSampleSampler.FromWaveFile` is the one-liner entry point.
   The shared `WaveSampleLoader` (mono down-mix) now backs both this and the SFZ
   WAV loader. The interactive waveform/loop editor UI is the step-9 demo.
9. **Demos:** live MIDI, offline render, single-sample/recording editor.

**Testing.** Lean on deterministic offline render → golden-WAV comparisons (the
drum demo's "render matches live playback" path in #1324 is the model), plus
pure unit tests on interpolation, envelopes, generator accumulation, and
modulator transforms. Mark anything needing real hardware
`TestCategory=IntegrationTest`.

## 11. Deferred / open work

- **DLS, Kontakt, EXS, Decent Sampler and other formats** — out of scope; SF2 +
  SFZ cover the open-format need. A new front-end onto the resolved model is the
  extension point if demand appears.
- **SoundFont *writing*** and SFZ export — separate authoring feature.
- **Disk streaming for large libraries** — v1 loads samples into memory
  (`CachedSound`-style). Streamed voices are a later optimisation.
- **Windowed-sinc interpolation tier** for `InterpolatingSampleReader`. The
  shipped reader offers None/Linear/Hermite, with Hermite as the transparent
  default. A table-driven windowed-sinc (e.g. Lanczos/Blackman, 8–32 taps) is a
  higher-quality option worth adding *if* demand appears — it mainly helps
  extreme *upward* pitch shifts (imaging near Nyquist) and offline rendering,
  where the extra taps per sample are affordable. Cost: more CPU and a wider
  read window, so the loop-boundary wrap needs more guard samples either side of
  the loop points. `WdlResampler` already has a windowed-sinc kernel that can
  serve as the math reference (it is fixed-ratio/block-based, so it can't be
  dropped into the per-sample varying-rate reader directly).
- **Advanced SFZ (flex EGs, `<curve>`, full ARIA set)** — Tier 3, demand-driven.
- **SFZ Tier 2** — the next tranche of opcodes once Tier 1 is solid: keyswitches
  (`sw_lokey`/`sw_hikey`/`sw_last`/`sw_default`), round-robin
  (`seq_position`/`seq_length`), random layers (`lorand`/`hirand`), CC gating
  (`locc`/`hicc`, `on_loccN`/`on_hiccN`), key/velocity crossfades (`xfin_*`/
  `xfout_*`), `rt_decay`, the filter/pitch EGs and the `amplfo_*`/`fillfo_*`/
  `pitchlfo_*` LFOs, `eq1/2/3_*`, and `effect1`/`effect2` sends. Maps onto the
  same modulation/region model the SF2 side already uses.
- **SFZ Tier-1 finish** — the documented Tier-1 shortcuts in §11.1 (stereo
  samples, release/first/legato triggers, high/band-pass filters, cross-group
  `off_by`, one-shot note-off, FLAC/Ogg loading).
- **Built-in algorithmic-reverb send default** — start by routing to the
  existing `ReverbEffect`/`FdnReverbEffect`; a sampler-tuned default is polish.

### 11.1 Carried-forward implementation gaps

Small, deliberate shortcuts taken in shipped steps, tracked here so they are not
forgotten:

- **Pitch-wheel modulation (§8.4.10)** is realised by the channel pitch-bend
  path (`MidiChannelState.PitchBendRatio`), *not* the modulator list. Two
  consequences: a *file-defined* modulator whose destination is initial pitch is
  ignored, and pitch-bend range is the channel default rather than driven by the
  pitch-wheel-sensitivity RPN as a modulator amount source. Revisit if a bank
  relies on a custom pitch modulator.
- **Poly (per-note) pressure** as a modulator source is not tracked — it
  evaluates as 0. Channel pressure, velocity, key, CC and pitch-wheel sources
  all work. **NRPN** is likewise not yet decoded.
- **Modulator destinations outside `GeneratorEnum`** (e.g. the SF2 "initial
  pitch" virtual destination) are ignored by the engine — only real generator
  destinations are summed.
- **Very short one-shot samples** (a handful of frames, non-looped) can fail to
  feed audio through `InterpolatingSampleReader` (found via a reverb-send test
  that used a 4-frame one-shot). Normal-length and looped samples are
  unaffected; the reader's end/guard handling for tiny one-shots wants a look.
- **SFZ Tier-1 gaps (step 7c):** regions with a `release`/`first`/`legato`
  trigger are dropped (only `attack` plays); stereo samples are down-mixed to
  mono (the voice is mono-source + pan); only low-pass `fil_type` is honoured;
  `off_by` maps to an exclusive class only when `group == off_by`; `one_shot`
  loop mode still respects note-off rather than always playing to the end; and
  only WAV samples load (FLAC/Ogg await `NAudio.SoundFile`).
- **(Closed)** ~~Reverb/chorus sends evaluated but not rendered~~ — done in
  step 5 (the send-bus).

## 12. Non-goals for NAudio 3

- A full DAW / arrangement layer — the sequencing core schedules; the sampler
  plays. Composition tooling is out.
- A plugin host or VST3 sampler shell — the engine is designed to *be hostable*
  (the VST3 POC consumes the same sequencing/effects APIs), but the host is a
  separate concern.
- Bit-exact reproduction of any specific reference synth — the bar is
  "musically faithful to the SF2/SFZ spec," not "sample-identical to
  FluidSynth/sfizz."
