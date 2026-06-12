# NAudio 3 — Sampler: architecture and design

How the NAudio 3 sampler is put together and why. For how to *use* it — loading
instruments, playing MIDI live or offline, and the definitive list of supported
and unsupported SF2/SFZ features — see [Sampler.md](../Sampler.md), which is
published on the docs site (this document is internal).

Related reading: [EffectsDesign.md](EffectsDesign.md) (the effects suite the
send buses reuse), [Sequencing.md](Sequencing.md) (the scheduling core that
MIDI-file playback rides on), and
[NAudio3AssemblyLayoutPlan.md](NAudio3AssemblyLayoutPlan.md) (packaging
principles).

## 1. Goal

A first-class software sampler for NAudio 3:

- **Plays SoundFont 2 and SFZ instruments** — from one-shot drum hits through
  multisampled melodic instruments to a full multi-timbral General MIDI bank —
  plus a third front-end that maps a **single sample** across the keyboard with
  editable loop points (the sampler's "hello world", and a useful instrument in
  its own right).
- **Is driven by MIDI**, live (`MidiIn`, an on-screen keyboard) and offline (a
  `MidiFile` rendered to WAV faster than real time).
- **Is pure managed, cross-platform and AOT-safe** — no Windows dependency in
  the engine; audio out and MIDI in are supplied by the host through the
  existing backends.
- **Fidelity bar:** musically faithful to the SF2.04 spec and the SFZ opcode
  reference — not sample-identical to any particular reference synth
  (FluidSynth, sfizz). Known deviations are documented in
  [Sampler.md](../Sampler.md).

## 2. Where things live

The guiding rule: **`NAudio.Sampler` contains only the pieces that are
sampler-specific**; anything reusable in a wider context lives in the package
that already owns its category.

| Piece | Home | Why |
|---|---|---|
| DSP kernels: `InterpolatingSampleReader`, `DahdsrEnvelope`, `Lfo`, `BiQuadFilter` extensions, `SynthMath` | `NAudio.Core` (`NAudio.Dsp`) | Instrument-agnostic primitives, shared with any future synth |
| `SendBus` (aux send/return plumbing) | `NAudio.Core` (`NAudio.Effects`) | Generic effect routing; the effects themselves already existed |
| SoundFont parser + resolved-instrument layer (`SoundFontInstrumentResolver`, `SoundFontRegion`, `SoundFontGenerators`, `SoundFont.ReadSampleDataFloat`) | `NAudio.Core` (`NAudio.SoundFont`) | Format I/O plus pure SF2 interpretation — useful to anyone reading or converting fonts, no synthesis dependency |
| SFZ parser + semantic layer (`SfzParser`, `SfzMappedRegion`) | `NAudio.Core` (`NAudio.Sfz`) | Same principle: text/structure and typed opcode semantics are format I/O |
| MIDI playback hosts: `IMidiInstrument`, `MidiFileSequence`, `SequencedMidiPlayer`, `OfflineMidiRenderer`, `LiveMidiInstrument` | `NAudio.Midi` | `NAudio.Core` stays MIDI-agnostic; the hosts are *instrument*-agnostic so other `IMidiInstrument` implementations (e.g. a hosted VSTi) reuse them unchanged |
| The engine: `SamplerEngine`, `SamplerVoice`, the region model, the modulator engine, the three front-ends | **`NAudio.Sampler`** | This *is* the sampler |
| Demo panels (SoundFont/MIDI player, live sampler, single-sample editor) | `NAudioWpfDemo` | Hosts composing the engine with the output/input stack |

`NAudio.Sampler` is a portable package: `net9.0`, AOT-compatible, depending on
`NAudio.Core` + `NAudio.Midi` + `NAudio.SoundFile` (the libsndfile wrapper that
decodes FLAC/Ogg SFZ samples; absent at runtime, those regions are skipped
gracefully). It versions in lockstep with the other NAudio packages.

## 3. Architecture — three layers

Mirrors the effects framework's "kernels vs streaming adapter" split:

1. **DSP kernels** (`NAudio.Core`) — plain classes that process their own
   buffers with no MIDI or format awareness.
2. **The synthesis engine** (`NAudio.Sampler`) — a `SamplerVoice` wires the
   kernels together; `SamplerEngine` owns the voice pool and MIDI dispatch; a
   format-neutral *region model* feeds them. The engine is one
   `ISampleProvider` (stereo float), so it drops straight into the existing
   graph, and one `IMidiInstrument`, so the `NAudio.Midi` hosts can drive it.
3. **Hosts and demos** — live playback, offline render, the waveform editor.

## 4. The format-neutral region model

The key design decision. Both formats normalise onto one internal model so the
voice engine never sees a format:

- An **instrument** is a set of **regions**. A region has a sample reference
  (shared data + addressing + loop points + root key), a key/velocity
  rectangle, a fully resolved parameter set, and a list of modulator routings.
  One note-on may start **several regions at once** (layers, stereo pairs,
  velocity splits) — each becomes a voice.
- **SF2 generator units are the neutral currency**: `SamplerRegion` carries its
  parameters as `SoundFontGenerators` (cents, timecents, centibels), because
  they are the most precisely specified unit system and the voice can be
  written once against them. SFZ concepts with no SF2 slot get dedicated
  fields on `SamplerRegion` instead (linear gain boost, velocity curves,
  key/velocity crossfades, triggers, choke groups with `off_mode`, per-region
  polyphony, CC gates, keyswitches, EQ bands, embedded WAV loops).
- **Three producers project onto it:**
  - **SF2**: `Preset.ResolveRegions()` (in `NAudio.Core`) applies the §9.4
    generator model — instrument-zone values absolute over spec defaults,
    preset-zone values additive, global zones, range intersection — and
    `SoundFontSampler` projects the result, slicing one shared `float[]`
    sample pool (no per-region copies).
  - **SFZ**: `SfzParser` flattens the section hierarchy; `SfzMappedRegion`
    (in `NAudio.Core`) interprets opcodes into typed values in natural SFZ
    units; `SfzRegionProjector` (in `NAudio.Sampler`) translates them to
    engine units and loads samples via `ISfzSampleLoader`.
  - **Single sample**: `SingleSampleInstrument` is the simplest producer — one
    buffer, editable start/end/loop/envelope, re-projected when edited.
- **Deliberate simplification:** the voice has two LFOs and one modulation
  envelope; SFZ's per-target EGs/LFOs (`fileg_*`, `pitchlfo_*`, …) share those
  slots, so a region using several with different rates shares the rate with
  independent depths. Dedicated per-target sources are the upgrade path if a
  real bank needs them.

## 5. The voice and the engine

Per-voice signal chain: interpolating reader (Hermite by default, double
phase accumulator, the three SF2 loop modes, optional loop-seam crossfade;
stereo samples run a second reader in lockstep) → per-channel resonant biquad
(low/high/band-pass/band-reject, Q gain-compensated per §8.1.2) → up to three
peaking-EQ bands → per-sample amplitude envelope × control-rate gains →
equal-power pan → dry mix plus reverb/chorus sends.

**Control-rate modulation.** Pitch, filter, volume and modulator evaluation run
on 64-sample control blocks; the modulation sources advance by bit-exact block
`Advance` calls. The control phase is carried across `Read` segment boundaries,
so a render chopped into arbitrary segments (as event-dense sequenced playback
does) is bit-identical to a monolithic one.

**The modulator engine** implements the ten §8.4 default modulators and
file-defined modulators merged per §9.5, with the four source curves and both
transforms. Routings are split at build time into static sources (velocity,
key — evaluated once at note-on) and dynamic sources (CC, pitch wheel, channel
pressure — re-evaluated only when the channel state's version stamp changes).
Modulators with unknown, unsupported or spec-prohibited sources are disabled
entirely, per §7.4.

**The engine** owns everything cross-voice: the note-on gate chain (per-key
region buckets, trigger type, keyswitches, CC gates, random layers,
round-robin, crossfade gain), exclusive/choke groups (sparing same-dispatch
siblings, honouring `off_mode`), the sustain pedal (re-strike supersedes a
parked note; pedal-up fires release triggers with correct `rt_decay` timing),
audibility-ranked voice stealing with a short fade summed over the new note,
RPN 0 bend-range decoding, and the percussion rules (forced channel 10, the
GS/XG bank-MSB heuristic, melodic bank fallback that never lands on a drum
kit).

## 6. Performance invariants

These are design constraints, not incidental optimisations — they are guarded
by tests and should be preserved by future changes:

- **Zero heap allocation on the audio thread in steady state** (a CI test
  asserts exactly zero bytes across warm note-on/render/note-off cycles).
  Voices are pooled with persistent readers, filters and EQ; presets are
  resolved and projected at construction; region lookups go through cached
  per-key indexes with O(1) empty checks for release/CC triggers; sequenced
  playback queries the `EventTimeline` through lock-free immutable snapshots.
- **Per-sample work is minimal**: transcendentals only at control rate,
  multiply-per-sample envelopes, a block fast path in the reader over the
  contiguous safe window (per-sample code only near loop seams and ends),
  filter retunes skipped when nothing changed, idle send buses skipping their
  effects, and silent-sustain voices reaped.
- **Fast paths must be provably equivalent**: every block-processing path has
  a seeded equivalence test asserting bit-identical output against its
  per-sample reference, and the engine suite asserts exact rendered
  waveforms — so an optimisation cannot silently change the sound.
- Manual `[Explicit]` benchmarks live in `NAudio.Sampler.Tests`
  (`SamplerBenchmarks`): throughput for 64 busy voices and note-churn
  allocations. Run them in Release before and after engine work.

## 7. Testing approach

Everything is deterministic and hardware-free: SF2 binaries are built in
memory by test builders, SFZ samples come from stub loaders, and behaviour is
asserted on rendered output (pitch ratios, envelope timing, choke fades,
modulation depth). Spec-sensitive math (generator accumulation, modulator
curves, unit conversions, the 16/24-bit sample decode) has direct unit tests
with SF2 section citations. The full suite runs on Linux CI in seconds; only
the benchmarks are excluded (`[Explicit]`).

## 8. Deliberate v1 constraints

- **Engine subclassing is internal.** `SamplerEngine` is `public abstract`,
  but the region supply is `private protected` and the region model is
  `internal`: the model's shape (SF2-unit currency plus SFZ side-fields) is
  not frozen yet, and publishing it would freeze it prematurely. Widening
  later is non-breaking; narrowing back would not be. External composition
  happens at the `IMidiInstrument` seam, and new formats are added inside
  `NAudio.Sampler` as new producers of the region model.
- **The engine is single-threaded.** `Read` and all MIDI entry points must be
  called from one thread; `LiveMidiInstrument` is the lock-free cross-thread
  bridge.
- **Samples decode fully into RAM** (float, so ~2× the 16-bit footprint) —
  glitch-free and simple; no disk streaming.
- **Spec-literal levels**: SF2 attenuation is applied at full spec value (no
  FluidSynth-style 0.4× "EMU" scaling), and all filter shapes are single
  2-pole biquads. These and the other known deviations are listed in
  [Sampler.md](../Sampler.md).

## 9. Future enhancements (not in this first round)

- **Windowed-sinc interpolation tier** — mainly helps extreme upward pitch
  shifts and offline rendering; needs wider guard handling around loop seams.
- **Disk streaming** for multi-gigabyte libraries (changes the
  everything-in-RAM contract above).
- **NRPN decoding and SF2 §9.6 real-time generator control** (NRPN selection
  currently only blocks RPN data entry).
- **Full GS/XG SysEx mode detection** (the bank-MSB 120/127 heuristic ships
  now; SysEx is currently dropped by `MidiFileSequence`).
- **Dedicated per-SFZ-EG/LFO modulation sources**, and **poly (per-note)
  pressure** as a modulator source.
- **SoundFont writing / SFZ export** — a separate authoring feature.
- **Further formats** (DLS, Decent Sampler, …) as new front-ends onto the
  region model, and a **`NAudio.Synth`** sibling built on the same
  `NAudio.Core` primitives.
