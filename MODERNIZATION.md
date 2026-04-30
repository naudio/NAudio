# NAudio 3.0 Modernization — Design Document

This document records the architectural decisions, rationale, and progress for the NAudio 3.0 modernization. It serves as a reference for ongoing development, release notes, and migration guidance.

---

## Goals

### Overall
- Establish .NET 9 as the minimum supported platform for NAudio 3.0
- Drop legacy targets (netstandard2.0, .NET Framework, netcoreapp3.1, .NET 6) across all projects
- Simplify the project structure by retiring projects that no longer have a distinct purpose
- Use the `Microsoft.NET.Sdk` for all projects (drop `Microsoft.NET.Sdk.WindowsDesktop`)

### WASAPI (NAudio.Wasapi)
- Build a complete, efficient, idiomatic .NET wrapper for the Windows Core Audio APIs
- Zero-copy buffer access via `Span<T>` — avoid unnecessary memory copies
- Reliable COM object lifetime management — no unsafe finalizers, deterministic release where it matters
- Expose all WASAPI capabilities including modern features (IAudioClient3 low-latency, process-specific loopback capture)
- Idiomatic .NET developer experience — builder pattern, async patterns, rich exceptions
- Graceful handling of versioned COM interfaces (IAudioClient2/3) with runtime capability detection

---

## Target Framework Decisions

### Minimum platform: .NET 9

**Decision:** NAudio 3.0 requires .NET 9 as a minimum. All legacy targets have been removed.

**Rationale:** .NET 9 unlocks `[GeneratedComInterface]` (source-generated COM interop), `Span<T>` in interop signatures, and trimming support — and, crucially, fixes a `IDynamicInterfaceCastable` CastCache poisoning bug that affected ComWrappers cross-casts under .NET 8 (see below).

**Why not net8.0:** the original plan was net8.0 (LTS), and the CoreAudio modernization initially shipped on it. During Phase 2e a finalizer-thread fast-fail (`Invalid Program: attempted to call a UnmanagedCallersOnly method from managed code`) was reproduced reliably in NAudioDemo. Root cause: [dotnet/runtime#90234](https://github.com/dotnet/runtime/issues/90234) regressed `IDynamicInterfaceCastable`'s CastCache in .NET 8, causing cached vtable slots from cross-casts (e.g. `audioClient as IAudioClient2`, `(IMMEndpoint)deviceInterface`) to point at freed native memory by the time the GC finalizer ran on them. The fix shipped in .NET 9 via [dotnet/runtime PR #110007](https://github.com/dotnet/runtime/pull/110007). Confirmed via A/B test: on net10 NAudioDemo runs cleanly; on net8 the same code fast-fails. Bumping the floor to net9 retires the workaround entirely (`WrapUnique`'s `GC.SuppressFinalize` was deleted) and lets the wrapper finalizer release the COM ref correctly when callers miss `Dispose`. Related runtime issues that informed the decision: [#96901](https://github.com/dotnet/runtime/issues/96901) (UniqueComInterfaceMarshaller double-release), [#125221](https://github.com/dotnet/runtime/issues/125221) (OleDb finalizer AV), [#100645](https://github.com/dotnet/runtime/issues/100645) (deterministic Release in ComWrappers).

**Migration impact:** Users on .NET Framework, .NET 6, .NET 7, or .NET 8 should stay on NAudio 2.x.

### Per-project TFMs

| Project | NAudio 2.x TFM(s) | NAudio 3.0 TFM(s) | Rationale |
| ------- | ------------------ | ------------------ | --------- |
| NAudio.Core | netstandard2.0 | net9.0 | Moved to net9.0 to unlock `Span<T>` for `IWaveProvider`/`ISampleProvider` interfaces and align with the .NET 9 floor |
| NAudio.Midi | netstandard2.0 | net9.0 | Follows NAudio.Core (depends on it) |
| NAudio.Asio | netstandard2.0; net8.0-windows | net9.0-windows | Dropped netstandard2.0 leg. Removed `Microsoft.Win32.Registry` polyfill |
| NAudio.WinMM | netstandard2.0; net6.0 | net9.0-windows | Windows-only (P/Invoke into winmm.dll). Removed netstandard2.0 and `Microsoft.Win32.Registry` polyfill |
| NAudio.WinForms | net472; netcoreapp3.1 | net9.0-windows | Windows-only WinForms controls. Removed net472 workarounds. SDK changed from `Microsoft.NET.Sdk.WindowsDesktop` to `Microsoft.NET.Sdk` |
| NAudio.Wasapi | netstandard2.0 | net9.0-windows10.0.19041.0 | Windows 10 2004 (build 19041) minimum required for process-specific loopback capture. .NET 9 minimum to dodge the .NET 8 ComWrappers DICASTABLE CastCache regression — see "Minimum platform" above |
| NAudio.Extras | net472; netcoreapp3.1; net8.0-windows10.0.19041.0; net8.0 | net9.0; net9.0-windows10.0.19041.0 | Dual target: cross-platform core + Windows-specific `AudioPlaybackEngine` (gated by `WINDOWS` define) |
| NAudio (umbrella) | net472; netcoreapp3.1; net6.0-windows; net6.0; net8.0-windows10.0.19041.0 | net9.0; net9.0-windows10.0.19041.0 | Dual target: cross-platform (Core + Midi) + Windows (WinMM, WinForms, ASIO, WASAPI). SDK changed from `Microsoft.NET.Sdk.WindowsDesktop` to `Microsoft.NET.Sdk` |
| NAudio.Uap | net9.0-windows10.0.26100 | **Retired** | See "NAudio.Uap project retirement" below |
| NAudioWpfDemo | net8.0-windows10.0.19041.0 | net9.0-windows10.0.19041.0 | Demo app. SDK changed from `Microsoft.NET.Sdk.WindowsDesktop` to `Microsoft.NET.Sdk` |
| NAudioConsoleTest | net9.0-windows10.0.26100 | net9.0-windows10.0.26100 | Test console app |

---

## NAudio.Uap Project Retirement

**Decision:** The NAudio.Uap project (containing `WasapiCaptureRT`, `WasapiOutRT`, and `WaveFileWriterRT`) is retired and will not be carried forward into NAudio 3.0.

**Background:** NAudio.Uap was created for UWP (Universal Windows Platform) apps that couldn't use the classic COM activation path (`MMDeviceEnumerator`, `CoCreateInstance`). The RT classes used WinRT APIs (`Windows.Devices.Enumeration`, `Windows.Media.Devices`, `ActivateAudioInterfaceAsync`) to work within the UWP sandbox.

**Why retire rather than merge:**

1. **Same TFM, no longer a separate concern.** NAudio.Wasapi now targets the same Windows SDK, so the WinRT APIs are equally accessible from NAudio.Wasapi. There is no platform reason for a separate assembly.

2. **Superseded by modern replacements.** `WasapiCaptureRT` and `WasapiOutRT` implement the old `IWaveIn`/`IWavePlayer` patterns — the same patterns already marked `[Obsolete]` on `WasapiCapture` and `WasapiOut`. The new `WasapiPlayer` and `WasapiRecorder` (builder pattern, zero-copy, async) are strictly more capable.

3. **WaveFileWriterRT is niche.** Its only differentiator is `Windows.Storage.StorageFile`-based async I/O for UWP sandboxed file access. This is not relevant for modern WinAppSDK/desktop apps and doesn't justify a separate project.

4. **No unique capability worth preserving.** Any WinRT device enumeration scenarios that the RT classes supported can be added to `WasapiPlayerBuilder`/`WasapiRecorderBuilder` if needed in the future.

**Classes retired:**

| Class | Replacement |
| ----- | ----------- |
| `WasapiCaptureRT` | `WasapiRecorder` via `WasapiRecorderBuilder` |
| `WasapiOutRT` | `WasapiPlayer` via `WasapiPlayerBuilder` |
| `WaveFileWriterRT` | `WaveFileWriter` (NAudio.Core) — UWP-specific StorageFile API not carried forward |

---

## WASAPI Modernization (NAudio.Wasapi)

### Key Decisions

#### 1. `[GeneratedComInterface]` for consumed interfaces, `[ComImport]` for callbacks

**Decision:** 31 COM interfaces that NAudio *calls into* (obtained from Windows) were converted to `[GeneratedComInterface]`. 7 interfaces remain as `[ComImport]`:

| Interface | Reason for staying `[ComImport]` |
| --------- | -------------------------------- |
| `IAudioSessionEvents` | Implemented by managed code as callback |
| `IAudioEndpointVolumeCallback` | Implemented by managed code as callback |
| `IMMNotificationClient` | Implemented by managed code as callback |
| `IAudioSessionNotification` | Implemented by managed code as callback |
| `IControlChangeNotify` | Implemented by managed code as callback |
| `IActivateAudioInterfaceCompletionHandler` | Implemented by managed code as callback |
| `MMDeviceEnumeratorComObject` | COM coclass for activation, not an interface |

`IPropertyStore` was migrated in Phase 2d (see below) — `GetValue` / `SetValue` now take an `IntPtr` to an unmanaged PROPVARIANT buffer, with the caller using `Unsafe.Read<PropVariant>` / `Unsafe.Write` and `PropVariantClear` for ownership. This also fixed a pre-existing leak where COM-allocated string/blob memory was never released.

**Rationale:** `[GeneratedComInterface]` is designed for calling *into* COM objects, not for implementing interfaces that COM calls *back into*. The two systems coexist correctly at runtime — confirmed by 836 passing tests and manual testing of the demo app.

#### 2. Versioned interfaces: single wrapper class with runtime QI

**Decision:** `AudioClient` wraps IAudioClient, IAudioClient2, and IAudioClient3 in a single class. At construction, it QueryInterfaces for the newer versions and stores them if available.

```csharp
public bool SupportsAudioClient2 => audioClientInterface2 != null;
public bool SupportsAudioClient3 => audioClientInterface3 != null;
```

Methods requiring newer interfaces throw `PlatformNotSupportedException` if unavailable.

**Rationale:** Users shouldn't need to know which COM interface version exists on their machine. A single `AudioClient` class with capability-check properties is more discoverable than separate `AudioClient`, `AudioClient2`, `AudioClient3` classes.

#### 3. COM lifetime: deterministic release via `Marshal.Release(IntPtr)` on stream-path objects

**Decision:** Wrapper classes that hold COM objects on the audio stream path (AudioRenderClient, AudioCaptureClient, AudioClockClient, AudioStreamVolume) store the raw COM `IntPtr` from `GetService` and call `Marshal.Release(nativePointer)` in `Dispose()`.

Other wrapper classes (AudioEndpointVolume, AudioSessionManager, PropertyStore, etc.) let the GC handle COM release.

**Rationale:** In exclusive mode, the audio device cannot be re-opened until all COM references are released. `Marshal.ReleaseComObject` doesn't work with `[GeneratedComInterface]` types (it only works with classic RCWs). `Marshal.Release(IntPtr)` directly decrements the COM reference count regardless of the managed wrapper type.

Finalizers were removed from all classes — calling `Marshal.Release` from a finalizer is undefined behavior since the COM runtime may already be torn down.

#### 4. Error handling: `CoreAudioException` hierarchy replacing `Marshal.ThrowExceptionForHR`

**Decision:** All HRESULT checks in CoreAudioApi/ use `CoreAudioException.ThrowIfFailed(hr)` instead of `Marshal.ThrowExceptionForHR`. CoreAudioException inherits from `COMException` for backwards compatibility.

Specific exception types for common failure modes:
- `AudioDeviceDisconnectedException` (AUDCLNT_E_DEVICE_INVALIDATED)
- `AudioFormatNotSupportedException` (AUDCLNT_E_UNSUPPORTED_FORMAT)
- `AudioDeviceInUseException` (AUDCLNT_E_DEVICE_IN_USE)
- `AudioExclusiveModeNotAllowedException` (AUDCLNT_E_EXCLUSIVE_MODE_NOT_ALLOWED)

**Rationale:** Human-readable error messages for all 30+ AUDCLNT_E_* codes. Callers can catch specific exceptions instead of checking HRESULT codes. Inheriting from `COMException` means existing `catch (COMException)` handlers still work.

#### 5. `MMDevice.AudioClient` property renamed to `CreateAudioClient()` method

**Decision:** New `CreateAudioClient()` method with clear ownership semantics. Old property kept with `[Obsolete]`.

**Rationale:** The property created a new `AudioClient` instance on every access, which violates the principle of least surprise for a property. The method name makes the allocation and ownership transfer explicit.

### What's Been Done

#### Phase 1: Safety cleanup
- Removed dangerous finalizers from 5 classes (AudioEndpointVolume, MMDevice, AudioSessionControl, AudioSessionManager, SimpleAudioVolume)
- Standardized IDisposable patterns across all wrapper classes
- Fixed callback unregistration in Dispose (no longer throws via `Marshal.ThrowExceptionForHR`)

#### Phase 2a: Infrastructure
- Changed target framework from `netstandard2.0` to `net8.0-windows10.0.19041.0`
- Updated downstream projects to compatible TFMs
- Added `WASAPI` conditional define in NAudio umbrella project for `AudioFileReader` MediaFoundation fallback
- Created `CoreAudioException` hierarchy with human-readable messages for all AUDCLNT_E_* codes
- Added MMCSS P/Invoke (`AvSetMmThreadCharacteristics`, `AvRevertMmThreadCharacteristics`) to NativeMethods
- `IWaveProvider` interface updated to Span-based `Read(Span<byte>)` signature (moved to NAudio.Core in Phase 7a)
- Uncommented and made public `AudioClientActivationParams`, `AudioClientProcessLoopbackParams`, `ProcessLoopbackMode`, `AudioClientActivationType` for process-specific loopback capture

#### Phase 2b: COM interface conversion
- Converted 30 interfaces to `[GeneratedComInterface]` (all `internal partial`, all methods `[PreserveSig]`)
- Created new `IAudioClient3` interface (low-latency shared mode: `GetSharedModeEnginePeriod`, `InitializeSharedAudioStream`, `GetCurrentSharedModeEnginePeriod`)
- Versioned interfaces (IAudioClient2, IAudioClient3, IAudioClock2, IAudioSessionControl2, IAudioSessionManager2, IAudioVolumeLevel) redeclare parent methods in vtable order
- All interface output parameters that return COM objects use `out IntPtr` with `Marshal.GetObjectForIUnknown` in wrappers
- All callback parameters use `IntPtr` with `Marshal.GetComInterfaceForObject` in wrappers

#### Phase 2c: Wrapper class modernization
- `AudioClient`: versioned QI (IAudioClient2/3), `CoreAudioException.ThrowIfFailed`, WaveFormat marshaled to IntPtr, IAudioClient3 low-latency methods (`GetSharedModeEnginePeriod`, `InitializeSharedAudioStream`)
- `AudioRenderClient`: `RenderBufferLease` ref struct for zero-copy rendering, `nativePointer` deterministic release
- `AudioCaptureClient`: `CaptureBufferLease` ref struct for zero-copy capture, `nativePointer` deterministic release
- `AudioClockClient`, `AudioStreamVolume`: `nativePointer` deterministic release pattern
- `MMDevice`: `CreateAudioClient()` method, old property `[Obsolete]`, all Activate calls use `Marshal.GetObjectForIUnknown`
- `MMDeviceEnumerator`: `TryGetDefaultAudioEndpoint` added, COM activation bridged via `GetIUnknownForObject`/`GetObjectForIUnknown`
- `AudioEndpointVolume`: SynchronizationContext for event marshaling, callback registration via `Marshal.GetComInterfaceForObject`
- `AudioSessionControl`, `AudioSessionManager`: callback registration/unregistration updated for IntPtr pattern
- All 14 CoreAudioApi wrapper files migrated from `Marshal.ThrowExceptionForHR` to `CoreAudioException.ThrowIfFailed` (50 call sites)

#### Phase 2d: IPropertyStore migration

- `IPropertyStore` converted to `[GeneratedComInterface]` with `IntPtr` PROPVARIANT parameters; `PropertyStore` now allocates a stack PROPVARIANT, calls the COM method, projects via `Unsafe.Read<PropVariant>`, and calls `PropVariantClear` in a `finally` block — closing a pre-existing leak of COM-allocated LPWSTR/BLOB/CLSID memory
- `PropertyStoreProperty.Value` now stores the resolved managed object (string, uint, byte[], Guid, ...) rather than a `PropVariant` struct, so callers cannot hold dangling pointers after Clear
- `PropertyStore.GetValue(int) → PropVariant` marked `[Obsolete]` (returned struct may contain dangling pointers for VT_LPWSTR/VT_BLOB/VT_CLSID); the indexer overloads are the safe replacement
- New `NAudio.CoreAudioApi.Interfaces.VarType` enum replaces `System.Runtime.InteropServices.VarEnum` (`SYSLIB0050`-obsolete) on `PropVariant.DataType` and in switch logic — values match the COM ABI so it's interchangeable with `PROPVARIANT.vt`. Both `#pragma warning disable CS0618` suppressions for `VarEnum` are removed
- `PropVariantNative` class folded into `PropVariant` itself as a private `[DllImport]` (the dead `WINDOWS_UWP` branch and the unused `ref PropVariant` overload are removed); `PropVariant.Clear(IntPtr)` remains the public API
- Dead `propertyStoreInterface` fields removed from `WindowsMediaMp3Decoder` and `DmoResampler` (assigned but never used — they aliased the same coclass already released elsewhere)
- AOT smoke-tested with `dotnet publish -p:PublishTrimmed=true`: `IPropertyStore`/`PropVariant` paths produce zero IL2026/IL2050/IL3050 warnings; remaining trim/AOT blockers are in the `[ComImport]` activation pattern (`MMDeviceEnumeratorComObject`) and the pervasive `Marshal.GetObjectForIUnknown` bridging — to be addressed in a follow-up Phase 2e
- `<IsAotCompatible>true</IsAotCompatible>` deliberately not yet enabled — Phase 2e (CoreAudio activation via raw `CoCreateInstance` + `StrategyBasedComWrappers`) and the seven remaining `[ComImport]` callback interfaces (Phase 2f) still need to land first

#### Phase 2e: CoreAudio activation + ComWrappers bridging

- All 30 `Marshal.GetObjectForIUnknown` bridge sites in `CoreAudioApi/` migrated to `ComActivation.ComWrappers.GetOrCreateObjectForComInstance(ptr, CreateObjectFlags.UniqueInstance)`. Headline AOT activation crash (`MMDeviceEnumerator..ctor` throwing `NotSupportedException` under `BuiltInComInteropSupport=false`) is gone; `dotnet publish -p:PublishTrimmed=true -p:BuiltInComInteropSupport=false` runs the demo end-to-end with zero IL2026/IL3050 warnings.
- Closed five pre-existing under-release leaks that classic RCWs were hiding via finalizer: `MMDevice.deviceInterface`, `MMDeviceCollection.mmDeviceCollection`, `AudioClient.audioClientInterface`, `AudioSessionManager.audioSessionInterface`, `SessionCollection.audioSessionEnumerator` — all now FinalRelease the wrapper deterministically on Dispose.
- DICASTABLE-confirmed cross-casts (same `ComObject` returned, no second wrapper allocated): `audioClientInterface as IAudioClient2/3`, `audioSessionInterface as IAudioSessionManager2`, `audioSessionControlInterface as IAudioSessionControl2 / IAudioMeterInformation / ISimpleAudioVolume`, `(IMMEndpoint)deviceInterface`, `connectorInterface as IPart`. The classic borrowed-RCW pattern in `AudioMeterInformation` / `SimpleAudioVolume` is preserved.
- Investigation that drove the .NET 9 floor decision: NAudioDemo's `WasapiPlayer → Volume Mixer` repro consistently fast-failed with `Invalid Program: attempted to call a UnmanagedCallersOnly method from managed code` on the GC finalizer thread under .NET 8. Root-cause traced through [dotnet/runtime#79971](https://github.com/dotnet/runtime/issues/79971) (the exact UCO message), [#96901](https://github.com/dotnet/runtime/issues/96901) (UniqueComInterfaceMarshaller double-release), [#125221](https://github.com/dotnet/runtime/issues/125221) (OleDb finalizer AV), to [PR #110007](https://github.com/dotnet/runtime/pull/110007) — `IDynamicInterfaceCastable` CastCache poisoning, regressed in .NET 8 via [#90234](https://github.com/dotnet/runtime/issues/90234), fixed in .NET 9. Confirmed via A/B test: temp-bumping NAudioDemo to net10 with `GC.SuppressFinalize` removed runs cleanly across multiple repros; net8 fast-fails. Bumping the floor to net9 retired the workaround entirely — call sites now project directly via `(T)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(...)`.
- `<IsAotCompatible>true</IsAotCompatible>` still pending — needs Phase 2f (the seven `[ComImport]` callback interfaces) before it can be flipped.

#### Phase 2f: Callback interfaces (CCW direction migrated; IsAotCompatible flip deferred)

- Six remaining `[ComImport]` callback interfaces converted to `[GeneratedComInterface]`: `IAgileObject` (empty marker — the source generator accepts method-less interfaces), `IMMNotificationClient` (public, `StringMarshalling.Utf16`), `IAudioEndpointVolumeCallback`, `IAudioSessionEvents` (public, `StringMarshalling.Utf16`), `IAudioSessionNotification`, `IActivateAudioInterfaceCompletionHandler`. The unused `IControlChangeNotify` (declared but never implemented or registered) was deleted.
- All four managed implementor classes decorated with `[GeneratedComClass]` and made `partial`: `AudioEndpointVolumeCallback`, `AudioSessionEventsCallback` (public), `AudioSessionNotification`, `ActivateAudioInterfaceCompletionHandler` (which also implements `IAgileObject`, so the CCW now exposes both vtables).
- All nine `Marshal.GetComInterfaceForObject` sites in `CoreAudioApi/` migrated to `ComActivation.ComWrappers.GetOrCreateComInterfaceForObject` followed by `Marshal.QueryInterface` for the specific callback IID before the pointer is handed to native. Each call site is funnelled through a `Query<X>Interface` helper (`AudioEndpointVolume.QueryCallbackInterface`, `AudioSessionControl.QueryEventsInterface`, `AudioSessionManager.QueryNotificationInterface`, `MMDeviceEnumerator.QueryNotificationClientInterface`) and wrapped in `try/finally` so the CCW IUnknown ref is released even if Register/Unregister throws. Sites covered: `AudioEndpointVolume.cs` (register + Dispose), `AudioSessionControl.cs` (Dispose, RegisterEventClient, UnRegisterEventClient), `AudioSessionManager.cs` (RefreshSessions + UnregisterNotifications), `MMDeviceEnumerator.cs` (RegisterEndpointNotificationCallback + UnregisterEndpointNotificationCallback).
- `ActivateAudioInterfaceAsync` P/Invoke refactored from `[DllImport]` (with typed COM interface argument and `PreserveSig=false`) to `[LibraryImport]` with raw `IntPtr` and `int` HRESULT return. The single caller (`AudioClient.ActivateAsync`) `Marshal.QueryInterface`s the CCW for `IID_IActivateAudioInterfaceCompletionHandler` before passing the pointer to native.
- **CCW QI-for-IID rule, generalised.** ComWrappers CCWs return distinct `IntPtr`s per interface — *including* a separate IUnknown vtable for single-interface `[GeneratedComClass]` types. The Phase 2e hazard note that originally read as "multi-vtable CCWs need QI" was misread as "only multi-interface CCWs need QI"; the original Phase 2f migration shipped without QI on the single-interface callbacks and access-violated on the WASAPI worker the first time `IAudioEndpointVolumeCallback.OnNotify` fired (master volume slider in NAudioDemo, repro'd standalone with `Tools/CallbackRepro`). The fix above + the clarified hazard wording in `CoreAudioActivationModernization.md` close that.
- **Verification:**
  - NAudioTests: 1179 / 14 skipped / 0 failed.
  - `Tools/CallbackRepro` console smoke: 50 master-volume changes drive 42 callbacks cleanly, no AV.
  - `dotnet publish -p:PublishTrimmed=true -p:BuiltInComInteropSupport=false` against NAudioAotSmokeTest: runs end-to-end, zero IL2026/IL3050 against `NAudio.Wasapi`.
  - **`PublishAot=true` + `BuiltInComInteropSupport=false` against NAudioAotSmokeTest** (RCW + CCW directions): 7 endpoints enumerated with full property reads (VT_LPWSTR / VT_UI4 / VT_BLOB), `IMMNotificationClient` registers HR=0, four master-volume changes drove four `OnVolumeNotification` callbacks, clean exit. Strongest signal Phase 2f can have under genuine NativeAOT whole-program analysis.
  - Manual smoke in NAudioDemo Volume Mixer panel: master fader, mute, session faders all work without crashing. New session panels appear live via `IAudioSessionNotification.OnSessionCreated`.
- **`<IsAotCompatible>true</IsAotCompatible>` deliberately deferred.** The CoreAudio path is genuinely AOT-correct, but `NAudio.Wasapi/MediaFoundation/` still uses legacy `Marshal.GetObjectForIUnknown`. The trim/AOT analyzer doesn't flag those calls (they're not annotated with `[RequiresUnreferencedCode]`/`[RequiresDynamicCode]`) so the build is clean either way — but a `BuiltInComInteropSupport=false` consumer that touches MediaFoundation would still fail at runtime. Flipping the flag now would overstate readiness; flip it once Phase 2e′ lands.

**Test-harness coverage added in this phase:**

- `NAudioDemo` "Device Notifications" plugin panel — registers an `IMMNotificationClient` and shows a live event log + auto-refreshing endpoint snapshot. Exercises the public CCW interface that previously had no coverage anywhere in the repo.
- `NAudioDemo` Recording and WASAPI playback panels now self-refresh their device combos when devices are added/removed/state-changed/default-changed, via `Utils/DeviceChangeNotifier`. Selection is preserved by ID across refreshes. This exercises `IMMNotificationClient` during normal demo use.
- `NAudioDemo` Volume Mixer now subscribes to `AudioSessionManager.OnSessionCreated` and adds new session panels live, with a `Debug.WriteLine` breadcrumb. Closes the long-standing "TODO: Sessions create and dispose events are not handled".
- `NAudioConsoleTest` WASAPI menu adds two callback-direction modes: "Watch device notifications" (logs `IMMNotificationClient` events for headless / AOT-publish smoke) and "Stress endpoint volume callbacks" (50-set master-volume bash; this is the test that actually surfaced the QI-for-IID bug when run as a standalone repro).
- `NAudioAotSmokeTest` (promoted from a local-only `Tools/AotSmoke` sandbox into a tracked solution project — built as part of `NAudio.slnx` so the trim/AOT analyzer runs on every CI build, with `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` so a future `IL2026`/`IL3050` regression in `NAudio.Wasapi` or `NAudio.Core` fails the PR build). Exercises both directions of the source-generated COM bridging: RCW (property reads via `IPropertyStore`/`PropVariant`) and CCW (`IMMNotificationClient` registration + `IAudioEndpointVolumeCallback` dispatch under `PublishAot=true`). The runtime smoke (publish + run) is a manual local validation step because CI agents typically lack a real audio device — see `NAudioAotSmokeTest/README.md`.

**Breaking changes:**

- `IMMNotificationClient` is now `[GeneratedComInterface]`. User implementations should add `[GeneratedComClass]` and `partial` to their classes for AOT/trim cleanliness; the runtime fallback in `StrategyBasedComWrappers` handles un-decorated implementations but with reflective vtable computation (no AOT guarantee).
- `IAudioSessionEvents` shape: `[MarshalAs(UnmanagedType.Bool)] bool isMuted` → `int isMuted` on `OnSimpleVolumeChanged` (the new source generator does not project `UnmanagedType.Bool` for CCWs; `AudioSessionEventsCallback` converts `isMuted != 0` before delegating to the managed `IAudioSessionEventsHandler`, so the user-facing handler interface is unchanged). Per-method `[MarshalAs(UnmanagedType.LPWStr)]` removed in favour of interface-level `StringMarshalling.Utf16`.

**Out of scope, deferred to follow-up phases:**

- ~~Phase 2e′: MediaFoundation bridge sweep~~ — landed 2026-04-30, see Phase 2e′ section below.
- ~~AOT-flag flip on `NAudio.Wasapi`~~ — landed 2026-04-30 with Phase 2e′. `<IsAotCompatible>true</IsAotCompatible>` is now on `NAudio.Wasapi.csproj`.
- AOT story for `NAudio.Core`, `NAudio.WinMM`, `NAudio.Midi`, `NAudio.Asio`: untouched by this phase. Core/WinMM/Midi probably small audits each; ASIO is multi-phase like WASAPI.
- `AudioClient.ActivateAsync` (the only path that exercises `IActivateAudioInterfaceCompletionHandler`): zero callers in the repo, never invoked at runtime by the test harness. The QI-for-IID logic is correct by inspection but unverified end-to-end. Will get exercised when process-loopback capture lands.

#### Phase 2e′: MediaFoundation bridge sweep + IsAotCompatible flag flip ([MediaFoundationActivationModernization.md](MediaFoundationActivationModernization.md))

Branch `naudio3dev-mediafoundation-bridge`. Closes the MediaFoundation half of NAudio.Wasapi's COM modernization; the CoreAudio side (Phase 2e + 2f) was AOT-correct from the runtime perspective but the assembly couldn't honestly carry `<IsAotCompatible>` because MediaFoundation still failed under `BuiltInComInteropSupport=false` (the analyzer was silent because `Marshal.GetObjectForIUnknown` carries no `[RequiresUnreferencedCode]`/`[RequiresDynamicCode]` annotation — Hazard H11 in the working doc).

**Touch surface (5 categories, ~50+ sites):**

- **Cat A — bridge sites (12):** `Marshal.GetObjectForIUnknown` → `ComActivation.ComWrappers.GetOrCreateObjectForComInstance(ptr, UniqueInstance)` across `MediaFoundationHelpers.cs`, `MfSample.cs`, `MfActivate.cs`, `MfTransform.cs`, `MfSourceReader.cs`, `MediaFoundationResampler.cs`. Centralised inside `MediaFoundationApi` factories, reducing per-consumer projection noise.
- **Cat B — `[DllImport]` parameter types (~14 across 11 declarations):** `MediaFoundationInterop.cs` p/invokes now take `IntPtr` instead of legacy `[ComImport]` interface types — the runtime's classic-COM marshaller for `out IMFMediaType ppMFType` etc. is unavailable under `BuiltInComInteropSupport=false`, so the modern shape is `out IntPtr` with explicit `ComWrappers` projection at the call site. Removed unused `MFCreateMFByteStreamOnStreamEx` (the `[MarshalAs(UnmanagedType.IUnknown)] object` site).
- **Cat C — `Marshal.ReleaseComObject` sites (22):** all replaced with `((ComObject)(object)x).FinalRelease()` — the cast through `object` is required because `is ComObject` from an interface-typed variable doesn't compile (Phase 2e Hazard #2). Net result: zero `Marshal.ReleaseComObject` calls in `NAudio.Wasapi`.
- **Cat D — type cascade (5 files):** `MediaFoundationTransform.transform`, `MediaFoundationApi` factory return types (now `(IntPtr Ptr, T Rcw)` tuples), `MediaFoundationReader.pReader`, `MediaFoundationEncoder` locals, `MediaType.mediaType`. `MediaType.MediaFoundationObject` accessor changed semantically from `IMFMediaType` to `IntPtr` for native pass-through to the modern IntPtr-typed signatures. `MftOutputDataBuffer` struct fields changed from `IMFSample`/`IMFCollection` to `IntPtr` so the struct is fully blittable and can be pinned for `ProcessOutput`.
- **Cat E — ComStream CCW direction (Step 5):** new local `[GeneratedComInterface] IStream` (IID `0000000C-0000-0000-C000-000000000046`) in `NAudio.MediaFoundation.Interfaces`, plus a blittable `StorageStat` mirror struct. ComStream is now `[GeneratedComClass] partial`. `MediaFoundationApi.CreateByteStream` applies the Phase 2f H3 QI-for-IID rule with explicit `Marshal.QueryInterface(unkPtr, in IID_IStream, out streamPtr)` before handing the pointer to `MFCreateMFByteStreamOnStream` — the IUnknown returned by `GetOrCreateComInterfaceForObject` is NOT the IStream vtable, and skipping the QI would AV on the first `IStream::Stat` call.

**Closed pre-existing leaks (4):** `partialMediaType` in `MediaFoundationReader.CreateReader` (wrapped in try/finally), `outputMediaType` in `GetCurrentWaveFormat` (`using var`), `byteStream` in stream-overload `MediaFoundationEncoder.CreateSinkWriter`, and the latent classic-RCW finalizer-deferred releases in all `Mf*` wrappers (now FinalRelease deterministically on Dispose). Same precedent as Phase 2e closing 5 leaks in CoreAudio.

**Legacy file deletion (Step 6):** 13 files removed (12 legacy `[ComImport] IMF*.cs` at the root of `NAudio.Wasapi/MediaFoundation/`, plus the unused modern `Interfaces/IMFReadWriteClassFactory.cs` whose interface and coclass had no callers). -1492 lines. Net result: zero `[ComImport]` declarations in the entire `NAudio.Wasapi/MediaFoundation/` directory tree.

**Smoke test extension + flag flip (Step 7):** `NAudioAotSmokeTest/Program.cs` gained a third "MediaFoundation round-trip" section that exercises encode-to-stream → decode-from-stream → `MediaFoundationResampler` (covering RCW + CCW + IMFTransform paths). Annotated `FieldDescriptionHelper.Describe` with `[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]` to clear an IL2070 surfaced by the new section. `<IsAotCompatible>true</IsAotCompatible>` flipped on `NAudio.Wasapi.csproj`.

**Verification:**

- `dotnet build NAudio.slnx -c Release` clean (zero IL2026/IL3050; `NAudioAotSmokeTest` has `TreatWarningsAsErrors=true` for those codes).
- `dotnet publish NAudioAotSmokeTest -c Release -p:PublishTrimmed=true -p:BuiltInComInteropSupport=false` runs all three sections end-to-end: 8 endpoints enumerated with property reads, 4 master-volume callbacks fired, 25158-byte MP3 encoded → 361920 bytes PCM decoded → 180960 bytes resampled to 22050Hz.
- NAudioTests: 1180 / 14 skipped / 0 failed (one new test `CanRoundTripStreamThroughMediaFoundationCcwPath` added — round-trips through both CCW legs, fails fast with an AV if the QI-for-IID handoff regresses).
- Manual MP3 streaming smoke in NAudioDemo confirmed the production code path works end-to-end on top of the migrated stack.

**Breaking changes:**

- `MediaType.MediaFoundationObject` (internal) accessor type changed from `IMFMediaType` to `IntPtr`. Internal-only — no public API surface impact.
- `MediaFoundationTransform.transform` (private) field type changed from legacy `[ComImport] IMFTransform` to `Interfaces.IMFTransform`. Subclassers (rare — the only known one in the codebase is `MediaFoundationResampler`) need to update their `CreateTransform` override to return the modern type. Binary-compat break, not source.
- The legacy `[ComImport]` `IMFXxx` interface declarations in `NAudio.MediaFoundation` namespace are gone. They were `internal` so no public surface impact, but anyone who reflectively referenced them (or built via `InternalsVisibleTo`) is affected.

**Out of scope (still deferred):**

- AOT story for sister assemblies (`NAudio.Core`, `NAudio.WinMM`, `NAudio.Midi`, `NAudio.Asio`).
- Process-loopback capture activation (the only consumer of `AudioClient.ActivateAsync` and therefore the only path that exercises `IActivateAudioInterfaceCompletionHandler`). Currently throws `NotImplementedException`.
- Full `PublishAot=true` validation from a Visual Studio Developer Command Prompt — the trimmed-only run with `BuiltInComInteropSupport=false` is a strong proxy because the failure modes are the same machinery; the AOT step adds whole-program code generation but the same dispatch shape.

#### Phase 2g: DirectSoundOut migration + NAudio.Core AOT enablement ([DirectSoundActivationModernization.md](DirectSoundActivationModernization.md))

Branch `naudio3dev-directsound-migration`. Closes [GitHub issue #1191](https://github.com/naudio/NAudio/issues/1191) — `DirectSoundOut` was failing under `<PublishTrimmed>true</PublishTrimmed>` because the trimmer strips `System.StubHelpers.InterfaceMarshaler` (the type the legacy `[ComImport] [Out, MarshalAs(UnmanagedType.Interface)] out IDirectSound` marshalling depends on). Also enables `<IsAotCompatible>true</IsAotCompatible>` on `NAudio.Core.csproj` — the move and an incidental `NativeMethods.cs` deletion left `NAudio.Core` with zero P/Invokes and zero COM interop.

**Scope (single-file migration, no cascade):**

- **DirectSoundOut moves assemblies** — `NAudio.Core/Wave/WaveOutputs/DirectSoundOut.cs` → `NAudio.Wasapi/DirectSound/DirectSoundOut.cs`. DirectSoundOut had zero internal references in `NAudio.Core` (it implemented `IWavePlayer` from `NAudio.Core` but nothing consumed it back), so the cut was clean. `namespace NAudio.Wave` preserved; the `NAudio` meta-package already references both assemblies so consumers of the meta-package see no API change. **Breaking change** for direct consumers of `NAudio.Core` who use `DirectSoundOut` — they now need a `NAudio.Wasapi` package reference.
- **3 `[ComImport]` interfaces → `[GeneratedComInterface]`** (`IDirectSound`, `IDirectSoundBuffer`, `IDirectSoundNotify`) broken out into `NAudio.Wasapi/DirectSound/Interfaces/`. All slots `[PreserveSig] int` returning HRESULT. Unused vtable slots declared with `IntPtr` placeholders — in particular, `IDirectSoundBuffer.SetFormat`'s `WAVEFORMATEX` parameter (a managed-reference-type-at-a-COM-boundary hazard) is dead code; the wave format reaches the secondary buffer via `BufferDescription.lpwfxFormat` (pinned `GCHandle`).
- **4 `[DllImport]` P/Invokes → `[LibraryImport]`** (`DirectSoundCreate`, `DirectSoundEnumerate`, `GetDesktopWindow`, plus the previously-elided `DirectSoundCaptureCreate` was never declared). `DirectSoundCreate` switches from `out IDirectSound` (the failing-under-trim path) to `out IntPtr` + `ComActivation.ComWrappers` projection.
- **`DSEnumCallback` delegate → `[UnmanagedCallersOnly]` static thunk + C# function pointer** (`delegate* unmanaged[Stdcall]<...>`). Zero allocation, no GCHandle pinning, no `Marshal.GetFunctionPointerForDelegate` indirection — fully AOT-clean dispatch.
- **3 `Marshal.ReleaseComObject` sites → `((ComObject)(object)x).FinalRelease()`** in `StopPlayback` (Phase 2e Hazard #2 cast-through-object pattern).
- **QI cascade `IDirectSoundBuffer → IDirectSoundNotify`** (Phase 2g-specific, similar shape to Phase 2f H3 ComStream → IStream): source-gen RCWs do not auto-QI on a sibling-interface cast. Resolved with explicit `Marshal.QueryInterface(secondaryBufferPtr, in IID_IDirectSoundNotify, out notifyPtr)` from the secondary buffer's raw `IUnknown`, projection of `notifyPtr`, and deterministic release after `SetNotificationPositions` returns. Wrapped in try/finally so a `SetNotificationPositions` failure cannot leak the wrapper.
- **`BufferDescription` / `BufferCaps` class → struct** (separate, low-risk commit before the source-gen migration). Both are blittable (no string fields), declared `[StructLayout(LayoutKind.Sequential, Pack = 2)]`. Interface signatures changed from `[In] BufferDescription` to `in BufferDescription`, etc. Eliminates the managed-reference-at-a-`[GeneratedComInterface]`-boundary hazard.
- **`DirectSoundException : COMException`** introduced parallel to `MediaFoundationException`. Existing `catch (COMException)` consumers keep working because the `[ComImport]` interfaces previously auto-threw `COMException` on failure HRESULTs.
- **`NAudio.Core/Utils/NativeMethods.cs` deleted** (incidental). Held three kernel32 P/Invokes (`LoadLibrary`/`GetProcAddress`/`FreeLibrary`); only consumer was `AcmDriver` in `NAudio.WinMM`. Replaced with `System.Runtime.InteropServices.NativeLibrary` (`TryLoad` / `TryGetExport` / `Free`) — cross-platform, AOT/trim-clean. Without this deletion, the headline claim "NAudio.Core has no Windows-specific code" would have been false. **Breaking change** for any external consumer of `NAudio.Utils.NativeMethods` (was public; should have been internal — no documented consumer).

**Smoke test extension + flag flip:** `NAudioAotSmokeTest/Program.cs` gained a fourth section "DirectSound playback (RCW direction + QI cascade)" that enumerates devices (exercising the `[UnmanagedCallersOnly]` thunk) then drives a 250 ms silent-gain Init/Play/Stop/Dispose cycle (exercising the QI cascade and the playback notification thread). `<IsAotCompatible>true</IsAotCompatible>` flipped on `NAudio.Core.csproj`. `NAudioConsoleTest` gained a DirectSound menu (List devices, Play tone) for manual verification.

**Verification:**

- `dotnet build NAudio.slnx -c Release` clean (zero IL2026/IL3050 against `NAudio.Core` and `NAudio.Wasapi`).
- `dotnet publish NAudioAotSmokeTest -c Release -p:PublishAot=false -p:PublishTrimmed=true -p:BuiltInComInteropSupport=false` runs all four sections end-to-end. DirectSound section enumerated 8 devices and completed Init/Play/Stop/Dispose without exception — the exact path issue #1191 was failing on.
- NAudioTests: 1071 / 1073 (2 skipped, 0 failed), DirectSound `CanEnumerateDevices` integration test passes — exercises the `[UnmanagedCallersOnly]` enumeration path.

**Breaking changes:**

- `DirectSoundOut` and `DirectSoundDeviceInfo` moved from `NAudio.Core.dll` to `NAudio.Wasapi.dll`. `namespace NAudio.Wave` preserved. Consumers of the `NAudio` meta-package see no change; direct consumers of `NAudio.Core` need to add a `NAudio.Wasapi` package reference. Type-forwarding is not viable — would invert the `Core ← Wasapi` dependency direction.
- `NAudio.Utils.NativeMethods` (held three kernel32 P/Invokes) deleted. Was public but should have been internal. Migrate to `System.Runtime.InteropServices.NativeLibrary`.
- `BufferDescription` / `BufferCaps` (internal) changed from `class` to `struct`. Internal-only — no public API surface impact.
- The legacy `[ComImport]` `IDirectSound*` interface declarations under the inner `DirectSoundOut.NativeDirectSoundCOMInterface` region are gone. They were nested-private within `DirectSoundOut`, so no public surface impact.

**Out of scope (still deferred):**

- AOT story for the remaining sister assemblies (`NAudio.WinMM`, `NAudio.Midi`, `NAudio.Asio`). The `AcmDriver` port to `NativeLibrary` lands incidentally; the broader MMSYS surface audit is its own phase.
- Full `PublishAot=true` validation from a VS Developer Command Prompt (same proxy argument as Phase 2e′ — trimmed-only run is a strong signal for the same dispatch shape).

#### Phase 2h: Mp3FileReader lazy table-of-contents ([Tools/prompts/phase-2h-mp3filereader-lazy-toc.md](Tools/prompts/phase-2h-mp3filereader-lazy-toc.md))

Branch `naudio3dev`. Closes [GitHub issue #1119](https://github.com/naudio/NAudio/issues/1119) — opening a multi-hour MP3 over a network share blocked for seconds because the constructor walked every frame in the file to build the table-of-contents (TOC). Now the TOC is built opportunistically: seeded with the first frame at construction, extended as `Read` consumes frames or as `Position` is set forward past the scanned tail. Total length is reported from the Xing/Info `Frames` field when present (free, exact for any encoder that wrote one), or estimated from the first frame's bitrate (exact for CBR, approximate for headerless VBR), with a new opt-in `EnsureExactLengthAsync` for the rare consumer who needs an exact duration on a headerless VBR file without playing it.

**Scope (single-file change in `NAudio.Core`, no API moves):**

- **`Mp3FileReaderBase` constructor no longer scans the file.** The old `CreateTableOfContents()` (which read every frame header in the file before returning) is replaced by `SeedTableOfContents(firstFrame)` (one entry) plus `EstimateTotalSamples(firstFrame)`. Open time is now bounded by ID3v2 read + first-frame parse + Xing detection + ID3v1 read, independent of file size.
- **`Length` is best-estimate by default.** Tier 1: Xing/Info `Frames` field (most VBR encoders write one) → exact. Tier 2: `mp3DataLength × 8 × sampleRate / firstFrameBitRate` → exact for CBR, approximate for headerless VBR. Tier 3: replaced with the exact frame-summed value once a sequential read reaches EOF or `EnsureExactLengthAsync` runs. **Behavioural breaking change** for callers who relied on `Length` being exact for headerless VBR — see migration note below.
- **New `IsLengthExact { get; }` property.** Returns `true` if `Length` is the exact frame-summed value, `false` if it is an estimate. Lets consumers decide whether to call `EnsureExactLengthAsync`.
- **New `EnsureExactLengthAsync(CancellationToken)` method.** Async-only (the operation is pure I/O on a potentially large or networked file; sync would be a UI-thread footgun). Idempotent (returns `Task.CompletedTask` if already exact). Restores `Position` after scanning. Safe to call concurrently with `Read` / `Position` set — guarded by the same `repositionLock` that already serialises playback and seek.
- **`ReadNextFrame` and the `Position` setter extend the TOC inline.** When a `Read` consumes a frame past `scannedToFilePosition`, a TOC entry is appended (zero extra I/O — the frame is already being read). When `Position` is set past `scannedToSamplePosition`, the setter calls `ExtendTableOfContentsTo(target)` first, scanning forward frame-by-frame and appending entries until reaching the target or EOF; backward seeks within the scanned region use the existing TOC lookup unchanged.
- **`Mp3WaveFormat.AverageBytesPerSecond` now reflects the first frame's bitrate, not the file-wide average.** The averaged value required a full file scan; ACM/DMO/MFT/NLayer decoders read frame-by-frame and ignore this field, so the visible behaviour change is informational metadata only.
- **Dead code removed:** `CreateTableOfContents()`, the private `TotalSeconds()` helper, the redundant second `Mp3WaveFormat` allocation, and an unused `bitRate` local that the old code overwrote anyway.

**Test infrastructure additions:**

- `NAudioTests/Utils/CountingStream.cs` — `Stream` wrapper that counts bytes returned from `Read`. Used by `Constructor_DoesNotScanEntireFile` / `Constructor_ReadCount_IsIndependentOfFileSize` to assert bounded constructor I/O.
- `TestFileBuilder.CreateMp3FileWithInfoHeader(...)` — generates a CBR MP3 then injects a synthetic Info tag (with Frames flag + true audio-frame count) into the first frame. Needed because the `MediaFoundationEncoder.EncodeToMp3` path produces CBR MP3s without any Xing/Info tag, so the existing `CreateMp3File` actually exercises the *headerless* path. The injection helper computes the Xing offset from MPEG version × channel mode and writes only the Frames field — enough for the reader to short-circuit to "exact".

**Verification:**

- `dotnet build NAudio.slnx -c Release` clean: zero warnings, zero errors.
- `NAudioTests` full run: 1192 / 1192 succeeded, 13 skipped, 1 pre-existing hardware failure (`CanInitializeInExclusiveMode` — WASAPI exclusive-mode test, unrelated). New `Mp3FileReaderLazyTocTests` (12 tests) all pass: bounded constructor I/O, IsLengthExact for tagged vs headerless, EnsureExactLengthAsync correctness/idempotency/cancellation, sequential-read flips IsLengthExact at EOF, forward-seek-past-tail produces identical PCM, backward-seek-within-tail produces identical PCM, concurrent Read + EnsureExactLengthAsync is safe, ReadNextFrame still advances Position. Existing `Mp3FileReaderTests` (`ReadFrameAdvancesPosition`, `CopesWithZeroLengthMp3`) unchanged.

**Breaking changes:**

- `Mp3FileReaderBase.Length` (and therefore `Mp3FileReader.Length`, `WaveStream.TotalTime`) is no longer guaranteed exact immediately after the constructor returns for **headerless VBR** MP3s. CBR files and VBR files with a Xing/Info header (the vast majority in the wild) are unaffected — `Length` is exact on open. **Migration:** check `IsLengthExact` and call `await reader.EnsureExactLengthAsync()` if exact length is required before playback. For UI progress bars and similar consumers, the estimate is within a few percent and updates to exact automatically once the file plays through.
- `Mp3WaveFormat.AverageBytesPerSecond` is the first frame's bitrate rather than the file-wide average. Informational only — no decoder branches on this field.

**Out of scope (deferred to subsequent phases):**

- Async I/O all the way down. `EnsureExactLengthAsync` wraps the sync `Mp3Frame.LoadFromStream` parser in `Task.Run`; replumbing through `Stream.ReadAsync` would require parser changes orthogonal to this phase.
- Binary-search lookup in `Position` setter. The TOC is sorted by `SamplePosition` — log-n binary search would be a real improvement on multi-hour files. Trivial follow-up.
- `IProgress<double>`-reporting overload of `EnsureExactLengthAsync` for waveform-display tools that want to show a loading indicator. Defer until a real consumer asks.

#### Phase 3: High-level API redesign

Existing `WasapiOut` and `WasapiCapture` are kept with `[Obsolete]` attributes pointing to the new APIs. New classes are added alongside to avoid breaking existing code immediately.

**Note:** `IWaveProvider` and `ISampleProvider` are now in NAudio.Core (namespace `NAudio.Wave`) as the foundation for all playback types (WASAPI, ASIO, WinMM, DirectSound). `WaveFormat` (based on WAVEFORMATEX) may eventually be replaced with a platform-agnostic `AudioFormat` descriptor, but this is deferred.

**3a: WasapiPlayer (builder + playback engine) — DONE**
- [x] `WasapiPlayerBuilder` — fluent configuration (device, share mode, latency, event sync, audio category, MMCSS task name, low-latency preference)
- [x] `WasapiPlayer` — the built player, implements `IWavePlayer`, `IWavePosition`, `IDisposable` and `IAsyncDisposable`
- [x] Span-based render loop using `RenderBufferLease` — no `Marshal.Copy`
- [x] Accept `IWaveProvider` (zero-copy Span-based)
- [x] MMCSS thread priority elevation via `AvSetMmThreadCharacteristics` in the audio thread
- [x] IAudioClient3 low-latency auto-negotiation: `WithLowLatency()` uses `InitializeSharedAudioStream` if available, falls back to standard initialization
- [x] AudioStreamCategory support via builder's `WithCategory()`
- [x] Format handling: shared mode uses `AutoConvertPcm`. Exclusive mode requires the caller to provide a natively supported format — `IsFormatSupported()` and `DeviceMixFormat` are exposed for callers to query. No built-in resampler (the old WasapiOut's embedded resampler caused threading/latency issues; callers who need SRC should do it upstream)
- [x] `IWavePosition` support — `GetPosition()` reports playback position via `AudioClockClient`
- [x] Three-tier volume API:
  - `Volume` / `IsMuted` — simple float + bool properties backed by `SimpleAudioVolume` (session volume). This is the correct default: it controls your app's volume in the Windows mixer without affecting other applications. This fixes the old `WasapiOut` design where `Volume` changed the device endpoint volume system-wide.
  - `SessionVolume` (`SimpleAudioVolume`) — the backing object for `Volume`/`IsMuted`, exposing the full session volume API
  - `StreamVolume` (`AudioStreamVolume`) — per-channel volume for balance/pan (shared mode only; throws `InvalidOperationException` in exclusive mode)
  - `DeviceVolume` (`AudioEndpointVolume`) — device endpoint volume with per-channel levels, mute, step control, dB ranges, and change notifications. Affects all applications on the device.

**3b: WasapiRecorder (builder + capture engine) — DONE**
- [x] `WasapiRecorderBuilder` — fluent configuration (device, share mode, format, buffer length, event sync, MMCSS)
- [x] `WasapiRecorder` — the built recorder, implements `IDisposable` and `IAsyncDisposable`
- [x] Span-based capture using `CaptureBufferLease` — zero-copy on the synchronous event path
- [x] `DataAvailable` event via `CaptureDataAvailableHandler(ReadOnlySpan<byte> buffer, AudioClientBufferFlags flags)` — synchronous, zero-alloc
- [x] `IAsyncEnumerable<AudioBuffer>` via `CaptureAsync()` for async consumption (uses `Marshal.Copy` since Span can't cross yield boundaries)
- [x] MMCSS thread priority in capture thread

**3c: Process-specific loopback capture — PARTIAL**
- [x] `WasapiRecorderBuilder.WithProcessLoopback(uint processId, ProcessLoopbackMode mode)` — builder API defined
- [x] `AudioClientActivationParams` and `AudioClientProcessLoopbackParams` structs are public and ready
- [ ] Actual activation via `ActivateAudioInterfaceAsync` with marshaled activation params — not yet implemented (throws `NotImplementedException`)

**3d: Deprecate old APIs — DONE**
- [x] `WasapiOut` marked `[Obsolete]` pointing to `WasapiPlayerBuilder`
- [x] `WasapiCapture` marked `[Obsolete]` pointing to `WasapiRecorderBuilder`
- [x] `WasapiLoopbackCapture` marked `[Obsolete]` pointing to `WasapiRecorderBuilder`

### What Remains

#### Phase 4: Extended Core Audio APIs
- Spatial Audio (`ISpatialAudioClient`, `ISpatialAudioObject`, etc.) — Win10 1703+
- Audio Effects Manager (`IAudioEffectsManager`) — Win11+
- `IChannelAudioVolume` wrapper
- Volume ducking notifications (`IAudioVolumeDuckNotification`)

#### Phase 7: Span-based IWaveProvider / ISampleProvider — DONE

**Decision:** Rather than introducing separate `IAudioSource` / `ISampleSource` interfaces alongside the originals (with adapter classes bridging them), the existing `IWaveProvider` and `ISampleProvider` interfaces were retrofitted to use `Span<T>` signatures directly. This preserves all existing class names, eliminates the adapter layer, and provides a simpler mental model.

**7a: Foundation — DONE**

- [x] NAudio.Core TFM: `netstandard2.0` → `net8.0` (unlocks `Span<T>`)
- [x] NAudio.Midi TFM: `netstandard2.0` → `net8.0`
- [x] NAudio.Asio TFM: `netstandard2.0;net8.0-windows` → `net8.0-windows`
- [x] `IWaveProvider.Read` signature changed: `Read(byte[] buffer, int offset, int count)` → `Read(Span<byte> buffer)`
- [x] `ISampleProvider.Read` signature changed: `Read(float[] buffer, int offset, int count)` → `Read(Span<float> buffer)`
- [x] `IWavePlayer.Init` signature changed: `Init(IWaveProvider waveProvider)` — takes the now-Span-based `IWaveProvider`
- [x] `WaveStream` implements `IWaveProvider` via `Stream.Read(Span<byte>)`. NAudio's own concrete `WaveStream` subclasses override this directly (see Phase 7d). Third-party subclasses that only override `Read(byte[], int, int)` continue to work via the pooled bridge inherited from `Stream`. The array-based `Read(byte[], int, int)` remains as a convenience overload
- [x] `DirectSoundOut` fixed for net8.0: `Thread.Abort()` removed (not supported), `nint` null check fixed

**7b: All implementations use Span-based Read — DONE**

**Byte-level producers (`IWaveProvider`):**
- [x] `MediaFoundationTransform` — constructor accepts `IWaveProvider`. `MediaFoundationResampler` inherits this
- [x] `MediaFoundationReader` — overrides `Read(Span<byte>)` with span-based `ReadFromDecoderBuffer`
- [x] `ResamplerDmoStream` — overrides `Read(Span<byte>)` using span-based `DmoOutputDataBuffer.RetrieveData(Span<byte>)`
- [x] `DmoEffectWaveProvider` — reads directly into span and processes in-place via pinned `MediaObjectInPlace.Process(Span<byte>)`
- [x] `BufferedWaveProvider` — `Read(Span<byte>)` backed by `CircularBuffer`
- [x] `VolumeWaveProvider16`, `MonoToStereoProvider16`, `StereoToMonoProvider16`, `Wave16toFloatProvider`, `WaveFloatTo16Provider`
- [x] `MixingWaveProvider32`, `MultiplexingWaveProvider`, `SilenceWaveProvider`, `WaveRecorder`, `WaveInProvider`
- [x] `WaveFormatConversionProvider` (NAudio.WinMM)

**Sample-level producers (`ISampleProvider`):**
- [x] `VolumeSampleProvider`, `MixingSampleProvider`, `FadeInOutSampleProvider`, `MonoToStereoSampleProvider`, `StereoToMonoSampleProvider`
- [x] `MeteringSampleProvider`, `NotifyingSampleProvider`, `PanningSampleProvider`, `OffsetSampleProvider`
- [x] `MultiplexingSampleProvider`, `ConcatenatingSampleProvider`, `AdsrSampleProvider`, `SmbPitchShiftingSampleProvider`
- [x] `SignalGenerator`, `WdlResamplingSampleProvider`, `SampleChannel`
- [x] `SimpleCompressorEffect` (renamed from `SimpleCompressorStream`)

**PCM-to-sample converters (`ISampleProvider`, via `SampleProviderConverterBase`):**
- [x] `Pcm8BitToSampleProvider`, `Pcm16BitToSampleProvider`, `Pcm24BitToSampleProvider`, `Pcm32BitToSampleProvider`
- [x] `WaveToSampleProvider`, `WaveToSampleProvider64`

**Sample-to-wave converters (`IWaveProvider`):**
- [x] `SampleToWaveProvider`, `SampleToWaveProvider16`, `SampleToWaveProvider24`

**Base classes:**
- [x] `WaveProvider16` — abstract `Read(Span<short>)` method
- [x] `WaveProvider32` — `IWaveProvider` + `ISampleProvider`. Abstract `Read(Span<float>)` method
- [x] `SampleProviderConverterBase` — `ISampleProvider`, constructor accepts `IWaveProvider`

**Infrastructure span additions:**
- [x] `MediaBuffer` — `LoadData(ReadOnlySpan<byte>)` and `RetrieveData(Span<byte>)`
- [x] `DmoOutputDataBuffer` — `RetrieveData(Span<byte>)`
- [x] `MediaObjectInPlace` — `Process(Span<byte>, ...)` pins the span directly
- [x] `CircularBuffer` — `Read(Span<byte>)` and `Write(ReadOnlySpan<byte>)`
- [x] `WaveExtensionMethods` — `ToMono()`, `ToStereo()`, `FollowedBy()`, `Skip()`, `Take()` extensions; `Init(IWavePlayer, ISampleProvider)` extension method

**7c: Consumer updates — DONE**

- [x] `IWavePlayer.Init(IWaveProvider)` — all implementations updated (WaveOut, DirectSoundOut, AsioOut, WasapiPlayer)
- [x] `MediaFoundationEncoder` — all `Encode` methods accept `IWaveProvider`. `ConvertOneBuffer` reads directly into locked `IMFMediaBuffer` via span (zero-copy)
- [x] `WaveFileWriter` — `CreateWaveFile` / `WriteWavFileToStream` accept `IWaveProvider`; `CreateWaveFile16` accepts `ISampleProvider`; `Write(ReadOnlySpan<byte>)` span-based write path
- [x] `AudioFileReader` — implements `ISampleProvider` alongside `WaveStream`
- [x] `SampleProviderConverters.ConvertWaveProviderIntoSampleProvider` — single method, no adapter bridging
- [x] NAudio.Extras — all updated to `ISampleProvider`
- [x] Demo apps and test helpers — all updated

**Notes:**
- `DmoMp3FrameDecompressor` does NOT implement `IWaveProvider` — it implements `IMp3FrameDecompressor` (frame-based API)
- `AudioFormat` replacing `WaveFormat` is deferred — orthogonal to span migration

**7d: WaveStream concrete-reader span overrides — DONE**

**Problem:** After 7a, `WaveStream` provided a `Read(Span<byte>)` override that bridged to the legacy `Read(byte[], int, int)` via a per-instance `spanBridgeBuffer` field. Because no concrete reader actually overrode `Read(Span<byte>)`, every span-based read on a `WaveFileReader`, `Mp3FileReaderBase`, `WaveChannel32`, etc. paid an extra buffer copy. Additionally, `spanBridgeBuffer` was never freed — each `WaveStream` instance grew a buffer to match the largest read it had ever serviced and held it for the stream's lifetime.

**Decision:** Follow the .NET `Stream` convention — `Read(byte[], int, int)` is the baseline override point (abstract on `Stream`), and `Read(Span<byte>)` is virtual with a default bridge that rents from `ArrayPool<byte>.Shared`. NAudio's own readers implement the real logic in `Read(Span<byte>)` and forward the byte[] overload to it; third-party subclasses that only override the byte[] overload keep working unchanged (now via the pooled, freed-after-use `ArrayPool` bridge rather than a retained per-instance buffer).

**Changes:**

- [x] Removed `WaveStream.spanBridgeBuffer` field and the `Read(Span<byte>)` override that used it — the base-class default from `Stream` (ArrayPool-backed, released after each call) takes over for non-overriding subclasses
- [x] `WaveFileReader` — `Read(Span<byte>)` reads directly from the underlying source stream
- [x] `AiffFileReader` — `Read(Span<byte>)` reads big-endian source bytes into the caller's span and byte-swaps in place (the old implementation allocated a new byte[] per read for the swap)
- [x] `RawSourceWaveStream` — `Read(Span<byte>)` forwards directly to the underlying `Stream.Read(Span<byte>)`
- [x] `WaveChannel32` — `Read(Span<byte>)` uses `MemoryMarshal.Cast<byte, float>` to write float samples without the `WaveBuffer` `[FieldOffset]` union
- [x] `Wave32To16Stream` — `Read(Span<byte>)` with `MemoryMarshal.Cast` for the 32→16 conversion (removes the `unsafe`/`fixed` block)
- [x] `WaveOffsetStream` — `Read(Span<byte>)` uses span slicing instead of offset arithmetic
- [x] `BlockAlignReductionStream` — `Read(Span<byte>)` fills the `CircularBuffer`-backed output span directly
- [x] `WaveMixerStream32` — `Read(Span<byte>)` pools its per-source read buffer via `BufferHelpers.Ensure` instead of `new byte[count]` every call. `Sum32BitAudio` rewritten to use `MemoryMarshal.Cast<byte, float>`
- [x] `Mp3FileReaderBase` — `Read(Span<byte>)` uses span-based copies from `decompressBuffer` to the caller's span
- [x] `WaveFormatConversionStream` — `Read(Span<byte>)` forwards to `WaveFormatConversionProvider.Read(Span<byte>)` (already span-based)
- [x] `AudioFileReader` — `Read(Span<byte>)` reinterpret-casts to `Span<float>` via `MemoryMarshal.Cast` (no intermediate buffer)
- [x] `LoopStream` (NAudio.Extras) — `Read(Span<byte>)` with span slicing
- [x] `IgnoreDisposeStream` — added `Read(Span<byte>)` and `Write(ReadOnlySpan<byte>)` pass-throughs
- [x] `ComStream` (NAudio.Wasapi) — added `Read(Span<byte>)` and `Write(ReadOnlySpan<byte>)` pass-throughs
- [x] `MediaFoundationReader`, `ResamplerDmoStream` — already had `Read(Span<byte>)` overrides from Phase 7b

**Convention for third-party `WaveStream` authors:**

Implement your read logic in `Read(Span<byte>)` and forward the byte[] overload:

```csharp
public override int Read(Span<byte> buffer) { /* real read logic */ }
public override int Read(byte[] array, int offset, int count)
    => Read(array.AsSpan(offset, count));
```

Subclasses that only override `Read(byte[], int, int)` still work correctly — but pay one `ArrayPool<byte>.Shared` rent and one copy per span-based read. The XML doc on `WaveStream` describes the pattern.

**Test coverage:** `WaveStreamSpanReadTests` asserts that (a) each NAudio reader produces identical data when read via `Read(byte[], int, int)` versus `Read(Span<byte>)`, (b) third-party subclasses overriding only the byte[] method continue to work through the bridge, and (c) reflection-based architectural check — every concrete `WaveStream` subclass in the NAudio assemblies overrides `Read(Span<byte>)`.

**7e: SmbPitchShifter span-based API and per-Read allocation removal — DONE**

**Problem:** `SmbPitchShiftingSampleProvider.Read` allocated 1 (mono) or 2 (stereo) `float[]` arrays on **every** Read — real-time playback hits this 50-100 times per second, churning Gen0 unnecessarily.

**Changes:**

- [x] `SmbPitchShifter.PitchShift` and `ShortTimeFourierTransform` gained `Span<float>` overloads. The `float[]` overloads now forward to the span versions via `AsSpan()` — public API preserved, zero-copy path available
- [x] Internal loop counters changed from `long` to `int` (the buffers are capped at `MAX_FRAME_LENGTH = 16000` so int is always sufficient) — required for `Span<float>` indexing
- [x] `SmbPitchShiftingSampleProvider.Read` — **mono path** calls `shifterLeft.PitchShift(..., buffer.Slice(0, sampRead))` directly: zero intermediate buffer, limiter runs in place on the caller's span
- [x] `SmbPitchShiftingSampleProvider.Read` — **stereo path** uses two instance-field `float[]` buffers grown via `BufferHelpers.Ensure` (allocated on first Read at a given size, reused thereafter) for the deinterleave/shift/reinterleave step

**Verification:** `SmbPitchShiftingSampleProviderTests` includes zero-allocation assertions (`GC.GetAllocatedBytesForCurrentThread` before and after 50 Reads of the same size) for both the mono and stereo paths — both paths allocate 0 bytes on steady-state reads after warm-up. Parity tests confirm the `float[]` and `Span<float>` overloads produce byte-identical output on the same input.

**7f: SIMD mix-add and volume kernels via `System.Numerics.Tensors.TensorPrimitives` — DONE**

**Problem:** Three hot-path scalar loops in the mixing/volume pipeline that the JIT does not autovectorise reliably:
1. `MixingSampleProvider.Read` — `buffer[n] += sourceBuffer[n]` (plus a per-iteration "first-source: copy, later: add" branch)
2. `WaveMixerStream32.Sum32BitAudio` — `dest[n] += source[n]`
3. `VolumeSampleProvider.Read` — `buffer[n] *= Volume`

**Decision:** Use `System.Numerics.Tensors.TensorPrimitives.Add` and `TensorPrimitives.Multiply`. TensorPrimitives is the BCL's vectorised-kernels library — it picks SSE / AVX2 / AVX-512 / NEON at runtime, handles the remainder tail, and allows the destination span to alias an input. Ships as a `System.Numerics.Tensors` NuGet package (9.0.0) until it lands in the BCL proper.

**Changes:**

- [x] `VolumeSampleProvider.Read` — scalar `for` loop → `TensorPrimitives.Multiply(slice, Volume, slice)`
- [x] `WaveMixerStream32.Sum32BitAudio` — scalar `for` loop → `TensorPrimitives.Add`
- [x] `MixingSampleProvider.Read` — per-iteration branch hoisted out of the inner loop:
  - First source into a buffer region: `CopyTo` (no add needed)
  - Subsequent source within overlap: `TensorPrimitives.Add`
  - Subsequent source extending beyond previous range: `Add` on the overlap + `CopyTo` on the tail
- [x] New `NAudio.Benchmarks` project (net8.0, Release-only) with `BenchmarkDotNet` — raw kernel benchmarks (mix-add, volume) and an end-to-end `MixingSampleProvider.Read` benchmark at realistic buffer sizes (480/1920/9600 floats = 5/20/100 ms of stereo @ 48 kHz) and source counts (2/8/32)
- [x] Regression guard in NAudioTests: `MixingSampleProviderThroughputTests` asserts the mixer sustains at least 50× realtime for 8 sources × 100 ms @ 48 kHz stereo — a loose floor that catches egregious regressions (reintroducing allocations, O(n²) bugs) without flaking on slow CI machines

**Measured results** — BenchmarkDotNet short-run, x64 AVX2, .NET 8.0.26, net8.0 Release:

Raw mix-add kernel (`dest[n] += src[n]` over N floats):

| Floats | Scalar     | TensorPrimitives.Add | Speedup |
| -----: | ---------: | -------------------: | ------: |
|    480 |  149.77 ns |             17.82 ns |    8.4× |
|   1920 |  579.24 ns |             55.41 ns |   10.5× |
|   9600 | 2,922.70 ns |            555.36 ns |    5.3× |

Raw volume kernel (`dest[n] = src[n] * v` over N floats):

| Floats | Scalar      | TensorPrimitives.Multiply | Speedup |
| -----: | ----------: | ------------------------: | ------: |
|    480 |   110.80 ns |                  13.57 ns |    8.2× |
|   1920 |   417.40 ns |                  43.57 ns |    9.6× |
|   9600 | 2,057.12 ns |                 546.53 ns |    3.8× |

End-to-end `MixingSampleProvider.Read` (cycling in-memory sources, stereo 48 kHz, 1920-frame = 20 ms or 9600-frame = 100 ms buffers):

| Sources | Frames | Scalar      | TensorPrimitives | Speedup |
| ------: | -----: | ----------: | ---------------: | ------: |
|       2 |   1920 |    16.31 µs |         15.23 µs |    1.07× |
|       2 |   9600 |    82.06 µs |         78.19 µs |    1.05× |
|       8 |   1920 |    65.75 µs |         63.35 µs |    1.04× |
|       8 |   9600 |   330.61 µs |        311.43 µs |    1.06× |
|      32 |   1920 |   263.43 µs |        244.10 µs |    1.08× |
|      32 |   9600 | 1,325.28 µs |      1,238.66 µs |    1.07× |

The end-to-end speedup is modest because in this benchmark the cycling source's own `Read` dominates the total time — the mix-add kernel is only a fraction of each iteration. The raw-kernel numbers are what matter when:
- mixing many cheap sources (e.g. pre-decoded sample buffers)
- `WaveMixerStream32.Sum32BitAudio` running inside a larger mixing chain
- offline / faster-than-realtime mixdown

At 9600 floats all kernels are partly memory-bandwidth bound, which is why the ratio compresses from ~10× down to 3.8-5.3× at the larger buffer size. That's expected; the SIMD wins aren't in the FP pipeline latency but in avoiding the per-element bounds-check + scalar-dispatch overhead.

**Zero allocations** — both raw kernels and the end-to-end mixer report 0 bytes allocated per operation (the single 1-byte figure at 32 sources × 9600 frames is an `Allocated` rounding artefact from BDN's measurement overhead, not an actual allocation).

**7g: WaveBuffer deprecated in favour of `MemoryMarshal.Cast` — DONE**

**Problem:** `NAudio.Wave.WaveBuffer` is a pre-Span union (`[StructLayout(LayoutKind.Explicit)]` with `byte[]` / `float[]` / `short[]` / `int[]` all at `FieldOffset(8)`) used to read the same memory block as different typed arrays. Before `Span<T>` this was the only way to avoid per-sample `BitConverter` calls. It has several drawbacks now:
- The union trick relies on the CLR laying out managed array references identically across types — an implementation detail, not a language guarantee.
- It has no non-allocating way to view a subrange; you always operate on the whole underlying array.
- It is a public heap object with a finalizer-free but still allocating lifecycle per use.
- `MemoryMarshal.Cast<byte, T>` does the same reinterpretation on a `Span<byte>` at zero runtime cost (inlined to a pointer cast + length shift), supports slicing, and works on pooled/stack-allocated buffers too.

**Decision:** Mark `WaveBuffer` and `IWaveBuffer` `[Obsolete]` (warning, not error — both are in the public API surface and removing them is a breaking change that can wait for a future major version). Remove all internal uses so NAudio's own build is warning-clean.

**Changes:**

- [x] `WaveBuffer` — `[Obsolete("Use MemoryMarshal.Cast<byte, T>(span) instead…")]`
- [x] `IWaveBuffer` — `[Obsolete(…)]`
- [x] `Mono16SampleChunkConverter`, `MonoFloatSampleChunkConverter`, `Stereo16SampleChunkConverter`, `StereoFloatSampleChunkConverter` — dropped the `sourceWaveBuffer` field. `GetNextSample` now does `MemoryMarshal.Cast<byte, short>(sourceBuffer)` / `<byte, float>(sourceBuffer)` inline; the cast is a zero-cost reinterpret.
- [x] `NAudioTests/WaveStreams/SampleChunkConverterMappingTests.cs` — replaced `new WaveBuffer(dest)` with `MemoryMarshal.Cast<byte, float>(dest)` in the assertion helper.
- [x] `NAudioDemo/NetworkChatDemo/SpeexChatCodec.cs` — switched the running encoder input buffer from `WaveBuffer` to `short[] + int encoderInputSamples`. `FeedSamplesIntoEncoderInputBuffer` uses `MemoryMarshal.Cast<byte, short>` to copy the incoming byte buffer into the short buffer. `Decode` allocates a `short[]` for the decoder output and uses `MemoryMarshal.AsBytes` to copy the result into the returned `byte[]`.
- [x] `NAudioDemo/NetworkChatDemo/G722ChatCodec.cs` — per-call `WaveBuffer` replaced with a `short[]` allocated locally and populated via `MemoryMarshal.Cast` (encode) or copied back via `MemoryMarshal.AsBytes` (decode).

**Not touched:** `NAudio.WinMM/WaveOutBuffer.cs` and `NAudio.WinMM/WaveInBuffer.cs` contain two doc-comment mentions of "this WaveBuffer" each — these refer colloquially to the containing classes (`WaveOutBuffer` / `WaveInBuffer`), not the `WaveBuffer` type, so no code change is needed.

**Verification:** Full solution builds with 0 warnings after tagging both types `[Obsolete]` — confirming no internal NAudio code still references them. All 978 tests pass (14 skipped for missing external test files).

**Deferred to a future major version:** physically delete `WaveBuffer` and `IWaveBuffer`. They are listed in the "Breaking Changes" section of this document as candidates for a 3.0 cleanup.

**7h: `IMp3FrameDecompressor.DecompressFrame` span overload via default interface method — DONE**

**Problem:** `IMp3FrameDecompressor.DecompressFrame(Mp3Frame, byte[], int)` pre-dates `Span<T>`. Migrating it is complicated by NLayer — `NLayer.NAudioSupport.Mp3FrameDecompressor` lives in a separate repo (naudio/NLayer), implements this interface, and ships as a dependency of many MP3 consumers. Any addition to the interface that NLayer doesn't already provide would break callers who pull in a pre-update NLayer build.

**Decision:** Add a `DecompressFrame(Mp3Frame, Span<byte>)` overload as a **default interface method (DIM)**. The default implementation routes through the existing byte[] overload via an `ArrayPool<byte>.Shared` rental:

```csharp
int DecompressFrame(Mp3Frame frame, Span<byte> dest)
{
    byte[] rented = ArrayPool<byte>.Shared.Rent(dest.Length);
    try
    {
        int written = DecompressFrame(frame, rented, 0);
        rented.AsSpan(0, Math.Min(written, dest.Length)).CopyTo(dest);
        return written;
    }
    finally { ArrayPool<byte>.Shared.Return(rented); }
}
```

Why DIM works here:
- **Old NLayer + new NAudio:** NLayer's byte[] method satisfies the interface; calls to the Span overload dispatch to the DIM default, which rents a pool byte[], calls NLayer's existing method, and copies into the caller's Span. Zero breaking change.
- **Updated NLayer + new NAudio:** NLayer overrides the Span method directly; DIM default is never invoked. Zero pool bounce.
- **Third-party implementations (any shape):** same graceful degradation — if they haven't overridden the Span method, they still work via routing.

.NET 8+ supports DIMs natively; no TFM changes needed.

**Changes:**

- [x] `IMp3FrameDecompressor` — Span overload added as a DIM. byte[] overload left untouched (still required of all implementers).
- [x] `AcmMp3FrameDecompressor` (WinMM) — promoted the Span method to be the primary implementation; byte[] overload forwards via `dest.AsSpan(destOffset)`.
- [x] `DmoMp3FrameDecompressor` (Wasapi) — same: Span-primary, byte[] forwards.
- [x] `Mp3FileReaderBase.cs:388` — now calls the Span overload. Built-ins dispatch directly; NLayer (pre-update) gets the routed fallback.
- [x] `NAudioTests/Dmo/DmoMp3FrameDecompressorTests.cs:44` and `NAudioDemo/Mp3StreamingDemo/MP3StreamingPanel.cs:117` — migrated to Span overload.
- [x] New test `NAudioTests/Mp3/Mp3FrameDecompressorDimRoutingTests.cs` — `LegacyByteArrayOnlyDecompressor` test double mimics an NLayer-shape impl (overrides only the byte[] method) and proves the DIM fallback dispatches correctly, preserves the return value, copies the pool-buffer contents verbatim into the caller's Span, and doesn't leak rentals across many calls.

**Contract reminder:** `dest` must be large enough to hold one frame's PCM output (e.g. MPEG-1 L3 stereo: 1152 samples × 2 channels × 2 bytes = 4608 bytes). This is the same contract as the byte[] overload.

**Verification:** Full solution builds with 0 warnings. All 980 tests pass (14 skipped for missing external test files).

**Deferred to a future release:** once NLayer ships a version that overrides the Span method directly, mark the byte[] overload `[Obsolete("Prefer the Span<byte> overload")]` to nudge other third-party implementers. Do not mark it obsolete before then — NLayer's own build would show the warning on every implementing method.

**7i: `WaveInEventArgs` gains span/memory surface; zero-copy via `WasapiCapture` was reverted — DONE, partially reverted**

**Problem:** Every `DataAvailable` event from `WaveIn` / `WasapiCapture` forces consumers onto the `(byte[] Buffer, int BytesRecorded)` triple. `WaveInEventArgs` lacked any span surface, so even consumers who only wanted to iterate bytes had to accept the array-shaped API.

**Decision (kept):** Add a `ReadOnlySpan<byte> BufferSpan` property to `WaveInEventArgs`, sliced to `BytesRecorded`. Add a second `(ReadOnlyMemory<byte>)` constructor for producers that can hand out captured audio without copying into a managed array first. Legacy `Buffer` property and `(byte[], int)` ctor stay, with doc updates pointing callers at `BufferSpan`.

**Decision (reverted):** The original plan also rewired `WasapiCapture.ReadNextPacket` to fire `DataAvailable` with a `ReadOnlyMemory<byte>` wrapping the native WASAPI buffer directly — via a new `NativeAudioBufferMemoryManager` — skipping the per-packet `Marshal.Copy`. **This was backed out.** The aliasing contract ("do not retain past handler return") is incompatible with the `Control.BeginInvoke`/`Dispatcher.BeginInvoke` pattern that most real-world WinForms/WPF capture apps use: by the time the UI thread runs the handler, the capture thread has already called `ReleaseBuffer` and WASAPI has refilled the same native pages with new audio, so the deferred read got garbled/robotic playback. The zero-copy cost savings — a `Marshal.Copy` of a few hundred bytes per 10 ms packet — didn't justify breaking every async consumer.

Zero-copy capture is still available, just not on `WasapiCapture.DataAvailable`. The modern `WasapiRecorder` builder API has its own lease-based span callback (span-first since inception) that doesn't flow through `WaveInEventArgs` at all — new code that wants true zero-copy capture should use that.

**Changes that stuck:**

- [x] `NAudio.Core/Wave/WaveInputs/WaveInEventArgs.cs` — adds `BufferSpan` and a `(ReadOnlyMemory<byte>)` ctor alongside the existing `(byte[], int)` ctor; `Buffer` property now lazily materialises a cached `byte[]` from the memory backing when needed. Aliasing contract ("do not retain past handler return") documented on every accessor.
- [x] `NAudio.Core/Wave/WaveProviders/WaveInProvider.cs` — switched from `AddSamples(e.Buffer, 0, e.BytesRecorded)` to `AddSamples(e.BufferSpan)` so consumers of `WaveInProvider` do not trigger the lazy `Buffer` materialisation on memory-backed events.
- [x] Tests `NAudioTests/WaveStreams/WaveInEventArgsTests.cs` — cover both ctors, the array-sharing fast path, the materialise-and-cache path, and span slicing to `BytesRecorded`.

**Changes that were reverted:**

- `NAudio.Wasapi/NativeAudioBufferMemoryManager.cs` — file deleted.
- `NAudio.Wasapi/WasapiCapture.ReadNextPacket` — each packet now allocates its own fresh managed byte[] (sized to `framesAvailable * bytesPerFrame`), `Marshal.Copy`s the native buffer into it (or leaves it zero-filled for silent packets), and fires the event. A *shared* managed buffer is not sufficient: loopback capture commonly drains several packets per wake-up (the render engine bursts), and any consumer that defers handling would see whichever packet happened to be in the shared buffer by the time the handler ran. Per-packet allocations are <4 KB each for typical formats and collect in gen0 — the overhead is invisible compared with the cost of garbled audio.

**What this does *not* touch:**

- WinMM `WaveIn.cs` continues to pass a byte[] (the pinned `WaveHeader` buffer). No reason to change — the array is already native-addressable for WinMM's callback contract.
- `WasapiRecorder` continues to use its own lease-based `DataAvailable` callback (span-first since inception). It does not flow through `WaveInEventArgs` at all.

**Takeaway for future work:** public event surfaces on types consumers have been using for a decade cannot silently change their aliasing contract. If a zero-copy native-pointer path is needed on `WaveInEventArgs` in future, it must be opt-in at the *producer* side (e.g. a flag on `WasapiCapture`) with loud docs, not the default.

**Verification:** Full solution builds with 0 warnings. Tests pass; WASAPI capture and loopback verified manually in the demo app after the revert (previously all modes except lucky-timing mono captures sounded robotic due to post-release native reads).

**7j: Batch DSP and codec overloads — DONE**

**Problem:** Two span-hostile fragments of the API forced per-sample virtual calls or static-method dispatch for operations that are naturally batched:

- `BiQuadFilter.Transform(float)` — a biquad IIR is sample-by-sample correct for streaming, but offline processing, EQ chains, and filter-before-downsample flows want to process blocks. Each per-sample call reloads the five coefficients and four state variables from fields.
- `ALawDecoder.ALawToLinearSample(byte)` / `MuLawDecoder.MuLawToLinearSample(byte)` — table lookups. Anyone decoding a full frame of telephony audio pays a static-method call per byte.

**Decision:** Add batch overloads alongside the single-sample ones. Non-breaking — the existing entry points stay for streaming / one-at-a-time callers.

- `BiQuadFilter.Transform(ReadOnlySpan<float> source, Span<float> destination)`
- `ALawDecoder.Decode(ReadOnlySpan<byte> source, Span<short> destination)` (static)
- `MuLawDecoder.Decode(ReadOnlySpan<byte> source, Span<short> destination)` (static)

A biquad has a **forward dependency** (each output depends on the previous two outputs), so the inner loop can't vectorise. The speedup over the single-sample form comes entirely from hoisting the coefficient fields and state variables into locals for the duration of the call — the JIT can then hold them in registers instead of reloading from `this` on every iteration. For the codecs, the batch form gives the JIT a clean indexable lookup loop it can auto-unroll.

**Changes:**

- [x] `NAudio.Core/Dsp/BiQuadFilter.cs` — batch `Transform` overload. Hoists `a0-a4` and `x1/x2/y1/y2` into locals; writes the updated state back to fields once, at the end of the call. Supports in-place operation (source and destination being the same span is safe because the biquad reads `source[i]` before writing `destination[i]`). Throws `ArgumentException` when `destination` is shorter than `source`.
- [x] `NAudio.Core/Codecs/ALawDecoder.cs` — static batch `Decode`.
- [x] `NAudio.Core/Codecs/MuLawDecoder.cs` — static batch `Decode`.
- [x] New `NAudioTests/Dsp/BiQuadFilterTests.cs` — verifies batch output is byte-identical to sample-by-sample output, state survives splitting a batch across multiple calls, in-place operation matches out-of-place, empty input is a no-op, over-short destinations throw.
- [x] New `NAudioTests/Codecs/ALawDecoderTests.cs` / `MuLawDecoderTests.cs` — batch parity with single-sample form, encode/decode roundtrip across all 256 byte values, over-short destinations throw, larger destinations only write `source.Length` samples.

**Verification:** Full solution builds with 0 warnings. All 1005 tests pass (16 new, 14 skipped for missing external test files).

**7k: FFT modernisation — real-input specialisation, precomputed windowing, Span-first — DONE**

**Problem:** The static `FastFourierTransform.FFT(bool, int, Complex[])` does a full-size complex FFT even when the input is real audio. Audio callers zero out the imaginary part, which means roughly half the work is on known zeros. Worse, the existing `HammingWindow` / `HannWindow` / `BlackmanHarrisWindow` static methods recompute `cos()` on every call — `SampleAggregator.Add` invoked `HammingWindow(n, fftLength)` per sample per buffer, doing thousands of transcendentals just to apply a window.

**Decision:** Introduce `FftProcessor` — a reusable, fixed-size instance class that specialises for real-input audio and bakes in the window. Keep `FastFourierTransform.FFT` intact for back-compat; add a Span overload as originally scoped.

The real FFT trick: for N real samples, pack pairs of samples `{x[2k], x[2k+1]}` as real/imaginary components of an N/2-point complex sequence, run an N/2-point complex FFT, then an unpack pass recovers the N-point real FFT result. Total work is roughly half of a full complex FFT and output is the canonical N/2+1 half-spectrum (the upper half is the complex conjugate of the lower half by Hermitian symmetry).

**Design choices:**

- **Keep single (float) precision.** PCM audio carries ≤24 bits of signal; float32 gives 24 bits of mantissa — enough. Double would cost 2× memory bandwidth and 2× SIMD throughput for precision beyond the signal content. Not worth the regression.
- **Precomputed window table** allocated once in the constructor. Windowing becomes `samples[i] * windowTable[i]` — a free multiply rather than a `cos()` call.
- **No `System.Numerics.Complex` interop** — it's `double` and a reference type, so direct bridging would force precision loss and boxing. Users who need BCL Complex convert explicitly.
- **No radix-4 / split-radix / SIMD in this round.** The big wins are the real-FFT specialisation and the window table; vectorising the inner FFT kernel has a much higher risk/reward ratio and audio-typical sizes (1024) are already memory-bound. Defer to a future round once there's a clear need.

**Changes:**

- [x] `NAudio.Core/Dsp/Complex.cs` — added `Real`/`Imaginary` property aliases for `X`/`Y`. `[AggressiveInlining]` so the JIT collapses them to field reads. Fields retained for back-compat.
- [x] `NAudio.Core/Dsp/FastFourierTransform.cs` — added `FFT(bool, int, Span<Complex>)` overload (the original span task). `Complex[]` overload forwards via `AsSpan`.
- [x] `NAudio.Core/Dsp/FftProcessor.cs` — new `sealed` class. Constructor takes `(int fftSize, FftWindowType window)` and precomputes the window table and real-FFT unpack twiddles. Methods: `RealForward(ReadOnlySpan<float>, Span<Complex>)` emits an N/2+1 half-spectrum; `RealInverse(ReadOnlySpan<Complex>, Span<float>)` recovers the time-domain signal; `ComplexForward`/`ComplexInverse` for callers with genuine complex input. Scaling matches the static FFT (1/N on forward, no scale on inverse, so round-trip is identity). Zero allocations on the steady-state path.
- [x] `NAudio.Extras/SampleAggregator.cs` — migrated. Per-sample `HammingWindow(fftPos, fftLength)` call is gone; samples are collected into a plain `float[]` buffer and fed to an `FftProcessor` with `FftWindowType.Hamming` when full. Conjugate-symmetric upper half of the `FftEventArgs.Result` buffer is populated after the unpack so the existing `SpectrumAnalyser.xaml.cs` demo (which reads the full N-bin array) keeps working unchanged.
- [x] New `NAudioTests/Dsp/FftProcessorTests.cs` — 19 tests: parity with full complex FFT on real input (sizes 4/8/16/64/1024 with per-size tolerance scaling), DC/impulse/cosine analytical cases, RealForward→RealInverse round-trip, `ComplexForward` parity with the static FFT, window table behaviour matches manual windowing, constructor and argument validation, zero-allocation assertion on the steady-state path via `GC.GetAllocatedBytesForCurrentThread`.
- [x] New `NAudio.Benchmarks/FftBenchmarks.cs` — four cases at sizes 256/1024/4096: baseline FFT only, baseline FFT + per-sample window, new real FFT, new real FFT with window table.

**Measured results** (BenchmarkDotNet short-run, .NET 8, x64 AVX2 RyuJIT):

| Size | Baseline FFT only | Baseline FFT + per-sample window | `FftProcessor.RealForward` | `FftProcessor.RealForward` + Hamming |
| ---: | ----------------: | -------------------------------: | -------------------------: | -----------------------------------: |
|  256 |      1,639 ns     |         2,714 ns                 |          937 ns (0.57×)    |              959 ns (0.59×)           |
| 1024 |      7,366 ns     |        11,489 ns                 |        4,232 ns (0.57×)    |            4,240 ns (0.58×)           |
| 4096 |     32,725 ns     |        50,233 ns                 |       18,023 ns (0.55×)    |           19,199 ns (0.59×)           |

Two things to notice:
- Real FFT alone is ~**1.75× faster** than the complex FFT — consistent with halving the work (N/2-point FFT plus an O(N) unpack pass).
- The window table reduces windowing from "~50–67% overhead on top of the FFT" (baseline) to "essentially free" (the `RealForward`-with-window row is only ~3–7% slower than without). Precomputing `0.54 - 0.46·cos(2πn/(N-1))` once beats computing it per sample per call.

Overall, for `SampleAggregator`-style workloads (windowed real FFT — the realistic audio case), the new path is **~2.6× faster** at every size. Zero allocations on steady state on both paths (`MemoryDiagnoser` reports `-` for `Allocated`).

**Deferred (future rounds):**
- SIMD / radix-4 / split-radix FFT kernel — potentially another 20-40% at larger sizes, but significant implementation risk and uncertain payoff at audio-typical sizes.
- Consolidating `SmbPitchShifter`'s internal STFT into `FftProcessor` — different storage layout (interleaved float rather than `Complex`), not worth churn in this round.
- `IMemoryOwner<Complex>` rental for the half-spectrum — only worth it if a consumer wants to avoid the N/2+1 array allocation at spectrum-result time.

#### Phase 5: Media Foundation modernization — DONE

**5a: Infrastructure and naming — DONE**
- [x] `MediaFoundationException` — `MediaFoundationException : COMException` with `ThrowIfFailed(int hr)` and human-readable messages for ~30 common MF error codes
- [x] Enum renames — 9 underscore-prefixed enums (`_MFT_ENUM_FLAG`, etc.) renamed to PascalCase (`MftEnumFlags`, etc.) with PascalCase member names. `MF_SOURCE_READER_FLAG` renamed to `SourceReaderFlags`
- [x] Struct renames — 6 ALL_CAPS structs/enums (`MFT_INPUT_STREAM_INFO`, `MFT_MESSAGE_TYPE`, etc.) renamed to PascalCase (`MftInputStreamInfo`, `MftMessageType`, etc.) with PascalCase field names. `MFT_REGISTER_TYPE_INFO` changed from class to struct
- [x] `MediaFoundationInterop` changed from `public` to `internal`
- [x] `MediaFoundationApi.Startup()` — removed obsolete Windows Vista SDK version check
- [x] `MediaFoundationApi.EnumerateTransforms()` — fixed unsafe IntPtr arithmetic to use `nint`
- [x] Added `MFAudioFormat_Opus` to `AudioSubtypes` (Win10 1809+)
- [x] Added `CreateSourceReaderFromUrl`, `CreateSinkWriterFromUrl`, `GetAudioOutputAvailableTypes` methods to `MediaFoundationApi` (replacing direct `MediaFoundationInterop` calls in consumers)

**5b: COM interface conversion — DONE**
- [x] Created `MediaFoundation/Interfaces/` subfolder with 12 `[GeneratedComInterface]` interfaces (all `internal partial`, all methods `[PreserveSig]`)
- [x] Tier 1 (standalone): `IMFMediaBuffer` (5 methods), `IMFCollection` (6), `IMFByteStream` (15), `IMFReadWriteClassFactory` (2 + coclass)
- [x] Tier 2 (IMFAttributes hierarchy, flattened vtables): `IMFAttributes` (30 methods), `IMFMediaType` (35), `IMFMediaEvent` (34), `IMFSample` (44), `IMFActivate` (33)
- [x] Tier 3 (consumer interfaces): `IMFTransform` (23), `IMFSourceReader` (10), `IMFSinkWriter` (11)
- [x] All interface parameters that were COM types use `IntPtr` with `Marshal.GetObjectForIUnknown`/`Marshal.GetIUnknownForObject` in wrappers
- [x] Old `[ComImport]` interfaces retained (still used by existing consumer code) but changed from `public` to `internal`

**5c: Wrapper classes — DONE**
- [x] `MfMediaBuffer` — wraps `IMFMediaBuffer`. `Lock()` returns `MediaBufferLease` ref struct with `Span<byte>` for zero-copy buffer access. `CurrentLength`/`MaxLength` properties. Deterministic `Marshal.Release(IntPtr)` in Dispose
- [x] `MfSample` — wraps `IMFSample`. `SampleTime`/`SampleDuration`/`SampleFlags` properties. `AddBuffer`, `ConvertToContiguousBuffer`, `RemoveAllBuffers` methods
- [x] `MfMediaType` — wraps `IMFMediaType`. `MajorType`, `SubType`, `SampleRate`, `ChannelCount`, `BitsPerSample`, `AverageBytesPerSecond` properties. Attribute get/set helpers
- [x] `MfSourceReader` — wraps `IMFSourceReader`. `ReadSample`, `SetStreamSelection`, `SetCurrentMediaType`, `Flush` methods
- [x] `MfSinkWriter` — wraps `IMFSinkWriter`. `AddStream`, `SetInputMediaType`, `BeginWriting`, `WriteSample`, `DoFinalize` methods
- [x] `MfTransform` — wraps `IMFTransform`. `SetInputType`, `SetOutputType`, `ProcessInput`, `ProcessOutput`, `ProcessMessage` methods
- [x] `MfActivate` — wraps `IMFActivate`. `ActivateObject`, `ActivateTransform` methods. Attribute enumeration (`AttributeCount`, `GetAttributeByIndex`, `GetBlobAsArrayOf<T>`)
- [x] `MediaType` upgraded — now implements `IDisposable` with `Marshal.ReleaseComObject` in Dispose. `MediaFoundationObject` made `internal`. Added `AttributeCount`/`GetAttributeByIndex` public methods
- [x] All wrappers: no finalizers, deterministic `Marshal.Release(IntPtr)`, `MediaFoundationException.ThrowIfFailed`

**5d: Consumer updates — DONE**
- [x] `MediaFoundationTransform` — removed finalizer. Replaced `Marshal.ThrowExceptionForHR` with `MediaFoundationException.ThrowIfFailed`. `CreateTransform()` changed to `private protected`
- [x] `MediaFoundationReader` — replaced `Marshal.ThrowExceptionForHR` with `MediaFoundationException.ThrowIfFailed`. `using var` on `MediaType` for proper disposal. `CreateReader()` changed to `private protected`
- [x] `MediaFoundationEncoder` — removed finalizer. `using var` on `MediaType` for input format. Simplified `Dispose` to use `outputMediaType.Dispose()`
- [x] `MediaFoundationResampler` — `using` on `MediaType` for input/output format setup (replaces `Marshal.ReleaseComObject`)
- [x] `StreamMediaFoundationReader` — fixed leaked `MediaType` with `using var`
- [x] `MediaFoundationApi.EnumerateTransforms` — now returns `IEnumerable<MfActivate>` (wrapper) instead of raw `IMFActivate`
- [x] All factory methods returning old COM types made `internal`

#### Phase 6: DMO modernization — DONE

**6a: Infrastructure cleanup — DONE**

- [x] Fixed 3 enums with C-style ALL_CAPS members: `DmoInputStatusFlags.DMO_INPUT_STATUSF_ACCEPT_DATA` → `AcceptData`, `DmoEnumFlags.DMO_ENUMF_INCLUDE_KEYED` → `IncludeKeyed`, `MediaParamCurveType.MP_CURVE_*` → PascalCase (`Jump`, `Linear`, `Square`, `InverseSquare`, `Sine`)
- [x] Removed `MediaBuffer` finalizer (unsafe `Marshal.FreeCoTaskMem` from finalizer thread)
- [x] Replaced all `Marshal.ThrowExceptionForHR` in `MediaObject` with `MediaFoundationException.ThrowIfFailed` (~10 call sites)
- [x] Fixed `MediaObject.Dispose()` — removed "experimental, not sure if necessary" comment
- [x] Fixed `WindowsMediaMp3Decoder` — removed "WORK IN PROGRESS - DO NOT USE" label, cleaned up disposal comment
- [x] Updated `MediaBuffer` class documentation

**6b: COM interface conversion — DONE**

- [x] Created `Dmo/Interfaces/` subfolder with 5 `[GeneratedComInterface]` interfaces (all `internal partial`, all methods `[PreserveSig]`)
- [x] `IMediaObject` (23 methods) — `DmoMediaType` struct parameters changed to `IntPtr` (struct contains `IntPtr` fields incompatible with source-generated marshalling)
- [x] `IMediaObjectInPlace` (3 methods)
- [x] `IEnumDmo` (4 methods)
- [x] `IMediaParamInfo` (6 methods) — `MediaParamInfo` struct parameter changed to `IntPtr`
- [x] `IWMResamplerProps` (2 methods)
- [x] `IMediaBuffer` upgraded to `[GeneratedComInterface]` + `MediaBuffer` to `[GeneratedComClass]` so callback CCWs are produced by the same `StrategyBasedComWrappers` instance as the activated DMO — mixing legacy `Marshal.GetIUnknownForObject` CCWs with the source-generated dispatcher crashes the CLR

**6c: Activation modernization — DONE** ([DmoModernization.md](DmoModernization.md))

- [x] New `ComActivation` helper centralises `CoCreateInstance` + `StrategyBasedComWrappers.GetOrCreateObjectForComInstance(UniqueInstance)` — Phase 2e (CoreAudio activation) will reuse it
- [x] `DmoResampler` and `WindowsMediaMp3Decoder` activate via `ComActivation`, producing thread-agile wrappers (constructed-on-STA-used-on-MTA scenario now works — was failing with `InvalidComObjectException`)
- [x] `DmoMediaType` field types changed `bool` → `int` so the struct is blittable and can be pinned for the modern `IntPtr`-typed signatures
- [x] `MediaObject.ProcessInput` / `ProcessOutput` explicitly `Marshal.QueryInterface` for `IID_IMediaBuffer` before passing the pointer to native — `StrategyBasedComWrappers`' multi-vtable CCWs return distinct pointers per interface, unlike legacy CCWs where `IUnknown*` and `IMediaBuffer*` shared a pointer
- [x] All nine effect classes activate via shared `DmoEffectActivation` helper. `IDirectSoundFX*` per-effect property interfaces stay `[ComImport]` per scope — effects obtain the modern wrappers for `IMediaObject`/`IMediaObjectInPlace` and a legacy RCW for `IDirectSoundFX*` via `Marshal.QueryInterface` on the same underlying COM object
- [x] `MediaFoundationResampler` probe + `CreateTransform` use `ComActivation`. `IMFTransform` projection still uses `Marshal.GetObjectForIUnknown` (the `MediaFoundationTransform` base type is part of a separate Phase-5 modernization scope); `IWMResamplerProps` uses the modern `[GeneratedComInterface]` partial
- [x] `DmoEnumerator` uses modern `IEnumDmo`
- [x] All legacy `[ComImport]` declarations under `NAudio.Wasapi/Dmo/` deleted: `IMediaObject.cs`, `IMediaObjectInPlace.cs`, `IWMResamplerProps.cs`, `IEnumDmo.cs`, `IMediaParamInfo.cs`, `ResamplerMediaComObject` and `WindowsMediaMp3DecoderComObject` coclasses. `IPropertyStore` (deliberately out of scope) and `IDirectSoundFX*` (per-effect, deferred) remain
- [x] Disposal switched from `Marshal.ReleaseComObject` to `ComObject.FinalRelease()` for source-generated wrappers

#### Phase 8: WinMM modernization (NAudio.WinMM) — IN PROGRESS

**8a: Bug fixes — DONE**
- [x] Fixed `WaveOutBuffer.Dispose` ordering — `waveOutUnprepareHeader` now called *before* freeing GCHandles (previously the pinned header was freed first, causing undefined behavior)
- [x] Fixed `SignedMixerControl.GetDetails` — was using `mixerControlDetails.paDetails` instead of the `pDetails` parameter (worked by coincidence since they held the same value)
- [x] Fixed `UnsignedMixerControl.GetDetails` — now advances pointer per channel instead of reading channel 0's value for all channels
- [x] Removed no-op `Debug.Assert(true, ...)` from `WaveOutBuffer` and `WaveInBuffer` finalizers (condition was inverted — always passed)

**8b: Memory safety — DONE**
- [x] Added try-finally around `Marshal.AllocHGlobal`/`Marshal.FreeHGlobal` in `BooleanMixerControl`, `SignedMixerControl`, `UnsignedMixerControl` setters (native memory leaked if P/Invoke threw)
- [x] Added try-finally to `AcmDriver.ShowFormatChooseDialog` (two `AllocHGlobal` allocations leaked on exception)
- [x] Fixed `AcmDriver.Dispose` — proper dispose pattern with unconditional `GC.SuppressFinalize`, protected virtual `Dispose(bool)`, and finalizer (previously `SuppressFinalize` was conditional and no finalizer existed)
- [x] Fixed `WaveInEvent` — added `callbackEvent` disposal and finalizer (previously the `AutoResetEvent` leaked and there was no finalizer despite managing a native `waveInHandle`)

**8c: API consolidation — DONE**

**Decision:** The WinForms `WaveOut`, `WaveIn`, `WaveCallbackInfo`, and `WaveWindow` classes are removed. `WaveOutEvent` and `WaveInEvent` (event-callback based) are the only WinMM playback/recording classes going forward.

**Rationale:** The old `WaveOut`/`WaveIn` classes supported three callback strategies: function callbacks, window callbacks (via `WaveWindow`), and event callbacks. Function callbacks were unreliable (known to cause crashes under load). Window callbacks required WinForms dependencies and a message pump. Event callbacks (`WaveOutEvent`/`WaveInEvent`) are the only strategy that works reliably across all application types (console, WinForms, WPF, headless). Maintaining three callback paths for a legacy API added complexity with no benefit.

**Classes removed:**

| Class | Location | Replacement |
| ----- | -------- | ----------- |
| `WaveOut` | NAudio.WinForms | `WaveOutEvent` (NAudio.WinMM) |
| `WaveIn` | NAudio.WinForms | `WaveInEvent` (NAudio.WinMM) |
| `WaveCallbackInfo` | NAudio.WinForms | Not needed — `WaveOutEvent`/`WaveInEvent` always use event callbacks |
| `WaveWindow` / `WaveWindowNative` | NAudio.WinForms | Not needed — window callbacks removed |
| `WaveCallbackStrategy` enum | NAudio.WinMM | Not needed — only one strategy remains |

**Other changes:**
- [x] Added `DeviceCount` and `GetCapabilities(int)` static methods to `WaveOutEvent` (previously only on the deleted `WaveOut`)
- [x] NAudio.WinForms now contains only GUI controls (Fader, PanSlider, Pot, VolumeSlider, VolumeMeter, WaveViewer, WaveformPainter, ProgressLog)

**8d: Public API surface reduction — DONE**

**Decision:** All interop types that are implementation details of `WaveOutEvent`/`WaveInEvent` are now `internal`. Only the high-level classes (`WaveOutEvent`, `WaveInEvent`) and their configuration types (`WaveOutCapabilities`, `WaveInCapabilities`) remain public.

| Type | Previous | Now | Rationale |
| ---- | -------- | --- | --------- |
| `WaveInterop` | `public` | `internal` | Raw P/Invoke — consumers should use `WaveOutEvent`/`WaveInEvent` |
| `WaveHeader` | `public` | `internal` | WAVEHDR interop struct — no public consumer |
| `WaveHeaderFlags` | `public` | `internal` | Flags for WaveHeader — no public consumer |
| `WaveOutBuffer` | `public` | `internal` | Buffer management detail of `WaveOutEvent` |
| `WaveInBuffer` | `public` | `internal` | Buffer management detail of `WaveInEvent` |
| `WaveOutUtils` | `public` | `internal` | Utility methods used only by `WaveOutEvent` |
| `MmTime` | `public` | `internal` | MMTIME interop struct — no public consumer |

**8e: WaveOutEvent / WaveInEvent modernization — DONE**

- [x] Added `isDisposed` guard to both `WaveOutEvent` and `WaveInEvent` — prevents crashes on double-dispose
- [x] `WaveOutEvent.Dispose(bool)` — moved `Stop()` inside `disposing == true` guard. Previously `Stop()` was called from the finalizer path, which took locks and signaled events — both dangerous during finalization
- [x] Removed ASP.NET `SynchronizationContext` string-name check from `WaveOutEvent` constructor — dead code on .NET 8 (ASP.NET Core has no `SynchronizationContext` by default)
- [x] Removed `hThis` GCHandle from `WaveOutBuffer` and `WaveInBuffer` — this pinned the buffer object and stored it in `header.userData` for function-callback recovery. With event callbacks, it's never read, so it was wasting memory and preventing GC compaction
- [x] `WaveOutBuffer.OnDone` — replaced byte-by-byte buffer zeroing loop with `Array.Clear`
- [x] `WaveOutBuffer.OnDone` — replaced `lock (waveStream)` with a dedicated `waveStreamLock` object to prevent deadlocks if callers also lock on their `IWaveProvider`
- [x] `WaveInBuffer.Reuse` — removed unnecessary `waveInUnprepareHeader`/`waveInPrepareHeader` cycle. Per the Windows documentation, a prepared buffer can be re-added without re-preparation if the pointer and size haven't changed. This eliminates 2 P/Invoke calls per buffer rotation
- [x] Removed stale XP-era TODO comment from `WaveInEvent.GetMixerLine`
- [x] Updated class-level XML doc comments (no longer "Alternative" or "non-gui only")
- [x] Replaced `ThreadPool.QueueUserWorkItem` with dedicated `Thread` in both `WaveOutEvent` and `WaveInEvent` — audio threads are long-running (entire playback/recording duration) and should not occupy thread pool threads. Dedicated threads are named (`NAudio WaveOut Playback` / `NAudio WaveIn Recording`), set to `IsBackground = true`, and use `ThreadPriority.AboveNormal` for better timing
- [x] Changed `WaveInEvent` default `WaveFormat` from 8000 Hz / 16-bit / mono to 44100 Hz / 16-bit / stereo — the old default was telephony-era; 44100/16/stereo is what most users expect
- [x] Replaced `WaveOutEvent.DesiredLatency` with `BufferMilliseconds` (default 100ms) — matches `WaveInEvent`'s property name. `DesiredLatency` was confusing because it specified *total* latency across all buffers, so the actual per-buffer size depended on `NumberOfBuffers`. `BufferMilliseconds` directly specifies each buffer's size, making behavior predictable regardless of buffer count
- [x] Renamed `WaveOutEvent` → `WaveOut` and `WaveInEvent` → `WaveIn` — now that the old WinForms `WaveOut`/`WaveIn` classes are gone, the "Event" suffix is unnecessary. Obsolete `WaveOutEvent` and `WaveInEvent` shim classes (inheriting from the renamed classes) are provided for migration

**8f: Window-callback variants restored — DONE**

**Decision:** Reintroduce window-callback playback/capture classes as `WaveOutWindow` and `WaveInWindow` in `NAudio.WinForms` (not in NAudio.WinMM). This restores the `CALLBACK_WINDOW` behaviour from the old `WaveOut`/`WaveIn` without reviving function callbacks and without polluting NAudio.WinMM with a WinForms dependency.

**Rationale:** Phase 8c dropped all three legacy callback strategies in one go. After that shipped, it became clear that the window-callback mode had genuine value for a specific audience: WinForms apps that want `DataAvailable` / `PlaybackStopped` to arrive directly on the UI thread with no manual `SynchronizationContext.Post` per event. The event-callback `WaveIn` / `WaveOut` always delivers buffer events from a dedicated background thread, forcing UI consumers to marshal every event. The function-callback path (which was unreliable under load) is *not* restored — only the window-callback path, which is reliable, is brought back.

**How it's scoped:**

| Aspect | Choice | Why |
| ------ | ------ | --- |
| Package | `NAudio.WinForms` | `Form` / `NativeWindow` are WinForms types; they don't belong in NAudio.WinMM |
| Access to WinMM internals | `[InternalsVisibleTo("NAudio.WinForms", PublicKey=...)]` on NAudio.WinMM | `WaveInterop`, `WaveHeader`, `WaveInBuffer`, `WaveOutBuffer`, `WaveOutUtils` stay `internal` — only the strong-named NAudio.WinForms assembly can reach them |
| Constructors | `WaveOutWindow()` / `WaveOutWindow(IntPtr hwnd)` (same on `WaveInWindow`) | Replaces the old three-way `WaveCallbackInfo` strategy with two simple overloads — parameterless owns a hidden window, `IntPtr` subclasses an existing window |
| UI-thread requirement | Ctor throws `InvalidOperationException` if `SynchronizationContext.Current` is null | Make it explicit — function-callback fallback is gone, and callback delivery depends on a running message pump |
| Buffer identification | Cyclic `nextBufferIndex` field | The old code relied on `WaveHeader.userData` holding a `GCHandle` to the `WaveOutBuffer`, but 8e removed that allocation. MME returns buffers in the order they were queued, so a monotonic index is sufficient |
| Original features preserved | `IWavePosition.GetPosition` on `WaveOutWindow`; `GetMixerLine` / `GetPosition` on `WaveInWindow` | These survived the original removal and are still useful |
| `WaveInWindow.DataAvailable` payload | `WaveInEventArgs(byte[], int)` | Matches the event-based `WaveIn` — pinned managed array, zero-copy via `BufferSpan` on the consumer side (the type's own doc calls this the idiomatic WinMM path) |

**Files:** `NAudio.WinForms/WaveWindow.cs` (shared `Form` / `NativeWindow` message-pump hosts and a `WaveCallbackHost` for lifetime management), `NAudio.WinForms/WaveOutWindow.cs`, `NAudio.WinForms/WaveInWindow.cs`. NAudio.WinMM is unchanged except for the `InternalsVisibleTo` attribute.

### What Remains (WinMM)

- Review ACM compression classes for further cleanup (dispose patterns, naming, visibility)
- Review Mixer classes for further cleanup
- Consider whether `WaveFormatConversionStream` should be deprecated in favor of `WaveFormatConversionProvider`

---

## Phase 9: WAV chunk API redesign — DONE

**Decision:** Replace `WaveFileReader.ExtraChunks` / `GetChunkData` and the `CueWaveFileReader` subclass with a single `Chunks` property on `WaveFileReader` that exposes a `WaveChunks` collection with pluggable interpreters.

**Rationale:** The old design required a reader subclass for each well-known chunk type (e.g. `CueWaveFileReader.Cues`). That doesn't compose — reading both cues and BWF metadata from the same file required writing a custom hybrid reader, and callers had to switch reader type just to ask whether a given chunk was present. The new design uses composition: any number of chunk interpreters can run against a single `WaveFileReader` without inheritance.

**Design:**

```csharp
public sealed class WaveChunks : IReadOnlyList<RiffChunk>
{
    public bool Contains(string chunkId);
    public RiffChunk Find(string chunkId);
    public IEnumerable<RiffChunk> FindAll(string chunkId);
    public byte[] GetData(RiffChunk chunk);                  // lazy read from stream
    public T Read<T>(IWaveChunkInterpreter<T> interpreter);
}

public interface IWaveChunkInterpreter<out T>
{
    T Interpret(WaveChunks chunks);                          // returns default if required chunks absent
}
```

Built-in interpreters ship as extension methods over `WaveChunks`:

```csharp
using var reader = new WaveFileReader("file.wav");
CueList           cues = reader.Chunks.ReadCueList();
BroadcastExtension bwf = reader.Chunks.ReadBroadcastExtension();
InfoMetadata      info = reader.Chunks.ReadInfoMetadata();
CustomThing      mine = reader.Chunks.Read(new MyCustomInterpreter());
```

**Key properties:**
- **Lazy I/O preserved**: `RiffChunk` still carries `(identifier, length, streamPosition)`. Data is only read from the stream when `GetData` or an interpreter asks for it.
- **No inheritance, composable**: any combination of interpreters works against one reader.
- **Interpreters are stateless singletons** (`CueListInterpreter.Instance`, etc.) invoked fresh each time — interpreted results are not cached. Callers hold onto the returned object if they want to reuse it.
- **No registry / reflection**: extension methods give built-ins one-line call-site ergonomics; user interpreters are discoverable via `IWaveChunkInterpreter<T>`.

**Built-in interpreters shipped:**

| Interpreter | Returns | Source chunks |
| ----------- | ------- | ------------- |
| `CueListInterpreter` | `CueList` | `cue ` + `LIST` (with `adtl` type header) |
| `BextInterpreter` | `BroadcastExtension` | `bext` (EBU Tech 3285 — supports v1 and v2 loudness fields) |
| `InfoListInterpreter` | `InfoMetadata` | `LIST` (with `INFO` type header) — common tags (`INAM`, `IART`, `ICMT`, `ICOP`, `ICRD`, `IENG`, `IGNR`, `ISFT`, etc.) exposed as named properties; arbitrary ids via indexer |

`CueListInterpreter` and `InfoListInterpreter` both correctly filter `LIST` chunks by type header, so a file with both `adtl` and `INFO` lists is handled.

**Classes removed:**

| Class | Replacement |
| ----- | ----------- |
| `CueWaveFileReader` | `new WaveFileReader(...).Chunks.ReadCueList()` |
| `CueList.FromChunks` (internal) | `CueListInterpreter.Instance.Interpret(chunks)` |

**Writer-side symmetry — DONE.** `WaveFileWriter` now exposes a `ChunkPosition` enum, an `IWaveChunkWriter` interface, and two `AddChunk(...)` overloads that mirror the read-side `IWaveChunkInterpreter<T>` / `WaveChunks.Read` shape. Built-in extension methods (`WriteCueList`, `WriteBroadcastExtension`, `WriteInfoMetadata`) package each DTO into the right chunk(s) at the conventional position. A convenience `AddCue(int position, string label)` stays on `WaveFileWriter` for callers who just want to drop a few markers; it internally buffers a `CueList` and emits it at close time.

```csharp
using var w = new WaveFileWriter("out.wav", format,
    new WaveFileWriterOptions { EnableRf64 = true });
w.WriteBroadcastExtension(bext);         // BeforeData (bext convention)
w.AddChunk("iXML", xmlBytes, ChunkPosition.BeforeData);
w.AddCue(1000, "Intro");                 // buffered, written AfterData
w.Write(audio, 0, audio.Length);
w.WriteInfoMetadata(info);               // AfterData
```

**Configuration via `WaveFileWriterOptions`.** All writer configuration lives on a single options class rather than being spread across constructor overloads. Today it holds `EnableRf64` and (test-only) `Rf64PromotionThreshold`; future settings (`WriteFactChunk`, `FileShare`, custom buffer sizes, etc.) can be added as `init` properties without touching the constructor surface. The writer accepts `null` for options as shorthand for "use defaults", so the simple case remains `new WaveFileWriter(path, format)`.

**Header flow:** the writer defers emitting the `data` chunk header until the first `Write`/`WriteSample`/`AddChunk(AfterData)` call. At that point any buffered BeforeData chunks are flushed in order, the `fact` chunk is emitted (if applicable), and the `data` header is written. Attempts to add a BeforeData chunk after audio has started throw `InvalidOperationException`. At close, the data chunk is padded to word alignment, then AfterData chunks and any buffered cues are appended, and header sizes are fixed up.

**RF64 promotion — DONE.** `WaveFileWriter(stream, format, enableRf64: true)` reserves a 28-byte `JUNK` placeholder immediately after the RIFF/WAVE header (per EBU Tech 3306 — `ds64` must be the first chunk after `RIFF`). At close, if the data chunk exceeds 4 GB the writer overwrites `RIFF`→`RF64`, the placeholder `JUNK`→`ds64`, and sets the 32-bit RIFF and data sizes to `0xFFFFFFFF` with the real 64-bit sizes in `ds64`. Small files on an RF64-enabled writer stay as normal RIFF (with a harmless 36-byte `JUNK` chunk). Files written with `enableRf64: false` still throw `ArgumentException` if the audio would exceed 4 GB. A test-only constructor overload (hidden with `[EditorBrowsable(Never)]`) lets tests exercise promotion against a lowered threshold without having to write 4 GB of audio.

**Classes retired in this pass:**

| Class | Replacement |
| ----- | ----------- |
| `CueWaveFileWriter` | `new WaveFileWriter(...)` + `AddCue(position, label)` or `WriteCueList(cueList)` |
| `BwfWriter` | `new WaveFileWriter(..., enableRf64: true)` + `WriteBroadcastExtension(bext)` |
| `BextChunkInfo` | `BroadcastExtension` (the read-side DTO — now used on both sides, supports v1 and v2) |

The `WaveFileBuilder` test helper was shrunk to just the generic `Build(format, audio, params Chunk[])` primitive for edge-case/malformed-file tests; all happy-path chunk tests now round-trip through the real `WaveFileWriter`, giving reader/writer symmetry coverage in a single assertion.

**Cue label encoding reverted to UTF-8.** An earlier commit (pre-NAudio-3) had switched `CueList`'s label read/write paths from UTF-8 to Windows-1252 on the basis that the sonicspot.com RIFF reference describes `labl` text as *"extended ASCII"*. The RIFF spec itself does not mandate an encoding, and Windows-1252 has two real drawbacks: it requires `CodePagesEncodingProvider` registration on .NET Core (absent registration, any file with a non-ASCII label throws `NotSupportedException` at read time) and it cannot represent characters outside the Windows-1252 range. UTF-8 is the modern convention, works zero-config on every .NET target, and round-trips any character. Tests cover round-tripping of Björk, Ω-section, and 音楽 to prove it.

---

## Breaking Changes from 2.x

These will need to be documented in the migration guide:

### Platform requirements
| Change | Migration |
| ------ | --------- |
| Minimum target is net9.0 across all projects (including NAudio.Core and NAudio.Midi) | .NET Framework / .NET 6-8 users stay on NAudio 2.x. The .NET 9 floor avoids a .NET 8 ComWrappers DICASTABLE CastCache regression — see "Minimum platform" in *Target Framework Decisions* |
| NAudio.Wasapi requires net9.0-windows10.0.19041.0 | Windows 10 2004 or later, .NET 9 or later |
| NAudio.Uap package is removed | Use `WasapiPlayerBuilder` / `WasapiRecorderBuilder` from NAudio.Wasapi |
| NAudio.WinForms no longer supports net472 | Use NAudio.WinForms 2.x on .NET Framework |
| NAudio.WinMM no longer supports netstandard2.0 | Use NAudio.WinMM 2.x on .NET Framework |

### WASAPI API changes

| Change | Migration |
| ------ | --------- |
| COM interfaces are `internal` | Use wrapper classes, not raw COM interfaces |
| `MMDevice.AudioClient` property is `[Obsolete]` | Use `MMDevice.CreateAudioClient()` |
| `AudioClient` constructor is `internal` | Obtain via `MMDevice.CreateAudioClient()` or `AudioClient.ActivateAsync()` |
| Exceptions are `CoreAudioException` (subclass of `COMException`) | Existing `catch (COMException)` still works; new code can catch specific types |
| `AudioEndpointVolume` notifications may arrive on different thread | If constructed on UI thread, notifications are posted to UI thread via SynchronizationContext |
| `WasapiOut` is `[Obsolete]` | Use `new WasapiPlayerBuilder()...Build()` to create a `WasapiPlayer` |
| `WasapiPlayer.Volume` uses session volume, not device volume | `Volume` now controls your app's mixer slider (via `SimpleAudioVolume`), not the system-wide device volume. To control device volume, use `DeviceVolume.MasterVolumeLevelScalar`. For per-channel stream control, use `StreamVolume` (shared mode only) |
| `WasapiCapture` is `[Obsolete]` | Use `new WasapiRecorderBuilder()...Build()` to create a `WasapiRecorder` |
| `WasapiLoopbackCapture` is `[Obsolete]` | Use `WasapiRecorderBuilder` with process loopback or render device |
| `PropertyStoreProperty.Value` type changed from `PropVariant` to `object` | `Value` now exposes the resolved managed value (string, uint, byte[], Guid, …). Cast to the expected type. The previous `PropVariant` exposed pointer fields (LPWSTR/BLOB/CLSID) that were unsafe to read after the underlying COM-allocated memory was cleared |
| `PropVariant.DataType` returns `NAudio.CoreAudioApi.Interfaces.VarType` instead of `System.Runtime.InteropServices.VarEnum` | `VarType` is a NAudio-defined `[Flags]` enum with the COM-ABI `VT_*` values. It's a binary break, but `VarEnum` is `[Obsolete]` (`SYSLIB0050`) so callers were already on a deprecated path. Bitwise operations (`VT_VECTOR \| VT_UI1`) keep the same numeric values |
| `PropertyStore.GetValue(int)` is `[Obsolete]` | Returned `PropVariant` may carry dangling pointers for VT_LPWSTR/VT_BLOB/VT_CLSID. Use the `PropertyStore[int]` or `PropertyStore[PropertyKey]` indexers, which resolve the value before clearing the underlying buffer |
| `PropVariantNative` class removed | Internal cleanup — call `PropVariant.Clear(IntPtr)` directly (the public API is unchanged) |

### IWaveProvider / ISampleProvider migration

| Change | Migration |
| ------ | --------- |
| `IWaveProvider.Read` signature: `Read(byte[], int, int)` → `Read(Span<byte>)` | Change implementations to `int Read(Span<byte> buffer)`. Callers with `byte[]` use `source.Read(buffer.AsSpan(offset, count))` |
| `ISampleProvider.Read` signature: `Read(float[], int, int)` → `Read(Span<float>)` | Change implementations to `int Read(Span<float> buffer)`. Callers with `float[]` use `source.Read(buffer.AsSpan(offset, count))` |
| `IWavePlayer.Init()` takes `IWaveProvider` (now Span-based) | `WaveStream` works directly; `ISampleProvider` can use the `Init()` extension method in `WaveExtensionMethods` |
| `Init(IWavePlayer, ISampleProvider, bool convertTo16Bit)` extension removed | Use `Init(IWavePlayer, ISampleProvider)` (always IEEE float) or convert to 16-bit upstream via `SampleToWaveProvider16` |
| `WaveProvider32` abstract method changed to `Read(Span<float>)` | Custom subclasses must update their override signature |
| `WaveProvider16` abstract method changed to `Read(Span<short>)` | Custom subclasses must update their override signature |
| `SimpleCompressorStream` renamed to `SimpleCompressorEffect` | Update class name references; now implements `ISampleProvider` and takes `ISampleProvider` in constructor |

### Media Foundation API changes

| Change | Migration |
| ------ | --------- |
| All MF COM interfaces (`IMFSourceReader`, `IMFSinkWriter`, etc.) are `internal` | Use wrapper classes (`MfSourceReader`, `MfSinkWriter`, etc.) |
| `MediaFoundationInterop` is `internal` | Use `MediaFoundationApi` methods instead |
| `MediaType.MediaFoundationObject` is `internal` | Use `MediaType` properties (`SampleRate`, `SubType`, etc.) |
| `MediaType(IMFMediaType)` constructor is `internal` | Use `MediaType()` or `MediaType(WaveFormat)` |
| `MediaFoundationApi.EnumerateTransforms` returns `MfActivate` wrappers | `MfActivate` has `AttributeCount`, `GetAttributeByIndex`, `GetString`, `GetUInt32`, `GetGuid`, `ActivateTransform` |
| Enums renamed: `_MFT_ENUM_FLAG` → `MftEnumFlags`, etc. | All 9 underscore-prefixed enums renamed to PascalCase with PascalCase members |
| Structs renamed: `MFT_INPUT_STREAM_INFO` → `MftInputStreamInfo`, etc. | All 6 ALL_CAPS structs renamed to PascalCase with PascalCase fields |
| `MF_SOURCE_READER_FLAG` → `SourceReaderFlags` | Members renamed: `MF_SOURCE_READERF_ENDOFSTREAM` → `EndOfStream`, etc. |
| `MFT_MESSAGE_TYPE` → `MftMessageType` | Members renamed: `MFT_MESSAGE_COMMAND_FLUSH` → `Flush`, etc. |
| MF errors throw `MediaFoundationException` (subclass of `COMException`) | Existing `catch (COMException)` still works; new code can catch `MediaFoundationException` |
| `MediaFoundationTransform` finalizer removed | Ensure `Dispose()` is called |
| `MediaFoundationEncoder` finalizer removed | Ensure `Dispose()` is called |
| `MediaType` now implements `IDisposable` | Call `Dispose()` or use `using` |

### WinMM API changes

| Change | Migration |
| ------ | --------- |
| `WaveOut` class removed (NAudio.WinForms) | Default: use `WaveOut` (NAudio.WinMM) — event-callback, same `IWavePlayer` interface. If you relied on callbacks arriving on the UI thread (i.e. used `WaveCallbackInfo.NewWindow` / `ExistingWindow`), use `WaveOutWindow` (NAudio.WinForms) — same behaviour under the hood, two constructors (parameterless = owns a hidden window, `IntPtr` = subclasses your window). Function callback mode (`FunctionCallback`) is gone for good — it was never reliable |
| `WaveIn` class removed (NAudio.WinForms) | Default: use `WaveIn` (NAudio.WinMM) — event-callback, same `IWaveIn` interface. For UI-thread `DataAvailable`, use `WaveInWindow` (NAudio.WinForms) — same two-constructor pattern as `WaveOutWindow` |
| `WaveCallbackInfo` class removed | The old three-way strategy (`FunctionCallback` / `NewWindow` / `ExistingWindow`) is replaced by picking a class: `WaveOut`/`WaveIn` for event callbacks (no UI thread needed), `WaveOutWindow`/`WaveInWindow` for window callbacks. Function callback mode is permanently gone |
| `WaveWindow` / `WaveWindowNative` classes removed | Not exposed anymore — the window message pump is an internal detail of `WaveOutWindow`/`WaveInWindow` (both host types live in NAudio.WinForms but are `internal`) |
| `WaveCallbackStrategy` enum removed | Not needed — strategy is encoded in the chosen class and constructor |
| `WaveOut.DeviceCount` / `WaveOut.GetCapabilities()` | Use `WaveOutEvent.DeviceCount` / `WaveOutEvent.GetCapabilities()` |
| `WaveIn.DeviceCount` / `WaveIn.GetCapabilities()` | Use `WaveInEvent.DeviceCount` / `WaveInEvent.GetCapabilities()` |
| `WaveInterop` is `internal` | Use `WaveOutEvent`/`WaveInEvent`, not raw P/Invoke |
| `WaveHeader`, `WaveHeaderFlags`, `MmTime` are `internal` | These interop types were never intended for direct use |
| `WaveOutBuffer`, `WaveInBuffer` are `internal` | Buffer management is an implementation detail of `WaveOutEvent`/`WaveInEvent` |
| `WaveOutUtils` is `internal` | Use `WaveOutEvent.Volume` / `WaveOutEvent.GetPosition()` |
| `WaveInEvent` default format changed from 8000/16/mono to 44100/16/stereo | Set `WaveFormat` explicitly if you need the old default |
| `WaveOutEvent.DesiredLatency` replaced by `BufferMilliseconds` | `BufferMilliseconds` specifies the size of each individual buffer (default 100ms). Old `DesiredLatency` was the total across all buffers, which was confusing with `NumberOfBuffers > 1` |
| `WaveOutEvent` renamed to `WaveOut` | `WaveOutEvent` still exists as an obsolete subclass for migration — update to `WaveOut` |
| `WaveInEvent` renamed to `WaveIn` | `WaveInEvent` still exists as an obsolete subclass for migration — update to `WaveIn` |

### WAV chunk API changes

| Change | Migration |
| ------ | --------- |
| `WaveFileReader.ExtraChunks` removed | Use `WaveFileReader.Chunks` (returns `WaveChunks`) — same `RiffChunk` elements, now via a richer collection |
| `WaveFileReader.GetChunkData(RiffChunk)` removed | Use `WaveFileReader.Chunks.GetData(RiffChunk)` — same lazy read semantics |
| `CueWaveFileReader` removed | `new WaveFileReader(...).Chunks.ReadCueList()` returns a `CueList` (or `null` if absent). No subclass required — works on any `WaveFileReader` |
| `CueList.FromChunks` (was internal) removed | `CueListInterpreter.Instance.Interpret(chunks)` — or just `chunks.ReadCueList()` |
| New: `WaveChunks.Find(id)` / `FindAll(id)` / `Contains(id)` | Use these instead of iterating `ExtraChunks` and comparing `IdentifierAsString` |
| New: `IWaveChunkInterpreter<T>` extension point | Implement to plug in support for additional chunk types without subclassing the reader |
| New: `BroadcastExtension` + `BextInterpreter` | Unified read/write DTO for BWF `bext` (replaces the old write-only `BextChunkInfo`) |
| New: `InfoMetadata` + `InfoListInterpreter` | Read and write `LIST/INFO` metadata (artist, title, copyright, etc.) via a single type |
| `CueWaveFileWriter` removed | `new WaveFileWriter(path, format)` + `AddCue(position, label)` — or `WriteCueList(cueList)` extension for callers that already have a populated `CueList` |
| `BwfWriter` removed | `new WaveFileWriter(path, format, enableRf64: true)` + `WriteBroadcastExtension(bext)` extension. RF64 promotion now belongs to `WaveFileWriter` rather than being tied to BWF |
| `BextChunkInfo` removed | `BroadcastExtension` — same fields, plus v2 loudness support and a `ToChunkData()` serialiser. `OriginationDateTime` replaced by separate `OriginationDate`/`OriginationTime` strings (helpers `BroadcastExtension.FormatOriginationDate(DateTime)` / `FormatOriginationTime(DateTime)` match the BWF `yyyy-MM-dd` / `HH:mm:ss` form) |
| New: `WaveFileWriter.AddChunk(string, byte[], ChunkPosition)` | Low-level entry point for adding arbitrary RIFF chunks before or after the data chunk |
| New: `WaveFileWriter.AddChunk(IWaveChunkWriter)` | Interface-based entry point (symmetric with `IWaveChunkInterpreter<T>` on the read side) |
| New: `WaveFileWriterOptions` + constructor overload `WaveFileWriter(stream/path, format, options)` | Single configuration surface; `new WaveFileWriterOptions { EnableRf64 = true }` reserves a `JUNK` placeholder and promotes to `RF64` + `ds64` at close when the data chunk exceeds 4 GB. Passing `null` for options uses defaults |

### Sample / demo updates

| Change | Notes |
| ------ | ----- |
| NAudioDemo network chat: retired the vendored `Lib/NSpeex/NSpeex.dll` and replaced the three Speex codecs with Opus codecs (Narrow 8 kHz / Wide 16 kHz / Full 48 kHz) via the `Concentus` NuGet package. Roundtrip-tested by `OpusChatCodecTests`. | Speex was deprecated by Xiph in 2012 in favour of Opus; this removes the only binary in the repo |

### DMO API changes

| Change | Migration |
| ------ | --------- |
| `DmoInputStatusFlags.DMO_INPUT_STATUSF_ACCEPT_DATA` renamed | Use `DmoInputStatusFlags.AcceptData` |
| `DmoEnumFlags.DMO_ENUMF_INCLUDE_KEYED` renamed | Use `DmoEnumFlags.IncludeKeyed` |
| `MediaParamCurveType.MP_CURVE_*` members renamed | Use PascalCase: `Jump`, `Linear`, `Square`, `InverseSquare`, `Sine` |
| `MediaBuffer` finalizer removed | Ensure `Dispose()` is called |
| `MediaObject` errors throw `MediaFoundationException` | Existing `catch (COMException)` still works |
| `WindowsMediaMp3Decoder` "DO NOT USE" label removed | Class is now properly documented; use `DmoMp3FrameDecompressor` for high-level MP3 decoding |

---

## Verification Status

- 834 tests passing, 0 failures, 12 skipped (skipped tests require specific hardware or files)
- Manual testing: WASAPI playback and capture confirmed working in demo app
- All `CoreAudioApi/` wrapper classes use `CoreAudioException.ThrowIfFailed` (zero `Marshal.ThrowExceptionForHR` remaining)
- All consumed Core Audio COM interfaces use `[GeneratedComInterface]` (31 interfaces — including `IPropertyStore` from Phase 2d)
- Callback interfaces correctly remain `[ComImport]` (6 interfaces)
- New `WasapiPlayer` and `WasapiRecorder` compile and are ready for integration testing
- All 12 Media Foundation COM interfaces have `[GeneratedComInterface]` versions in `MediaFoundation/Interfaces/`
- 7 new MF wrapper classes (`MfMediaBuffer`, `MfSample`, `MfMediaType`, `MfSourceReader`, `MfSinkWriter`, `MfTransform`, `MfActivate`) with deterministic COM release
- All MF consumer classes (`MediaFoundationReader`, `MediaFoundationEncoder`, `MediaFoundationTransform`, `MediaFoundationResampler`) use `MediaFoundationException.ThrowIfFailed`
- Finalizers removed from `MediaFoundationTransform` and `MediaFoundationEncoder`
- All old MF COM interfaces are `internal` — no COM types in the public API surface
- All 5 consumed DMO COM interfaces have `[GeneratedComInterface]` versions in `Dmo/Interfaces/`
- `IMediaBuffer` correctly remains `[ComImport]` (callback interface implemented by managed `MediaBuffer` class)
- `MediaObject` uses `MediaFoundationException.ThrowIfFailed` (zero `Marshal.ThrowExceptionForHR` remaining)
- `MediaBuffer` finalizer removed
- `IWaveProvider` and `ISampleProvider` are now Span-based: `Read(Span<byte>)` and `Read(Span<float>)` respectively
- All 23 sample providers in NAudio.Core implement `ISampleProvider` with `Read(Span<float>)`
- All 16 non-Stream wave providers implement `IWaveProvider` with `Read(Span<byte>)`
- All concrete `WaveStream` subclasses in NAudio's assemblies override `Read(Span<byte>)` directly (enforced by architectural test) — no bridge copy on the span path
- All 5 active output devices (`WaveOut`, `DirectSoundOut`, `AsioOut`, `WasapiPlayer`, `WasapiOut` [deprecated]) accept `IWaveProvider` via `Init()`
- No adapter classes needed — single set of interfaces throughout the codebase
- `WaveStream` base class implements `IWaveProvider` via `Stream.Read(Span<byte>)` override
