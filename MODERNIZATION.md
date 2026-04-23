# NAudio 3.0 Modernization — Design Document

This document records the architectural decisions, rationale, and progress for the NAudio 3.0 modernization. It serves as a reference for ongoing development, release notes, and migration guidance.

---

## Goals

### Overall
- Establish .NET 8 as the minimum supported platform for NAudio 3.0
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

### Minimum platform: .NET 8

**Decision:** NAudio 3.0 requires .NET 8 as a minimum. All legacy targets have been removed.

**Rationale:** .NET 8 is LTS (supported until November 2026). It unlocks `[GeneratedComInterface]` (source-generated COM interop), `Span<T>` in interop signatures, and trimming support. There is no multi-targeting for .NET 9 — consumers on .NET 9+ will reference the net8.0 builds without issue.

**Migration impact:** Users on .NET Framework, .NET 6, or .NET 7 should stay on NAudio 2.x.

### Per-project TFMs

| Project | NAudio 2.x TFM(s) | NAudio 3.0 TFM(s) | Rationale |
| ------- | ------------------ | ------------------ | --------- |
| NAudio.Core | netstandard2.0 | net8.0 | Moved to net8.0 to unlock `Span<T>` for `IWaveProvider`/`ISampleProvider` interfaces |
| NAudio.Midi | netstandard2.0 | net8.0 | Follows NAudio.Core (depends on it) |
| NAudio.Asio | netstandard2.0; net8.0-windows | net8.0-windows | Dropped netstandard2.0 leg (NAudio.Core now requires net8.0). Removed `Microsoft.Win32.Registry` polyfill |
| NAudio.WinMM | netstandard2.0; net6.0 | net8.0-windows | Windows-only (P/Invoke into winmm.dll). Removed netstandard2.0 and `Microsoft.Win32.Registry` polyfill |
| NAudio.WinForms | net472; netcoreapp3.1 | net8.0-windows | Windows-only WinForms controls. Removed net472 workarounds (`GenerateResourceUsePreserializedResources`, `System.Resources.Extensions`). SDK changed from `Microsoft.NET.Sdk.WindowsDesktop` to `Microsoft.NET.Sdk` |
| NAudio.Wasapi | netstandard2.0 | net8.0-windows10.0.19041.0 | Windows 10 2004 (build 19041) minimum required for process-specific loopback capture. Single target — no separate net9.0 build needed |
| NAudio.Extras | net472; netcoreapp3.1; net8.0-windows10.0.19041.0; net8.0 | net8.0; net8.0-windows10.0.19041.0 | Dual target: cross-platform core + Windows-specific `AudioPlaybackEngine` (gated by `WINDOWS` define) |
| NAudio (umbrella) | net472; netcoreapp3.1; net6.0-windows; net6.0; net8.0-windows10.0.19041.0 | net8.0; net8.0-windows10.0.19041.0 | Dual target: cross-platform (Core + Midi) + Windows (WinMM, WinForms, ASIO, WASAPI). SDK changed from `Microsoft.NET.Sdk.WindowsDesktop` to `Microsoft.NET.Sdk` |
| NAudio.Uap | net9.0-windows10.0.26100 | **Retired** | See "NAudio.Uap project retirement" below |
| NAudioWpfDemo | net8.0-windows10.0.19041.0 | net8.0-windows10.0.19041.0 | Demo app. SDK changed from `Microsoft.NET.Sdk.WindowsDesktop` to `Microsoft.NET.Sdk` |
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

**Decision:** 30 COM interfaces that NAudio *calls into* (obtained from Windows) were converted to `[GeneratedComInterface]`. 8 interfaces remain as `[ComImport]`:

| Interface | Reason for staying `[ComImport]` |
| --------- | -------------------------------- |
| `IAudioSessionEvents` | Implemented by managed code as callback |
| `IAudioEndpointVolumeCallback` | Implemented by managed code as callback |
| `IMMNotificationClient` | Implemented by managed code as callback |
| `IAudioSessionNotification` | Implemented by managed code as callback |
| `IControlChangeNotify` | Implemented by managed code as callback |
| `IActivateAudioInterfaceCompletionHandler` | Implemented by managed code as callback |
| `IPropertyStore` | Uses `PropVariant` with `[StructLayout(LayoutKind.Explicit)]` — not compatible with source-generated marshaling |
| `MMDeviceEnumeratorComObject` | COM coclass for activation, not an interface |

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

**7i: Zero-copy recording on `WaveInEventArgs` (WASAPI capture path) — DONE**

**Problem:** Every `DataAvailable` event from `WaveIn` / `WasapiCapture` forces consumers onto the `(byte[] Buffer, int BytesRecorded)` triple. For WASAPI capture specifically the cost was worse: `WasapiCapture.ReadNextPacket` did a `Marshal.Copy` from the native WASAPI buffer into a managed `byte[]` *on every packet*, solely because the event signature demanded an array. Consumers then often did nothing but forward those bytes into a `BufferedWaveProvider` or WAV file — never benefiting from the materialisation.

`WasapiRecorder` (the new builder-based API) already bypassed this with its own span-based lease callback — but `WasapiCapture` is still the API most existing users pick, and the `WaveIn` / `WasapiCapture` event surface is what every third-party recording app in the wild uses.

**Decision:** Add a `ReadOnlySpan<byte> BufferSpan` property to `WaveInEventArgs`, sliced to `BytesRecorded`. Add a second constructor that lets a producer hand out captured bytes without first copying into a managed array — used by `WasapiCapture` to skip the `Marshal.Copy` and hand the native WASAPI buffer straight to the event handler. Legacy `Buffer` property stays, with doc updates pointing callers at `BufferSpan` and noting that on the WASAPI zero-copy path reading `Buffer` allocates a fresh byte[] (cached on the event — second read is free).

**Changes:**

- [x] `NAudio.Core/Wave/WaveInputs/WaveInEventArgs.cs` — adds `BufferSpan` and a zero-copy ctor; keeps the existing `(byte[], int)` ctor and `Buffer` property for backward compatibility. Aliasing contract ("do not retain past handler return") documented on every accessor.
- [x] `NAudio.Wasapi/NativeAudioBufferMemoryManager.cs` — new internal `sealed unsafe MemoryManager<byte>` wrapping a native IntPtr + length. Lifetime is caller-controlled (bound to the `GetBuffer`/`ReleaseBuffer` window on the WASAPI capture client).
- [x] `NAudio.Wasapi/WasapiCapture.ReadNextPacket` — rewritten. Non-silent packets hand the native WASAPI buffer straight to the handler — no `Marshal.Copy`. Silent packets still materialise into a (renamed) `silenceBuffer` byte[] (zero-filled) because the Silent-flag contract means the consumer expects zeros and we can't overwrite a read-only native buffer. The packet-accumulation + mid-packet-split logic has been retired — now one event per WASAPI packet.
- [x] `NAudio.Core/Wave/WaveProviders/WaveInProvider.cs` — switched from `AddSamples(e.Buffer, 0, e.BytesRecorded)` to `AddSamples(e.BufferSpan)`. Without this change, the main internal consumer of `WaveInProvider` would have materialised every native-backed event via the legacy `Buffer` property, defeating the zero-copy optimisation.
- [x] New tests `NAudioTests/WaveStreams/WaveInEventArgsTests.cs` — cover both ctors, verify the byte[] ctor hands back the same array reference from `Buffer`, verify the zero-copy ctor materialises a fresh byte[] via `Buffer` only when no backing array is available, and verify the materialised `Buffer` is cached across repeat accesses.

**Behaviour changes:**

- **WasapiCapture event cadence is now one-event-per-packet** instead of batched-per-capture-cycle. For a typical 10 ms-period capture that usually means the same number of events per second; for unusually large packets it could be more. Consumers that assumed one event per capture cycle may see different timing. Total data and ordering are unchanged.
- **`e.Buffer` may allocate on WASAPI capture events.** Consumers that still read `e.Buffer` instead of `e.BufferSpan` pay one allocation per event on the WASAPI path (cached per event — second read is free). Recommendation: migrate to `e.BufferSpan`. Performance regression, not correctness.
- **`e.Buffer.Length` equals `e.BytesRecorded`** on zero-copy events, but may exceed it on byte[]-backed events (unchanged legacy semantics). Code that compares those two is suspect; code that asserts equality may start passing on WASAPI paths where it previously failed.

**What this does *not* touch:**

- WinMM `WaveIn.cs` continues to pass a byte[] (the pinned `WaveHeader` buffer). No reason to change — the array is already native-addressable for WinMM's callback contract.
- `WasapiRecorder` continues to use its own lease-based `DataAvailable` callback (span-first since inception). It does not flow through `WaveInEventArgs` at all.

**Verification:** Full solution builds with 0 warnings. All 989 tests pass (14 skipped for missing external test files).

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
- [x] `IMediaBuffer` kept as `[ComImport]` — it's a callback interface implemented by managed `MediaBuffer` class
- [x] Old `[ComImport]` interfaces retained for existing consumers (already internal)

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
| Minimum target is net8.0 across all projects (including NAudio.Core and NAudio.Midi) | .NET Framework / .NET 6-7 users stay on NAudio 2.x |
| NAudio.Wasapi requires net8.0-windows10.0.19041.0 | Windows 10 2004 or later |
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
| `WaveOut` class removed (NAudio.WinForms) | Use `WaveOutEvent` (NAudio.WinMM) — same `IWavePlayer` interface, same `DeviceNumber`/`NumberOfBuffers` properties. `DesiredLatency` replaced by `BufferMilliseconds` |
| `WaveIn` class removed (NAudio.WinForms) | Use `WaveInEvent` (NAudio.WinMM) — same `IWaveIn` interface |
| `WaveCallbackInfo` class removed | Not needed — `WaveOutEvent`/`WaveInEvent` always use event callbacks |
| `WaveWindow` / `WaveWindowNative` classes removed | Not needed — window callbacks removed |
| `WaveCallbackStrategy` enum removed | Not needed — only event callbacks remain |
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
- All consumed Core Audio COM interfaces use `[GeneratedComInterface]` (30 interfaces)
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
