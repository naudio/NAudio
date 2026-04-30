# CoreAudio Activation + ComWrappers Bridging Modernization

> **Status: COMPLETE** (Phase 2e in [MODERNIZATION.md](MODERNIZATION.md)). Working branch: `naudio3dev-coreaudio-activation`.

Phase 2e migrated `NAudio.Wasapi/CoreAudioApi/` off the legacy `[ComImport]` activation pattern (`new SomeComObject()`) and the classic-RCW `Marshal.GetObjectForIUnknown` bridge. Both are replaced with raw `CoCreateInstance` + the shared `StrategyBasedComWrappers` instance owned by `ComActivation`. The AOT activation crash at `MMDeviceEnumerator..ctor` (`NotSupportedException` under `BuiltInComInteropSupport=false`) is gone; the remaining `IL2026` warning will fully clear when the MediaFoundation sweep also lands (Phase 2e′ — see below).

---

## Scope: NARROW (CoreAudio only)

`NAudio.Wasapi/MediaFoundation/` and the top-level `MediaFoundationResampler.cs` continue to use the legacy `Marshal.GetObjectForIUnknown` bridge for now. Reasons:

- The actual AOT activation crash (`MMDeviceEnumeratorComObject`) lives in CoreAudio; narrow ships the unblock.
- Wide requires retyping `MediaFoundationTransform.transform` (currently legacy `[ComImport] IMFTransform`) to a `[GeneratedComInterface]` version, which cascades through `MediaFoundationReader`, `MediaFoundationEncoder`, `StreamMediaFoundationReader`, and `MediaFoundationResampler.CreateTransform()`. Different blast radius, separate work unit.
- Narrow is bisectable: any regression on `MMDevice.Properties` / `FriendlyName` / device enumeration is unambiguously CoreAudio.

---

## Hazards (from DMO modernization — every one of these tripped us up)

1. **`StrategyBasedComWrappers` wrappers are NOT `IDisposable`.** Disposal goes through `((ComObject)(object)wrapper).FinalRelease()`. The pattern `if (foo is IDisposable d) d.Dispose()` compiles cleanly and silently no-ops, leaking the COM reference. **Audit every disposal site.**
2. **Direct `is ComObject` pattern matching from an interface type fails to compile** ("An expression of type 'X' cannot be handled by a pattern of type 'ComObject'"). Cast through `object` first: `if ((object)wrapper is ComObject co) co.FinalRelease();`.
3. **CCWs return distinct `IntPtr`s per interface — *always*, including single-interface classes.** `ComWrappers.GetOrCreateComInterfaceForObject` returns the IUnknown vtable, which lives at a different address than each typed-interface vtable even when a `[GeneratedComClass]` implements only one `[GeneratedComInterface]`. Before passing a CCW pointer to a native API typed as `IFoo*`, **always** call `Marshal.QueryInterface(unknownPtr, in IID_IFoo, out var fooPtr)` and pass the QI'd pointer. Skipping the QI makes native dispatch against the IUnknown vtable where the typed methods are expected — access violation on first invocation, frequently on a worker thread. (Phase 2f got bitten here: nine callback registration sites that initially passed the IUnknown pointer directly worked through a console smoke but AV'd in NAudioDemo's Volume Mixer the moment a registered callback fired. Fixed by funnelling each registration through a `Query<X>Interface` helper.)
4. **Same-namespace types beat `using` aliases.** Don't keep both legacy `[ComImport]` and modern `[GeneratedComInterface]` declarations of the same interface in the same namespace.
5. ~~**Callback interfaces are out of scope for Phase 2e.**~~ Resolved in Phase 2f (branch `naudio3dev-callback-interfaces`). All seven callback interfaces are now `[GeneratedComInterface]` and their managed implementors `[GeneratedComClass]`; the unused `IControlChangeNotify` was deleted. Every `Marshal.GetComInterfaceForObject` call site in `CoreAudioApi/` now goes through `ComActivation.ComWrappers.GetOrCreateComInterfaceForObject`. The `ActivateAudioInterfaceAsync` P/Invoke is `[LibraryImport]` and the caller QIs the multi-vtable CCW for `IID_IActivateAudioInterfaceCompletionHandler` before handing the pointer to native.

---

## CreateObjectFlags decision per call site

- `UniqueInstance` — for things obtained from `Activate` / `GetService` / `EnumAudioEndpoints` item access. Fresh COM objects we own. Caller is responsible for `FinalRelease()`.
- `None` — for QI'd interfaces on an object whose lifetime is owned elsewhere. Wrapper piggy-backs on the cache.

---

## Call-site inventory — CoreAudioApi/

30 sites across 18 files, plus the activation site in `MMDeviceEnumerator..ctor`. All migrated.

### Activation site

- `MMDeviceEnumerator.cs:48-51` — `new MMDeviceEnumeratorComObject()` + `GetIUnknownForObject` + `GetObjectForIUnknown` chain → `ComActivation.CreateInstance<IMMDeviceEnumerator>(CLSID, IID)`. `Interfaces/MMDeviceEnumeratorComObject.cs` deleted.

### Bridge sweep (Marshal.GetObjectForIUnknown → ComActivation.ComWrappers.GetOrCreateObjectForComInstance)

- `AudioRenderClient.cs`, `AudioCaptureClient.cs`, `AudioClockClient.cs`, `AudioStreamVolume.cs` (4 leaf wrappers)
- `AudioMeterInformation.cs`, `SimpleAudioVolume.cs` (dual-ctor with `ownsInterface` flag for borrowed-RCW pattern)
- `MMDevice.cs` (3 sites: `IPropertyStore`, `IAudioClient`, `IDeviceTopology`); `MMDevice.Dispose` now FinalReleases its `deviceInterface`
- `MMDeviceCollection.cs` (1 site, indexer); now also implements `IDisposable`
- `MMDeviceEnumerator.cs` (4 sites: `EnumerateAudioEndPoints`, `GetDefaultAudioEndpoint`, `TryGetDefaultAudioEndpoint`, `GetDevice`), via `WrapDevicePointer` helper
- `AudioSessionManager.cs` (1 site); `as IAudioSessionManager2` cross-cast preserved
- `AudioSessionControl.cs` (1 site); dual-ctor; `is IAudioMeterInformation` / `is ISimpleAudioVolume` borrowed-RCW pattern preserved
- `SessionCollection.cs` (1 site)
- `DeviceTopology.cs`, `Connector.cs`, `Part.cs` (7 sites consolidated into `WrapAndRelease<T>`), `PartsList.cs`
- `AudioEndpointVolume.cs` (1 site). Callback registration via `Marshal.GetComInterfaceForObject<AudioEndpointVolumeCallback, IAudioEndpointVolumeCallback>` is **deliberately preserved** on the legacy CCW path — `IAudioEndpointVolumeCallback` is one of the seven Phase 2f callback interfaces still on `[ComImport]`.
- `ActivateAudioInterfaceCompletionHandler.cs` (2 sites). The class itself remains `[ComImport]`-implementing (Phase 2f).

DICASTABLE-confirmed cross-casts (same `ComObject` returned, no second wrapper allocated): `audioClientInterface as IAudioClient2/3`, `audioSessionInterface as IAudioSessionManager2`, `audioSessionControlInterface as IAudioSessionControl2 / IAudioMeterInformation / ISimpleAudioVolume`, `(IMMEndpoint)deviceInterface`, `connectorInterface as IPart`.

Closed five pre-existing under-release leaks that classic RCWs were hiding via finalizer: `MMDevice.deviceInterface`, `MMDeviceCollection.mmDeviceCollection`, `AudioClient.audioClientInterface`, `AudioSessionManager.audioSessionInterface`, `SessionCollection.audioSessionEnumerator`. All now FinalRelease deterministically on Dispose.

---

## Resolution: the .NET 9 floor decision

A finalizer-thread fast-fail (`Invalid Program: attempted to call a UnmanagedCallersOnly method from managed code`) reproduced reliably in NAudioDemo (WasapiPlayer → stop → Volume Mixer). Investigation eventually root-caused it to a **.NET 8 ComWrappers regression in `IDynamicInterfaceCastable`'s CastCache** ([dotnet/runtime#90234](https://github.com/dotnet/runtime/issues/90234)) — cached vtable slots from cross-casts could point at freed native memory by the time the GC finalizer ran on them. Fixed in .NET 9 by [PR #110007](https://github.com/dotnet/runtime/pull/110007).

**Decisions:**

- **NAudio 3 floor TFM bumped from net8.0 to net9.0** across all projects.
- **No `WrapUnique` / `GC.SuppressFinalize` band-aid in NAudio.Wasapi** — call sites project COM pointers directly via `(T)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(ptr, CreateObjectFlags.UniqueInstance)`. On net9+ the wrapper finalizer correctly releases the COM ref, so missed `Dispose` is a recoverable leak rather than a process kill.
- **`AudioClient.Dispose` keeps the single-`FinalRelease` fix** — DICASTABLE returns the same `ComObject` for `audioClientInterface as IAudioClient2/3`, so a single `FinalRelease` releases all three views at once. Releasing each separately double-frees.

**How we got there:** the breakthrough was capturing the actual fast-fail string by temporarily removing `SuppressFinalize` and watching the console output. The string is searchable and led directly to runtime issues [#79971](https://github.com/dotnet/runtime/issues/79971) (mechanism), [#96901](https://github.com/dotnet/runtime/issues/96901) (UniqueComInterfaceMarshaller double-release), [#125221](https://github.com/dotnet/runtime/issues/125221) (OleDb finalizer AV), and finally PR #110007. An A/B test (NAudioDemo retargeted to net10 with `SuppressFinalize` removed) confirmed the runtime fix was the cause: clean across multiple repros on net10, fast-fails on first attempt on net8.

---

## Phase 2e′ — MediaFoundation bridge sweep (deferred)

These ~12 sites still use `Marshal.GetObjectForIUnknown`:

| File | Sites | Notes |
| --- | --- | --- |
| `NAudio.Wasapi/MediaFoundation/MediaFoundationHelpers.cs:40` | 1 | `IMFActivate` projection in enumeration |
| `NAudio.Wasapi/MediaFoundation/MfSample.cs:96, 116` | 2 | `IMFMediaBuffer` from `GetBufferByIndex` / `ConvertToContiguousBuffer` |
| `NAudio.Wasapi/MediaFoundation/MfActivate.cs:44` | 1 | `IMFTransform` from `IMFActivate::ActivateObject` |
| `NAudio.Wasapi/MediaFoundation/MfTransform.cs:78, 91, 125, 137` | 4 | `IMFMediaType` getters |
| `NAudio.Wasapi/MediaFoundation/MfSourceReader.cs:50, 62, 112` | 3 | `IMFMediaType` and `IMFSample` |
| `NAudio.Wasapi/MediaFoundationResampler.cs:74` | 1 | Bridges activated `IUnknown*` onto legacy `[ComImport] IMFTransform` because `MediaFoundationTransform.transform` is typed as the legacy interface |

The dominant blocker is **`MediaFoundationTransform.transform` being typed as legacy `[ComImport] IMFTransform`**. Migrating the field to `[GeneratedComInterface]` cascades through `MediaFoundationResampler`, `MediaFoundationReader`, `MediaFoundationEncoder`, `StreamMediaFoundationReader`, and `MfActivate`.

---

## Out of scope (deliberately deferred)

- `MediaFoundation/` bridge sweep + `MediaFoundationTransform.transform` field type — Phase 2e′ above.
- `IMMDevice.OpenPropertyStore` trimming AV — separate investigation.

---

## Progress log

| Date | Step | Notes |
| --- | --- | --- |
| 2026-04-28 | Branch created | `naudio3dev-coreaudio-activation` from `naudio3dev` |
| 2026-04-28 | Headline activation fix landed | `MMDeviceEnumerator..ctor` migrated to `ComActivation.CreateInstance<IMMDeviceEnumerator>(...)`; `Interfaces/MMDeviceEnumeratorComObject.cs` deleted |
| 2026-04-28 | AOT smoke checkpoint | `dotnet publish -p:PublishTrimmed=true -p:BuiltInComInteropSupport=false`: zero IL2026/IL3050 warnings; runtime no longer throws `NotSupportedException` from `MMDeviceEnumerator..ctor` |
| 2026-04-28 | All 30 CoreAudio bridge sites migrated | AOT smoke runs end-to-end: 7 active render endpoints enumerated, all property paths (VT_LPWSTR / VT_UI4 / VT_BLOB) read correctly. Five pre-existing under-release leaks closed |
| 2026-04-28 | Phase 2e RESOLVED — .NET 9 floor adopted | Captured the actual fast-fail message (`Invalid Program: attempted to call a UnmanagedCallersOnly method from managed code`); traced to dotnet/runtime PR #110007 (DICASTABLE CastCache fix in .NET 9). A/B test confirmed runtime regression as cause. Bumped floor TFM to net9.0 across all 14 projects, removed the `WrapUnique` / `GC.SuppressFinalize` band-aid, inlined `(T)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(ptr, CreateObjectFlags.UniqueInstance)` at 25+ sites. NAudioTests: 1171 / 15 skipped / 0 failed |
| 2026-04-29 | Phase 2f — callback interfaces migrated; `IsAotCompatible=true` | Branch `naudio3dev-callback-interfaces`. Converted six callback interfaces (`IAgileObject`, `IMMNotificationClient`, `IAudioEndpointVolumeCallback`, `IAudioSessionEvents`, `IAudioSessionNotification`, `IActivateAudioInterfaceCompletionHandler`) to `[GeneratedComInterface]` and decorated their internal/public implementors with `[GeneratedComClass]`. Deleted the unused `IControlChangeNotify`. Migrated nine `Marshal.GetComInterfaceForObject` sites across `AudioEndpointVolume`, `AudioSessionControl`, `AudioSessionManager`, and `MMDeviceEnumerator` to `ComActivation.ComWrappers.GetOrCreateComInterfaceForObject` (each in `try/finally`). Refactored the `ActivateAudioInterfaceAsync` P/Invoke from `[DllImport]` with typed COM interface to `[LibraryImport]` with raw `IntPtr`, and the single caller (`AudioClient.ActivateAsync`) now `QueryInterface`s the multi-vtable CCW for `IID_IActivateAudioInterfaceCompletionHandler` before handing it to native. Flipped `<IsAotCompatible>true</IsAotCompatible>` on `NAudio.Wasapi.csproj`: 0 IL2026/IL3050 warnings. Trimmed AotSmoke (`PublishTrimmed=true`, `BuiltInComInteropSupport=false`) runs end-to-end and enumerates endpoints. NAudioTests: 1179 / 14 skipped / 0 failed. |
