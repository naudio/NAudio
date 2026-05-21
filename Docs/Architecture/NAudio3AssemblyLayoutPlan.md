# NAudio 3 — Assembly layout & packaging

**Goal:** For the v3 clean break, restructure the package set so that the cross-platform surface is honestly cross-platform, all winmm interop is consolidated into a single Windows assembly, and a single `NAudio` meta-package gives Windows users the full stack while leaving Linux/macOS users with the portable subset (and an explicit choice of backend).

This design fixes two of the inherited problems from NAudio 1.x/2.x: `NAudio.Midi` mixing portable file I/O with winmm-only `MidiIn`/`MidiOut`, and `NAudio.Core` carrying Win32-shaped types (`MmResult`, `MmException`, `Manufacturers`) it never used. The first round of work covers exactly those two problems plus a multi-targeted meta-package — the bits we're already certain about. Other v3 candidates (splitting `NAudio.Wasapi`, landing `NAudio.Midi.WinRT`, rehousing `NAudio.Extras`) are sequenced as **subsequent phases** (see §"Subsequent phases") to be attempted *after* the first round is committed. They're not definitely-out-of-scope for NAudio 3 — but each is a separate piece of work that may run into unexpected coupling or homeless types, and we want the option to back any of them out without rolling back the certain wins.

## Problems with the current layout

1. **`NAudio.Midi` is dishonestly portable.** It targets `net9.0` (no platform suffix) but `MidiIn` / `MidiOut` P/Invoke `winmm.dll`, so a Linux consumer who only wants `.mid` file parsing still gets a `DllNotFoundException` waiting to happen if they reach for the wrong type.
2. **`NAudio.Core` carries Win32-shaped types it doesn't use.** `MmResult`, `MmException`, and the `Manufacturers` enum are all winmm/MIDI concepts. They're consumed only by `NAudio.WinMM`, `NAudio.Midi`, and `NAudio.WinForms` — never by `NAudio.Core` itself. Their presence in Core is a historical accident from the era when everything lived in one assembly.
3. **No coherent meta-package story.** `NAudio` is currently a Windows umbrella by accident, not by design. Cross-platform users have no clear "pick this" recommendation, and the package's transitive deps include things they cannot use.
4. **`NAudio.Wasapi` is misnamed.** It contains four distinct Windows APIs — WASAPI/CoreAudio, Media Foundation, DMO, and DirectSound (and also currently the WinRT Midi)— each with its own SDK history, deprecation curve, and audience. *(Acknowledged but **deferred** beyond the first round — see §"Subsequent phases".)*

## Design principles

- **Cross-platform code lives in `net9.0` assemblies; OS-specific code lives in `net9.0-windows` assemblies marked `[SupportedOSPlatform("windows")]`.** No exceptions, no `PlatformNotSupportedException`-at-runtime surprises.
- **Shared types live where their consumers live, not in Core.** Core stays pure DSP / codecs / format I/O.
- **All winmm.dll interop lives in one assembly (`NAudio.WinMM`).** That includes legacy `MidiIn`/`MidiOut` and the win32 error/manufacturer types they share with mixer/ACM/waveOut.
- **The meta-package multi-targets** so that a Windows consumer auto-receives the full Windows audio stack and a `net9.0` consumer receives only the portable subset. This is how NuGet is designed to handle the OS split, and it works without RID-conditional restore tricks (which NuGet doesn't really support for managed-package selection anyway). The meta-package's Windows leg uses `net9.0-windows10.0.19041.0` to match `NAudio.Wasapi`'s floor — see §"Meta-package" for the rationale.
- **This is a pure structural restructure — assembly moves and type moves only.** No source-level API changes, no `[Obsolete]` annotations, no behavior changes, and **no version bumps**. Package versions stay at their current `2.3.0` for the duration of the first round; pre-release/version-strategy work is its own follow-on (see §"Out of scope for the first round"). Anything called out as "deprecated" or "legacy" in `MODERNIZATION.md` is also out of scope and can be addressed in a separate pass.
- **`NAudio.Wasapi` keeps its current scope in the first round.** It continues to ship WASAPI/CoreAudio + MediaFoundation + DMO + DirectSound in a single assembly. The "one assembly per underlying API" split is sequenced as a subsequent phase that may or may not land in NAudio 3 depending on what the trial split surfaces.
- **`NAudio.Midi.WinRT` is also deferred to a subsequent phase.** Finishing and testing the [`winrtmidi`](https://github.com/naudio/NAudio/tree/winrtmidi) prototype is its own piece of work; the first round ships only the existing winmm MIDI backend (relocated into `NAudio.WinMM`). The portable `NAudio.Midi` assembly is structured so the WinRT package can be added on top later without revisiting the layout.

## Proposed v3 layout

### Cross-platform (`net9.0`)

| Package | Contents | Notes |
|---|---|---|
| `NAudio.Core` | DSP, codecs (G.711, G.722), format readers/writers (WAV, AIFF, SoundFont, MP3 frame parsing), sample providers, resamplers (`WdlResampler`), FFT, biquad filters | Already 100% portable as of the modernization work. No structural change needed beyond moving `MmResult`/`MmException`/`Manufacturers` *out* (see §"Type moves"). |
| `NAudio.Midi` | MIDI message types, `MidiFile` reader/writer, `MidiEvent` hierarchy | Becomes truly portable. `MidiIn` and `MidiOut` move out (see `NAudio.WinMM`). Once the winmm types are gone the assembly is pure managed file I/O — opt it in to `<IsAotCompatible>true</IsAotCompatible>` to match `NAudio.Core`. |

### Windows-specific (`net9.0-windows`, `[SupportedOSPlatform("windows")]`)

| Package | Contents | Notes |
|---|---|---|
| `NAudio.Wasapi` | WASAPI/CoreAudio (`AudioClient`, `MMDevice`, `MMDeviceEnumerator`, `WasapiCapture`, `WasapiLoopbackCapture`, `WasapiOut`, `WasapiPlayer`, `WasapiRecorder`), Media Foundation (`MediaFoundationReader`, `StreamMediaFoundationReader`, `MediaFoundationEncoder`, `MediaFoundationResampler`), WinRT MIDI shim | Scope narrowed in the second round (see §"Execution status — second round"): DMO and DirectSound carved out into `NAudio.Dmo`. The "kitchen-sink" feel is gone; what remains is WASAPI + MF + WinRT MIDI, all on the modern, non-legacy Windows audio stack. |
| `NAudio.Dmo` | DMO effects (echo, chorus, compressor, distortion, flanger, gargle, I3DL2 reverb, param EQ, waves reverb), DMO MP3 decoder (`DmoMp3FrameDecompressor`), DMO resampler stream (`ResamplerDmoStream`), DMO enumeration (`DmoEnumerator`), DirectSound (`DirectSoundOut`, `DirectSoundException`) | New package as of the second round. Carved out of `NAudio.Wasapi`. Targets `net9.0-windows` (no 19041 floor — DMO/DirectSound don't need anything newer than vanilla Windows COM). Bundles two legacy Windows audio APIs that are likely future obsolescence candidates — packaging them together keeps the eventual deprecation surface compact rather than spreading it across two tiny packages. |
| `NAudio.WinMM` | `WaveOut`, `WaveIn`, `WaveInEvent`, `WaveOutEvent`, mixer (`Mixer`, `MixerLine`, `MixerControl`), ACM (`AcmStream`, `AcmDriver`), **legacy `MidiIn` / `MidiOut` / `MidiInCapabilities` / `MidiOutCapabilities`**, `MmResult`, `MmException`, `Manufacturers` | The home for everything that calls `winmm.dll`. Adopting legacy MIDI here (instead of in a separate `NAudio.Midi.Windows`) keeps all winmm interop in one assembly and gives `MmResult`/`MmException`/`Manufacturers` their natural owner. See §"MIDI: portable core + winmm backend" for why this beats a separate Midi.Windows + shared-internals split. |
| `NAudio.Asio` | ASIO bridge | Unchanged. Self-contained, obviously Windows-specific, no reason to disturb. |
| `NAudio.WinForms` | `WaveInWindow`, `WaveOutWindow`, WinForms-specific helpers | Unchanged. Already references `NAudio.WinMM`, so it picks up `MmResult`/`MmException`/`Manufacturers` transitively. |

### MIDI: portable core + winmm backend

An earlier draft proposed a tiny `NAudio.Win32.Common` shared assembly to hold `MmResult`, `MmException`, and `Manufacturers` so that `NAudio.WinMM` and a hypothetical `NAudio.Midi.Windows` could share them without one depending on the other. That shape is unnecessary given the actual coupling: `MidiIn` / `MidiOut` already P/Invoke `winmm.dll`, the same DLL that `WaveOut` / `WaveIn` / mixer / ACM use. They belong in the same assembly.

The first-round split is therefore:

| Package | Target | Contents |
|---|---|---|
| `NAudio.Midi` | `net9.0` | MIDI message types (`MidiEvent` and subclasses) and `MidiFile` reader/writer. 100% portable, AOT-compatible. |
| `NAudio.WinMM` | `net9.0-windows` | Legacy `MidiIn` / `MidiOut` / capabilities live here, alongside `WaveOut` / `WaveIn` / mixer / ACM. They're all winmm.dll consumers — they belong together. |

This puts `Manufacturers`, `MmResult`, and `MmException` in their natural home (`NAudio.WinMM`, the only assembly that calls `winmm.dll`) and removes the need for a shared-internals package. `NAudio.WinForms` already references `NAudio.WinMM` for its window-handle wrappers, so it gets the types transitively.

**Reference direction:** `NAudio.WinMM` gains a `ProjectReference` to `NAudio.Midi`, because relocated `MidiIn` / `MidiOut` continue to expose `MidiEvent` / `MidiInMessageEventArgs` (which stay in the portable assembly). The dependency graph stays acyclic: portable `NAudio.Core` ← portable `NAudio.Midi` ← Windows-only `NAudio.WinMM`. This is the trade for keeping namespace `NAudio.Midi` split across two assemblies — slightly less elegant than a single home, but cheaper than an extra `NAudio.Midi.Windows` assembly or leaving the winmm types in `NAudio.Midi`. .NET handles split namespaces routinely (`System.IO`, `System.Collections.Generic`, etc.).

This layout also leaves room for `NAudio.Midi.WinRT` to slot in later as a third package without revisiting the structure: portable `NAudio.Midi` would gain shared `IMidiInput` / `IMidiOutput` interfaces, the winmm `MidiIn` / `MidiOut` in `NAudio.WinMM` would implement them, and `WinRTMidiIn` / `WinRTMidiOut` would ship in the new package. The meta-package's Windows leg is already at `net9.0-windows10.0.19041.0` (which exposes the WinRT MIDI projections), so no new TFM is required — only the new `PackageReference`. None of that lands in the first round — see §"Subsequent phases".

### Future cross-platform backends

These don't ship in v3 but the layout must accommodate them naturally:

| Package | Target | Notes |
|---|---|---|
| `NAudio.Alsa` | `net9.0` (with `[SupportedOSPlatform("linux")]`) | Pending evaluation of community contribution. |
| `NAudio.PulseAudio` | `net9.0` (with `[SupportedOSPlatform("linux")]`) | Speculative — only if there's demand once ALSA lands. |
| `NAudio.CoreAudio.Mac` | `net9.0` (with `[SupportedOSPlatform("macos")]`) | Speculative. Note the namespace collision risk with WASAPI's existing `CoreAudioApi/` folder — pick a different internal namespace if/when this exists. |
| `NAudio.AAudio` | `net9.0-android` | Speculative, far future. |

These all follow the same per-backend pattern, so adding them later doesn't require revisiting the v3 structure.

### Meta-package

| Package | Target | Contents |
|---|---|---|
| `NAudio` | `net9.0;net9.0-windows10.0.19041.0` (multi-targeted) | TFM-conditional `PackageReference` set: `net9.0` pulls Core + Midi only; the Windows leg pulls the full win32 stack (Core + Midi + Asio + Wasapi + WinMM + WinForms — note `Wasapi` still bundles MediaFoundation/DMO/DirectSound, and `WinMM` brings legacy `MidiIn`/`MidiOut` along). Mirrors the v2 meta-package shape so existing Windows consumers don't need to add `PackageReference`s on upgrade. When `NAudio.Midi.WinRT` lands later, the new package is referenced from the existing Windows leg (the 19041 TFM already exposes WinRT MIDI projections) — purely additive, no new TFM required. |

**Why `net9.0-windows10.0.19041.0` and not `net9.0-windows`?** `NAudio.Wasapi` already targets `net9.0-windows10.0.19041.0` because process-loopback capture (`ActivateAudioInterfaceAsync` + `AUDIOCLIENT_PROCESS_LOOPBACK_PARAMS`) needs that floor. The Windows-leg TFM has to match or be higher than its dependencies, and Windows 10 2004 (May 2020) is the practical minimum for any NAudio 3 consumer anyway — anyone older can stay on NAudio 2.x. The constituent packages `NAudio.WinMM`, `NAudio.Asio`, and `NAudio.WinForms` continue to target `net9.0-windows` themselves (they have no 19041 dependency), so they remain individually consumable on lower Windows versions when referenced directly without the meta-package.

The multi-targeted meta-package is the answer to "can the meta-package adapt to the consumer's platform?" — yes, **at the TFM level**. NuGet auto-selects the right dependency set based on the consumer project's `<TargetFramework>`. There is no `net9.0-linux` TFM, so Linux-specific backends like `NAudio.Alsa` cannot be auto-included at restore time; Linux consumers add them explicitly. That's idiomatic .NET — no clever workaround needed.

## Type moves

| Type | From | To | Reason |
|---|---|---|---|
| `MmResult` | `NAudio.Core` (namespace `NAudio`) | `NAudio.WinMM` (keep namespace `NAudio`) | Pure winmm error code enum; only winmm consumers need it. |
| `MmException` | `NAudio.Core` | `NAudio.WinMM` | Pure winmm error wrapper. |
| `Manufacturers` | `NAudio.Core` | `NAudio.WinMM` | Win32 mmreg.h enum, used only by WinMM mixer caps and legacy MIDI caps (which now also live in `NAudio.WinMM`). |
| `MidiIn`, `MidiOut`, `MidiInCapabilities`, `MidiOutCapabilities`, `MidiInterop` | `NAudio.Midi` | `NAudio.WinMM` (keep namespace `NAudio.Midi`) | All winmm.dll P/Invoke — belongs with the rest of NAudio's winmm interop. |

**Namespace stability:** keep the original namespaces on every moved type. `MmResult` / `MmException` / `Manufacturers` stay in namespace `NAudio`; legacy MIDI types stay in namespace `NAudio.Midi`. Existing `using` directives keep compiling. The break is at the *assembly* level (consumers may need new `PackageReference` entries), not the *source* level.

For users on the `NAudio` meta-package, the new package references come in transitively and nothing changes at all.

**No `[TypeForwardedTo]` shims.** A type-forwarder lives in the *old* assembly and points to the *new* one, which means the old assembly must reference the new one at compile time. Both moves go in a direction that would create a cycle:

- `MmResult` / `MmException` / `Manufacturers` → forwarding from `NAudio.Core` to `NAudio.WinMM` would require `NAudio.Core` to reference `NAudio.WinMM`, but `NAudio.WinMM` already references `NAudio.Core`.
- `MidiIn` / `MidiOut` / etc. → forwarding from `NAudio.Midi` to `NAudio.WinMM` would require `NAudio.Midi` to reference `NAudio.WinMM`, but per §"MIDI: portable core + winmm backend" `NAudio.WinMM` will reference `NAudio.Midi`.

The only ways to break the cycle (a third "common" assembly, or leaving the types in their old homes) are worse trades than the clean break. Source compatibility via namespace stability covers the realistic upgrade path; the migration guide in `MODERNIZATION.md` will call out the one PackageReference change a `NAudio.Midi`-only consumer of `MidiIn` / `MidiOut` needs to make.

## Migration impact summary

| Consumer scenario | Action required |
|---|---|
| Uses `NAudio` meta-package on Windows | None — new transitive deps (`NAudio.WinMM` for relocated MIDI/Mm* types) come in automatically. |
| Uses `NAudio` meta-package on Linux/macOS (currently broken anyway) | Add `NAudio.Alsa` (or other backend) explicitly when v3 lands and that package is published. |
| References specific packages today (`NAudio.Wasapi`, `NAudio.WinMM`, `NAudio.Midi`) | `NAudio.Wasapi` is unchanged in scope — no new packages needed for MF/DMO/DirectSound users. Legacy `MidiIn` / `MidiOut` users now need `NAudio.WinMM` (likely already referenced if they were doing playback). |
| Catches `MmException` or references `MmResult` / `Manufacturers` | These types now live in `NAudio.WinMM`. Source code unchanged — namespace `NAudio` is preserved. Anyone catching `MmException` is already a winmm consumer, so the package is almost certainly already referenced. |
| MIDI-only consumer who never touched waveOut | Now needs `NAudio.WinMM` for live `MidiIn` / `MidiOut`. The portable `NAudio.Midi` package alone gives you message types and file I/O — but no live device backend. |

## Resolved decisions

1. **`NAudio.WinForms` is keeping its future.** WinForms remains widely used and the package isn't going away. Its long-term value will increasingly skew from playback (where `WasapiPlayer` is the better answer) toward the custom UI controls it provides.

## Subsequent phases

Work that is **deferred from the first round, but still candidate for NAudio 3**. Each is a separate piece of work to attempt after the first round is committed; any that surfaces unexpected coupling, homeless types, or test-coverage gaps should be backed out without rolling back the first-round changes. Listed in roughly the order they should be attempted (cheapest first), but each is independent.

1. **Split `NAudio.Wasapi` into per-API packages.** **Partially landed** — see §"Execution status — second round". `NAudio.Dmo` (bundling DMO + DirectSound) has been carved out. A further split of MediaFoundation into its own package is *not currently planned* for NAudio 3: WASAPI and MediaFoundation are tightly co-used (the modern audio stack reads files through MF and plays through WASAPI), and the value of separating them is much lower than separating the legacy DMO/DirectSound APIs that have their own deprecation curve. Leaving them bundled keeps the package count manageable.

2. **Rehouse `NAudio.Extras` contents.** Audit the package and move each piece to its most appropriate assembly (Core, Wasapi, WinMM, etc.). Risk: some items may have no natural home and should stay where they are — this is an evaluate-then-decide phase, not a foregone conclusion. If the audit comes back inconclusive, leave `NAudio.Extras` as-is.

3. **Behaviour / API hygiene work flagged in `Docs/Architecture/MODERNIZATION.md`.** Anything that would mean editing code (slimming `WasapiOut`'s built-in resampler, applying `[Obsolete]` annotations, removing legacy types) is its own concern, separate from the structural work above.

## Execution status — first round

Each step is a separate commit with its own validation. Once the first round is committed and stable, attempt the §"Subsequent phases" items in order — each is independent and any can be backed out without disturbing the rest.

### 1. Move legacy MIDI + win32 types into `NAudio.WinMM` — **Done (2026-05-10)**

Move `MidiIn` / `MidiOut` / `MidiInCapabilities` / `MidiOutCapabilities` / `MidiInterop` from `NAudio.Midi` into `NAudio.WinMM` (preserve namespace `NAudio.Midi`; add the `NAudio.Midi` `ProjectReference` to `NAudio.WinMM` so relocated types can still see `MidiEvent` / `MidiInMessageEventArgs`). Move `MmResult` / `MmException` / `Manufacturers` from `NAudio.Core` into `NAudio.WinMM` (preserve namespace `NAudio`). Add `<IsAotCompatible>true</IsAotCompatible>` to `NAudio.Midi.csproj` once the winmm types are gone. After this step `NAudio.Midi` is portable (`net9.0`) and contains only message types + file I/O.

**Result:** Eight files relocated as pure `git mv` renames (zero content edits). `NAudio.WinMM.csproj` gained the `NAudio.Midi` `ProjectReference`; `NAudio.Midi.csproj` gained `<IsAotCompatible>true</IsAotCompatible>`. All 19 projects in the solution build clean (0 warnings, 0 errors) on a non-incremental rebuild — including `NAudioAotSmokeTest`, which confirms the AOT opt-in did not introduce trim warnings. No consumer-side `PackageReference` adjustments were required: every sample/tool that used the moved types either already references `NAudio.WinMM` directly or pulls it transitively via the `NAudio` meta-package's Windows leg.

### 2. Validate consumer projects build & run cleanly — **Done (2026-05-10)**

Build the full solution and confirm `NAudioDemo`, `NAudioConsoleTest`, `NAudioTests`, `NAudioWpfDemo`, `MidiFileConverter`, `NAudio.Extras`, `MfStressTest`, `AudioFileInspector`, `MixDiff`, `NAudioAotSmokeTest`, and the benchmarks all still compile and run. Capture any consuming-side fix required (added `PackageReference`s, namespace surprises, transitive-dep gaps) in `MODERNIZATION.md` — these are the seed entries for the v2 → v3 migration guide. The sample apps are deliberately doing double duty here as a real-world signal of upgrade impact.

**Result:** Compile-side validation green from step 1 (full clean rebuild, 0 warnings / 0 errors). Runtime smoke pass confirmed — including the `NAudioDemo` MIDI panel, the only non-test consumer of the relocated `MidiIn` / `MidiOut`. Nothing to capture in `MODERNIZATION.md` yet: the structural moves were fully transparent to every consumer in this repo, no `PackageReference` adjustments, no namespace surprises, no transitive-dep gaps.

### 3. Verify the multi-targeted `NAudio` meta-package — **Done (2026-05-10)**

Confirm/preserve the `net9.0;net9.0-windows10.0.19041.0` TFM set with the appropriate dep subsets per the §"Meta-package" section. (Most of the multi-targeting is already in place; this step is more "verify and tidy" than "introduce.")

**Result:** TFM set already correct (`net9.0;net9.0-windows10.0.19041.0`). Portable-leg dep set already correct (Core + Midi only). Windows-leg dep set already correct (Core + Midi + Asio + Wasapi + WinMM + WinForms). No csproj changes required. An earlier draft of this plan proposed removing `NAudio.Asio` from the meta-package on a "most users don't want it" rationale, but that argument didn't hold up next to keeping `NAudio.WinForms` in (equally niche in 2026), and removing it would have forced existing v2 meta-package consumers of `AsioOut` to add a new `PackageReference` on upgrade for no real upside — §"Meta-package" updated to reflect the kept-in decision. Solution rebuilds clean across all 19 projects (0 warnings, 0 errors).

### 4. Document everything — **Done in MODERNIZATION.md (2026-05-10); RELEASE_NOTES.md deferred**

Update `MODERNIZATION.md` and `RELEASE_NOTES.md`. Add a top-level migration guide for v2 → v3, populated from the gotchas captured in step 2.

**Result:** `MODERNIZATION.md` updated — added a *Phase 10: Assembly layout reshape (NAudio 3 first round) — DONE* section, an *Assembly layout reshape (winmm consolidation)* subsection inside Breaking Changes, and refreshed the NAudio.Midi row of the per-project TFMs table. Both new sections defer to this layout plan for the full design rather than duplicating it. `RELEASE_NOTES.md` is intentionally not touched in this round — it will be populated from `MODERNIZATION.md` closer to the v3 preview cut.

## Execution status — second round

The second round attempts the first §"Subsequent phases" item — splitting `NAudio.Wasapi`. Scope is narrower than the original five-package proposal: only DMO and DirectSound move out, into one combined `NAudio.Dmo` package. The MediaFoundation split is *not* attempted (WASAPI + MF are too closely co-used to justify the extra package); the WinRT MIDI split stays deferred as before.

### 1. Drop the embedded resampler from `WasapiOut` — **Done (2026-05-21)**

`WasapiOut` previously wrapped its source in `ResamplerDmoStream` whenever `IsFormatSupported` rejected the input format in exclusive mode. That was the only intra-assembly DMO dependency in `NAudio.Wasapi`, and the embedded resampler was already a known threading/latency problem — `WasapiPlayer` ships without one. Removed the `dmoResamplerNeeded` field, the resampler wiring in `PlayThread`, the format-probe try/catch in `Init`, and the now-unused `GetFallbackFormat` helper. Exclusive-mode callers whose format is not natively supported now get a `NotSupportedException` from `Init` with a message pointing at `MediaFoundationResampler` / shared mode / `WasapiPlayerBuilder`. Shared mode continues to auto-convert via `AudioClientStreamFlags.AutoConvertPcm` (unaffected).

### 2. Relocate `IWMResamplerProps` into MediaFoundation interfaces — **Done (2026-05-21)**

`IWMResamplerProps` lived under `NAudio.Wasapi/Dmo/Interfaces/` but had no DMO-side consumer — `MediaFoundationResampler` was the only call site. Moved the file to `NAudio.Wasapi/MediaFoundation/Interfaces/` and switched its namespace to `NAudio.MediaFoundation.Interfaces`. Interface is `internal`, so the namespace change is source-private. Lets a subsequent DMO split take the `Dmo/Interfaces/` folder cleanly.

### 3. Carve `NAudio.Dmo` out of `NAudio.Wasapi` — **Done (2026-05-21)**

New `NAudio.Dmo` project (`net9.0-windows`, references only `NAudio.Core`). Three categories of move:

- `NAudio.Wasapi/Dmo/` → `NAudio.Dmo/Dmo/` (~45 files: enums, descriptors, COM interfaces, `MediaObject` / `MediaObjectInPlace`, `ResamplerMediaObject`, `WindowsMediaMp3Decoder`, the nine effect wrappers, etc.)
- `NAudio.Wasapi/DirectSound/` → `NAudio.Dmo/DirectSound/` (6 files: `DirectSoundOut`, `DirectSoundException`, and four `[GeneratedComInterface]` interface declarations)
- The three root-level Dmo wrappers in `NAudio.Wasapi/` — `DmoMp3FrameDecompressor.cs`, `DmoEffectWaveProvider.cs`, `ResamplerDmoStream.cs` — move to the new package root

Two pieces of plumbing had to be severed:

- **`ComActivation`**: the `internal static partial class` in `NAudio.Wasapi/CoreAudioApi/` is the common COM-activation helper used by WASAPI, MediaFoundation, DMO, and DirectSound. Duplicated as `NAudio.Dmo/Interop/ComActivation.cs` (internal, partial, namespace `NAudio.Dmo.Interop`). ~110 lines of duplication, but cheaper than introducing a third "common" assembly. The DMO/DirectSound files swap their `using NAudio.Wasapi.CoreAudioApi;` for `using NAudio.Dmo.Interop;` — that's a one-line change in 7 files.
- **`MediaFoundationException.ThrowIfFailed(hr)`** in `MediaObject.cs` (12 sites): replaced with `Marshal.ThrowExceptionForHR(hr)`. The MF exception type provided pretty MF-specific error messages, but DMO HRESULTs weren't in its lookup table anyway, so the runtime surface is unchanged — `Marshal.ThrowExceptionForHR` constructs the same `COMException` shape with the OS's localized message.

Solution wired up: `NAudio.Dmo` added to `NAudio.slnx`; the meta-package's Windows leg picks it up via `<ProjectReference>` (under `'$(TargetPlatformIdentifier)' == 'windows'`); `release.yml` packs it alongside the other NAudio packages. In-repo consumers that touched the moved types directly (`NAudioTests`, `NAudioConsoleTest`, `NAudioAotSmokeTest`) gained explicit `NAudio.Dmo` `ProjectReference`s. `NAudioDemo` is unaffected — it consumes the moves transitively through the meta-package, as do external meta-package users.

Namespaces preserved across the board: `NAudio.Dmo`, `NAudio.Dmo.Effect`, `NAudio.Dmo.Interfaces`, `NAudio.Wave` (for `DirectSoundOut`). Source-level upgrade impact is zero. The break is at the `<PackageReference>` level, and only for consumers that reference `NAudio.Wasapi` directly *and* use one of the moved types.

### 4. Document — **Done (2026-05-21)**

`RELEASE_NOTES.md` gained two bullets under *Breaking changes* (the `WasapiOut` resampler removal, and the new `NAudio.Dmo` package). `MODERNIZATION.md` gained a *Phase 11* section and a *Breaking Changes* row for the split. The §"Subsequent phases" item 1 in this file is rewritten to mark the DMO split as landed and to record the decision *not* to pursue a further MediaFoundation split.

### Out of scope for the second round

- **Type-forwarders from `NAudio.Wasapi` to `NAudio.Dmo`.** Technically possible (no cycle: `NAudio.Wasapi` no longer needs DMO after Step 1), but deliberately not added. The split is an NAudio 3 *clean break* — sources stay source-compatible via namespace preservation, and the migration impact for direct `NAudio.Wasapi` consumers is one `<PackageReference>` line. Adding forwarders would carry the legacy surface forward for no obvious upside on a major-version release.
- **`AudioMediaSubtypes` namespace cleanup.** `NAudio.Core/Wave/WaveFormats/AudioMediaSubtypes.cs` lives in namespace `NAudio.Dmo` (used by `WaveFormatExtensible` for subtype name lookup). After the split, both `NAudio.Core` and `NAudio.Dmo` contribute to namespace `NAudio.Dmo` — legal but a minor smell. The GUIDs aren't really DMO-specific (they're cross-API media subtype GUIDs from `wmcodecdsp.h` / `mediaobj.h`), so a future rename to `NAudio.MediaTypes` or just `NAudio` would be cleaner. Out of scope here — it would be a public-API rename touching every consumer of `AudioMediaSubtypes.*`.
