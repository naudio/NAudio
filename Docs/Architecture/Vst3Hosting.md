# NAudio.Vst3 — VST 3 plug-in hosting

`NAudio.Vst3` is an optional NAudio package that discovers, loads, and hosts VST 3® plug-ins — both effects and instruments — inside an NAudio pipeline. The first release targets **Windows** with full audio, MIDI, and native-UI hosting. macOS / Linux audio + MIDI (headless) and cross-platform UI are deferred (see [Deferred and out of scope](#deferred-and-out-of-scope)).

The package is **strictly opt-in**: it is not part of the `NAudio` meta-package's dependency closure, and `NAudio.Core` / `NAudio.Wasapi` / `NAudio.WinMM` have no reference to it. Consumers who don't host plug-ins pay no size or load-time cost.

There is no established C# library that does this — VST.NET is mature but VST2-focused, and the various partial VST3 bindings are not complete, maintained, idiomatic .NET hosts. That's the opportunity.

---

## What's delivered

The first release hosts VST 3 effects and instruments end-to-end on Windows:

- **Discovery & module loading** — `Vst3PluginScanner` enumerates the standard Windows VST 3 folders (recursing vendor sub-folders); `Vst3Module.Load` handles the bundle/binary resolution and module lifecycle with reference counting. `Vst3ClassInfo` carries vendor/version/category, and `Kind` / `IsInstrument` for type-filtered pickers.
- **Audio effect hosting** — `Vst3Plugin` drives the full SDK lifecycle; `Vst3EffectSampleProvider : ISampleProvider` makes a loaded effect consumable in any NAudio chain, with block-size buffering, optional latency compensation, and output-RMS tail detection for offline renders.
- **Parameters** — `Vst3ParameterCollection` / `Vst3Parameter` expose the plug-in's parameters (normalised and plain values, plug-in-formatted display strings, step counts, flags). Host-side automation queues (`IParameterChanges`) are applied sample-accurately each block.
- **State & presets** — component + controller state save/load (`SaveState` / `LoadState`), and the `.vstpreset` file format (`Vst3Preset`, `Vst3Plugin.SavePreset` / `LoadPreset`) with class-id verification on load.
- **Programs & units** — `IUnitInfo` enumeration into `Units` / `ProgramLists` / `ActiveProgramList` / `CurrentProgram`; program selection routed through the `IsProgramChange` parameter (VST 3 has no program-change event).
- **Native UI hosting (Windows)** — `Vst3PluginView` embeds the plug-in's own editor via HWND, handles `IPlugFrame` resize (bidirectional, REAPER-style) and DPI scale push. Control edits flow back through the host component handler. Validated in a bare-Win32 harness and a WPF `HwndHost`.
- **Instruments & live MIDI** — `Vst3InstrumentSampleProvider` / `Vst3MidiInstrument` host VSTis (event-input bus, optional audio-in for vocoders). Note on/off, CC, pitch-bend, channel pressure, program change, and SysEx are supported, routed via `IMidiMapping` where applicable, with sample-accurate event timing, all-notes-off/panic, and `ProcessContext` (tempo/transport) population.
- **MIDI-file & sequencer rendering** — driving a hosted instrument from an `EventTimeline<MidiEvent>` (offline-to-WAV or live-with-seek), built on the shared `NAudio.Midi` sequencing types via a `Vst3MidiInstrument : IMidiInstrument` adapter.
- **Compatibility** — validated bit-identical state round-trips and clean renders across a deliberately diverse plug-in set spanning JUCE, the Steinberg SDK helpers, and several proprietary frameworks (see [Test plug-in matrix](#test-plug-in-matrix)).

Demos live in `NAudioWpfDemo` (VST3 effect host, realtime effect, live instrument, MIDI-file player) and `NAudioConsoleTest` (headless smoke/render harnesses).

### Licensing & trademark

- **SDK licence — resolved.** The VST 3 SDK is MIT-licensed since v3.8 (no GPLv3/proprietary option, no developer signup). Compatible with NAudio's MIT licence: we hand-write `[GeneratedComInterface]` bindings from the ABI described in the SDK headers; vendoring SDK source is permitted but unnecessary.
- **Trademark.** "VST" is a registered trademark of Steinberg Media Technologies GmbH. We ship as `NAudio.Vst3` (descriptive sub-namespace; VST.NET precedent), carry the ® on first occurrence plus the attribution line, and avoid the VST Compatible Logo. Fallback names if Steinberg objects: `NAudio.Vst3Host`, `NAudio.PluginHost`.

---

## Design principles

- **MIT-clean interop, no SDK vendoring.** Hand-written `[GeneratedComInterface]` bindings (the WASAPI COM-modernisation work is the template) — source-generated marshalling, no `ComImport`, no IL emission.
- **Component / Controller split honoured.** VST 3's two-object model — `IComponent` (DSP) and `IEditController` (UI + parameter authority) — is surfaced in the public API rather than hidden; the host mediates the `IComponentHandler` callbacks and the `IConnectionPoint` link between them.
- **`NAudio`-idiomatic surface.** `ISampleProvider` / `IWaveProvider` are the integration points: an effect is consumable as `ISampleProvider`; an instrument exposes a send-event API plus an `ISampleProvider` realtime surface and an `IMidiInstrument` adapter.
- **Strict thread discipline mirrored from VST 3.** UI / audio / setup thread roles are encoded in the API with affinity captured at construction and asserted on entry — host bugs that violate this otherwise look like plug-in crashes.
- **Strictly opt-in dependency.** Depends only on `NAudio.Core` and `NAudio.Midi`; no reference to `NAudio.Wasapi`/`NAudio.Asio`/`NAudio.WinMM`. The library hosts plug-ins; demos wire it into WASAPI/ASIO.

---

## Package layout

| Package | Current TFM | Eventual TFM | Notes |
|---|---|---|---|
| `NAudio.Vst3` | `net9.0-windows` | `net9.0;net9.0-windows` | The `-windows` leg adds UI hosting (HWND embed). A portable `net9.0` leg for headless audio + MIDI on all OSes comes with macOS support. |

**Dependencies:** `NAudio.Core` (for `ISampleProvider`/`IWaveProvider` and buffer types) and `NAudio.Midi` (for `MidiEvent`). Nothing else. Lives at `NAudio.Vst3/NAudio.Vst3.csproj` alongside the other component packages.

**Reused from elsewhere in NAudio 3:** the realtime demos lift the `RealtimeAudioEngine` pattern (ASIO duplex, atomic chain swap, feedback auto-mute) from the managed-effects work, but typed over `Vst3Plugin` so each slot keeps its native editor and state. Live MIDI capture uses the WinRT-backed `IMidiInput` (`WinRTMidiIn`), not legacy WinMM. MIDI-file rendering builds on the shared `NAudio.Sequencing` / `NAudio.Midi` types (`EventTimeline<MidiEvent>`, `MidiFileSequence`, `SequencedMidiPlayer`, `OfflineMidiRenderer`, `IMidiInstrument`) — see [Sequencing.md](Sequencing.md).

**`<IsAotCompatible>`:** deferred. Native COM dynamic instantiation across arbitrary plug-in vendors is fragile under trim-warning analysis; revisit once the audio path is stable.

---

## Architecture overview

### Object model

```
Vst3PluginScanner            ← enumerates the OS plug-in folders
   └─ Vst3ModuleInfo[]       ← discovered .vst3 modules (path, factory class infos, not yet loaded)

Vst3Module                   ← a loaded .vst3 DLL/bundle (one per file)
   ├─ IPluginFactory[2|3]    ← native pointer behind GeneratedComInterface
   ├─ Vst3ClassInfo[]        ← classes the factory advertises
   └─ CreateInstance(classId) → Vst3Plugin

Vst3Plugin                   ← one instantiated plug-in (IComponent + IEditController, connected)
   ├─ audio side             ← ProcessSetup, bus arrangements, process()
   ├─ Vst3ParameterCollection← parameter id → metadata + get/set + automation queue
   ├─ state / presets        ← SaveState/LoadState, SavePreset/LoadPreset
   ├─ Units / ProgramLists   ← IUnitInfo unit hierarchy + programs
   └─ CreateView()           → Vst3PluginView (Windows only)

Vst3EffectSampleProvider     ← wraps a Vst3Plugin effect as ISampleProvider
Vst3InstrumentSampleProvider ← wraps a Vst3Plugin instrument — MIDI in, audio out
Vst3MidiInstrument           ← IMidiInstrument adapter (sequencer / MIDI-file driving)
Vst3PluginView               ← HWND embedding, resize, DPI (Windows)
```

### Lifecycle (single plug-in)

1. **Discover** — scan the standard VST 3 folders; each `.vst3` is a directory whose DLL lives at `<bundle>/Contents/x86_64-win/<name>.vst3`.
2. **Load module** — `NativeLibrary.Load` the DLL, resolve `InitDll` (optional) and `GetPluginFactory`. One module load can spawn many instances; unload only when the last is released.
3. **Inspect factory** — walk `countClasses()` / `getClassInfo()`, filter to `kVstAudioEffectClass`, capture category strings.
4. **Create component + controller** — `createInstance(classId, IComponent::iid)`, then resolve the controller (its class ID, or QI the same class for `IEditController`). Both are `initialize`d with the host's `IHostApplication`.
5. **Connect** — bind component and controller via `IConnectionPoint::connect` (required before `setComponentState` for many JUCE-wrapped plug-ins to publish their parameter list).
6. **Sync initial state** — feed `getState` from the component into `setComponentState` on the controller, else many plug-ins start with the wrong UI values.
7. **Negotiate buses** — prefer each plug-in's declared default arrangement, falling back to stereo; match bus count to `getBusCount` (fixes sidechain/multi-bus plug-ins).
8. **Configure / activate / process** — `setupProcessing` → `setActive(true)` → `setProcessing(true)`, then per-block `process()` with input/output `AudioBusBuffers`, optional `IParameterChanges`, `IEventList`, and `ProcessContext`.
9. **Tear down** in reverse: `setProcessing(false)` → `setActive(false)` → `terminate()` → `Release()`.

### Threading model

| Thread role | Calls allowed | Calls forbidden |
|---|---|---|
| Audio thread | `process`, parameter reads via `IParameterChanges` | anything that allocates, blocks, or touches the UI |
| UI thread (STA on Windows) | `IPlugView` methods, parameter sets via controller, `IComponentHandler` callbacks | `process`, `setActive` |
| Setup thread | `initialize`, `terminate`, bus setup, `setActive`, state save/load | `process` |

Affinity is captured at construction and asserted on each public entry point — fail loudly in dev rather than crash mysteriously in production.

### Host-implemented interfaces

The host supplies CCW implementations of:

- **`IHostApplication`** (+ `createInstance` for `IMessage` / `IAttributeList`) — plug-ins may refuse to instantiate without it.
- **`IComponentHandler`** / **`IComponentHandler2`** — receive `beginEdit` / `performEdit` / `endEdit` from the UI, plus `restartComponent` re-query flags (parameters / latency / buses).
- **`IPlugFrame`** — receives `resizeView` so the plug-in can resize its window.
- **`IPlugViewContentScaleSupport`** — pushes host DPI scale to the view.
- **`IBStream`** (with `ISizeableStream` / `IStreamAttributes`) — a managed `MemoryStream` wrapped for state save/load.
- **`IPlugInterfaceSupport`** — advertises which host extensions are implemented.

A class of early crashes traced to passing a CCW's bare `IUnknown` identity where an interface dispatch was required (e.g. `setComponentHandler`, `setFrame`); the rule and evidence are in **[Vst3CcwInteropCrash.md](Vst3CcwInteropCrash.md)**.

---

## Test plug-in matrix

Compatibility is validated against a deliberately diverse set spanning the major plug-in frameworks. State round-trips are bit-identical and renders crash-free across, among others: TAL-Chorus-LX / TAL-Dub-X / TAL-Sampler (Togu Audio Line, JUCE), CHOWTapeModel (chowdsp, JUCE), BABY Audio Crystalline, Arturia FX/synths (Steinberg SDK helpers, large parameter trees), Native Instruments effects, several iZotope titles, ValhallaSupermassive, and Pianoteq (programs). Other representative targets: TAL-NoiseMaker, Surge XT, Vital, Spitfire LABS, OTT.

---

## Deferred and out of scope

### Deferred (planned, not in the first release)

- **UI polish** — DPI-matrix validation (100/125/150/200 % + multi-monitor move with runtime content-scale re-push), a WinForms host smoke test (the framework-agnostic `AttachTo(IntPtr hwnd)` already proves out under WPF), and a Vital + Surge XT pass to exercise the OpenGL / VSTGUI rendering paths.
- **Preset/program runtime re-query** — handling `restartComponent(kParamValuesChanged / kParamTitlesChanged)` after a preset/program change (the flag-dispatch plumbing exists from the latency-changed handler); `IProgramListData` / `IUnitData` (most hosts never touch it).
- **Tempo-following synths** — `ProcessContext.tempo` is single-valued per block; splitting a block at an intra-block tempo change is deferred until a tempo-following VSTi is in the matrix.
- **macOS / Linux audio + MIDI** — multi-target `net9.0;net9.0-windows`; `CFBundle` loading on macOS, `dlopen` on Linux. Headless audio/MIDI demos should run unchanged once the loader lands.
- **Cross-platform UI** — NSView (macOS) / X11 XEmbed (Linux) embedding depends on the consumer's managed UI framework; deferred indefinitely. Workaround: run plug-ins headlessly with parameter automation.
- **64-bit (`kSample64`) processing** — 32-bit float covers nearly every plug-in and matches `ISampleProvider`; the `SymbolicSampleSize` constant is exposed so adding it later isn't a breaking change.

### Known plug-in-specific issues

- **NI Neutron 4 Elements** — instantiates and renders cleanly but `getParameterCount` returns 0, blocking by-name parameter selection. Likely an unimplemented host capability gating the controller's parameter registration (cf. the JUCE `IConnectionPoint` ordering fix). Next step: log every QI the controller performs during initialise and cross-reference the SDK's `editorhost`.

### Out of scope for v1

- **VST 2 hosting** — Steinberg withdrew the VST 2 SDK in 2018; no licence-clean path. Use VST.NET.
- **AudioUnit / CLAP / LV2** — possible future packages (`NAudio.AudioUnit`, `NAudio.Clap`, …); this one stays VST 3-focused.
- **Plug-in authoring** — host-only; writing a VST 3 plug-in in C# is a separate problem.
- **Sandboxing / out-of-process hosting** — a misbehaving plug-in crashes the host process; sandboxing is a major project in its own right.
- **Note expression / MPE deep support** (`INoteExpressionController`) — recognised so plug-ins that query it don't crash, but no public note-expression input API.
- **32-bit-only plug-ins** — NAudio 3 is 64-bit only on Windows; no bridge process.

---

## Related documents

- [Vst3CcwInteropCrash.md](Vst3CcwInteropCrash.md) — the CCW identity-vs-dispatch interop bug and the general rule.
- [Sequencing.md](Sequencing.md) — the beats↔samples primitives the MIDI-file path is built on.
- [MODERNIZATION.md](MODERNIZATION.md) — overall NAudio 3 design context (`NAudio.Vst3` is additive, not part of the modernisation core).
- [NAudio3AssemblyLayoutPlan.md](NAudio3AssemblyLayoutPlan.md) — package layout; `NAudio.Vst3` follows the same per-feature-package pattern.
- [ReleaseStrategy.md](ReleaseStrategy.md) — branch / version flow; the package shares the repo-wide `<VersionPrefix>`.

## External references

- [VST 3 SDK on GitHub](https://github.com/steinbergmedia/vst3sdk) (MIT, current 3.8.x)
- [VST 3 Developer Portal](https://steinbergmedia.github.io/vst3_dev_portal/) — licensing, technical docs
- [VST 3 Interface Reference](https://steinbergmedia.github.io/vst3_doc/vstinterfaces/index.html)
- [Usage Guidelines](https://steinbergmedia.github.io/vst3_dev_portal/pages/VST+3+Licensing/Usage+guidelines.html) — trademark rules

---

*VST® is a registered trademark of Steinberg Media Technologies GmbH.*
