# NAudio 3 — Sequencing

**Goal:** Provide a small set of portable primitives that convert musical time (bar/beat/tick, with a tempo map) into a real-time event schedule, so that multiple consumers — a drum machine, a SoundFont/sfz sampler, a hosted VST3, and live MIDI-out — can all drive themselves from the same timing core.

This document captures the decisions and the plan so that work-in-progress consumers (notably the VST3 hosting proof-of-concept on a separate local branch) can react before the API hardens.

## Status

| Phase | State | Notes |
| --- | --- | --- |
| 0. Design + this doc | ✅ done | Decisions captured below. Branch: `feature/sequencing-core`. |
| 1. Core primitives + drum-machine demo rebuilt on them | ✅ done | Primitives in `NAudio.Core/Sequencing/`; 58 unit tests passing in `NAudio.Core.Tests/Sequencing/`. Drum-machine demo carries both engines side by side with a "Use legacy PatternSequencer" toggle, a swing knob, and a Render-to-WAV command. |
| 2. MIDI-file ingestion → `StaticTempoMap` + `EventTimeline` | future | Unlocks "render `.mid` to WAV". |
| 3. SoundFont / sfz sampler consumer | future | Likely also lands a leaner `ScheduledMixer`. |
| 4. Wall-clock driver + `IMidiOutput` sink (WinMM + WinRT) | future | The non-audio consumer. |
| 5. VST3 offline render glue | future | Falls out as the intersection of (2) + a working VST3 host. |

## Use cases driving the design

1. **Drum machine demo** (WPF) — step pattern, tempo knob with live changes, swing knob; plays through `WasapiOut`, can also render the pattern to a WAV file.
2. **Drum machine → MIDI out** — same source-of-truth pattern, dispatched as MIDI note messages to a `IMidiOutput` (WinMM or WinRT).
3. **VST3 hosting** — feed MIDI events into the plugin's `process()` block with sample-accurate offsets; populate `ProcessContext` (tempo, time signature, project time in samples / quarter notes / bars) from the same tempo map. Works both real-time (`WasapiOut` pulls) and offline (render loop pulls).
4. **Sampler + MIDI file** — load a `.mid`, play it live through a SoundFont/sfz sampler out the speakers; the same code path renders it offline to WAV with every note event landing at its precise tick.

The variability is in (a) what's at the *source* (a pattern, a `.mid` file, a live keyboard), (b) what drives the *clock* (sample-block pull, wall-clock timer), and (c) what consumes the *events* (a mixer of one-shots, a MIDI-out device, a VST3 process call, a sampler voice allocator). The shared substance underneath all of them is the same.

## Key insight: sample position is the master clock

For everything that produces audio — drum machine, sampler, VST3 (real-time or offline) — NAudio's existing pull-based `ISampleProvider.Read(Span<float>)` model already supplies an ideal clock:

- The soundcard (real-time) or a render loop (offline) pulls *N* samples.
- The sequencer answers: *"which events fall in `[currentSample, currentSample + N)`, and at what sub-buffer sample offset?"*.
- Events are dispatched to the consumer with that offset.

This makes **offline WAV render and real-time playback the same code path** — the only difference is who calls `Read` and whether it's rate-limited by hardware. VST3 fits perfectly too: `process()` natively takes a list of events with sample offsets within the block, which is exactly the query output.

The one use case that does *not* have samples is real-time MIDI-out to a hardware/WinRT device. There the clock is a wall-clock timer. The sequencing core must therefore be **agnostic about what advances time**; the timeline just answers "what events occur in this time window," and a transport/clock layer (sample-block driver *or* wall-clock driver) supplies the window.

## Key decisions (with rationale)

### 1. Layered primitives, not one monolithic `Sequencer` class

Four collaborating concerns, each separately testable:

1. **`TempoMap`** — pure math, maps musical position (ticks) ↔ seconds ↔ samples.
2. **`EventTimeline<T>`** — ordered set of timed events with a generic payload, queryable per range.
3. **`Transport`** — tracks playback state and current position (in both ticks and samples to avoid drift); owns the tempo map; holds optional loop region.
4. **Sinks** — consume events: trigger a sample into a mixer, send to `IMidiOutput`, feed a VST3 process block, drive a sampler voice allocator.

The event payload is generic and the driver/sink are pluggable; that's where the flexibility across the four use cases comes from.

### 2. Lives in `NAudio.Core`, under a `Sequencing` namespace

Everything in the primitives is pure data + math with no OS dependency, so it stays portable `net9.0`, AOT-friendly, and reuses the existing `MixingSampleProvider` / `OffsetSampleProvider` / `ISampleProvider` substrate without crossing assembly boundaries. Device sinks (`IMidiOutput` wall-clock driver, WASAPI playback) stay in their existing assemblies and just consume these primitives.

A separate `NAudio.Sequencing` package was considered but rejected for the first round: there's no platform split to model, and the primitives sit naturally next to `MixingSampleProvider`.

### 3. Full musical model from day one

Bar/beat/tick addressing with a time-signature map is in scope from the start, not deferred. This matches how users actually think ("measure 4, beat 2, tick *nnn*") and avoids an API break later when seek-to-musical-position becomes necessary.

Internally everything is stored as `long` ticks at a single canonical PPQN (proposed: **960**, which is the common DAW default and divides cleanly into common subdivisions). Bar/beat/tick is a *view* computed via the time-signature map, never a storage format — this avoids the "what does beat 2 mean if the time signature changed?" ambiguity.

### 4. Tempo ramps: design-only, not implemented

The `ITempoMap` API carries a "segment kind" enum (`Step`, future `LinearBpm`) from the start so continuous ramps can be added later without breaking the public API. The first cut implements only stepped tempo changes. Real-world ramps in MIDI files are almost always faked as many small step changes, which the first cut handles fine.

When `LinearBpm` lands, the tick ↔ seconds conversion for a ramp segment uses the closed-form integral; design notes belong with that future PR, not this one.

### 5. Two `ITempoMap` implementations

- **`LiveTempoMap`** — single current tempo, mutable, future-only changes. Past tempo is frozen as it's observed (so seek-back stays consistent with what was played). Drives the drum machine's tempo knob.
- **`StaticTempoMap`** — immutable piecewise segments built from a sorted set of `TempoEvent`s. Drives MIDI-file playback. Pre-computes cumulative seconds at each segment boundary so any tick → seconds lookup is O(log n).

Same interface, same consumers — the consumer doesn't care which is plugged in.

### 6. Position transforms (swing, quantize, humanize) are a layer above the timeline

A transform exposes two things: `effectiveTick = transform(nominalTick)`, *and* a `MaxShiftTicks` bound. The bound is essential for correctness (see below). Swing is the first concrete transform; quantize and humanize fit the same shape.

### 7. Swing must be applied *before* the range filter

This was a bug in the first draft of the design. The correct query order per buffer:

1. Compute the buffer's effective tick range `[bufferStartTicks, bufferEndTicksExclusive)`.
2. Ask the transform for its `MaxShiftTicks`.
3. Query the timeline over the **expanded** nominal range `[bufferStartTicks - maxShift, bufferEndTicksExclusive + maxShift)`.
4. For each candidate event, compute `effectiveTick = transform(nominalTick)`.
5. Keep only those whose `effectiveTick` falls in the actual buffer range.
6. Convert each kept `effectiveTick` → sub-buffer sample offset via the tempo map and dispatch.

`OffsetSampleProvider` only places audio at the right sub-sample offset *within* the buffer — it cannot rescue an event that the range filter dropped. The over-scan + post-filter is what stops swing-shifted events being silently lost.

A live swing-knob change can in principle cause sub-millisecond timing wobble for an event straddling a buffer boundary at the moment of the change. That's musically imperceptible; not worth designing around.

### 8. `OffsetSampleProvider` is good enough for milestone 1, not the long-term answer

Current `OffsetSampleProvider` allocates one object per triggered note and carries a phase machine for `DelayBy` / `SkipOver` / `Take` / `LeadOut` even though only `DelayBy` is ever set in this context. For the drum-machine's trigger rate (a few voices per beat) this is a non-issue. For a polyphonic sampler playing a busy MIDI file, sustained allocation in the audio thread is exactly what Phase 7's Span-based work was trying to remove.

Plan: ship milestone 1 with `OffsetSampleProvider` unchanged (known-working pattern, lets the sequencing primitives be validated against a known-good reference), then introduce a dedicated `ScheduledMixer` inside `NAudio.Core/Sequencing/` *before* the sampler consumer lands. That mixer holds an array of `(ISampleProvider source, int samplesUntilStart)` tuples and decrements/mixes them in one tight loop — no per-trigger wrapper object, no unused state.

### 9. Thread-safety policy: matches `MixingSampleProvider` for now

Lock around mutating timeline/tempo/transport state, matching the lock used by `MixingSampleProvider.AddMixerInput`. A lock-free command queue from the UI thread to the audio thread is a reasonable future optimisation if jitter becomes measurable; not premature in milestone 1.

### 10. Stuck-notes / transport-state invariant

Seek, loop, stop, and tempo-change must guarantee that any logical note-off paired with a previously-fired note-on still gets dispatched (MIDI panic / all-notes-off semantics). Voice management itself belongs to the sink (sampler / VST3 / `IMidiOutput`), but the timeline contract is "for every note-on you delivered, the matching note-off will be delivered before this transport stops or jumps." Worth designing into the timeline now; the cost of getting it wrong later is stuck notes in user-facing demos.

## Proposed types

All under `NAudio.Core/Sequencing/`:

| Type | Kind | Purpose |
| --- | --- | --- |
| `MusicalTime` | static helpers / constants | Canonical PPQN, helpers for converting between subdivisions. |
| `BarBeatTick` | `readonly record struct` | Display + addressing only — never used as storage. |
| `TimeSignature` | `readonly record struct` | (numerator, denominator). |
| `TimeSignatureMap` | class | Ordered (tick → `TimeSignature`) entries; `BarBeatTick FromTicks(long)` + `long ToTicks(BarBeatTick)`. |
| `ITempoMap` | interface | `SecondsFromTicks`, `TicksFromSeconds`, `SamplesFromTicks`, `TicksFromSamples`, current/at-tick BPM. |
| `LiveTempoMap` | class | Single mutable current tempo; future-only changes. |
| `StaticTempoMap` | class | Immutable piecewise tempo segments; built from `IEnumerable<TempoEvent>`. |
| `Transport` | class | Position (ticks + samples), play/stop, optional musical loop region, sample rate, tempo map. |
| `SequencerEvent<T>` | `readonly record struct` | `(long Tick, T Payload)`. |
| `EventTimeline<T>` | class | Ordered events; `EventsInRange(long startTicks, long endTicksExclusive)`. Locked add/remove. |
| `IPositionTransform` | interface | `long Transform(long nominalTick)` + `long MaxShiftTicks { get; }`. |
| `SwingTransform` | class | Implements `IPositionTransform`. |
| `SequencedSampleProvider` | class | The audio bridge — wraps `MixingSampleProvider`, holds a `Transport` + `EventTimeline<ITriggerable>` (or similar payload), implements the swing-aware per-buffer query + dispatch described in §7. |

## Milestone 1

1. The types above in `NAudio.Core/Sequencing/`, with `OffsetSampleProvider`-based dispatch.
2. NUnit tests in `NAudio.Core.Tests` for `StaticTempoMap` / `LiveTempoMap` / `TimeSignatureMap` / `EventTimeline` / `SwingTransform` / `Transport` / `SequencedSampleProvider` (pure math — fast, deterministic, run on Linux).
3. Drum-machine demo rebuilt on the new primitives, exercising:
   - Live tempo change during playback.
   - Swing knob (proves the over-scan + filter ordering).
   - Transport play/stop.
   - A "Render to WAV" command that drives the identical sample-driven path offline (proves the unified clock).
4. A/B comparison against the existing `PatternSequencer` for correctness.
5. `RELEASE_NOTES.md` entry under `### Unreleased`.

## Deferred / explicitly not in milestone 1

- MIDI-file ingestion (populating `StaticTempoMap`, `TimeSignatureMap`, `EventTimeline` from a `MidiFile`). Becomes its own PR ahead of the sampler.
- The `ScheduledMixer` allocation-conscious replacement for per-trigger `OffsetSampleProvider`. Lands ahead of the sampler consumer.
- Wall-clock driver + `IMidiOutput` sink. Interface-shaped in milestone 1 only if it costs nothing.
- Continuous linear tempo ramps (`LinearBpm` segment kind).
- Sampler (SoundFont / sfz) — large feature in its own right; the sequencing slice of it is small.
- VST3 glue — the VST3 hosting work proceeds independently real-time first; the offline-render-through-VST3 use case is the intersection of (2) above and a working VST3 host, and gets wired last.

## Open questions for the VST3 POC

Anyone working on VST3 hosting should glance over the following and push back if any of them are inconvenient:

1. **`ProcessContext` population.** Does the VST3 host want the `Transport` + `ITempoMap` injected directly so it can read tempo / bar position / project time at process-block boundaries, or would it prefer a narrower per-block "context snapshot" struct passed in?
2. **Event payload type for VST3.** A raw 32-bit MIDI message (matching `IMidiOutput.Send(int)`) is the obvious common payload between MIDI-out and VST3 sinks. VST3 also supports note-expression / parameter-change events that don't fit a 3-byte MIDI message — does the POC need those in the first cut, or is "MIDI bytes only" acceptable for now?
3. **Sample-offset units.** The timeline produces offsets in samples-into-current-buffer at the consumer's sample rate. VST3 wants the same. Confirm no conversion shim is needed.
4. **Block size constraints.** Does the VST3 host need to be in control of the block size (e.g. for plugin `setupProcessing` constraints), or is it happy to render whatever block size `WasapiOut` pulls? The latter is simpler from the sequencer's side.
