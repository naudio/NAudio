# CoreAudio Activation + ComWrappers Bridging Modernization

> **Status: IN PROGRESS** (Phase 2e in [MODERNIZATION.md](MODERNIZATION.md)). Working branch: `naudio3dev-coreaudio-activation`.

This document tracks the working plan and progress for Phase 2e — migrating `NAudio.Wasapi/CoreAudioApi/` off the legacy `[ComImport]` activation pattern (`new SomeComObject()`) and the classic-RCW `Marshal.GetObjectForIUnknown` bridge, replacing both with raw `CoCreateInstance` + the shared `StrategyBasedComWrappers` instance owned by `ComActivation`.

When this work lands, the AOT activation crash at `MMDeviceEnumerator..ctor` (`NotSupportedException` under `BuiltInComInteropSupport=false`) goes away. The remaining `IL2026` warning will only fully clear when the MediaFoundation sweep also lands (Phase 2e′ — see "Wide scope deferred" below).

---

## Scope decision: NARROW (CoreAudio only)

**This phase migrates `NAudio.Wasapi/CoreAudioApi/` only.** `NAudio.Wasapi/MediaFoundation/` and the top-level `MediaFoundationResampler.cs` continue to use the legacy `Marshal.GetObjectForIUnknown` bridge for now.

Reasons for narrow:

- The actual AOT activation crash (`MMDeviceEnumeratorComObject`) lives in CoreAudio. Narrow ships the unblock.
- Wide requires retyping `MediaFoundationTransform.transform` (currently legacy `[ComImport] IMFTransform`) to a `[GeneratedComInterface]` version. That cascades through `MediaFoundationReader`, `MediaFoundationEncoder`, `StreamMediaFoundationReader`, plus `MediaFoundationResampler.CreateTransform()` (which currently bridges legacy on purpose). Different blast radius, different risk profile, separate work unit.
- Narrow is bisectable: any regression on `MMDevice.Properties` / `FriendlyName` / device enumeration is unambiguously CoreAudio.

The IL2026 warning will partially persist after this phase (MediaFoundation sites still pull `ComActivator` into the trim graph). That should be called out explicitly in the PR.

---

## Wide scope deferred — starting prompt for Phase 2e′

Once narrow lands, the wide scope work is its own phase. Capture the following so a clean prompt can be written from this:

### MediaFoundation call sites still on the legacy bridge after narrow lands

These ~12 sites need `Marshal.GetObjectForIUnknown` → `ComActivation.ComWrappers.GetOrCreateObjectForComInstance` migration:

| File | Sites | Notes |
| --- | --- | --- |
| `NAudio.Wasapi/MediaFoundation/MediaFoundationHelpers.cs:40` | 1 | `IMFActivate` projection in enumeration |
| `NAudio.Wasapi/MediaFoundation/MfSample.cs:96, 116` | 2 | `IMFMediaBuffer` from `GetBufferByIndex` / `ConvertToContiguousBuffer` |
| `NAudio.Wasapi/MediaFoundation/MfActivate.cs:44` | 1 | `IMFTransform` from `IMFActivate::ActivateObject` — see field-type cascade below |
| `NAudio.Wasapi/MediaFoundation/MfTransform.cs:78, 91, 125, 137` | 4 | `IMFMediaType` from `GetInputAvailableType` / `GetOutputAvailableType` / `GetInputCurrentType` / `GetOutputCurrentType` |
| `NAudio.Wasapi/MediaFoundation/MfSourceReader.cs:50, 62, 112` | 3 | `IMFMediaType` and `IMFSample` from source reader |
| `NAudio.Wasapi/MediaFoundationResampler.cs:74` | 1 | **Deliberately kept legacy** in narrow scope — projects activated `IUnknown*` onto legacy `[ComImport] IMFTransform` because `MediaFoundationTransform.transform` is typed as the legacy interface. See below. |

### The `MediaFoundationTransform.transform` field-type cascade

The dominant blocker for wide scope. `MediaFoundationTransform.cs:30` declares:

```csharp
private IMFTransform transform; // legacy [ComImport]
```

Migrating this field to `[GeneratedComInterface]` requires touching every caller. Verify before starting Phase 2e′ — likely consumers:

- `MediaFoundationTransform.cs` itself (the abstract base)
- `MediaFoundationResampler.cs` — overrides `CreateTransform()`, currently does `(IMFTransform)Marshal.GetObjectForIUnknown(unknown)` against the activated resampler MFT pointer
- `MediaFoundationReader.cs` — owns its own `IMFSourceReader` rather than `IMFTransform`, so probably not affected directly, but verify
- `MediaFoundationEncoder.cs` — uses `IMFTransform` via `MfTransform` wrapper, verify
- `StreamMediaFoundationReader.cs` — verify
- `MfActivate.cs:44` — already returns `IMFTransform`, so the field-type change forces this to project to the modern interface

### Phase 2e′ starting prompt outline

When ready to start the wide-scope follow-up, the prompt should:

1. State scope: MediaFoundation `Marshal.GetObjectForIUnknown` sweep + `IMFTransform` field-type migration.
2. Reference this section as the call-site inventory.
3. Keep the same hazard list as Phase 2e (multi-vtable CCWs, same-namespace types beating `using` aliases, `is ComObject` casting through `object`, `IDisposable` no-op trap for ComWrappers wrappers).
4. Decide upfront on `IMFTransform` modernisation strategy: full field-type migration, or scoped to the projection sites only (less work, less benefit). Probably full migration since the goal is no IL2026.
5. Verification: `NAudioTests/MediaFoundation/*` (especially `MediaFoundationResamplerTests.Experimental_*`), `MediaFoundationReaderTests`, encoder tests if present. AOT smoke should produce zero IL2026 after both phases land.

---

## Already in place (read first)

Phase 6c (DMO modernization) shipped these and Phase 2e reuses them — do not duplicate:

- **`NAudio.Wasapi/CoreAudioApi/ComActivation.cs`** — internal helper exposing:
  - `IntPtr CoCreateInstance(Guid clsid, Guid iid)` — raw activation; caller must `Marshal.Release`
  - `T CreateInstance<T>(Guid clsid, Guid iid)` where `T : class` — activation + projection onto a `[GeneratedComInterface]` wrapper using `CreateObjectFlags.UniqueInstance`. Caller releases via `((ComObject)(object)wrapper).FinalRelease()`.
  - `ComActivation.ComWrappers` — the **shared static** `StrategyBasedComWrappers` instance. Use this for all `GetOrCreateObjectForComInstance` calls. Do not new up another instance.
  - `ComActivation.IID_IUnknown` constant.
- **`NAudio.Wasapi/CoreAudioApi/NativeMethods.cs`** — already has `[LibraryImport] CoCreateInstance` and `CLSCTX_INPROC_SERVER`. Reuse.

---

## Hazards (from DMO modernization — every one of these tripped us up)

1. **`StrategyBasedComWrappers` wrappers are NOT `IDisposable`.** Disposal goes through `((ComObject)(object)wrapper).FinalRelease()`. The pattern `if (foo is IDisposable d) d.Dispose()` compiles cleanly and silently no-ops, leaking the COM reference. **Audit every disposal site.**
2. **Direct `is ComObject` pattern matching from an interface type fails to compile** ("An expression of type 'X' cannot be handled by a pattern of type 'ComObject'"). Cast through `object` first: `if ((object)wrapper is ComObject co) co.FinalRelease();`.
3. **Multi-vtable CCWs return distinct `IntPtr`s per interface.** When passing a CCW pointer back into native, `Marshal.QueryInterface` for the target IID — passing `IUnknown*` causes native to dereference the wrong vtable (STATUS_STACK_BUFFER_OVERRUN / `ExecutionEngineException`). CoreAudio is mostly RCW-direction, but watch any path that hands a managed callback object to native.
4. **Same-namespace types beat `using` aliases.** Don't keep both legacy `[ComImport]` and modern `[GeneratedComInterface]` declarations of the same interface in the same namespace. Move one (`Interfaces` sub-namespace already does this for the modern set in CoreAudio) or delete the legacy declaration.
5. **Callback interfaces are out of scope for Phase 2e.** `IMMNotificationClient`, `IControlChangeNotify`, `IAudioEndpointVolumeCallback`, `IAudioSessionNotification`, `IAudioSessionEvents`, `IActivateAudioInterfaceCompletionHandler`, `IAgileObject` — these stay on `[ComImport]` and the legacy CCW path until Phase 2f. But if a CoreAudio call signature in this phase passes a managed-callback object into native (e.g. `RegisterEndpointNotificationCallback`), the existing `Marshal.GetComInterfaceForObject` bridge stays in place for now.

---

## CreateObjectFlags decision per call site

- `UniqueInstance` — for things obtained from `Activate` / `GetService` / `EnumAudioEndpoints` item access. Fresh COM objects we own. Caller is responsible for `FinalRelease()`.
- `None` — for QI'd interfaces on an object whose lifetime is owned elsewhere. Wrapper piggy-backs on the cache.

Decide per-site, be explicit. The disposal pattern differs.

---

## Call-site inventory — CoreAudioApi/

30 sites across 18 files. Plus the activation site in `MMDeviceEnumerator..ctor`.

### Activation site (the headline AOT fix)

- [ ] `MMDeviceEnumerator.cs:48-51` — `new MMDeviceEnumeratorComObject()` + `GetIUnknownForObject` + `GetObjectForIUnknown` chain → `ComActivation.CreateInstance<IMMDeviceEnumerator>(CLSID, IID)`. Update `Dispose(bool)` to call `((ComObject)(object)realEnumerator).FinalRelease()`.
- [ ] Delete `NAudio.Wasapi/CoreAudioApi/Interfaces/MMDeviceEnumeratorComObject.cs` once nothing references it.

### Bridge sweep (Marshal.GetObjectForIUnknown → ComActivation.ComWrappers.GetOrCreateObjectForComInstance)

Suggested order: leaf wrappers first, MMDevice/MMDeviceCollection next, session/topology after, callback-mixing files last.

- [x] `AudioRenderClient.cs:18` (1)
- [x] `AudioCaptureClient.cs:18` (1)
- [x] `AudioClockClient.cs:19` (1)
- [x] `AudioStreamVolume.cs:19` (1)
- [x] `AudioMeterInformation.cs:43` (1) — Ray Molenkamp header removed, docs refreshed; dual-ctor handled with `ownsInterface` flag
- [x] `SimpleAudioVolume.cs:27` (1) — dual-ctor handled with `ownsInterface` flag
- [x] `MMDevice.cs:64, 71, 95` (3) — IPropertyStore, IAudioClient, IDeviceTopology. Ray Molenkamp header removed; class summary doc refreshed. AudioClient.Dispose updated to FinalRelease the wrapper (was a leak waiting to happen under ComWrappers — classic RCWs auto-released on GC, ComWrappers UniqueInstance does not). Confirmed `audioClientInterface as IAudioClient2/3` cross-casts work via `IDynamicInterfaceCastable` emitted by the GeneratedComInterface source generator.
- [x] `MMDeviceCollection.cs` indexer (1) — now also implements `IDisposable` to release the underlying `IMMDeviceCollection`. Was an existing leak under classic RCW (mostly hidden by GC); now deterministic.
- [x] `MMDeviceEnumerator.cs` remaining 4 sites (`EnumerateAudioEndPoints`, `GetDefaultAudioEndpoint`, `TryGetDefaultAudioEndpoint`, `GetDevice`). Helper `WrapDevicePointer` consolidates the IMMDevice projection. `MMDevice.Dispose` now FinalReleases its `deviceInterface` (was leaking — `readonly` field never released). Confirmed `(IMMEndpoint)deviceInterface` cross-cast at MMDevice.DataFlow works via DICASTABLE.
- [x] `AudioSessionManager.cs:46` (1) — `as IAudioSessionManager2` cross-cast preserved (DICASTABLE-confirmed); Dispose now FinalReleases the wrapper.
- [x] `AudioSessionControl.cs:30` (1) — dual-ctor with `ownsInterface` flag; `is IAudioMeterInformation` / `is ISimpleAudioVolume` borrowed-RCW pattern preserved (relies on DICASTABLE — works with both classic and ComWrappers underlying interfaces).
- [x] `SessionCollection.cs:23` (1) — Dispose now FinalReleases.
- [x] `DeviceTopology.cs:39` (1)
- [x] `Connector.cs:86` (1)
- [x] `Part.cs:104, 119, 137, 170, 185, 200, 209` (7) — consolidated into `WrapAndRelease<T>` helper.
- [x] `PartsList.cs:49` (1)
- [x] `AudioEndpointVolume.cs:150` (1) — Ray Molenkamp header removed. Callback registration (`Marshal.GetComInterfaceForObject<AudioEndpointVolumeCallback, IAudioEndpointVolumeCallback>`) **deliberately preserved** on legacy CCW path — `IAudioEndpointVolumeCallback` is one of the seven Phase 2f callback interfaces still on `[ComImport]`. Dispose now FinalReleases the wrapper.
- [x] `ActivateAudioInterfaceCompletionHandler.cs:24, 34` (2) — line 24's `activateOperationPtr` is a **borrowed** callback parameter (we don't own a ref), so wrapper's QI'd ref is FinalReleased before returning. Line 34's `unkPtr` is owned (from `GetActivateResult`); existing `Marshal.Release(unkPtr)` drops our ownership while the wrapper takes its own ref. Class itself remains `[ComImport]`-implementing — that's Phase 2f.

### Disposal-pattern audit

After each file is migrated, check the corresponding `Dispose` / `~ClassName` / cleanup paths for the `if (x is IDisposable d) d.Dispose()` no-op trap. Replace with `((ComObject)(object)field).FinalRelease()` (cast through `object`).

---

## Verification

### Tests (NAudioTests)

Must remain green:

- [ ] `NAudioTests/Wasapi/MMDeviceEnumeratorTests.cs` — `FriendlyName`, enumeration
- [ ] `NAudioTests/Wasapi/AudioClientTests.cs` — device discovery + activation
- [ ] All other `NAudioTests/Wasapi/*` (29 runnable tests typical)
- [ ] `NAudioTests/Dmo/*` — guard against ComActivation regression spilling into DMO
- [ ] `NAudioTests/MediaFoundation/*` — guard against the bridge sweep leaking out of CoreAudio

### AOT smoke

`tools/AotSmoke/AotSmoke.exe` published with `PublishTrimmed=true`:

- [ ] No `NotSupportedException` from `MMDeviceEnumerator..ctor` (the headline AOT crash — should disappear after the activation site lands)
- [ ] IL2026 warning status logged. After narrow scope: warning may still appear because MediaFoundation paths remain on legacy bridging — note this in the PR. After Phase 2e′ (wide): warning should be gone entirely.
- [ ] **Out of scope:** the `AccessViolationException` in `IMMDevice.OpenPropertyStore` under trimming — separate investigation.

### Manual

- [ ] `NAudioDemo` device enumeration / playback / capture
- [ ] `NAudioWpfDemo` end-to-end (STA thread surface)

### Diff-check tripwire (these must show zero diff in the PR)

- [ ] `NAudio.Wasapi/MediaFoundation/MediaFoundationTransform.cs` (narrow scope only — wide phase touches it)
- [ ] `NAudio.Wasapi/CoreAudioApi/Interfaces/IPropertyStore.cs`
- [ ] `NAudio.Wasapi/CoreAudioApi/PropertyStore.cs`
- [ ] `NAudio.Wasapi/CoreAudioApi/PropVariant.cs`

---

## Out of scope (deliberately deferred)

- The seven remaining `[ComImport]` callback interfaces in `CoreAudioApi/Interfaces/` (`IMMNotificationClient`, `IControlChangeNotify`, `IAudioEndpointVolumeCallback`, `IAudioSessionNotification`, `IAudioSessionEvents`, `IActivateAudioInterfaceCompletionHandler`, `IAgileObject`) — Phase 2f.
- `<IsAotCompatible>true</IsAotCompatible>` flag on the project — don't flip until Phase 2f also lands.
- `MediaFoundation/` bridge sweep + `MediaFoundationTransform.transform` field type — Phase 2e′ (see "Wide scope deferred" above).
- `IMMDevice.OpenPropertyStore` trimming AV — separate investigation.

---

## Next steps — pre-merge investigation plan

> **Status: HOLD merge of Phase 2e until at least step 1 below is complete.** The
> `WrapUnique` suppression empirically prevents the demo crash, but we don't yet
> understand the underlying mechanism, and we haven't confirmed
> `StrategyBasedComWrappers` is even the right primitive for an AOT-friendly
> classic-COM consumer. Shipping the unmask-when-we-don't-understand-the-mask
> position into NAudio 3 is uncomfortable for a foundational library.

### What we know going in

The investigation captured in the 2026-04-28 progress log entry produced these
concrete findings; a future session should start from these rather than
re-deriving them.

#### Two distinct bugs, not one

1. **Bug A (FIXED)** — `AudioClient.Dispose`'s defensive triple `FinalRelease`
   from commit 9adbdf4 was self-inflicted. DICASTABLE returns the *same*
   `ComObject` for `audioClientInterface as IAudioClient2/3`, and although
   `ComObject.FinalRelease` is documented idempotent, calling it three times on
   the alias empirically AVs in `Marshal.Release`. Single `FinalRelease` works.

2. **Bug B (MASKED)** — a separate finalizer-thread AV (or `Invalid Program:
   UnmanagedCallersOnly` fast-fail on a freed trampoline). Surfaces when
   `StrategyBasedComWrappers`' generated wrapper finalizer runs on the GC
   thread for some wrapper whose `_unknown` (or cached QI'd vtable) is no
   longer valid. `GC.SuppressFinalize(wrapper)` inside `WrapUnique` masks it.
   Mechanism not yet identified.

#### CoreAudio interface agility matrix

From `NAudioTests/Wasapi/IAgileObjectProbeTests` — probes each wrapper with
`ComWrappers.TryGetComInstance` + `Marshal.QueryInterface` for `IAgileObject`,
plus a vtable-dispatch on `IMarshal::GetUnmarshalClass` to detect FTM
aggregation. Run from an MTA test thread:

| Interface | IAgileObject | IMarshal | Effective status |
| --- | --- | --- | --- |
| `IMMDeviceEnumerator` | no | FTM | functionally agile |
| `IMMDeviceCollection` | no | none | apartment-affine (no PSR) |
| `IMMDevice` | no | none | apartment-affine (no PSR) |
| `IPropertyStore` | no | none | apartment-affine (no PSR) |
| `IDeviceTopology` | no | none | apartment-affine (no PSR) |
| `IAudioEndpointVolume` | no | FTM | functionally agile |
| `IAudioClient` | YES | FTM | truly agile |
| `IAudioRenderClient` | YES | FTM | truly agile |
| `IAudioClockClient` | YES | FTM | truly agile |
| `IAudioSessionManager` | YES | FTM | truly agile |
| `IAudioSessionEnumerator` | YES | FTM | truly agile |
| `IAudioSessionControl` | YES | FTM | truly agile |

#### The apartment-Release hypothesis is FALSIFIED

The natural hypothesis ("non-agile object Released on the wrong apartment from
the GC finalizer thread") doesn't survive the matrix above. The four
apartment-affine-looking interfaces (`IMMDeviceCollection`, `IMMDevice`,
`IPropertyStore`, `IDeviceTopology`) have *neither* `IAgileObject` *nor*
`IMarshal` *nor* a registered proxy/stub class — confirmed by
`RoGetAgileReference` returning `REGDB_E_IIDNOTREG`. There is no defined
cross-apartment marshaling path for these IIDs at all. Yet classic RCW
released them safely from the finalizer thread for years, which means their
`Release` is implemented as a thread-safe `InterlockedDecrement` and apartment
crossing isn't the issue.

The remaining hypothesis is a **use-after-free on a cached QI'd vtable inside
`StrategyBasedComWrappers`'s default cache strategy** — but this is a guess,
not evidence. Future session needs to confirm or refute it.

### The plan, ordered by leverage

#### 1. CsWin32 / first-party-pattern spike (highest leverage)

[Microsoft.Windows.CsWin32](https://github.com/microsoft/CsWin32) generates
AOT-friendly P/Invoke + COM bindings from Win32 metadata. Generate
`IMMDeviceEnumerator` + `IMMDevice` + `IAudioClient` via CsWin32 in a sandbox
project. Look at:

- How does the generated code wrap `IUnknown*`? `ComPtr<T>`-style? Custom
  `ComWrappers` subclass? Something else?
- How does it handle `Dispose` / finalization?
- How does it project `[ComImport]`-style multi-vtable scenarios (the things
  we need for the seven Phase 2f callback interfaces)?
- How does it handle DICASTABLE-style `as IFoo2` casts — same wrapper, distinct
  wrapper, or no support?
- How does it do CCWs (managed objects implementing COM interfaces, e.g.
  `IMMNotificationClient` callback)?

If the CsWin32 pattern is materially different and looks more correct, that's
the strong signal that we're hand-rolling an inferior approach. Decision
point: switch to CsWin32-generated wrappers, or stay on hand-written
`[GeneratedComInterface]` and accept what we have.

Also check whether the **Windows App SDK (WinAppSDK)** ships AOT-targeted
CoreAudio bindings — if so, that's another reference implementation to
compare.

**Estimated effort:** 2–3 hours.
**Output:** "use CsWin32" / "stay on `[GeneratedComInterface]`" decision with
reasoning.

#### 2. Search dotnet/runtime for known issues

30-minute task. Search [dotnet/runtime issues](https://github.com/dotnet/runtime/issues)
for:

- `StrategyBasedComWrappers UniqueInstance finalizer`
- `[GeneratedComInterface] DICASTABLE FinalRelease`
- `ComObject.Finalize AccessViolationException`
- `ComWrappers cache strategy use-after-free`

Also check the .NET 9 release notes / breaking changes for `ComWrappers`
behavior changes — there were some between .NET 8 and 9.

If our crash class is a known issue, the path forward becomes whatever the
runtime team recommends. If not, our minimal repro (once we have one) is a
candidate for filing.

**Estimated effort:** 30 minutes.
**Output:** linked issue numbers (if any) and runtime-team-recommended
mitigation.

#### 3. Build a deterministic headless repro

The current `WrapUniqueCrashRepro` test is `[Explicit]` because it doesn't
trip the demo crash even with `WrapUnique` suppression off. The most likely
missing ingredient is a real WinForms message pump on the STA thread —
`Application.Run` against a hidden `Form`, post the player + dispose +
mixer-construct sequence as queued operations, exit the pump when done.
NUnit's bare `[Apartment(STA)]` thread doesn't pump.

Once we have a deterministic crash repro, every other downstream activity
(bisect, root-cause confirmation, regression test) becomes vastly cheaper.
Worth doing regardless of which architectural direction step 1 points to.

**Estimated effort:** 2 hours.
**Output:** a non-`[Explicit]` test that fast-fails the runner *without*
`WrapUnique` and runs cleanly *with* it.

#### 4. Bisect WrapUnique against the demo

With `MiniDumpInstaller` already in NAudioDemo (committed in `c8a4916`), this
is now a 30-minute exercise: comment out `GC.SuppressFinalize(wrapper)` for
specific call sites only — agile types first per the matrix above — rebuild,
run the demo, see if it crashes. The minimum subset that must keep
suppression IS the actual bug surface.

Strong prior: the four apartment-affine-looking types
(`IMMDevice`/`IMMDeviceCollection`/`IPropertyStore`/`IDeviceTopology`) are
where suppression is load-bearing; the eight FTM/agile types are
over-suppression that costs an avoidable leak on missed Dispose.

If the bisect confirms the prior, narrow `WrapUnique` accordingly and update
the agile-type call sites to use plain
`comWrappers.GetOrCreateObjectForComInstance` — no leak, no crash.

**Estimated effort:** 30 minutes.
**Output:** narrowed suppression set, restored finalizer behavior for agile
types.

#### 5. Confirm the cached-vtable hypothesis

With the bisected minimum set in hand, instrument `ComObject.FinalRelease` (or
write a debug-build wrapper) to log the `_unknown` pointer value just before
it would be Released. Compare to known-still-live wrapper pointers. If the
AV's IntPtr matches a still-live wrapper's `_unknown`, we have a use-after-
free *across two wrappers for the same underlying COM object*. If it's a
freed-then-reallocated address, we have a deeper `StrategyBasedComWrappers`
cache-strategy issue.

This is the diagnostic that answers "what is `WrapUnique` actually masking".
Without it, the rationale comment stays at "use-after-free, mechanism
unknown".

**Estimated effort:** 1–2 hours, plus whatever follows from what we find.
**Output:** named root cause for Bug B, ideally pointing at a fix that
removes the need for `WrapUnique` entirely.

#### 6. Audit existing CCW-direction interop

Independent of the wrapper-direction work above, the code review you asked
about. Specific surfaces:

- **Multi-vtable CCWs.**
  `Marshal.GetComInterfaceForObject<T1, T2>(callback)` is used in
  `AudioEndpointVolume`, `AudioSessionControl`, `AudioSessionManager`,
  `MMDeviceEnumerator`. Each takes a managed callback and produces an
  `IUnknown*` for native consumption. With `BuiltInComInteropSupport=false`
  (the AOT goal) this fails — we need
  `ComWrappers.GetOrCreateComInterfaceForObject` for the CCW direction too.
  The seven Phase 2f callback interfaces are all in this category.
- **CCW lifetime vs subscription.** When
  `RegisterAudioSessionNotification(callbackPtr)` runs, the COM object holds a
  ref to the CCW. If the user forgets `UnRegisterEventClient` *and* the
  wrapper is finalized, what happens? Currently `WrapUnique` masks the
  symptom; once we narrow suppression, we need the lifetime contract to be
  explicit and the docs to reflect it.
- **`IMMNotificationClient` registration.** Same shape as multi-vtable CCWs.
- **`AudioRenderClient.GetBufferLease` hot path.** `ref struct` lease pattern.
  Confirm there's no `[GeneratedComInterface]` method-call quirk where the
  JIT-emitted dispatch produces additional vtable QIs we're not accounting
  for.
- **The seven Phase 2f callback interfaces.** Survey the migration path to
  confirm it's clear before we ship Phase 2e — if any are blocked by .NET
  source generator limitations, we want to know now, not after.

**Estimated effort:** 3–4 hours for a thorough pass.
**Output:** Phase 2f scoping doc; AOT readiness assessment.

### Decision gates before merging Phase 2e

Don't merge until:

- Step 1 produces a "stay on current approach" decision, **OR** points at
  CsWin32 / WinAppSDK pattern as a clear replacement (in which case we hold
  and re-do Phase 2e on the better foundation).
- Step 2 either finds a known issue + recommended workaround, or confirms our
  scenario isn't already filed (in which case we plan to file it).
- We have **either** step 5's named root cause, **or** an explicit
  hold-our-nose statement in the rationale comment that says "we shipped
  WrapUnique without understanding it because: \<concrete reason\>" — not
  just "empirically works".

### What's already in place to support the work

- `ComActivation.WrapUnique` rationale comment with the falsified hypothesis
  and the remaining candidate documented.
- `AudioClient.Dispose` rationale comment for the single-FinalRelease fix.
- `NAudioTests/Wasapi/IAgileObjectProbeTests` — agility matrix probe.
- `NAudioTests/Wasapi/WrapUniqueCrashRepro` — `[Explicit]` repro harness with
  notes on what was tried; starting point for step 3.
- `NAudioDemo/MiniDumpInstaller` — vectored exception handler + minidump
  writer + log file at `%LOCALAPPDATA%\NAudioDemo\dumps\`. Already validated
  against the manual repro; ready for step 4's bisect work.

---

## Progress log

| Date | Step | Notes |
| --- | --- | --- |
| 2026-04-28 | Branch created | `naudio3dev-coreaudio-activation` from `naudio3dev` |
| 2026-04-28 | Headline activation fix landed | `MMDeviceEnumerator..ctor` migrated to `ComActivation.CreateInstance<IMMDeviceEnumerator>(...)`; `Dispose(bool)` now does `((object)realEnumerator is ComObject co) co.FinalRelease()`; `Interfaces/MMDeviceEnumeratorComObject.cs` deleted. Build clean. NAudioTests: 1170 passed / 14 skipped (missing local files) / 0 failed across all 1184 tests. |
| 2026-04-28 | AOT smoke checkpoint | `dotnet publish -p:PublishTrimmed=true -p:BuiltInComInteropSupport=false`: publish has zero IL2026/IL3050 warnings; runtime no longer throws `NotSupportedException` from `MMDeviceEnumerator..ctor` line 48. Next crash now lands at `EnumerateAudioEndPoints` line 66 — a `Marshal.GetObjectForIUnknown` bridge site, which is exactly the sweep work captured in the call-site inventory above. Under `BuiltInComInteropSupport=true`: also zero warnings at publish time (better than the prompt anticipated — trim graph from this smoke entry point doesn't pull MediaFoundation paths). PublishAot blocked on local VS toolchain (vswhere/link.exe), unrelated to this work. |
| 2026-04-28 | Leaf wrappers migrated | `AudioRenderClient`, `AudioCaptureClient`, `AudioClockClient` now project via `ComActivation.ComWrappers.GetOrCreateObjectForComInstance(ptr, UniqueInstance)`, release the input IntPtr immediately, and dispose via `((object)field is ComObject co) co.FinalRelease()`. The dual-pointer (`nativePointer` + RCW) pattern is gone — single ownership through the wrapper. NAudioTests: 1170/14/0 (no regressions). |
| 2026-04-28 | Phase 2e narrow scope COMPLETE | All 30 CoreAudio bridge sites migrated. AOT smoke (`PublishTrimmed=true -p:BuiltInComInteropSupport=false`) now runs end-to-end: 7 active render endpoints enumerated, all property paths (VT_LPWSTR / VT_UI4 / VT_BLOB) read correctly. NAudioTests: 1170/14/0. DICASTABLE-confirmed cross-casts: `audioClientInterface as IAudioClient2/3`, `audioSessionInterface as IAudioSessionManager2`, `audioSessionControlInterface as IAudioSessionControl2 / IAudioMeterInformation / ISimpleAudioVolume`, `connectorInterface as IPart`, `(IMMEndpoint)deviceInterface`. Closed pre-existing under-release leaks: `MMDevice.deviceInterface`, `MMDeviceCollection.mmDeviceCollection`, `AudioClient.audioClientInterface`, `AudioSessionManager.audioSessionInterface`, `SessionCollection.audioSessionEnumerator`. Manual smoke testing of NAudioDemo / NAudioWpfDemo still TODO before merge. |
| 2026-04-28 | GC-safety investigation | NAudioDemo manual repro (WasapiPlayer playback → stop → Volume Mixer) fast-fails with two distinct bugs that were getting conflated: (1) `AudioClient.Dispose`'s defensive triple-`FinalRelease` from earlier commit 9adbdf4 was self-inflicted — DICASTABLE returns the same `ComObject` for all three IAudioClient/2/3 references, and calling `FinalRelease` repeatedly on the alias AVs in `Marshal.Release`. (2) A separate finalizer-thread AV class on whichever wrapper goes through `ComObject.Finalize` first — captured via `MiniDumpInstaller` console + log scaffolding in NAudioDemo. Apartment-Release hypothesis falsified: `IAgileObjectProbeTests` shows 4 of 12 CoreAudio interfaces have no agile / no FTM / no PSR (so classic RCW couldn't have marshaled either), implying their `Release` is just `InterlockedDecrement` and apartment isn't the issue. Likely cause is a use-after-free on a cached QI'd vtable inside `StrategyBasedComWrappers`. Fixes applied: single `FinalRelease` in `AudioClient.Dispose` (closes Bug A), `WrapUnique`'s `GC.SuppressFinalize` retained (masks Bug B). New diagnostic tests: `IAgileObjectProbeTests` (agility matrix), `WrapUniqueCrashRepro` (`[Explicit]` — does not yet trip the demo crash headlessly). Comments in `ComActivation.WrapUnique` and `AudioClient.Dispose` updated with full rationale. |
