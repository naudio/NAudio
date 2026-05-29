# NAudio 3 — Sequencing

**Goal:** Provide a small set of portable primitives that convert musical time (bar/beat/tick, with a tempo map) into a real-time event schedule, so that multiple consumers — a drum machine, a SoundFont/sfz sampler, a hosted VST3, and live MIDI-out — can all drive themselves from the same timing core.

This document captures the decisions and the plan so that work-in-progress consumers (notably the VST3 hosting proof-of-concept on a separate local branch) can react before the API hardens.

## Status

| Phase | State | Notes |
| --- | --- | --- |
| 0. Design + this doc | ✅ done | Decisions captured below. Branch: `feature/sequencing-core`. |
| 1. Core primitives + drum-machine demo rebuilt on them | ✅ done | Primitives in `NAudio.Core/Sequencing/`; 58 unit tests passing in `NAudio.Core.Tests/Sequencing/`. Drum-machine demo now uses the sequencing primitives exclusively (the old `PatternSequencer` is gone), has a swing knob, a Render-to-WAV command, and a hi-hat choke group via `FadeInOutSampleProvider`. |
| 2a. VST3 prep — `EventBufferQuery` factoring + `ITempoMap.NextChangeAfter` + `MusicalTime.RescaleFromPpq` | ✅ done | The three API additions identified in the VST3 POC review. `SequencedSampleProvider<T>` is now a thin wrapper over `EventBufferQuery`. 77 sequencing tests passing. |
| 2b. MIDI-file ingestion → `StaticTempoMap` + `TimeSignatureMap` + `EventTimeline<MidiEvent>` from `MidiFile` | future | Unlocks "render `.mid` to WAV" and is the prerequisite for VST3 offline render. |
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
- A proper voice manager — what choke groups, polyphony limits, voice stealing, and release-tail handling all belong to. Sink-side concern, lands with the sampler. The drum machine's hi-hat choke today uses a small local `ChokeableVoice` wrapper from the dispatcher (deferred per-frame fade-out, truncates the source when the fade completes so the mixer drops the voice). It's the right shape for the demo but doesn't generalise to SoundFont/sfz `exclusiveClass` semantics, polyphony, or release tails — that all lives in the voice manager.
- Sampler (SoundFont / sfz) — large feature in its own right; the sequencing slice of it is small.
- VST3 glue — the VST3 hosting work proceeds independently real-time first; the offline-render-through-VST3 use case is the intersection of (2) above and a working VST3 host, and gets wired last.

## Confirmed with the VST3 POC

After review by the VST3 hosting POC author, the four open questions above are answered as follows:

1. **`ProcessContext` population.** Inject `Transport` + `ITempoMap` + `TimeSignatureMap` directly into the VST3 consumer; it reads them at block start. A separate snapshot struct would just add copying.
2. **Event payload type.** Use `NAudio.Midi.MidiEvent` as the common payload between the VST3 sink and the `IMidiOutput` sink. Translating to VST3's structured `NoteOnEvent` (float velocity, noteId, tuning, channel) is much cleaner from a structured type, and SysEx → `DataEvent` needs byte arrays — both awkward with a packed 32-bit MIDI int. The packed-int payload remains useful for lightweight sample-trigger consumers (like the drum machine, which still uses `EventTimeline<int>`); it's just not the right common payload for the MIDI-aware sinks.
3. **Sample-offset units.** Confirmed identical to VST3's `IEventList.sampleOffset` — no conversion shim.
4. **Block size control.** VST3 doesn't drive block size. The host negotiates a `maxSamplesPerBlock` at `setupProcessing` (e.g. 4096) and splits incoming buffers internally if they exceed it. The sequencer renders whatever its puller asks for.

Three additional shape decisions came out of the same review:

5. **`EventBufferQuery.Query<T>` is stateless on an arbitrary `[startFrame, endFrameExclusive)` range**, not on "since the last call". Required because the VST3 consumer may split a single WASAPI pull into multiple `process()` calls when the plugin's `maxSamplesPerBlock` is smaller than the host buffer. `SequencedSampleProvider<T>` becomes a thin wrapper that calls the query with `[Transport.CurrentFrames, Transport.CurrentFrames + frames)` and routes the dispatched events into its `MixingSampleProvider`.
6. **`ITempoMap.NextChangeAfter(long tick)`** lands as a new interface method (returning `long?` or `long.MaxValue` for "no further change"). VST3's `ProcessContext.tempo` is single-valued per block, so the consumer needs this to decide whether to split a block at a tempo-change boundary or to use the start-of-block tempo and accept tiny inaccuracy. Implementations are trivial for both `StaticTempoMap` (binary search for next segment) and `LiveTempoMap` (last appended segment's start, or none).
7. **PPQ rescaling lives on `MusicalTime`.** Add `MusicalTime.RescaleFromPpq(long fileTick, int fileDeltaTicksPerQuarterNote)` implementing `fileTick * CanonicalPpq / fileDeltaTicksPerQuarterNote` so MIDI-file ingestion and any other rescaling caller share one implementation. Apply it to every tick — event absolute times, tempo events, time-sig events.

These three are the API additions that need to land alongside the milestone-2 work (MIDI-file ingestion + `EventBufferQuery.Query<T>` factoring); none of them disturb anything already shipped in milestone 1.

## Notes for the VST3 POC reviewer

A few practical notes alongside the open questions, so the POC author can plan around the current state of the code rather than have to read it to find out.

### The dispatch loop needs factoring out before VST3 can reuse it

`SequencedSampleProvider<T>` is currently hardwired to a `MixingSampleProvider` sink and exposes its per-buffer event-query logic only via a `dispatcher` callback. That fits an audio-mixer consumer (drum machine, sampler) but not a VST3 instrument provider, which needs to take the same `(event, frameOffset)` stream and pack it into the plugin's `process()` event input alongside an updated `ProcessContext` — no mixer involved.

Before the VST3 strand can land, refactor `DispatchEventsForBuffer` into a reusable `EventBufferQuery.Query<T>` (or similar) that takes `EventTimeline<T>`, `ITempoMap`, `IPositionTransform`, and an arbitrary `[startFrame, endFrameExclusive)` range, yielding `(event, frameOffset)` pairs. **Stateless by design** — see decision 5 in "Confirmed with the VST3 POC" above. `SequencedSampleProvider<T>` becomes a thin wrapper that supplies the range from `Transport.CurrentFrames` and routes the dispatched events into its `MixingSampleProvider`.

### `ProcessContext` field mapping

VST3 plugins read process context every block. Mapping from the sequencing layer is straightforward:

| VST3 `ProcessContext` field | Source |
|---|---|
| `tempo` | `transport.TempoMap.BpmAtTicks(transport.CurrentTicks)` |
| `timeSigNumerator` / `timeSigDenominator` | `timeSignatureMap.SignatureAt(transport.CurrentTicks)` |
| `projectTimeSamples` | `transport.CurrentFrames` |
| `projectTimeMusic` (in quarter notes) | `transport.CurrentTicks / (double)MusicalTime.CanonicalPpq` |
| `barPositionMusic` (in quarter notes) | derive from `timeSignatureMap.FromTicks(transport.CurrentTicks)` — start-of-bar tick converted to quarter notes |
| `cycleStartMusic` / `cycleEndMusic` | `transport.Loop` if set, expressed in quarter notes |
| `state` | combine `kPlaying` from `transport.IsPlaying`, `kCycleActive` from `transport.Loop`, plus the `kTempoValid` / `kTimeSigValid` / etc. flags as appropriate |

`samplesToNextClock` (24 PPQN MIDI clock alignment) is the only field that doesn't fall out of the existing types and would need its own helper. Out of scope for the first cut unless the POC needs it.

### "Render MIDI through VST3 to WAV" — end-to-end shape

The intended flow once both strands are present:

1. Load `.mid` via `MidiFile`, then **rescale ticks** from the file's `DeltaTicksPerQuarterNote` to the canonical PPQ: `canonicalTick = fileTick * MusicalTime.CanonicalPpq / fileDeltaTicksPerQuarterNote`. Apply this everywhere (events, tempo events, time-sig events).
2. Build `StaticTempoMap` from the file's `TempoEvent`s (post-rescale), and `TimeSignatureMap` from the `TimeSignatureEvent`s.
3. Build an `EventTimeline<MidiEvent>` from the file's note/control/SysEx events (`NAudio.Midi.MidiEvent` is the confirmed common payload between the VST3 and `IMidiOutput` sinks — see decision 2 above).
4. Construct a `Vst3InstrumentProvider : ISampleProvider` wrapping the plugin instance, holding the `Transport`, the timeline, and the position transform. Its `Read` calls the (refactored) `EventBufferQuery` to get events for the upcoming buffer, populates `ProcessContext`, calls `plugin.process()`, and copies the plugin's audio output into the read buffer.
5. Drive it with the same offline render loop the drum machine already uses (`WaveFileWriter` pulling until `EventTimeline.LastTick + release-tail margin` has been passed). The live-playback path is identical except `WasapiOut` does the pulling instead.

So the VST3 work doesn't have to wait for MIDI-file ingestion — you can hand-build an `EventTimeline` in tests to drive the plugin while the ingestion path lands in parallel.

