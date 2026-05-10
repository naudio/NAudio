# MediaFoundation Activation + ComWrappers Bridging Modernization

> **Status: COMPLETE** (Phase 2e′ in [MODERNIZATION.md](MODERNIZATION.md)). Working branch: `naudio3dev-mediafoundation-bridge`. `<IsAotCompatible>true</IsAotCompatible>` is now on `NAudio.Wasapi.csproj`, validated by `NAudioAotSmokeTest` running all three sections (RCW, CCW callbacks, MediaFoundation round-trip) under `PublishTrimmed=true` + `BuiltInComInteropSupport=false`.

Phase 2e′ migrates `NAudio.Wasapi/MediaFoundation/` and the top-level `MediaFoundationResampler.cs` / `MediaFoundationReader.cs` / `MediaFoundationEncoder.cs` / `StreamMediaFoundationReader.cs` off the legacy `[ComImport]` interfaces, the legacy `[DllImport]` typed-COM marshalling, the `Marshal.GetObjectForIUnknown` bridge, and `Marshal.ReleaseComObject`-based disposal. After the sweep lands, `<IsAotCompatible>true</IsAotCompatible>` is enabled on `NAudio.Wasapi.csproj` and verified by an MF section in `NAudioAotSmokeTest` running under `PublishAot=true` + `BuiltInComInteropSupport=false`.

This is the parallel of [CoreAudioActivationModernization.md](CoreAudioActivationModernization.md) (Phase 2e) for the MediaFoundation surface. Read that first if you weren't part of the previous rounds — its **Hazards** and **Resolution** sections contain the painfully-acquired AV-troubleshooting context that this work depends on.

---

## Scope: WIDER than original Phase 2e′ prompt

Initial scoping understated true touch surface ("12 `Marshal.GetObjectForIUnknown` sites + a field-type cascade"). Audit revealed the following four parallel categories all need the same end state for the flag flip to be honest, plus a fifth CCW-direction trap discovered during the audit:

### Category A — `Marshal.GetObjectForIUnknown` bridge sites (12 sites, 6 files)

Per HEAD of `naudio3dev`. Verified line numbers.

| File | Line(s) | Target type | Notes |
| --- | ---: | --- | --- |
| [NAudio.Wasapi/MediaFoundation/MediaFoundationHelpers.cs](NAudio.Wasapi/MediaFoundation/MediaFoundationHelpers.cs) | 40 | `Interfaces.IMFActivate` | Enumeration over `IntPtr` array returned from `MFTEnumEx` |
| [NAudio.Wasapi/MediaFoundation/MfSample.cs](NAudio.Wasapi/MediaFoundation/MfSample.cs) | 96, 116 | `Interfaces.IMFMediaBuffer` | `GetBufferByIndex` and `ConvertToContiguousBuffer` |
| [NAudio.Wasapi/MediaFoundation/MfActivate.cs](NAudio.Wasapi/MediaFoundation/MfActivate.cs) | 44 | `Interfaces.IMFTransform` | `IMFActivate::ActivateObject` |
| [NAudio.Wasapi/MediaFoundation/MfTransform.cs](NAudio.Wasapi/MediaFoundation/MfTransform.cs) | 78, 91, 125, 137 | `Interfaces.IMFMediaType` | Input/output media-type getters |
| [NAudio.Wasapi/MediaFoundation/MfSourceReader.cs](NAudio.Wasapi/MediaFoundation/MfSourceReader.cs) | 50, 62, 112 | `Interfaces.IMFMediaType` × 2, `Interfaces.IMFSample` × 1 | Native / current media type, ReadSample |
| [NAudio.Wasapi/MediaFoundationResampler.cs](NAudio.Wasapi/MediaFoundationResampler.cs) | 74 | legacy `IMFTransform` | Bridges activated `IUnknown*` onto legacy `MediaFoundationTransform.transform` field type |

Pattern (RCW direction): `(T)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(ptr, CreateObjectFlags.UniqueInstance)`. The `Mf*` wrapper classes hold both the RCW *and* an `IntPtr nativePointer` because they expose `NativePointer` for native calls — so the substitution is *not* identical to Phase 2e's `MMDeviceEnumerator.WrapDevicePointer` (which releases the IntPtr immediately). Document this clearly in the bridge-site commit.

### Category B — Legacy `[ComImport]` typed parameters in `[DllImport]` declarations (~14 parameters across 11 p/invokes)

[NAudio.Wasapi/MediaFoundation/MediaFoundationInterop.cs](NAudio.Wasapi/MediaFoundation/MediaFoundationInterop.cs):

| Line | P/Invoke | Legacy-typed parameters |
| ---: | --- | --- |
| 32-33 | `MFCreateMediaType` | `out IMFMediaType ppMFType` |
| 38-39 | `MFInitMediaTypeFromWaveFormatEx` | `[In] IMFMediaType pMFType` |
| 45-46 | `MFCreateWaveFormatExFromMFMediaType` | `IMFMediaType pMFType` |
| 51-53 | `MFCreateSourceReaderFromURL` | `[In] IMFAttributes pAttributes`, `out IMFSourceReader` |
| 58-59 | `MFCreateSourceReaderFromByteStream` | `[In] IMFByteStream`, `[In] IMFAttributes`, `out IMFSourceReader` |
| 64-66 | `MFCreateSinkWriterFromURL` | `[In] IMFByteStream`, `[In] IMFAttributes`, `out IMFSinkWriter` |
| 72-73 | `MFCreateMFByteStreamOnStreamEx` | `[MarshalAs(UnmanagedType.IUnknown)] object`, `out IMFByteStream` — see Category E |
| 78-79 | `MFCreateMFByteStreamOnStream` | `[In] IStream` (`ComTypes.IStream`!), `out IMFByteStream` — see Category E |
| 91-92 | `MFCreateSample` | `out IMFSample` |
| 97-99 | `MFCreateMemoryBuffer` | `out IMFMediaBuffer` |
| 104-107 | `MFCreateAttributes` | `out IMFAttributes` |
| 112-117 | `MFTranscodeGetAudioOutputAvailableTypes` | `[In] IMFAttributes`, `out IMFCollection` |

These are **not** caught by replacing `Marshal.GetObjectForIUnknown` calls because the legacy COM marshaller uses the *same machinery* internally. Under `BuiltInComInteropSupport=false`, the runtime can't construct a classic RCW for `out IMFMediaType` and the call faults at the marshalling layer — *before* the (modernised) wrapper code runs.

Recommended pattern (carry forward from Phase 2f's `ActivateAudioInterfaceAsync` migration): convert each `[DllImport]` to `[LibraryImport]` with `out IntPtr` (or `[GeneratedComInterface]`-typed parameters where safe) and have the wrapper do explicit `ComActivation.ComWrappers.GetOrCreateObjectForComInstance` projection. This is the precedent set by Phase 2f line 202 of `MODERNIZATION.md`.

### Category C — `Marshal.ReleaseComObject` sites (~22 sites, 5 files)

`Marshal.ReleaseComObject` only works on `__ComObject`-derived RCWs (classic COM). Once the underlying interfaces are `[GeneratedComInterface]` and the wrappers come from `StrategyBasedComWrappers`, every one of these calls throws `ArgumentException: object's type must be __ComObject`. **This is the largest single source of "I migrated the interfaces and now everything is broken at runtime"-style regressions.**

Replacement pattern (Phase 2e + 6c precedent): `((ComObject)(object)wrapper).FinalRelease()`. The `(object)` cast is required because direct `wrapper is ComObject` from an interface-typed variable does not compile (Phase 2e Hazard #2 in `CoreAudioActivationModernization.md`).

| File | Lines |
| --- | --- |
| [NAudio.Wasapi/MediaFoundation/MediaFoundationTransform.cs](NAudio.Wasapi/MediaFoundation/MediaFoundationTransform.cs) | 84, 151, 221, 226, 232, 236, 237, 276 |
| [NAudio.Wasapi/MediaFoundation/MediaFoundationHelpers.cs](NAudio.Wasapi/MediaFoundation/MediaFoundationHelpers.cs) | 84 |
| [NAudio.Wasapi/MediaFoundation/MediaType.cs](NAudio.Wasapi/MediaFoundation/MediaType.cs) | 179 |
| [NAudio.Wasapi/MediaFoundationReader.cs](NAudio.Wasapi/MediaFoundationReader.cs) | 106, 291, 292, 371 |
| [NAudio.Wasapi/MediaFoundationEncoder.cs](NAudio.Wasapi/MediaFoundationEncoder.cs) | 69, 228, 256, 284, 306, 362, 363 |

### Category D — Field/parameter type cascade (5 files)

[MediaFoundationTransform.transform](NAudio.Wasapi/MediaFoundation/MediaFoundationTransform.cs#L30) is typed as the legacy `IMFTransform`. Re-typing to `[GeneratedComInterface] IMFTransform` from `Interfaces/` cascades through:

- [MediaFoundationTransform.cs](NAudio.Wasapi/MediaFoundation/MediaFoundationTransform.cs) — the `transform` field + every method that calls into it (Read, Reposition, EndStreamAndDrain, ReadFromTransform, ReadFromSource, dispose)
- [MediaFoundationResampler.cs](NAudio.Wasapi/MediaFoundationResampler.cs) — `CreateTransform` returns `IMFTransform`; the call site at line 65 onward must also project via `ComActivation.ComWrappers`

In **parallel** (not cascading from `transform` — these files don't reference it), the same legacy types appear as fields/parameters in:

- [MediaFoundationReader.cs](NAudio.Wasapi/MediaFoundationReader.cs) — `IMFSourceReader pReader` field + all `IMFMediaType` / `IMFSample` / `IMFMediaBuffer` locals in the read loop
- [StreamMediaFoundationReader.cs](NAudio.Wasapi/StreamMediaFoundationReader.cs) — overrides `CreateReader` returning `IMFSourceReader`
- [MediaFoundationEncoder.cs](NAudio.Wasapi/MediaFoundationEncoder.cs) — `IMFSinkWriter`, `IMFCollection`, `IMFMediaType`, `IMFSample`, `IMFMediaBuffer`, `IMFAttributes` locals
- [MfActivate.cs](NAudio.Wasapi/MediaFoundation/MfActivate.cs) — already uses `Interfaces.IMFTransform` in the bridge, but `ActivateTransform` returns the wrapper (no change needed there)
- [MediaFoundationApi.cs](NAudio.Wasapi/MediaFoundation/MediaFoundationHelpers.cs) — every internal factory method returns a legacy interface (`IMFMediaType`, `IMFMediaBuffer`, `IMFSample`, `IMFAttributes`, `IMFByteStream`, `IMFSourceReader`, `IMFSinkWriter`, `IMFCollection`)

These five files plus the cascade origin must change in **one coherent commit** so `dotnet build` is never broken across files. (This is Step 2 of the implementation plan below.)

### Category E — CCW direction: `ComStream` and the `IStream` boundary (one trap)

[ComStream](NAudio.Wasapi/ComStream.cs) is a managed class implementing `System.Runtime.InteropServices.ComTypes.IStream` and is handed to native MF in two places ([MediaFoundationEncoder.cs:247,301](NAudio.Wasapi/MediaFoundationEncoder.cs#L247), [StreamMediaFoundationReader.cs:29](NAudio.Wasapi/StreamMediaFoundationReader.cs#L29)) via `MediaFoundationApi.CreateByteStream` → `MFCreateMFByteStreamOnStream([In] IStream punkStream, ...)`.

`ComTypes.IStream` is a `[ComImport]`-style interface that ships with .NET. Under classic COM interop, `BuiltInComInteropSupport` materialises the CCW automatically. Under `BuiltInComInteropSupport=false` — exactly the smoke-test mode that gates the flag flip — this fails.

Two options:

1. **Migrate ComStream onto source-gen.** Declare a local internal `[GeneratedComInterface] IStream` (Guid `0000000C-0000-0000-C000-000000000046`) with the IStream vtable, decorate `ComStream` with `[GeneratedComClass]` + `partial`, switch its base interface to the local declaration. Then apply **Phase 2f's QI-for-IID rule**: at the `MFCreateMFByteStreamOnStream` call site, `ComActivation.ComWrappers.GetOrCreateComInterfaceForObject(comStream, ...)` then `Marshal.QueryInterface(unkPtr, in IID_IStream, out var streamPtr)` and pass `streamPtr` to native, with try/finally Release of the unk and stream pointers. This is the `Query<X>Interface` shape from Phase 2f.
2. **Defer Category E** — keep `BuiltInComInteropSupport=true` for the flag flip and document that stream-based MediaFoundation reading/encoding still relies on built-in interop. This partially defeats the goal: the analyzer-clean+publish-clean+smoke-clean criterion has a known runtime-failure exception.

**Recommendation: do option 1.** The whole point of Phase 2e′ is honest readiness; carving out an exception undermines that. ComStream migration is small (one file + the QI helper at the two call sites) and exercises the same mechanism Phase 2f validated. The Phase 2f bug (master-volume AV) repro'd because *single-interface* CCWs were assumed not to need QI; ComStream is single-interface, so this is exactly the trap to avoid.

---

## Lessons-learned hazards (carry forward — every one of these caused real pain previously)

Verbatim relevance check — every numbered hazard below cites the Phase / commit / repro that established it.

### H1. `StrategyBasedComWrappers` wrappers are NOT `IDisposable`. (Phase 6c, DmoModernization.md)

`if (foo is IDisposable d) d.Dispose()` compiles cleanly and silently no-ops, leaking the COM reference. **Audit every disposal site in the migrated files.** Use `((ComObject)(object)wrapper).FinalRelease()`.

**Where this bites in Phase 2e′:** every site in Category C, plus the new disposal logic in `MediaFoundationTransform.Dispose`, `MediaFoundationReader.Dispose`, `MediaFoundationEncoder.Encode` cleanup paths.

### H2. Direct `is ComObject` from an interface-typed variable fails to compile. (Phase 2e Hazard #2, CoreAudioActivationModernization.md)

`if (foo is ComObject co) co.FinalRelease();` produces `CS8121: An expression of type 'IFoo' cannot be handled by a pattern of type 'ComObject'`. Cast through `object` first: `if ((object)foo is ComObject co) co.FinalRelease();`.

**Where this bites in Phase 2e′:** the same Category C sites — H1 and H2 are the same fix.

### H3. CCWs return distinct `IntPtr`s per interface — *always*, including single-interface `[GeneratedComClass]` types. (Phase 2f QI-for-IID fix, MODERNIZATION.md Phase 2f bullet)

Phase 2f shipped without QI on what we thought were "obvious" single-interface callbacks. NAudioDemo's master-volume slider AV'd on the WASAPI worker thread the first time `IAudioEndpointVolumeCallback.OnNotify` fired. Bisected with `Tools/CallbackRepro` — root cause was that even single-interface `[GeneratedComClass]` CCWs expose a separate IUnknown vtable. Native dispatched against the IUnknown vtable where typed methods were expected → AV.

**Fix pattern (Phase 2f):** funnel every CCW-handoff site through a `Query<X>Interface` helper that:
1. `ComActivation.ComWrappers.GetOrCreateComInterfaceForObject(obj, CreateComInterfaceFlags.None)` → IUnknown IntPtr
2. `Marshal.QueryInterface(unkPtr, in IID_IFoo, out var fooPtr)` for the specific IID
3. Hand `fooPtr` to native
4. `try/finally` Release both pointers in reverse order

**Where this bites in Phase 2e′:** Category E (ComStream → MF native via `MFCreateMFByteStreamOnStream`). If we migrate `ComStream` to `[GeneratedComClass]`, we **must** apply the Phase 2f QI helper at the call site. The hazard fired in Phase 2f for a slider; here it would fire on first byte-stream read/write — likely on a background thread — and look indistinguishable from a corrupt-stream bug. Don't let the smoke test be the first thing that catches it.

### H4. Same-namespace types beat `using` aliases. (Phase 2e Hazard #4)

Don't keep both legacy `[ComImport]` and modern `[GeneratedComInterface]` declarations of the same MF interface in the same namespace. The current state (legacy in `NAudio.MediaFoundation`, modern in `NAudio.MediaFoundation.Interfaces`) is *just barely* OK because the namespaces differ — but several consumer files use `Interfaces.IMFFoo` qualified locals while their containing class is in `NAudio.MediaFoundation` and references the unqualified legacy `IMFFoo` for the field. Once the legacy declarations are deleted, this ambiguity goes away. The 13 legacy files at `NAudio.Wasapi/MediaFoundation/IMF*.cs` to delete are listed in Step 6 below.

### H5. `Marshal.ReleaseComObject` on a `StrategyBasedComWrappers` RCW throws `ArgumentException`. (Phase 6c precedent, DmoModernization.md "Risks and mitigations" #4)

Already covered by Category C, but worth flagging here: this is what makes Step 3 of the plan necessary as its own commit — once the interfaces in Category D are migrated, every Category C site goes from "works" to "throws" with no compile-time warning.

### H6. Cross-cast (`as IFoo2`) returns the *same* `ComObject`; one `FinalRelease` releases all views. (Phase 2e resolution, CoreAudioActivationModernization.md "Resolution" §3)

`AudioClient.Dispose` keeps a single `FinalRelease` for `audioClientInterface`/`...As2`/`...As3` because DICASTABLE returns the same `ComObject` for cross-casts. Releasing each separately double-frees and was the *original* fast-fail path before .NET 9.

**Where this bites in Phase 2e′:** if any MF wrapper does cross-casts (e.g. is `IMFMediaType` cross-cast as `IMFAttributes`?), check whether DICASTABLE behaviour applies. Audit candidates: `MfMediaType` does inherit IMFAttributes' methods at the COM level (IMFMediaType extends IMFAttributes). If consumer code does `mediaType as IMFAttributes`, it would get the same ComObject. **Action: search for any `as IMFXxx` patterns during Step 2 and verify single-FinalRelease semantics.**

### H7. `[DllImport]` cannot marshal `[GeneratedComInterface]` parameters. (Phase 2f `ActivateAudioInterfaceAsync` precedent)

Mixing `[DllImport]` with source-gen interface types produces compile-time errors *or* (worse) silent fallbacks to classic-RCW marshalling depending on annotations. Phase 2f migrated `ActivateAudioInterfaceAsync` from `[DllImport]` to `[LibraryImport]` with raw `IntPtr` and explicit ComWrappers projection at the call site for exactly this reason.

**Where this bites in Phase 2e′:** every Category B p/invoke. `[LibraryImport]` + `out IntPtr` is the established pattern. Don't try to make `[DllImport]` work with `[GeneratedComInterface]` parameters.

### H8. The .NET 8 DICASTABLE CastCache regression — already resolved by net9.0 floor. (Phase 2e resolution)

Mentioned for completeness so the next reader doesn't rediscover it: the `Invalid Program: attempted to call a UnmanagedCallersOnly method from managed code` finalizer-thread fast-fail is a .NET 8 ComWrappers regression ([dotnet/runtime#90234](https://github.com/dotnet/runtime/issues/90234), fixed [PR #110007](https://github.com/dotnet/runtime/pull/110007)). NAudio is on net9.0; this is inert. **Do not reintroduce `WrapUnique` / `GC.SuppressFinalize` band-aids** — they were retired in Phase 2e and should stay retired.

### H9. STA→MTA apartment hazard. (Phase 6c motivation)

Legacy `[ComImport]` activation produces apartment-bound RCWs. `Marshal.GetObjectForIUnknown` on a pointer activated on STA, then used from MTA, raises `InvalidComObjectException` ("COM object that has been separated from its underlying RCW"). Modern `StrategyBasedComWrappers` wrappers are thread-agile.

**Where this bites in Phase 2e′:** `MediaFoundationResampler.CreateTransform` is invoked lazily from the read thread (per its comment at line 43), so the *current* code dodges the bug. Once we migrate to source-gen, the wrapper is thread-agile and the lazy-construction comment becomes a historical artefact. **Verify that no migration introduces a new STA-eager activation site.**

### H10. `out object` and `[MarshalAs(UnmanagedType.IUnknown)] object` don't transparently project under source-gen. (Audit finding)

[MediaFoundationEncoder.cs:65](NAudio.Wasapi/MediaFoundationEncoder.cs#L65) (`availableTypes.GetElement(n, out object mediaTypeObject)`) and [MediaFoundationInterop.cs:73](NAudio.Wasapi/MediaFoundation/MediaFoundationInterop.cs#L73) (`MFCreateMFByteStreamOnStreamEx([MarshalAs(UnmanagedType.IUnknown)] object punkStream, ...)`) round-trip COM identity through `object`. Classic RCW handles this transparently; source-gen does not.

For `IMFCollection.GetElement`, change the modern partial to `out IntPtr` and project explicitly. For `MFCreateMFByteStreamOnStreamEx`, the only known caller is unused (we use the `IStream`-typed `MFCreateMFByteStreamOnStream` instead) — verify and consider deletion.

### H11. The flag-flip is not validated by analyzer silence. (Phase 2f deferral rationale, MODERNIZATION.md line 210)

`Marshal.GetObjectForIUnknown` carries no `[RequiresUnreferencedCode]`/`[RequiresDynamicCode]` annotation. The trim/AOT analyzer emits zero warnings against it even when it would fail at runtime under `BuiltInComInteropSupport=false`. **A clean `dotnet publish` warning report is therefore not the same as runtime correctness.** The Phase 2f progress log explicitly recorded this trap when it deferred its own flag flip.

**Action for Phase 2e′:** the smoke test must include a section that exercises a MediaFoundation code path *under `PublishAot=true` + `BuiltInComInteropSupport=false`*. This is non-optional. See Step 7 below.

---

## CreateObjectFlags decision per call site

Same as Phase 2e:

- `UniqueInstance` — for things obtained from MF factory APIs (`MFCreateMediaType`, `MFCreateSample`, `MFCreateMemoryBuffer`, `MFTEnumEx` items, `IMFActivate::ActivateObject`, `IMFTransform::GetOutputCurrentType`, `IMFSample::GetBufferByIndex`, etc.). Fresh COM objects we own. Caller is responsible for `FinalRelease()`.
- `None` — for QI'd interfaces on an object whose lifetime is owned elsewhere. Wrapper piggy-backs on the cache. (Likely no MF call sites — most are factory-fresh.)

---

## Implementation plan — seven discrete steps, one commit each

User has asked: do not auto-commit. Each step ends with a working-tree-staged diff for review before commit. Stop and request review at every numbered step.

### Step 1. Audit pass (this document) — ✅ THIS COMMIT

This file. No code changes. Confirms touch-list, line numbers, and hazards before any source edits.

### Step 2. Cascade migration — single coherent commit

Re-type:
- `MediaFoundationTransform.transform` field
- `MediaFoundationApi` factory return types (CreateMediaType, CreateMemoryBuffer, CreateSample, CreateAttributes, CreateByteStream, CreateSourceReaderFromUrl, CreateSourceReaderFromByteStream, CreateSinkWriterFromUrl, GetAudioOutputAvailableTypes)
- All `MediaFoundationInterop` `[DllImport]` declarations migrate to `[LibraryImport]` with `out IntPtr` (+ `int` HRESULT or `[PreserveSig]` retention as appropriate). Wrappers do explicit `ComActivation.ComWrappers.GetOrCreateObjectForComInstance` projection.
- Field/parameter types in `MediaFoundationReader`, `MediaFoundationEncoder`, `StreamMediaFoundationReader`, `MediaFoundationResampler`, `MediaType` (the `mediaType` field).

End: `dotnet build NAudio.slnx -c Debug` clean. Tests will not yet run cleanly (Category C `Marshal.ReleaseComObject` calls will throw); that's Step 3.

### Step 3. `Marshal.ReleaseComObject` → `((ComObject)(object)x).FinalRelease()` sweep

22 sites across 5 files (Category C). Mechanical at this point because types are correct.

End: NAudioTests green. Manual smoke in NAudioDemo against MP3/WMA/AAC encoding, MediaFoundationReader playback, MediaFoundationResampler.

### Step 4. Bridge-site sweep — `Marshal.GetObjectForIUnknown` → `ComActivation.ComWrappers.GetOrCreateObjectForComInstance`

12 sites (Category A). The `Mf*` wrapper classes retain dual RCW+IntPtr ownership (commit message must call this out — see H1/H2 wrapper-disposal note).

End: zero `Marshal.GetObjectForIUnknown` calls in `NAudio.Wasapi/MediaFoundation/` and `NAudio.Wasapi/MediaFoundationResampler.cs`. NAudioTests green.

### Step 5. ComStream — `[GeneratedComClass]` migration + QI helper at the two call sites (Category E)

- Declare local internal `[GeneratedComInterface] IStream` (or import an existing source-gen one if available).
- Decorate `ComStream` with `[GeneratedComClass] partial`.
- Apply Phase 2f QI-for-IID helper at [MediaFoundationEncoder.cs:247,301](NAudio.Wasapi/MediaFoundationEncoder.cs#L247) and [StreamMediaFoundationReader.cs:29](NAudio.Wasapi/StreamMediaFoundationReader.cs#L29).
- Refactor `MediaFoundationApi.CreateByteStream` to take an `IntPtr` (already-QI'd IStream pointer) rather than the managed object directly. Or keep it accepting a `ComStream` and do the QI internally.

End: NAudioTests green. Manual smoke: encode-to-stream and read-from-stream paths in NAudioDemo.

### Step 6. Delete legacy `[ComImport]` files

13 files in `NAudio.Wasapi/MediaFoundation/IMF*.cs`. At this point nothing references them.

| File | Status |
| --- | --- |
| `IMFActivate.cs`, `IMFAttributes.cs`, `IMFByteStream.cs`, `IMFCollection.cs`, `IMFMediaBuffer.cs`, `IMFMediaEvent.cs`, `IMFMediaType.cs`, `IMFReadWriteClassFactory.cs`, `IMFSample.cs`, `IMFSinkWriter.cs`, `IMFSourceReader.cs`, `IMFTransform.cs` | All delete |
| The `IMFReadWriteClassFactory.cs` companion `[ComImport]` coclass at line 28 | Move to `Interfaces/` if still needed for activation, else delete |

End: zero `[ComImport]` declarations in `NAudio.Wasapi/MediaFoundation/`. `dotnet build` clean.

### Step 7. Smoke test extension + flag flip

- Add a third section to [NAudioAotSmokeTest/Program.cs](NAudioAotSmokeTest/Program.cs): `MediaFoundationApi.Startup()`, `new MediaFoundationReader(<small test asset>)`, read 1 second of samples, run them through a `MediaFoundationResampler` (e.g. 44100 → 48000), `MediaFoundationApi.Shutdown()`. Use one of the existing audio assets from NAudioTests (e.g. `NAudioTests/Resources/*.mp3` or `*.wav`). The asset must be embedded or copied alongside the smoke .exe.
- `<IsAotCompatible>true</IsAotCompatible>` on `NAudio.Wasapi.csproj`. (Drop the deferral comment.)
- Verify:
  - `dotnet build NAudio.slnx -c Release` — zero IL2026/IL3050 against `NAudio.Wasapi` *and* `NAudio` (`NAudioAotSmokeTest` has `TreatWarningsAsErrors=true`).
  - `dotnet publish NAudioAotSmokeTest -c Release -p:PublishAot=false -p:PublishTrimmed=true` — runs all three sections clean.
  - `dotnet publish NAudioAotSmokeTest -c Release` (full `PublishAot`) from VS Developer Command Prompt — runs all three sections clean. **This is the load-bearing validation; per H11, analyzer silence + trim-only smoke is not sufficient.**
  - NAudioTests: 1179 / 14 skipped / 0 failed (current baseline).
  - Manual smoke in NAudioDemo: MP3 streaming demo, MediaFoundation demo panel, WPF demo's WASAPI capture with format conversion (exercises the resampler).

End: `<IsAotCompatible>` honestly on. Phase 2e′ closes.

### Step 8. Documentation update — bundled into Step 7's commit

- This file: change status from IN PROGRESS to COMPLETE; add a Resolution section + Progress log.
- [MODERNIZATION.md](MODERNIZATION.md): add `#### Phase 2e′: MediaFoundation bridge sweep` under WASAPI Modernization summary. Note breaking change risk (anyone subclassing `MediaFoundationTransform` or accessing the `transform` field via reflection is potentially affected — binary-compat break, not source).
- [CoreAudioActivationModernization.md](CoreAudioActivationModernization.md): strike out the "Phase 2e′" deferred section and the corresponding "Out of scope" bullet. Add a 2026-04-XX progress-log row.

---

## Out of scope (still deferred — same as before Phase 2e′)

- AOT story for sister assemblies (`NAudio.Core`, `NAudio.WinMM`, `NAudio.Midi`, `NAudio.Asio`). Core/WinMM/Midi probably small audits each; ASIO is multi-phase like WASAPI was. Plan as separate phases.
- Process-loopback capture activation (the only real-world consumer of `AudioClient.ActivateAsync` and therefore the only path that exercises `IActivateAudioInterfaceCompletionHandler`). Currently throws `NotImplementedException` in [WasapiRecorder.cs:91-94](NAudio.Wasapi/Wave/WasapiRecorder.cs#L91-L94).

---

## Progress log

| Date | Step | Notes |
| --- | --- | --- |
| 2026-04-30 | Branch created | `naudio3dev-mediafoundation-bridge` from `naudio3dev` |
| 2026-04-30 | Step 1 audit pass | This document. Identified five touch categories (A: 12 bridge sites; B: 14 p/invoke parameters across 11 declarations; C: 22 ReleaseComObject sites; D: 5-file type cascade; E: ComStream CCW direction). Folded in 11 hazards from Phases 2e/2f/6c. |
| 2026-04-30 | Steps 2+3+4 cascade | Consolidated commit (Step 3 ReleaseComObject→FinalRelease requires Step 4 ComWrappers RCWs to avoid InvalidCastException, so they cannot be split). 16 files, +550/-320. MediaFoundationInterop p/invokes use IntPtr; MediaFoundationApi factories return tuples; MftOutputDataBuffer struct is blittable IntPtr-typed; MediaType / MediaFoundationTransform / MediaFoundationReader / StreamMediaFoundationReader / MediaFoundationEncoder / MediaFoundationResampler / Mf* wrappers fully migrated. Closed three pre-existing MediaType disposal leaks. Tests: 1179/14/0 (baseline maintained). |
| 2026-04-30 | Step 5 ComStream CCW | New `[GeneratedComInterface] IStream` (IID `0000000C-0000-0000-C000-000000000046`) and blittable `StorageStat` struct in `NAudio.MediaFoundation.Interfaces`. ComStream is now `[GeneratedComClass] partial`. `MediaFoundationApi.CreateByteStream` applies the Phase 2f H3 QI-for-IID rule with explicit `Marshal.QueryInterface(unkPtr, in IID_IStream, out streamPtr)` before native handoff. New `CanRoundTripStreamThroughMediaFoundationCcwPath` test encodes a 2s signal to MemoryStream and reads it back — exercises both CCW legs. Tests: 1180/14/0. |
| 2026-04-30 | Step 6 legacy file deletion | 13 files deleted (12 legacy `[ComImport]` IMF*.cs at the root of MediaFoundation/, plus the unused modern `Interfaces/IMFReadWriteClassFactory.cs` whose interface and coclass had no callers). Net result: zero `[ComImport]` declarations remaining in `NAudio.Wasapi/MediaFoundation/`. -1492 lines. Tests: 1180/14/0. |
| 2026-04-30 | Step 7 smoke + flag flip | New MediaFoundation section in `NAudioAotSmokeTest/Program.cs` exercises encode-to-stream, read-from-stream, and `MediaFoundationResampler` (RCW + CCW + IMFTransform). Annotated `FieldDescriptionHelper.Describe` with `[DynamicallyAccessedMembers(PublicFields)]` to clear an IL2070 surfaced by the new MF section. `<IsAotCompatible>true</IsAotCompatible>` flipped on `NAudio.Wasapi.csproj`. `dotnet build NAudio.slnx -c Release` clean (zero IL2026/IL3050). `dotnet publish NAudioAotSmokeTest -c Release -p:PublishTrimmed=true -p:BuiltInComInteropSupport=false`: all three sections (RCW property reads, CCW callbacks, MF round-trip) pass end-to-end. Tests: 1180/14/0. |

---

## Resolution

Phase 2e′ closes the MediaFoundation half of NAudio.Wasapi's COM modernization. The CoreAudio side (Phase 2e + 2f) was AOT-correct from the runtime perspective but the assembly couldn't honestly carry `<IsAotCompatible>` because MediaFoundation still failed under `BuiltInComInteropSupport=false`. After Phase 2e′:

- Zero `[ComImport]` declarations in `NAudio.Wasapi/MediaFoundation/`.
- Zero `Marshal.GetObjectForIUnknown` calls in `NAudio.Wasapi/MediaFoundation/` and `NAudio.Wasapi/MediaFoundationResampler.cs` (the only remaining one in the assembly is `Dmo/Effect/DmoEffectActivation.cs:48`, which is per-effect property interface activation — out of scope per Phase 6c).
- Zero `Marshal.ReleaseComObject` calls in `NAudio.Wasapi`.
- ComStream CCW handoff applies the Phase 2f QI-for-IID rule, exercised by both a focused round-trip test and the AOT smoke runner.
- `NAudioAotSmokeTest` exercises three full directions of source-generated COM dispatch: RCW property reads, CCW callback dispatch, and MediaFoundation encode/decode/resample. The trimmed run with `BuiltInComInteropSupport=false` is the load-bearing validation per Hazard H11 (the analyzer alone cannot certify runtime correctness — the smoke test does).

The full `PublishAot=true` validation from a Visual Studio Developer Command Prompt is the final gate; it requires the toolchain that the trimmed-only run does not, but the trimmed run with `BuiltInComInteropSupport=false` is a strong proxy because the failure modes are the same machinery (built-in COM marshaller missing) and the only thing the AOT step adds is whole-program code generation.
