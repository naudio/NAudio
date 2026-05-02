# MfStressTest

Media Foundation reliability soak / stress test harness for NAudio 3.

## Goal

Answer one question for the NAudio 3 release:

> **Is NAudio's Media Foundation interop rock-solid, or did the recent ACM + MF
> AV expose a fatal release-blocker bug in the MF wrappers?**

The ACM-AV investigation (`Tools/prompts/acm-av-investigation.md`) traced an
intermittent native crash inside `acmFormatSuggest` to a confirmed cofactor:
**MF presence in the test process**. Removing MF dropped the crash rate from
~7% to 0/400. CI flakiness was patched by dropping the MF dependency from the
Mp3FileReaderLazyTocTests fixture (commit `e49a2a0`), but that sidestepped the
underlying question rather than answering it. See
`Tools/prompts/mf-reliability-investigation.md` for the full background.

This tool is the experiment that answers the question. Two useful outcomes:

1. **Clean across many runs** -> evidence MF is solid; the ACM AV is a
   coexistence quirk (heap layout, DLL load), not an MF bug. Strengthens
   release confidence.
2. **Reliable native AV** -> a tighter, MF-only repro than what we had. Lets us
   narrow the root cause (S1 dual-finalizer races in `MfActivate` /
   `MediaType` / `MfSinkWriter` / etc., or something else).

A clean run of this tool also has long-term value as a pre-release smoke test
for the major MF code paths.

### Current findings (as of 2026-05-02)

- **No native AV reproduced** across multiple 5-minute runs at
  `--threads 4`. Suggests an MF AV is either very rare or absent — the
  release-readiness picture is favourable.
- **A separate reliability issue surfaced**: `IMFSourceReader::ReadSample` can
  hang indefinitely with no managed-side recovery path. Captured in multiple
  process dumps, root cause confirmed as an internal MF source-reader pipeline
  starvation deadlock (main thread waits on transform output; MF worker thread
  waits on source event queue; neither makes progress). Native stack analysis
  shows zero NAudio frames in the wait chain — this is a Windows / MF bug, not
  an NAudio bug. See `Tools/prompts/mf-reliability-investigation.md` for full
  details and dump analysis.
- Hangs reproduce on **both** file-based (`MediaFoundationReader`) and
  stream-based (`StreamMediaFoundationReader`) readers, with stream-based
  appearing somewhat more common in the small samples we have. Concurrency
  (`--threads 4`) raises the hang rate noticeably vs single-threaded.
- This investigation also led to two small unrelated fixes in
  `MediaFoundationReader.Read`: handle `STREAMTICK` / `NEWSTREAM` /
  `NativeMediaTypeChanged` / `AllEffectsRemoved` flags as informational
  (continue) rather than fatal (throw).

## What it exercises

- **Encode**: `MediaFoundationEncoder.EncodeToMp3`, `EncodeToWma`,
  `EncodeToAac`, `EncodeToFlac`. Both file and `MemoryStream` sinks (FLAC is
  file-only). Multiple sample rates (22050, 44100, 48000) and channel counts
  (1, 2). Both PCM16 and IEEE-float input occasionally. For FLAC the harness
  peeks at the encoder's advertised output media types and rebuilds a 24-bit
  PCM source if 16-bit isn't offered for the (rate, channels) combo — some
  Windows SKUs only advertise 24-bit FLAC for some rates.
- **Decode**: every encoded clip is read back through `MediaFoundationReader`
  and `StreamMediaFoundationReader`. Roughly half of soak iterations also
  reposition mid-stream and continue reading.
- **Resample**: `MediaFoundationResampler` exercised on roughly two-thirds of
  soak iterations with a randomised target sample rate.
- **Enumeration / wrapper churn**: every ~50 soak iterations (and continuously
  in `--mode enum`) calls `MediaFoundationApi.EnumerateTransforms` over the
  audio decoder/encoder/effect categories and `GetOutputMediaTypes(MP3)`,
  abandoning the resulting `MfActivate` / `MediaType` wrappers undisposed.
  This feeds the S1 dual-finalizer pattern (raw IntPtr + RCW released by two
  independent finalizers in undefined order).
- **GC pressure**: `GC.Collect()` every N iterations (default 50) **without**
  `WaitForPendingFinalizers`, so the finalizer thread races with the next
  encode/decode call. Same shape as `AcmSuggestRepro` mode 2.

## Design at a glance

The harness in `--mode all` runs these phases in sequence (~70/10/10/10 budget split):

1. **Probe** (~2s). Enumerate output media types per codec; codecs with zero
   types (e.g. FLAC/ALAC on stripped Windows SKUs) are skipped.
2. **Breadth** (~30s, fixed). One pass through the working matrix. Each combo
   that succeeds (encode + decode + resample) is added to a working set for
   soak. Combos that fail with a managed `MediaFoundationException` /
   `InvalidOperationException` are logged and skipped (expected: not every
   bitrate is supported on every machine).
3. **Soak** (~70%, parallel). Random sampling from the working set with
   randomised duration / frequency / reposition / resample / float-input
   choices, plus the GC + enumeration churn described above. Multi-threaded
   via `--threads N` (default 1).
4. **Enum** (~10%). `EnumerateTransforms` + `GetOutputMediaTypes` churn,
   abandoning all wrappers undisposed to feed the S1 dual-finalizer chain.
5. **Abandon** (~10%). Construct `MediaFoundationEncoder` and abandon
   undisposed (no `Dispose`). GC pressure makes the finalizer thread release
   the wrapper chain. Direct stress on the same S1 hypothesis.
6. **STA** (~10%). Soak loop on a single STA-apartment thread with
   `MediaFoundationReaderSettings.SingleReaderObject = true` — the documented
   STA configuration, the WinForms-event-handler shape.

Each phase can also be run on its own via `--mode <name>` for targeted runs.

The csproj **deliberately does not reference `NAudio.WinMM`** — no ACM types
are present, so any native AV is unambiguously MF.

### Watchdog

A background watchdog thread monitors per-slot heartbeats. If any slot is
silent for `--watchdog N` seconds (default 10), the watchdog:

1. Tags the hang severity from the hung combo's sink (file = HIGH, stream =
   known-issue / lower priority).
2. Captures a full-memory `procdump` of the process to
   `%TEMP%\MfStressTest_dumps\hang_<file|stream>_<timestamp>_iter<N>.dmp`,
   capped at `--max-dumps` per run (default 3) so a long run can't fill disk.
3. By default, `Environment.FailFast`s. With `--soft-watchdog`, it instead
   marks the hung slot inactive and lets the rest of the soak continue (the
   hung worker thread itself stays blocked in MF code until process exit, but
   sibling workers and other phases keep running).

Workers stuck in MF native code can't be aborted from managed code, so the
soak phase joins workers with a 2-second timeout rather than blocking forever.

## Build & run

Always **Release** - the ACM bug never reproduced under Debug, and the MF
hypothesis is similarly likely to be JIT/optimiser-sensitive.

```sh
dotnet run -c Release --project MfStressTest
dotnet run -c Release --project MfStressTest -- --duration 300 --threads 4 --soft-watchdog
dotnet run -c Release --project MfStressTest -- --mode enum --duration 120
```

### CLI

```text
MfStressTest [options]
  --duration N      total seconds (default 180)
  --mode M          breadth | soak | enum | abandon | sta | all (default all)
  --threads N       parallel soak workers (default 1; affects soak / all)
  --gc-every N      GC.Collect every N iterations (default 50; 0 = off)
  --seed N          deterministic seed (default 42)
  --temp DIR        override temp dir
  --verbose         log every iteration
  --keep            do not delete the temp dir on exit
  --watchdog N      dump if no heartbeat for N seconds (default 10; 0 = off)
  --procdump PATH   path to procdump.exe (default C:\tools\procdump.exe)
  --soft-watchdog   on hang, dump but do not FailFast — mark slot inactive and
                    keep running so the rest of the soak still accumulates
                    AV-hunting time across other threads / phases
```

Modes:

- `breadth` — one-pass through the codec × rate × channel × sink matrix to
  identify what works on this machine.
- `soak` — randomised encode + decode + reposition + resample, parallelisable
  via `--threads`.
- `enum` — `EnumerateTransforms` + `GetOutputMediaTypes` churn for the S1
  dual-finalizer wrapper chain.
- `abandon` — construct `MediaFoundationEncoder` and abandon undisposed, with
  GC pressure. Direct stress on the `Mf*` wrapper finalizer chain.
- `sta` — soak loop on a single STA-apartment thread with
  `MediaFoundationReaderSettings.SingleReaderObject = true` (the documented
  STA configuration). Real-world WinForms users hit this exact shape.
- `all` — runs soak (~70%), enum / abandon / sta (~10% each).

### Reading the output

The harness emits a stable set of exit codes suitable for CI gating:

| Exit | Meaning | What it tells CI |
| --- | --- | --- |
| `0` | Clean run — no AVs, no hangs, no managed exceptions | Pass |
| `1` | Managed exception escaped the run loop | Fail (unexpected NAudio bug) |
| `2` | `--soft-watchdog` mode + at least one hang observed | Fail (MF reliability regression — investigate) |
| `3` | No encodable codecs detected on this machine (probe failed) | Fail (machine misconfigured) |
| `35` | `Environment.FailFast` — hard watchdog mode hit a hang | Fail (MF reliability regression — investigate) |
| native | Process killed by Windows (0xC0000005 access violation, 0xC0000409 FailFast) | Fail (real native AV — release blocker) |

For a CI gate, treat any non-zero as failure. The 0 vs 2 split is useful in
investigation runs — exit 2 means the run completed but accumulated hangs
that would have FailFast'd in hard mode.

The final line of normal output looks like:

```text
DONE in 304.1s - no AV observed (hangs: file=0 stream=0, exit=0)
```

Other useful signals:

- **Native AV**: process terminated by Windows with `0xC0000005` (access
  violation) or `0xC0000409` (FailFast). The last per-second progress line
  before termination tells you which codec/format was active. Check Event
  Viewer (Windows Logs → Application) for the WER record.
- **Managed exception** escaping the loop is printed to stderr. Treat as a
  real bug — the breadth phase swallows expected per-combo failures, and the
  soak phase only swallows `MediaFoundationException`. Anything else is
  unexpected.

### Suggested investigation runs

- AV hunt with maximum coverage:
  `--duration 300 --threads 4 --soft-watchdog`. Soft watchdog lets the run
  accumulate full duration even when MF source-reader hangs occur; hung threads
  are dumped and dropped, other workers keep going.
- Confidence baseline: `--duration 300` repeated five times with different
  `--seed` values. Watch the file/stream hang split in the final summary.
- S1 finalizer race focus: `--mode enum --duration 300 --gc-every 10`.
- Encoder-abandon S1 focus: `--mode abandon --duration 300 --gc-every 10`.
- STA-apartment regression: `--mode sta --duration 120`.

### Hang severity tagging

When the watchdog fires, the message identifies whether the hung reader was
file-backed or stream-backed:

- `[STREAM-BASED — KNOWN ISSUE: ComStream / IStream→IMFByteStream]` —
  documented hang class around `MFCreateMFByteStreamOnStream` interactions.
  Lower priority for NAudio 3 (deferred behind a future direct
  `IMFByteStream` implementation).
- `[FILE-BASED — HIGH SEVERITY: pure MF source-reader hang]` — hang is
  inside MF's own source-reader pipeline with no NAudio code in the byte
  stream path. These dumps land at `%TEMP%\MfStressTest_dumps\hang_file_*.dmp`
  and warrant deeper investigation (native stack via cdb + Microsoft symbols).

The end-of-run summary line reports `hangs: file=N stream=M` for at-a-glance
tracking across runs.

### Where dump files go

Watchdog-captured process dumps are written to:

```text
%TEMP%\MfStressTest_dumps\
```

(typically `C:\Users\<you>\AppData\Local\Temp\MfStressTest_dumps\`).

The exact path is logged at startup as `Watchdog: ... dumps -> ...`. Files are
named `hang_<file|stream>_<timestamp>_iter<N>.dmp` and are full-memory dumps
(~180-220 MB each). The harness does **not** delete them on exit — they're
forensic data and may be useful days later. Capped at 3 per run by default
(`--max-dumps`); raise the cap if you want more samples or set to 0 to
disable dumping entirely.

**Clean up periodically.** A long investigation can easily accumulate several
GB. Safe to delete the whole `MfStressTest_dumps` folder when you're done
with the dumps inside it; the harness recreates it on the next run.

**procdump is optional.** If `procdump.exe` isn't on the machine, the
watchdog just skips the dump step and prints a one-line warning at startup;
hang detection, severity tagging, hang summary, and FailFast / soft mode
all still work. Install procdump (`winget install Microsoft.Sysinternals.ProcDump`
or download from Sysinternals) only if you want the forensic memory dumps
for post-mortem analysis.

Symbol cache for `cdb` analysis (if you've used it) lives separately under
`MfStressTest/dumps/` — that whole folder is gitignored, so the symbol
cache and any local-only `.dmp` copies you've moved there are safe.

### Native-stack analysis on a hang dump

The watchdog dump is a full-memory minidump that includes the full native
state. Resolving `mfreadwrite!*` / `mfplat!*` symbols from the public
Microsoft symbol server lets you see exactly where MF is internally stuck.

`cdb.exe` (Windows Console Debugger) ships with WinDbg
(`winget install Microsoft.WinDbg`); the launcher alias is at
`%LOCALAPPDATA%\Microsoft\WindowsApps\cdbX64.exe`.

```text
cdbX64 -z <path-to-hang.dmp>

  .symfix C:\Users\you\code\mygithub\NAudio\MfStressTest\dumps\sym
  .sympath+ srv*https://msdl.microsoft.com/download/symbols
  .reload /f
  ~              ; list threads
  ~* k 100       ; native stacks for all threads
  !locks         ; held critical sections (usually empty for these hangs)
  q
```

The first run downloads symbols (slow, ~minutes); subsequent runs are fast.
For the MF source-reader hangs we've captured in this tool, the main thread
typically lands at `mfplat!CMFMediaEventGenerator::GetEvent` called from
`mfreadwrite!CMFSourceReaderStream::GetSampleFromMFT`.

## Known issues

These are MF / Windows reliability problems we've observed via this harness.
They are *not* NAudio bugs — the native call stacks are inside
`mfreadwrite.dll` / `mfplat.dll` with no NAudio frames in the wait/error
chain — but they affect NAudio users and so are worth tracking here.

### MF source-reader hang in `IMFSourceReader::ReadSample`

`MediaFoundationReader.Read` / `StreamMediaFoundationReader.Read` can stall
indefinitely waiting on MF's internal source-reader pipeline. Native stack
analysis shows a producer/consumer deadlock between MF's main thread (waiting
for transform output) and an MF worker thread (waiting for source input).
Affects both file- and stream-based readers, with stream-based historically
more common. The harness watchdog (`--watchdog N`) detects these and tags
severity. Mitigation candidates:

- Public `AbortPendingRead()` API on `MediaFoundationReader` that calls
  `IMFSourceReader::Flush` from any thread (lets users implement their own
  watchdog).
- Direct `IMFByteStream` implementation to replace the
  `ComStream → MFCreateMFByteStreamOnStream` shim used by
  `StreamMediaFoundationReader` — likely fixes most stream-based hangs.

See `Tools/prompts/mf-reliability-investigation.md` for full analysis and
captured dumps.

### WMA + `StreamMediaFoundationReader` returns `MF_E_ASF_INVALIDDATA`

WMA decoding via `StreamMediaFoundationReader` (i.e. `MemoryStream` wrapped
in `ComStream` and passed into MF's source resolver) sporadically returns
`MF_E_ASF_INVALIDDATA` (`0xC00D3A9A`) — the ASF parser rejecting bytes it
considers malformed. **File-based WMA reading is unaffected**; only the
Stream sink fails.

Most likely root cause is the same `IStream → IMFByteStream` shim that's
suspected in the stream-based hangs above: short or misaligned reads
returned to the ASF parser. Microsoft does not publicly document
`MF_E_ASF_INVALIDDATA` with prose; the constant lives only in `mferror.h`.

The harness skips the WMA + Stream combo by default to avoid drowning soak
logs in expected exceptions ([Phases/Probe.cs](Phases/Probe.cs) sets WMA's
`streamCapable: false`). Re-enable in code if you specifically want to
characterise this issue.

WMA is also a low-priority format in 2026 (Microsoft itself stopped pushing
it years ago) — recommend documenting "use file-based reading for WMA, or
prefer a different format" in NAudio's user docs rather than chasing this
internally.

## Scope, non-goals

Currently NOT covered (deliberate omissions / future work):

- **OPUS encode coverage**: OPUS is in the probe table for visibility but
  Windows 10/11 has only an OPUS decoder MFT, no encoder
  (`GetOutputMediaTypes(MFAudioFormat_Opus)` returns empty). Decode-only
  coverage would need a fixture file, which conflicts with the harness's
  fully-synthesized design — see `Docs/MediaFoundationEncoder.md` for the
  decode-only story and Concentus as the third-party alternative for
  encoding.
- **ALAC encode coverage**: Windows ships an ALAC encoder MFT but the MP4
  sink rejects every codec-private layout we tried (bare 24-byte
  `ALACSpecificConfig` and FFmpeg's 36-byte `'alac'`-FullBox wrapper) with
  `MF_E_SINK_HEADERS_NOT_FOUND` (`0xC00D4A45`). Microsoft's encoder is
  undocumented, so we'd be guessing at the format. Walked back —
  `Docs/MediaFoundationEncoder.md` documents this as not supported.
- **CCW/RCW refcount instrumentation**: separate tool if/when needed.
- **NAudio 2 backport**: a useful future experiment is running this same
  harness against NAudio 2's pre-`GeneratedComInterface` Media Foundation
  layer to determine whether the MF source-reader hang is new (introduced by
  the modernization) or pre-existing. If pre-existing, the bug is purely
  Windows / MF; if new, our COM interop layer changed something material.
- **Direct `IMFByteStream` implementation** for `StreamMediaFoundationReader`,
  bypassing `ComStream` + `MFCreateMFByteStreamOnStream`. May reduce or
  eliminate stream-based hangs (the IStream→IMFByteStream shim is a known
  source of edge-case stalls).

## File layout

```text
MfStressTest/
  MfStressTest.csproj   # references NAudio.Core + NAudio.Wasapi only
  Program.cs            # entry point: parse → MF startup → dispatch → summary
  Cli.cs                # argument parsing, --help, header banner
  Options.cs            # Options + RunMode + Sink + Counters
  Watchdog.cs           # heartbeat watchdog + procdump capture + severity tagging
  MfPrimitives.cs       # shared encode/decode/drain/signal helpers + Combo / EncodedClip
  Phases/
    Probe.cs            # codec discovery + breadth-first matrix sweep
    Soak.cs             # multi-threaded random soak (the main AV / hang hunter)
    EnumStress.cs       # EnumerateTransforms churn for S1 finalizer pressure
    Abandon.cs          # construct-and-abandon MediaFoundationEncoder
    StaSoak.cs          # STA-apartment soak with SingleReaderObject = true
  README.md
  dumps/                # gitignored cdb working area (symbol cache, local dumps)
```

**Adding a new test scenario**: drop a new file under `Phases/`, expose a
single static `Run` method, add a new value to `RunMode` in `Options.cs`,
and wire it into the dispatch switch in `Program.cs`. The watchdog, CLI,
and counters are already in place — your phase composes calls from
`MfPrimitives` and beats the watchdog before each potentially-blocking MF
call.

Watchdog-captured process dumps go to `%TEMP%\MfStressTest_dumps\` (see
"Where dump files go" above), not into the repo.

## Related

- `Tools/prompts/mf-reliability-investigation.md` - background and hypotheses.
- `Tools/prompts/acm-av-investigation.md` - the parent ACM-AV investigation.
- `Tools/AcmSuggestRepro/` - sibling harness for the ACM side. Uses the same
  prologue / progress / GC patterns this tool inherits.
