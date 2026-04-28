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

## Progress log

| Date | Step | Notes |
| --- | --- | --- |
| 2026-04-28 | Branch created | `naudio3dev-coreaudio-activation` from `naudio3dev` |
| 2026-04-28 | Headline activation fix landed | `MMDeviceEnumerator..ctor` migrated to `ComActivation.CreateInstance<IMMDeviceEnumerator>(...)`; `Dispose(bool)` now does `((object)realEnumerator is ComObject co) co.FinalRelease()`; `Interfaces/MMDeviceEnumeratorComObject.cs` deleted. Build clean. NAudioTests: 1170 passed / 14 skipped (missing local files) / 0 failed across all 1184 tests. |
| 2026-04-28 | AOT smoke checkpoint | `dotnet publish -p:PublishTrimmed=true -p:BuiltInComInteropSupport=false`: publish has zero IL2026/IL3050 warnings; runtime no longer throws `NotSupportedException` from `MMDeviceEnumerator..ctor` line 48. Next crash now lands at `EnumerateAudioEndPoints` line 66 — a `Marshal.GetObjectForIUnknown` bridge site, which is exactly the sweep work captured in the call-site inventory above. Under `BuiltInComInteropSupport=true`: also zero warnings at publish time (better than the prompt anticipated — trim graph from this smoke entry point doesn't pull MediaFoundation paths). PublishAot blocked on local VS toolchain (vswhere/link.exe), unrelated to this work. |
| 2026-04-28 | Leaf wrappers migrated | `AudioRenderClient`, `AudioCaptureClient`, `AudioClockClient` now project via `ComActivation.ComWrappers.GetOrCreateObjectForComInstance(ptr, UniqueInstance)`, release the input IntPtr immediately, and dispose via `((object)field is ComObject co) co.FinalRelease()`. The dual-pointer (`nativePointer` + RCW) pattern is gone — single ownership through the wrapper. NAudioTests: 1170/14/0 (no regressions). |
| 2026-04-28 | Phase 2e narrow scope COMPLETE | All 30 CoreAudio bridge sites migrated. AOT smoke (`PublishTrimmed=true -p:BuiltInComInteropSupport=false`) now runs end-to-end: 7 active render endpoints enumerated, all property paths (VT_LPWSTR / VT_UI4 / VT_BLOB) read correctly. NAudioTests: 1170/14/0. DICASTABLE-confirmed cross-casts: `audioClientInterface as IAudioClient2/3`, `audioSessionInterface as IAudioSessionManager2`, `audioSessionControlInterface as IAudioSessionControl2 / IAudioMeterInformation / ISimpleAudioVolume`, `connectorInterface as IPart`, `(IMMEndpoint)deviceInterface`. Closed pre-existing under-release leaks: `MMDevice.deviceInterface`, `MMDeviceCollection.mmDeviceCollection`, `AudioClient.audioClientInterface`, `AudioSessionManager.audioSessionInterface`, `SessionCollection.audioSessionEnumerator`. Manual smoke testing of NAudioDemo / NAudioWpfDemo still TODO before merge. |
