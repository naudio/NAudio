# NAudio.Wasapi DMO Modernization

This document captures the design, motivation, and verification plan for finishing the modernization of the `NAudio.Wasapi/Dmo` folder so that it matches the rest of NAudio.Wasapi (CoreAudioApi, MediaFoundation). It is a sub-plan of the broader [MODERNIZATION.md](MODERNIZATION.md).

---

## Current state

The DMO folder is part-way through a migration from legacy `[ComImport]` interop to source-generated `[GeneratedComInterface]` dispatch. The interface declarations exist in both forms, but **no consuming code uses the modern declarations** — they sit as unused parallel files.

### Parallel declarations

| Legacy (`NAudio.Dmo`) | Modern (`NAudio.Dmo.Interfaces`) | Consumed by |
| --- | --- | --- |
| [IMediaObject.cs](NAudio.Wasapi/Dmo/IMediaObject.cs) `[ComImport]` | [Interfaces/IMediaObject.cs](NAudio.Wasapi/Dmo/Interfaces/IMediaObject.cs) `[GeneratedComInterface]` | Legacy only |
| [IMediaObjectInPlace.cs](NAudio.Wasapi/Dmo/IMediaObjectInPlace.cs) `[ComImport]` | [Interfaces/IMediaObjectInPlace.cs](NAudio.Wasapi/Dmo/Interfaces/IMediaObjectInPlace.cs) `[GeneratedComInterface]` | Legacy only |
| [IWMResamplerProps.cs](NAudio.Wasapi/Dmo/IWMResamplerProps.cs) `[ComImport]` | [Interfaces/IWMResamplerProps.cs](NAudio.Wasapi/Dmo/Interfaces/IWMResamplerProps.cs) `[GeneratedComInterface]` | Legacy only (also stored-but-never-called in `DmoResampler`) |
| `IEnumDmo`, `IMediaParamInfo` | modern partials | Legacy only |
| `IPropertyStore` ([CoreAudioApi/Interfaces/IPropertyStore.cs](NAudio.Wasapi/CoreAudioApi/Interfaces/IPropertyStore.cs)) `[ComImport]` | *(no modern declaration — see "IPropertyStore is deliberately out of scope" below)* | `PropertyStore.cs` (live, used by `MMDevice.Properties`); also stored-but-never-called as a dead field in `WindowsMediaMp3Decoder` and `DmoResampler` |

### Activation pattern

Every DMO consumer activates its COM object via `new SomeMediaComObject()` against a `[ComImport]` coclass — the legacy CoCreateInstance path that produces a runtime callable wrapper bound to the calling thread's apartment.

Affected consumers:

- [DmoResampler](NAudio.Wasapi/Dmo/ResamplerMediaObject.cs) — wrapped by [ResamplerDmoStream](NAudio.Wasapi/ResamplerDmoStream.cs) and used by [MediaFoundationResampler](NAudio.Wasapi/MediaFoundationResampler.cs) for its existence probe.
- [WindowsMediaMp3Decoder](NAudio.Wasapi/Dmo/WindowsMediaMp3Decoder.cs) — wrapped by [DmoMp3FrameDecompressor](NAudio.Wasapi/DmoMp3FrameDecompressor.cs).
- All nine effect classes in [NAudio.Wasapi/Dmo/Effect/](NAudio.Wasapi/Dmo/Effect/) (Echo, Chorus, Flanger, Compressor, Distortion, Gargle, ParamEq, WavesReverb, I3DL2Reverb).

### Observable consequences

1. **STA→MTA threading bug.** A `DmoResampler`, `ResamplerDmoStream`, `WindowsMediaMp3Decoder`, or `DmoMp3FrameDecompressor` constructed on an STA thread (WinForms / WPF UI thread, or any code that has been `[STAThread]`-marked for ASIO) cannot subsequently be used from an MTA audio thread. The legacy RCW raises `InvalidComObjectException` ("COM object that has been separated from its underlying RCW") once the STA exits, and even before that, cross-apartment QueryInterface fails with `E_NOINTERFACE` because the resampler DMO and MP3 decoder DMO ship without proxy/stub registration. This is reproduced by [`Experimental_CanCreateOnStaThreadAndUseOnMtaThread`](NAudioTests/Dmo/ResamplerDmoTests.cs) in the resampler fixture (currently failing as expected).
2. **NativeAOT / trimming.** The rest of NAudio.Wasapi is moving toward AOT-friendliness via `[GeneratedComInterface]` (no reflection-based marshalling, full trimming support). The DMO folder is the last legacy-COM corner blocking a clean AOT story.
3. **Inconsistent lifetime model.** [ResamplerMediaObject.cs](NAudio.Wasapi/Dmo/ResamplerMediaObject.cs) still carries the comment *"Dispose code - experimental at the moment - was added trying to track down why Resampler crashes NUnit"* — a symptom of the nondeterministic legacy-RCW lifetime that the WASAPI modernization has been replacing with explicit release.

---

## Goals

- Bring the DMO folder onto the same dispatch and lifetime model as CoreAudioApi and MediaFoundation: `[GeneratedComInterface]` for interfaces, source-generated function-pointer dispatch, deterministic `Release`.
- Make `DmoResampler`, `ResamplerDmoStream`, `WindowsMediaMp3Decoder`, and `DmoMp3FrameDecompressor` safe to construct on any thread and use from any thread, in particular the STA→MTA handoff that NAudio's typical WinForms/WPF/ASIO usage produces.
- Delete the parallel legacy `[ComImport]` interface declarations once consumers have moved over, leaving a single set of declarations under `NAudio.Dmo.Interfaces`.
- Keep the existing public API of `DmoResampler`, `ResamplerDmoStream`, `WindowsMediaMp3Decoder`, `DmoMp3FrameDecompressor`, and `MediaObject` source-compatible. Internal types (`IMediaObject`, etc.) are already `internal`, so their signatures may change freely.

---

## Scope

### In scope

- `IMediaObject` — the central dispatch surface for every DMO. Switch all consumers to the modern partial.
- `IMediaObjectInPlace` — used by all the effect classes via `MediaObjectInPlace`.
- Activation: replace `new SomeComImportCoclass()` with raw `CoCreateInstance` (P/Invoke) followed by `StrategyBasedComWrappers.GetOrCreateObjectForComInstance`, then cast to the modern interface. This is what makes dispatch direct-vtable and avoids apartment marshalling entirely.
- `MediaObject` and `MediaObjectInPlace` wrapper classes: rewrite their internal method bodies to handle the modern interface signatures (which take `IntPtr` rather than typed structs / arrays — see "Interface signature shape change" below).
- Remove the dead `propertyStoreInterface` fields in `WindowsMediaMp3Decoder` and `DmoResampler` (see "IPropertyStore is deliberately out of scope" below) — these fields are assigned in the constructor and disposed, but no method on `IPropertyStore` is ever called through them, so they can simply be deleted.

### Out of scope (for this phase)

- **`IPropertyStore`.** See dedicated section below.
- Per-effect property interfaces (`IDirectSoundFXEcho`, `IDirectSoundFXChorus`, etc.). These are rarely used in real NAudio applications and don't suffer the threading bug at the same severity (effects are generally created and consumed on the same thread). Defer until a user reports a need.
- `IEnumDmo` / `IMediaParamInfo` — internal utilities; migrate opportunistically if the modernization touches them, otherwise leave.
- Adding new DMO surface area (extra effects, IMFTransform on the resampler, etc.).

### IPropertyStore is deliberately out of scope

`IPropertyStore` is **not** migrated as part of this work. Three reasons:

1. **`PropVariant` blocks source-generated marshalling.** Per the existing decision recorded in [MODERNIZATION.md](MODERNIZATION.md) ("`IPropertyStore` — Uses `PropVariant` with `[StructLayout(LayoutKind.Explicit)]` — not compatible with source-generated marshaling"), the runtime's source-generated COM marshaller cannot blit `PropVariant` cleanly. Any `[GeneratedComInterface]` rewrite would have to switch every parameter to `IntPtr` and reimplement `PropVariant` packing / unpacking by hand. This is meaningful work for zero gain on the DMO side (see point 3).
2. **`IPropertyStore` is a shared, live, public surface.** It backs [PropertyStore.cs](NAudio.Wasapi/CoreAudioApi/PropertyStore.cs), which is exposed via `MMDevice.Properties` — the API every NAudio caller uses to read device names, form factors, GUIDs, and arbitrary `PKEY_*` values. Touching `IPropertyStore` risks regressing all WASAPI device-enumeration consumers, not just DMO ones. The blast radius is wrong for a "fix the DMO threading bug" change.
3. **The DMO usage is dead code.** Both [WindowsMediaMp3Decoder](NAudio.Wasapi/Dmo/WindowsMediaMp3Decoder.cs) and [DmoResampler](NAudio.Wasapi/Dmo/ResamplerMediaObject.cs) cast `mediaComObject` to `IPropertyStore` in their constructors and store the result as a private field, but **no method on either field is ever called**. They exist purely so they can be `Marshal.ReleaseComObject`-ed during `Dispose`. Deleting the fields is a behavioural no-op and removes the entire `IPropertyStore` dependency from the DMO modernization.

Net effect: this plan does not change `IPropertyStore`'s declaration, does not change `PropertyStore.cs`, does not change `MMDevice.Properties`, and does not introduce a parallel modern declaration that would diverge from the legacy one. If at some point in the future the rest of NAudio.Wasapi pushes for a full ComWrappers / NativeAOT story, `IPropertyStore` can be tackled then as its own focused workstream.

---

## Interface signature shape change

The modern partials in `NAudio.Dmo.Interfaces` were generated to be ABI-faithful, which means they take `IntPtr` for everything that is passed by reference at the COM level. The legacy declarations took typed structs and `[MarshalAs]`-annotated arrays. Migrating `MediaObject` / `MediaObjectInPlace` therefore involves rewriting wrapper bodies to do the marshalling explicitly:

| Legacy wrapper call | Modern wrapper body |
| --- | --- |
| `mediaObject.GetInputType(idx, n, out DmoMediaType mt)` | Pin a `DmoMediaType` local, pass its address, unpin |
| `mediaObject.SetInputType(idx, ref mt, flags)` | Same — pin address-of-struct |
| `mediaObject.ProcessInput(idx, IMediaBuffer buf, ...)` | Get the `IUnknown*` for `buf` via ComWrappers and pass as `IntPtr` |
| `mediaObject.ProcessOutput(flags, n, DmoOutputDataBuffer[] bufs, out reserved)` | Pin the array, pass `IntPtr` to the first element |
| `mediaObject.GetInputStatus(idx, out DmoInputStatusFlags flags)` | Receive `out int` and cast to the flags enum |

This rewrite is mechanical but careful. Each wrapper method should be tested via the existing integration tests (see "Verification" below).

---

## Implementation plan

Suggested order, each step independently testable:

1. **Migrate `MediaObject` to the modern `IMediaObject`.** Change the field type, change the constructor parameter, rewrite each wrapper method body to do explicit `IntPtr` marshalling for `DmoMediaType`, `IMediaBuffer`, etc.
2. **Migrate `MediaObjectInPlace` similarly.**
3. **Switch `DmoResampler` to direct activation.** Replace `new ResamplerMediaComObject()` with P/Invoke `CoCreateInstance` + `StrategyBasedComWrappers.GetOrCreateObjectForComInstance` + cast to modern `IMediaObject`. Delete the dead `propertyStoreInterface` and `resamplerPropsInterface` fields entirely (they are never read — see "IPropertyStore is deliberately out of scope"). Update `Dispose` to release the ComWrappers `ComObject`.
4. **Switch `WindowsMediaMp3Decoder` to direct activation** with the same pattern. Delete its dead `propertyStoreInterface` field.
5. **Switch the nine `Effect/` classes to direct activation.** Same pattern, applied uniformly.
6. **Update `MediaFoundationResampler`'s constructor probe** to use the new activation path (it currently constructs `new ResamplerMediaComObject()` purely as an existence check, then immediately releases it).
7. **Delete legacy declarations.** Remove [IMediaObject.cs](NAudio.Wasapi/Dmo/IMediaObject.cs), [IMediaObjectInPlace.cs](NAudio.Wasapi/Dmo/IMediaObjectInPlace.cs), [IWMResamplerProps.cs](NAudio.Wasapi/Dmo/IWMResamplerProps.cs), the legacy `IEnumDmo` / `IMediaParamInfo`, and the `[ComImport]` coclass declarations (`ResamplerMediaComObject`, `WindowsMediaMp3DecoderComObject`, and the per-effect coclasses). **Do not touch `IPropertyStore`.**

Steps 1–2 are the heaviest. Steps 3–6 are largely pattern repetition once 1 is done.

---

## Verification

After migration, the following test fixtures must continue to pass with no regressions. They are the gate on shipping the migration.

### Resampler — must still work end-to-end

- [NAudioTests/MediaFoundation/MediaFoundationResamplerTests.cs](NAudioTests/MediaFoundation/MediaFoundationResamplerTests.cs) — every test, including all `ReadResamplesAndPreservesFrequency` cases (six sample-rate pairs), `ReadInSmallChunksEventuallyEndsWithZero`, `RepositionAfterRewindingSourceRepeatsOutput`, `ReadAfterDisposeThrows`, and the `Experimental_*` cross-thread tests.
- [NAudioTests/Dmo/ResamplerDmoTests.cs](NAudioTests/Dmo/ResamplerDmoTests.cs) — every test, including the format-support probes, `ResamplerCanCallProcessInput`, `ResamplerCanCallProcessOutput`, and the `Experimental_*` cross-thread tests.
- [NAudioTests/Dmo/ResamplerDmoStreamTests.cs](NAudioTests/Dmo/ResamplerDmoStreamTests.cs) — every test, including `CanResampleAWholeStreamTo*` for both PCM and IEEE float in both directions.

### MP3 frame decompressor — must still work end-to-end

- [NAudioTests/Dmo/DmoMp3FrameDecompressorTests.cs](NAudioTests/Dmo/DmoMp3FrameDecompressorTests.cs) — `CanCreateDmoMp3FrameDecompressor`, `CanDecompressAnMp3` (decodes a real MP3 file produced by `TestFileBuilder`), `CanExamineInputTypesOnMp3Decoder`, `CanExamineOutputTypesOnDecoder`, `WindowsMediaMp3DecoderSupportsStereoMp3`.
- [NAudioTests/Mp3/Mp3FrameDecompressorDimRoutingTests.cs](NAudioTests/Mp3/Mp3FrameDecompressorDimRoutingTests.cs) — exercises the higher-level routing that picks `DmoMp3FrameDecompressor` for various input formats.

### `IPropertyStore` consumers — must remain unaffected

Although `IPropertyStore` is explicitly out of scope, the migration touches files in the same assembly and uses overlapping `Marshal.GetObjectForIUnknown` / `StrategyBasedComWrappers` machinery, so it is worth confirming the property-store path is genuinely untouched.

- [NAudioTests/Wasapi/MMDeviceEnumeratorTests.cs](NAudioTests/Wasapi/MMDeviceEnumeratorTests.cs) — exercises `MMDevice.FriendlyName` (which goes through `PropertyStore.GetValue` → `IPropertyStore::GetValue` → `PropVariant`). Must continue to pass.
- [NAudioTests/Wasapi/AudioClientTests.cs](NAudioTests/Wasapi/AudioClientTests.cs) — also touches device discovery via `MMDevice`.
- A targeted manual smoke test: enumerate all output devices via `MMDeviceEnumerator`, read `FriendlyName`, `State`, `DataFlow`, and at least one `Properties[PKEY_*]` value per device. Confirm no exceptions and identical values to a pre-migration run.
- Diff check: when the migration PR is opened, [PropertyStore.cs](NAudio.Wasapi/CoreAudioApi/PropertyStore.cs), [IPropertyStore.cs](NAudio.Wasapi/CoreAudioApi/Interfaces/IPropertyStore.cs), [PropVariant.cs](NAudio.Wasapi/CoreAudioApi/PropVariant.cs), and [MMDevice.cs](NAudio.Wasapi/CoreAudioApi/MMDevice.cs) should appear in the diff with **zero** changes, or only with comment / using-statement edits. Any actual code change to those files is a sign the migration has crept out of scope and should be rejected at review.

### Threading — must flip from failing to passing

- `Experimental_CanCreateOnStaThreadAndUseOnMtaThread` in [ResamplerDmoTests.cs](NAudioTests/Dmo/ResamplerDmoTests.cs). Today: fails with `E_NOINTERFACE` on the read thread. After migration: must pass and validate that the resampled sine wave preserves its source frequency.
- The same-named test in [MediaFoundationResamplerTests.cs](NAudioTests/MediaFoundation/MediaFoundationResamplerTests.cs). Today: passes (because MF defers transform creation to the read thread). After migration: must continue to pass.

### Suggested additional tests to add as part of the migration

- An equivalent `Experimental_CanCreateOnStaThreadAndUseOnMtaThread` test for `DmoMp3FrameDecompressor` — construct the decompressor on an STA thread (passing in an MP3 frame's `WaveFormat`), then decode at least a few frames on a separate MTA thread, and verify that the output sample count matches the expected ratio.
- A `Dispose` test that creates and disposes 100 resamplers in a tight loop on a single thread, asserting the process does not leak handles or COM references — guards against the deterministic-release rewrite regressing into an under-release.

### Manual verification

- Run the existing `NAudioConsoleTest` resampler menu items ([NAudioConsoleTest/Dmo/DmoResamplerTests.cs](NAudioConsoleTest/Dmo/DmoResamplerTests.cs)) end-to-end with audible output.
- Play an MP3 via `Mp3FileReader` configured to use `DmoMp3FrameDecompressor` (the default on Windows).
- Run the `NAudioWpfDemo` MediaFoundationResample view to confirm the WPF-thread (STA) flow still works for resampling.

---

## Risks and mitigations

- **Subtle marshalling errors in the rewritten wrapper bodies.** A wrong pointer or missed pin would crash the process. Mitigation: do the rewrite method-by-method, run the integration tests after each method, do not batch.
- **Effect classes left on legacy path while their shared helpers move forward.** If `MediaObject` is migrated but the effects still pass legacy `IMediaObject`, builds break. Mitigation: migrate all effect activation in the same PR as the `MediaObject` rewrite, or keep a transitional adapter that accepts both for one PR cycle.
- **`MediaFoundationResampler` regresses.** Its constructor probe uses the same coclass type. Mitigation: explicit test pass through every `MediaFoundationResamplerTests` case after the change to step 7.
- **Deterministic Release semantics differ from `Marshal.ReleaseComObject`.** ComWrappers' `ComObject` exposes its own dispose pattern. Audit every `Marshal.ReleaseComObject` site in the DMO folder during the migration and replace with the ComWrappers equivalent — do not leave a mix.
- **Scope creep into `IPropertyStore`.** It would be tempting, partway through the migration, to "while we're here" produce a parallel `[GeneratedComInterface]` `IPropertyStore` so that the dead fields in `WindowsMediaMp3Decoder` / `DmoResampler` could keep their original cast. **Resist this.** The dead fields should be deleted outright, not preserved through a new interface declaration. Reasons:
  - The blast radius lands on every NAudio user that reads `MMDevice.Properties` — `IPropertyStore` is shared with [PropertyStore.cs](NAudio.Wasapi/CoreAudioApi/PropertyStore.cs).
  - `PropVariant`'s explicit-layout struct is documented in [MODERNIZATION.md](MODERNIZATION.md) as incompatible with source-generated marshalling; any rewrite must reimplement `PropVariant` packing manually with substantial regression risk.
  - There is no behavioural benefit on the DMO side — those fields are dead code today and stay dead after migration.
  Mitigation: enforce the diff check in the verification section ([PropertyStore.cs](NAudio.Wasapi/CoreAudioApi/PropertyStore.cs), [IPropertyStore.cs](NAudio.Wasapi/CoreAudioApi/Interfaces/IPropertyStore.cs), [PropVariant.cs](NAudio.Wasapi/CoreAudioApi/PropVariant.cs), and [MMDevice.cs](NAudio.Wasapi/CoreAudioApi/MMDevice.cs) must appear unchanged in the migration PR).

---

## Done definition

- All consuming code in `NAudio.Wasapi/Dmo/` references only modern `[GeneratedComInterface]` interfaces (with `IPropertyStore` excluded by design).
- No `[ComImport]` declarations remain in `NAudio.Wasapi/Dmo/` (including coclasses). The legacy `IPropertyStore` declaration in `NAudio.Wasapi/CoreAudioApi/Interfaces/` is left untouched.
- All resampler and MP3-decompressor tests listed under "Verification" pass.
- `Experimental_CanCreateOnStaThreadAndUseOnMtaThread` for both fixtures passes.
- `IPropertyStore` consumer tests (`MMDevice.Properties` paths) pass with no regressions, and the `IPropertyStore` / `PropertyStore` / `PropVariant` / `MMDevice` files are unchanged in the migration PR.
- `MODERNIZATION.md` cross-references this document and marks DMO migration as complete.
