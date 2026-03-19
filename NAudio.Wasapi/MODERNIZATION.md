# NAudio.Wasapi Modernization — Design Document

This document records the architectural decisions, rationale, and progress for the NAudio 3.0 modernization of the WASAPI interop layer. It serves as a reference for ongoing development, release notes, and migration guidance.

---

## Goals

Build a complete, efficient, idiomatic .NET wrapper for the Windows Core Audio APIs that:
- Avoids unnecessary memory copies (zero-copy buffer access via `Span<T>`)
- Manages COM object lifetimes reliably (no unsafe finalizers, deterministic release where it matters)
- Exposes all WASAPI capabilities including modern features (IAudioClient3 low-latency, process-specific loopback capture)
- Provides an idiomatic .NET developer experience (builder pattern, async patterns, rich exceptions)
- Handles versioned COM interfaces (IAudioClient2/3) gracefully with runtime capability detection

---

## Key Decisions

### 1. Drop netstandard2.0, target net8.0-windows minimum

**Decision:** NAudio.Wasapi targets `net8.0-windows10.0.19041.0` and `net9.0-windows10.0.26100`. The netstandard2.0 target was removed.

**Rationale:** Every consumer of WASAPI is Windows-only. .NET 8+ unlocks `[GeneratedComInterface]` (source-generated COM interop), `Span<T>` in interop signatures, and trimming support. The Windows 10 2004 (build 19041) minimum is required for process-specific loopback capture.

**Migration impact:** Users on .NET Framework or .NET 6/7 should stay on NAudio.Wasapi 2.x (final netstandard2.0 release).

### 2. `[GeneratedComInterface]` for consumed interfaces, `[ComImport]` for callbacks

**Decision:** 30 COM interfaces that NAudio *calls into* (obtained from Windows) were converted to `[GeneratedComInterface]`. 8 interfaces remain as `[ComImport]`:

| Interface | Reason for staying `[ComImport]` |
|-----------|----------------------------------|
| `IAudioSessionEvents` | Implemented by managed code as callback |
| `IAudioEndpointVolumeCallback` | Implemented by managed code as callback |
| `IMMNotificationClient` | Implemented by managed code as callback |
| `IAudioSessionNotification` | Implemented by managed code as callback |
| `IControlChangeNotify` | Implemented by managed code as callback |
| `IActivateAudioInterfaceCompletionHandler` | Implemented by managed code as callback |
| `IPropertyStore` | Uses `PropVariant` with `[StructLayout(LayoutKind.Explicit)]` — not compatible with source-generated marshaling |
| `MMDeviceEnumeratorComObject` | COM coclass for activation, not an interface |

**Rationale:** `[GeneratedComInterface]` is designed for calling *into* COM objects, not for implementing interfaces that COM calls *back into*. The two systems coexist correctly at runtime — confirmed by 836 passing tests and manual testing of the demo app.

### 3. Versioned interfaces: single wrapper class with runtime QI

**Decision:** `AudioClient` wraps IAudioClient, IAudioClient2, and IAudioClient3 in a single class. At construction, it QueryInterfaces for the newer versions and stores them if available.

```csharp
public bool SupportsAudioClient2 => audioClientInterface2 != null;
public bool SupportsAudioClient3 => audioClientInterface3 != null;
```

Methods requiring newer interfaces throw `PlatformNotSupportedException` if unavailable.

**Rationale:** Users shouldn't need to know which COM interface version exists on their machine. A single `AudioClient` class with capability-check properties is more discoverable than separate `AudioClient`, `AudioClient2`, `AudioClient3` classes.

### 4. COM lifetime: deterministic release via `Marshal.Release(IntPtr)` on stream-path objects

**Decision:** Wrapper classes that hold COM objects on the audio stream path (AudioRenderClient, AudioCaptureClient, AudioClockClient, AudioStreamVolume) store the raw COM `IntPtr` from `GetService` and call `Marshal.Release(nativePointer)` in `Dispose()`.

Other wrapper classes (AudioEndpointVolume, AudioSessionManager, PropertyStore, etc.) let the GC handle COM release.

**Rationale:** In exclusive mode, the audio device cannot be re-opened until all COM references are released. `Marshal.ReleaseComObject` doesn't work with `[GeneratedComInterface]` types (it only works with classic RCWs). `Marshal.Release(IntPtr)` directly decrements the COM reference count regardless of the managed wrapper type.

Finalizers were removed from all classes — calling `Marshal.Release` from a finalizer is undefined behavior since the COM runtime may already be torn down.

### 5. Error handling: `CoreAudioException` hierarchy replacing `Marshal.ThrowExceptionForHR`

**Decision:** All HRESULT checks in CoreAudioApi/ use `CoreAudioException.ThrowIfFailed(hr)` instead of `Marshal.ThrowExceptionForHR`. CoreAudioException inherits from `COMException` for backwards compatibility.

Specific exception types for common failure modes:
- `AudioDeviceDisconnectedException` (AUDCLNT_E_DEVICE_INVALIDATED)
- `AudioFormatNotSupportedException` (AUDCLNT_E_UNSUPPORTED_FORMAT)
- `AudioDeviceInUseException` (AUDCLNT_E_DEVICE_IN_USE)
- `AudioExclusiveModeNotAllowedException` (AUDCLNT_E_EXCLUSIVE_MODE_NOT_ALLOWED)

**Rationale:** Human-readable error messages for all 30+ AUDCLNT_E_* codes. Callers can catch specific exceptions instead of checking HRESULT codes. Inheriting from `COMException` means existing `catch (COMException)` handlers still work.

### 6. `MMDevice.AudioClient` property renamed to `CreateAudioClient()` method

**Decision:** New `CreateAudioClient()` method with clear ownership semantics. Old property kept with `[Obsolete]`.

**Rationale:** The property created a new `AudioClient` instance on every access, which violates the principle of least surprise for a property. The method name makes the allocation and ownership transfer explicit.

---

## What's Been Done (Phases 1, 2a–2c)

### Phase 1: Safety cleanup
- Removed dangerous finalizers from 5 classes (AudioEndpointVolume, MMDevice, AudioSessionControl, AudioSessionManager, SimpleAudioVolume)
- Standardized IDisposable patterns across all wrapper classes
- Fixed callback unregistration in Dispose (no longer throws via `Marshal.ThrowExceptionForHR`)

### Phase 2a: Infrastructure
- Changed target framework from `netstandard2.0;net9.0` to `net8.0-windows10.0.19041.0;net9.0-windows10.0.26100`
- Updated 8 downstream projects to compatible TFMs
- Added `WASAPI` conditional define in NAudio umbrella project for `AudioFileReader` MediaFoundation fallback
- Created `CoreAudioException` hierarchy with human-readable messages for all AUDCLNT_E_* codes
- Added MMCSS P/Invoke (`AvSetMmThreadCharacteristics`, `AvRevertMmThreadCharacteristics`) to NativeMethods
- Created `IAudioSource` interface (Span-based provider) with `WaveProviderAudioSource` bridge adapter
- Uncommented and made public `AudioClientActivationParams`, `AudioClientProcessLoopbackParams`, `ProcessLoopbackMode`, `AudioClientActivationType` for process-specific loopback capture

### Phase 2b: COM interface conversion
- Converted 30 interfaces to `[GeneratedComInterface]` (all `internal partial`, all methods `[PreserveSig]`)
- Created new `IAudioClient3` interface (low-latency shared mode: `GetSharedModeEnginePeriod`, `InitializeSharedAudioStream`, `GetCurrentSharedModeEnginePeriod`)
- Versioned interfaces (IAudioClient2, IAudioClient3, IAudioClock2, IAudioSessionControl2, IAudioSessionManager2, IAudioVolumeLevel) redeclare parent methods in vtable order
- All interface output parameters that return COM objects use `out IntPtr` with `Marshal.GetObjectForIUnknown` in wrappers
- All callback parameters use `IntPtr` with `Marshal.GetComInterfaceForObject` in wrappers

### Phase 2c: Wrapper class modernization
- `AudioClient`: versioned QI (IAudioClient2/3), `CoreAudioException.ThrowIfFailed`, WaveFormat marshaled to IntPtr, IAudioClient3 low-latency methods (`GetSharedModeEnginePeriod`, `InitializeSharedAudioStream`)
- `AudioRenderClient`: `RenderBufferLease` ref struct for zero-copy rendering, `nativePointer` deterministic release
- `AudioCaptureClient`: `CaptureBufferLease` ref struct for zero-copy capture, `nativePointer` deterministic release
- `AudioClockClient`, `AudioStreamVolume`: `nativePointer` deterministic release pattern
- `MMDevice`: `CreateAudioClient()` method, old property `[Obsolete]`, all Activate calls use `Marshal.GetObjectForIUnknown`
- `MMDeviceEnumerator`: `TryGetDefaultAudioEndpoint` added, COM activation bridged via `GetIUnknownForObject`/`GetObjectForIUnknown`
- `AudioEndpointVolume`: SynchronizationContext for event marshaling, callback registration via `Marshal.GetComInterfaceForObject`
- `AudioSessionControl`, `AudioSessionManager`: callback registration/unregistration updated for IntPtr pattern
- All 14 CoreAudioApi wrapper files migrated from `Marshal.ThrowExceptionForHR` to `CoreAudioException.ThrowIfFailed` (50 call sites)

---

## What Remains

### Phase 2d: High-level API redesign
- `WasapiPlayerBuilder` — builder pattern replacing WasapiOut's 4-constructor-overload pattern
  - MMCSS thread priority integration
  - AudioStreamCategory support via `AudioClientProperties`
  - IAudioClient3 low-latency auto-negotiation
  - Span-based render loop using `RenderBufferLease` and `IAudioSource`
- `WasapiCaptureBuilder` — builder pattern for capture
  - Process-specific loopback via `AudioClientActivationParams`
  - `IAsyncEnumerable<AudioBuffer>` capture pattern
  - Span-based event args using `CaptureBufferLease`
- `IAsyncDisposable` on player/capture classes

### Phase 3: Extended Core Audio APIs
- Spatial Audio (`ISpatialAudioClient`, `ISpatialAudioObject`, etc.) — Win10 1703+
- Audio Effects Manager (`IAudioEffectsManager`) — Win11+
- `IChannelAudioVolume` wrapper
- Volume ducking notifications (`IAudioVolumeDuckNotification`)

### Phase 4: DMO and Media Foundation modernization
- The `Dmo/` and `MediaFoundation/` directories in this assembly contain separate Windows APIs (DirectX Media Objects and Media Foundation)
- These are stable and functional but still use classic `[ComImport]` interop
- They will be modernized in a future pass, potentially as part of splitting this assembly into `NAudio.Wasapi`, `NAudio.MediaFoundation`, and `NAudio.Dmo`

---

## Breaking Changes from 2.x

These will need to be documented in the migration guide:

| Change | Migration |
|--------|-----------|
| Minimum target is `net8.0-windows10.0.19041.0` | .NET Framework / .NET 6-7 users stay on 2.x |
| COM interfaces are `internal` | Use wrapper classes, not raw COM interfaces |
| `MMDevice.AudioClient` property is `[Obsolete]` | Use `MMDevice.CreateAudioClient()` |
| `AudioClient` constructor is `internal` | Obtain via `MMDevice.CreateAudioClient()` or `AudioClient.ActivateAsync()` |
| Exceptions are `CoreAudioException` (subclass of `COMException`) | Existing `catch (COMException)` still works; new code can catch specific types |
| `AudioEndpointVolume` notifications may arrive on different thread | If constructed on UI thread, notifications are posted to UI thread via SynchronizationContext |

## Verification Status

- 836 tests passing (1 pre-existing ASIO test failure unrelated to changes)
- Manual testing: WASAPI playback and capture confirmed working in demo app
- All `CoreAudioApi/` wrapper classes use `CoreAudioException.ThrowIfFailed` (zero `Marshal.ThrowExceptionForHR` remaining)
- All consumed COM interfaces use `[GeneratedComInterface]` (30 interfaces)
- Callback interfaces correctly remain `[ComImport]` (6 interfaces)
