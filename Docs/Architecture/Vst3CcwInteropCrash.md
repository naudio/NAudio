# VST 3 host-CCW interop crash — root cause, two manifestations, and the rule (RESOLVED)

**Status (2026-05-25): RESOLVED.** A single bug — handing a plug-in the *wrong* COM pointer for a
host callback — produced two unrelated-looking CLR fatals (`0x80131506`). Both are fixed by the
same one-line pattern. This document captures the root cause, the two crashes it caused, the
evidence, and the general rule so it isn't reintroduced.

---

## The rule (read this first)

> `ComWrappers.GetOrCreateComInterfaceForObject(obj, …)` returns the CCW's **`IUnknown` identity**,
> whose vtable is a bare `QueryInterface`/`AddRef`/`Release` (3 slots). It is **not** a pointer to
> any of the object's user interfaces. Never hand that identity to a native API that expects a
> *specific* interface pointer **and stores it verbatim without QI-ing it back**. QI the identity
> for the concrete interface first, and hand over *that* dispatch pointer.

Passing the identity is only safe when the native side either (a) declares the parameter as
`FUnknown*`/`IUnknown*` and QIs it for what it needs (e.g. `IPluginBase::initialize(context)`), or
(b) QIs it before use. VST 3's `setComponentHandler` and `setFrame` do neither — they cast-and-store.

---

## Root cause

Our host callback objects are managed `[GeneratedComClass]` CCWs. The code obtained the pointer to
hand over with `GetOrCreateComInterfaceForObject`, which yields the **identity** (a 3-slot
`ManagedObjectWrapper_IUnknownImpl`). When that identity is stored verbatim by the plug-in and then
called as if it were the expected interface, the plug-in's method call (e.g. `performEdit`, vtable
slot 4) runs **off the end of the 3-slot vtable** into adjacent ComWrappers memory and makes a wild
virtual call that detonates inside the CLR.

The fix everywhere is the pattern the codebase already used for its `IBStream` /
`IParameterChanges` / `IAttributeList` / `IParamValueQueue` CCWs:

```csharp
var identity = GetOrCreateComInterfaceForObject(obj, None);
Marshal.QueryInterface(identity, in TargetInterface_iid, out var dispatch); // the real vtable
Marshal.Release(identity);
native.SetThing(dispatch);
```

---

## Manifestation 1 — editor parameter-edit crash (universal)

**Symptom:** clicking *any* control in *any* embedded editor killed the process. Universal across
vendors (Arturia ETERNITY, NI Raum, BABY Audio Crystalline) and in the bare-Win32 smoke harness.

**Path:** `IEditController::setComponentHandler(IComponentHandler*)`. The SDK's `EditController`
stores the pointer verbatim and only QIs it for the optional `IComponentHandler2` — never re-QIs it
for the base `IComponentHandler`. We passed the bare `IUnknown` identity, so the editor's
`beginEdit`/`performEdit` calls over-read.

**Fix:** `Vst3Plugin.Initialise` now QIs the handler identity for `IComponentHandler` before
`setComponentHandler`.

## Manifestation 2 — ValhallaSupermassive `IComponent::setState` crash

**Symptom:** loading state into Supermassive (e.g. `Vst3.SelfRoundtrip`) killed the process inside
`IComponent::setState`, *after* our `IBStream::Read` had cleanly returned all bytes. This had been
parked since Phase 5 as a suspected plug-in-internal defect ("crashes inside its own parser") and
was the lone known crash in the Phase 5 matrix.

**Actual cause:** the **same** bad handler pointer. Supermassive uses a separate controller; during
`setState` it calls back into the host component handler (a `restartComponent` / value push), which
over-read the bare-`IUnknown` pointer. The "after the read returns ⇒ plug-in's own parser"
conclusion was a hypothesis formed without a dump, and it was wrong.

**Fix:** the *same* `setComponentHandler` change. No Supermassive-specific work was needed.

**Proof (A/B):** at the pre-fix commit, `Vst3.SelfRoundtrip --plugin=Supermassive` crashes with
`IComponent.SetState → 0x80131506`; at the post-fix commit it passes byte-equal (710-byte state).
The only difference between the commits is the handler QI. Fresh-instance `Vst3.StateRoundtrip`
also no longer crashes (it loads and renders; the residual non-bit-identical render, max delta
~5e-2, is Supermassive's own reverb-tail non-determinism — the same category as NI Raum /
Supercharger GT, not a host gap).

---

## Audit — every CCW handoff in the host

| Handoff | Native contract | Correct? |
|---|---|---|
| `IPluginBase::initialize(context)` | `FUnknown*` — plug-in QIs | ✅ identity is fine |
| `IEditController::setComponentHandler` | `IComponentHandler*` — stored verbatim | ✅ fixed (QI for `IComponentHandler`) |
| `IPlugView::setFrame` | `IPlugFrame*` — stored verbatim | ✅ fixed (QI for `IPlugFrame`) |
| `getState`/`setState` streams | QI'd for `IBStream` | ✅ already correct |
| `ProcessData` input changes | QI'd for `IParameterChanges` | ✅ already correct |
| host `IMessage`/`IAttributeList`/`IParamValueQueue` | QI'd for the target IID | ✅ already correct |

`setFrame` had the identical verbatim-store anti-pattern and was fixed defensively at the same time
(it had been surviving by luck — the tested editors QI the frame, or the over-read landed benignly).

---

## How it was found (evidence, for future reference)

- **Full crash dump** of the editor crash (`DOTNET_DbgEnableMiniDump=1`, `DbgMiniDumpType=4`,
  analysed with `cdbX64`) showed a vtable over-read, but the *interpretation* ("editor calls
  `setParamNormalized` slot 15 on our handler ⇒ it treats us as its controller") was a red herring —
  that slot-15 landing was just where the over-read happened to resolve.
- **Two hand-rolled diagnostic handlers** (native vtables built from `[UnmanagedCallersOnly]` stubs,
  logging every call, with safe non-crashing over-slots) settled it: with a vtable that carries
  `beginEdit`/`performEdit`/`endEdit` at slots 3/4/5 the editor edits cleanly via the standard path;
  the plug-in was never misbehaving. The only thing wrong was that the *real* ComWrappers identity is
  a bare `IUnknown`, not an interface dispatch (confirmed: identity and the QI'd dispatch print 16
  bytes apart).
- **A/B at the parent commit** proved both crashes are the same bug (pre-fix crashes, post-fix
  passes; the handler QI is the only change).

### Mis-diagnoses to not resurrect

- ".NET 9 SEH removal" — unrelated; plain near-null over-read.
- "Editor treats our handler as its controller / deliberate slot-15 call" — symptom, not cause.
- "Supermassive crashes inside its own state parser" — wrong; it was calling our bad handler pointer.
- "Universal ⇒ non-standard call order / missing capability" — our call order and vtable defs match
  known-good hosts (gstreamer-vst3, SDK `PlugProvider`); the wrong *pointer* was the issue.

---

## Related

- [Vst3Hosting.md](Vst3Hosting.md) — overall plan and task list.
- Agent memory: `project_vst3_phase6_knob_crash`.
