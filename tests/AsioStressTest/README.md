# AsioStressTest

Reproduction harness for the **ASIO access violation after stop + GC + master-volume change**
fault observed in the NAudioDemo Audio File Playback demo.

## The fault

Playing audio via `AsioOut`, stopping, switching to the Device Notifications tab, and then
adjusting the Windows master volume sometimes crashes the process with `0xC0000005`
(access violation) on a native thread.

## Suspected cause

The four ASIO callback delegates registered in `AsioDriverExt` —
`bufferSwitch`, `bufferSwitchTimeInfo`, `asioMessage`, `sampleRateDidChange` — are bound to the
`AsioDriverExt` instance and rooted **only** through `AsioDevice.driver`. `AsioDriver.CreateBuffers`
marshals them to native function-pointer thunks (`Marshal.StructureToPtr`) which the ASIO driver
copies and keeps.

`AsioDevice.Dispose()` releases the COM object with `Marshal.Release`, which does **not** stop the
driver's background notification thread, then nulls `driver`. Once the app drops its `AsioOut`
reference, the wrapper and its delegates become collectable. A GC (e.g. the allocation pressure of
opening the Device Notifications tab) collects them. The next `asioMessage(kAsioResetRequest)` the
driver raises — for example when the Windows master volume changes on a shared-mode / WASAPI-backed
ASIO driver — calls a **dangling thunk** → access violation, uncatchable because it happens on the
driver's own thread.

## What this harness does

Automates the manual sequence in a tight loop:

1. Open `AsioOut`, `Init` a short quiet sine, `Play`.
2. Play for `--play-ms`, then `Stop` + `Dispose`, dropping the reference.
3. Force a full blocking GC (collects the abandoned callback delegates).
4. Nudge the default render endpoint's master volume down and back (provokes the driver's
   reset callback while the delegates are gone).
5. Repeat, reporting whether the previous iteration's player was actually collected.

## Running

```
dotnet run -c Release --project tests/AsioStressTest -- --list
dotnet run -c Release --project tests/AsioStressTest -- --iterations 200
```

Must run on Windows with at least one ASIO driver installed. The process runs STA (ASIO's
`CoCreateInstance` requires it).

## Control cases (to confirm the diagnosis, not just reproduce)

- `--keep-alive` roots every disposed player so its delegates can never be collected. If the crash
  **disappears** with this flag while occurring without it, the cause is confirmed as delegate
  collection — and the fix is to keep the callback delegates rooted for as long as the driver may
  call them (plus null-safe callback bodies).
- `--no-gc` skips the forced collection. If the crash is far rarer without the GC, that also points
  at collection timing.
- `--no-toggle` skips the volume nudge, isolating whether the driver needs the endpoint stimulus to
  fire its reset callback.

## Exit codes

| code | meaning |
|------|---------|
| 0 | all iterations completed, no access violation |
| 1 | argument / setup error, or no ASIO driver present |
| native (0xC0000005) | access violation — reproduction achieved |
