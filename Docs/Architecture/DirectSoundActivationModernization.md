# DirectSound Activation + ComWrappers Bridging Modernization

> **Status: COMPLETE** (Phase 2g in [MODERNIZATION.md](MODERNIZATION.md)). Working branch: `naudio3dev-directsound-migration`. `<IsAotCompatible>true</IsAotCompatible>` is now on `NAudio.Core.csproj` — the move of `DirectSoundOut` into `NAudio.Wasapi` and the removal of `NAudio.Core/Utils/NativeMethods.cs` left `NAudio.Core` with zero P/Invokes and zero COM interop. Validated by `NAudioAotSmokeTest` running all four sections (RCW property reads, CCW callbacks, MediaFoundation round-trip, DirectSound playback) under `PublishTrimmed=true` + `BuiltInComInteropSupport=false`.

Phase 2g closes [GitHub issue #1191](https://github.com/naudio/NAudio/issues/1191) — `DirectSoundOut` failed at runtime under `<PublishTrimmed>true</PublishTrimmed>` because the trimmer strips `System.StubHelpers.InterfaceMarshaler`, the type the legacy `[ComImport]` `out IDirectSound` marshalling depends on. The migration to `[GeneratedComInterface]` + explicit `Marshal.QueryInterface` + `[LibraryImport]` removes every machinery the trimmer was stripping.

This is the parallel of [CoreAudioActivationModernization.md](CoreAudioActivationModernization.md) (Phase 2e) and [MediaFoundationActivationModernization.md](MediaFoundationActivationModernization.md) (Phase 2e′) for the DirectSound surface. Read the latter first if you weren't part of those rounds — its **Hazards** section lists the AV-troubleshooting context this work depends on. DirectSound is significantly smaller in scope, so the document below is correspondingly briefer.

---

## Scope

DirectSoundOut was completely isolated in `NAudio.Core`: zero internal references, no class field types, no method parameter types in the rest of the assembly. The dependency cut was clean, allowing the file to relocate to `NAudio.Wasapi` (where it belongs alongside the other Windows-only audio APIs) without any cascade.

### What moved

| Before | After |
| --- | --- |
| `NAudio.Core/Wave/WaveOutputs/DirectSoundOut.cs` (single file, 942 lines) | `NAudio.Wasapi/DirectSound/DirectSoundOut.cs` + `NAudio.Wasapi/DirectSound/DirectSoundException.cs` + `NAudio.Wasapi/DirectSound/Interfaces/{IDirectSound,IDirectSoundBuffer,IDirectSoundNotify,DirectSoundTypes}.cs` |
| `NAudio.Core/Utils/NativeMethods.cs` (kernel32 LoadLibrary/GetProcAddress/FreeLibrary) | **Deleted.** `AcmDriver` (its only consumer) now uses `System.Runtime.InteropServices.NativeLibrary` |

`namespace NAudio.Wave` was preserved on `DirectSoundOut`, so direct consumers only need to add a `NAudio.Wasapi` package reference — no `using` updates. Consumers of the `NAudio` meta-package (which references both Core and Wasapi) see no API change.

### Touch-surface categories

#### A — `[ComImport]` interfaces (3) → `[GeneratedComInterface]`

| Interface | GUID | Methods invoked from `DirectSoundOut` |
| --- | --- | --- |
| `IDirectSound` | `279AFA83-4981-11CE-A521-0020AF0BE560` | `CreateSoundBuffer`, `SetCooperativeLevel` |
| `IDirectSoundBuffer` | `279AFA85-4981-11CE-A521-0020AF0BE560` | `GetCaps`, `GetCurrentPosition`, `GetStatus`, `Lock`, `Play`, `SetCurrentPosition`, `Stop`, `Unlock`, `Restore` |
| `IDirectSoundNotify` | `b0210783-89cd-11d0-af08-00a0c925cd16` | `SetNotificationPositions` |

All slots declared `[PreserveSig] int Foo(...)` returning HRESULT. Unused vtable slots declared with `IntPtr` placeholder parameters to preserve order without forcing marshalling decisions for code we never call. In particular, `IDirectSoundBuffer.SetFormat`'s `WAVEFORMATEX` parameter (a managed reference type at a `[GeneratedComInterface]` boundary) is dead code: the wave format reaches the secondary buffer via `BufferDescription.lpwfxFormat` (a pinned `GCHandle`), not via `SetFormat`. Declaring its slot as `(IntPtr)` sidesteps the marshalling concern entirely.

#### B — `[DllImport]` P/Invokes (4) → `[LibraryImport]`

| Function | Module | Notes |
| --- | --- | --- |
| `DirectSoundCreate` | dsound.dll | Was `[Out, MarshalAs(UnmanagedType.Interface)] out IDirectSound`. Now `out IntPtr` projected via `ComActivation.ComWrappers.GetOrCreateObjectForComInstance(ptr, UniqueInstance)`. This is the specific path issue #1191 was failing on — the `Out, MarshalAs(UnmanagedType.Interface)` parameter relied on `StubHelpers.InterfaceMarshaler` which the trimmer strips. |
| `DirectSoundEnumerate` | dsound.dll | Took a `DSEnumCallback` delegate parameter. Now takes `IntPtr`; callback dispatch via `[UnmanagedCallersOnly]` static thunk + C# function-pointer syntax (`delegate* unmanaged[Stdcall]<...>`). Zero allocation, no GCHandle pinning, no `Marshal.GetFunctionPointerForDelegate` indirection. |
| `GetDesktopWindow` | user32.dll | Trivial. |
| (none) | — | `DirectSoundCaptureCreate` was never declared — only playback enumeration + create are wired up. |

#### C — `Marshal.ReleaseComObject` sites (3) → `((ComObject)(object)x).FinalRelease()`

All three live in `DirectSoundOut.StopPlayback`. Cast through `object` is required because `wrapper is ComObject` from an interface-typed variable doesn't compile (Phase 2e Hazard #2 in `CoreAudioActivationModernization.md`).

#### D — QI cascade (1 site, Phase 2g-specific)

`DirectSoundOut.InitializeDirectSound` line 313 (pre-migration): `IDirectSoundNotify notify = (IDirectSoundNotify)soundBufferObj;`

This is a managed-side `QueryInterface` on a sibling interface of an existing RCW. Under built-in COM interop the cast triggers a QI; under source-gen `StrategyBasedComWrappers`, casting a wrapper to a sibling `[GeneratedComInterface]` does **not** auto-QI. The fix:

1. Capture the secondary buffer's raw `IUnknown` pointer (`out IntPtr secondaryBufferPtr` from `CreateSoundBuffer`).
2. `Marshal.QueryInterface(secondaryBufferPtr, ref IID_IDirectSoundNotify, out IntPtr notifyPtr)`.
3. Project `notifyPtr` via `ComActivation.ComWrappers.GetOrCreateObjectForComInstance(notifyPtr, UniqueInstance)`.
4. Call `notify.SetNotificationPositions(...)`. Notify-position array passed via pinned `GCHandle`.
5. `((ComObject)(object)notify).FinalRelease()` + `Marshal.Release(notifyPtr)` — the notify wrapper is single-use, released as soon as positions are registered.

Wrapped in try/finally so a `SetNotificationPositions` failure cannot leak the wrapper (`ComObject`'s own finalizer would eventually run, but deterministic release is the contract here).

Same pattern as the QI hazard called out in MediaFoundation's H3 (ComStream → IStream).

#### E — Class-to-struct conversion (2 types)

`BufferDescription` (DSBUFFERDESC) and `BufferCaps` (DSBCAPS) were declared as `[StructLayout(LayoutKind.Sequential, Pack = 2)] internal class`. With `[GeneratedComInterface]` they cannot be passed as managed references at the COM boundary. Converted to `internal struct`; both have only blittable fields (no strings) so the conversion is mechanical. Interface signatures changed to `in BufferDescription` / `ref BufferCaps` to match `LPCDSBUFFERDESC` / `LPDSBCAPS` by-pointer semantics. Verified under classic `[ComImport]` first as a separate commit before the source-gen migration.

#### F — `NativeMethods.cs` removal (incidental)

`NAudio.Core/Utils/NativeMethods.cs` held three `kernel32` P/Invokes (`LoadLibrary`, `GetProcAddress`, `FreeLibrary`). Its only consumer was `AcmDriver` in `NAudio.WinMM`. Without removing it, the headline claim "NAudio.Core has no Windows-specific code" would have been false. Replaced with `System.Runtime.InteropServices.NativeLibrary` (`TryLoad` / `TryGetExport` / `Free`) — cross-platform-friendly, AOT/trim-clean, and removes the file rather than relocating it.

---

## Resolution

Per-step commits on `naudio3dev-directsound-migration`:

| Commit | Step | Lines |
| --- | --- | ---: |
| `11ce983` | Move DirectSoundOut from NAudio.Core to NAudio.Wasapi | 0/0 (pure rename) |
| `9e20f43` | Convert BufferDescription/BufferCaps from class to struct | +32 / -29 |
| `332641d` | Migrate DirectSound interfaces to [GeneratedComInterface] | +428 / -270 |
| `303e95d` | Migrate DirectSound P/Invokes to [LibraryImport] + [UnmanagedCallersOnly] thunk | +27 / -25 |
| `2d36526` | Harden DirectSoundOut.InitializeDirectSound exception paths | +11 / -4 |
| `f2ef787` | Delete NAudio.Core/Utils/NativeMethods.cs, port AcmDriver to NativeLibrary | +5 / -38 |
| `bfe039f` | Add DirectSound playback section to NAudioAotSmokeTest | +42 / 0 |
| `9d7bddd` | Add DirectSound menu to NAudioConsoleTest | +122 / -1 |
| `e326751` | Set IsAotCompatible=true on NAudio.Core | +1 / 0 |

---

## Verification

- `dotnet build NAudio.slnx -c Release` clean: zero warnings (incl. IL2026/IL3050) against `NAudio.Core` and `NAudio.Wasapi`.
- `dotnet publish NAudioAotSmokeTest -c Release -p:PublishAot=false -p:PublishTrimmed=true -p:BuiltInComInteropSupport=false` clean. Resulting exe runs all four sections end-to-end:
  - 3 active render endpoints enumerated with property reads
  - 4 master-volume callbacks fired
  - 25158-byte MP3 encoded → 361920 bytes PCM decoded → 180960 bytes resampled
  - 8 DirectSound devices enumerated; `DirectSoundOut` Init/Play/Stop/Dispose lifecycle completes without exception
- `NAudioTests`: 1071 / 1073 passed (2 skipped, 0 failed). DirectSound `CanEnumerateDevices` integration test passes — exercises the new `[UnmanagedCallersOnly]` enumeration path.
- Manual smoke (NAudioConsoleTest → DirectSound → Play tone) and (NAudioDemo `DirectSoundOutPlugin` panel) — to be confirmed by maintainer before merge.

---

## Hazards (DirectSound-specific, beyond the carry-forward set)

- **`SetFormat` is dead code.** The `IDirectSoundBuffer.SetFormat` slot accepts a `WAVEFORMATEX*` but `DirectSoundOut` never invokes it. Format reaches the secondary buffer via `BufferDescription.lpwfxFormat` (pinned `GCHandle`). Anyone tempted to "wire up `SetFormat` properly" should notice that the existing path already works and that doing so would reintroduce a managed-reference-type-at-a-COM-boundary problem. Leave the slot as an `IntPtr` placeholder.
- **`BufferDescription` Pack=2 layout.** Predates the migration. The DirectSound SDK header doesn't specify packing, so the SDK uses default natural alignment; `Pack=2` is a NAudio choice that has been stable since the original DirectSoundOut import in 2008. Don't change it without re-verifying against actual DSBUFFERDESC bytes on x64 + x86 — specific historical commits were lost to time and the size/alignment that ships works.
- **STA/MTA.** DirectSound has historically required STA on the activating thread for some scenarios. `DirectSoundOut` creates a background `Thread` for playback management; the source-gen RCW is thread-agile by default, so this Just Works. Don't introduce an STA-eager activation site (e.g. `OleInitialize` calls) without auditing the existing background-thread model.
- **`[UnmanagedCallersOnly]` thunk constraints.** `EnumCallbackThunk` is a static method with `IntPtr`-only parameters and an `int` (BOOL) return. Don't add managed-type parameters or use `bool` directly — `[UnmanagedCallersOnly]` does not support managed marshalling, and `bool`'s managed→unmanaged width is undefined. The current `int` return with `1`/`0` for TRUE/FALSE is the correct shape.
- **`AcmDriver` IntPtr.Zero semantics.** The legacy `NativeMethods.LoadLibrary` / `GetProcAddress` returned `IntPtr.Zero` on failure. `NativeLibrary.Load` / `GetExport` *throw* on failure. Used `NativeLibrary.TryLoad` / `TryGetExport` to preserve the existing `if (handle == IntPtr.Zero) throw new ArgumentException(...)` patterns verbatim — don't switch to the throwing overloads without auditing the consumer error messages.

---

## Out of scope (deferred)

- **NAudio.Asio** — separate phase. Has different blockers (manual vtable extraction via reflection rather than `[ComImport]`). Two-tier path: "Quick AOT" (eliminate reflection, use C# function-pointer syntax) or "Full modernization" (`[GeneratedComInterface] IASIO`). Neither belongs in Phase 2g.
- **NAudio.WinMM** AOT enablement — the `AcmDriver` port lands incidentally as part of this phase, but the broader MMSYS surface (winmm.dll P/Invokes) hasn't been audited. Likely small (it's C-style P/Invokes, not COM), but not addressed here.
- **NAudio.Midi** — audit pending. Probably no COM at all.
- **`NAudio.Wasapi` rename to `NAudio.Windows`** — orthogonal naming question. DirectSoundOut belongs in `NAudio.Wasapi` either way; renaming the assembly is a separate decision driven by other Windows audio APIs (DirectShow, XAudio2) that may eventually land there.
- **Public-API tightening** — `DirectSoundOut.DSDEVID_DefaultPlayback` and similar might be candidates for `internal` once consumers have been audited. Defer until after the move is stable.
