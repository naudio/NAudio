# Handling ASIO Driver Resets

ASIO drivers can ask the host to reinitialize. The most common trigger is the user opening the driver's control panel and changing the sample rate, but it can also fire when the driver itself recovers from a fault, when a hardware setting changes, or when the driver wants to renegotiate buffer sizes. NAudio 3 surfaces this through the `DriverResetRequest` event with a supported recovery path: `Stop` → `Reinitialize` → `Start`.

Buffer-size-change requests (`kAsioBufferSizeChange`) are folded into the same event — the driver telling you it wants a new buffer size needs the same recovery as a sample-rate change, since `Reinitialize` rebuilds buffers against whatever the driver's current preferred size is.

## The event

```c#
device.DriverResetRequest += (sender, e) =>
{
    // The driver wants you to reconfigure. Don't do it from this handler — it's dispatched
    // on your captured SynchronizationContext but Reinitialize must be called when the
    // device is not Running.
};
```

`DriverResetRequest` is dispatched on the captured `SynchronizationContext` (the one current when you constructed `AsioDevice`). On a UI app, that's normally the UI thread.

## The canonical recovery pattern

```c#
device.DriverResetRequest += (_, _) =>
{
    device.Stop();
    device.Reinitialize();
    device.Start();
};
```

`Reinitialize()`:

1. Releases the driver's existing buffers (without releasing the COM driver — same instance is reused).
2. Reads the driver's possibly-changed capabilities.
3. Re-applies the most recent `InitPlayback` / `InitRecording` / `InitDuplex` options.

If the user changed the sample rate, `Reinitialize` will pick up the new rate from the driver's state and re-create buffers accordingly. Your source `IWaveProvider` (in playback or duplex mode) keeps its read position — it will resume from where it left off.

## State requirements

| Current state | Can call `Reinitialize`? |
| --- | --- |
| `Unconfigured` | No — there's no prior config to reapply. |
| `Configured` | Yes. |
| `Running` | No — call `Stop` first. |
| `Stopped` | Yes. |
| `Disposed` | No — throws `ObjectDisposedException`. |

If you call `Reinitialize` from `Running`, it throws `InvalidOperationException` with a clear message. The recovery handler must therefore call `Stop` first.

## What about my source?

For **playback**, the same `IWaveProvider` instance is reused. It resumes from its current read position. If you'd like to restart from the beginning, seek the underlying source before calling `Reinitialize`:

```c#
device.DriverResetRequest += (_, _) =>
{
    device.Stop();
    if (reader is WaveStream ws) ws.Position = 0;
    device.Reinitialize();
    device.Start();
};
```

For **recording**, there's no source state to worry about — `Reinitialize` re-arms the same `AudioCaptured` event subscribers against fresh buffers.

For **duplex**, the same `Processor` delegate is reused. Any state captured in your processor closure persists. If you want to clear it (filter taps, ring buffers, accumulators), do so explicitly between `Stop` and `Reinitialize`.

## Errors during recovery

If something goes wrong during `Reinitialize` — e.g. the driver no longer supports your previously-requested sample rate — the call throws synchronously and the device returns to `Unconfigured` state with the cached options cleared. From there, you can either call `Init*` again with new options, or `Dispose` the device.

```c#
device.DriverResetRequest += (_, _) =>
{
    try
    {
        device.Stop();
        device.Reinitialize();
        device.Start();
    }
    catch (Exception ex)
    {
        Log.Warn($"ASIO recovery failed: {ex.Message}");
        device.Dispose();
        // Reopen, re-configure from scratch, or surface the error to the user.
    }
};
```

## What if I want to handle the reset some other way?

You don't have to call `Reinitialize`. The event is just a notification — you can respond however you want:

```c#
device.DriverResetRequest += (_, _) =>
{
    // Tell the user, dispose the device, prompt them to pick a new driver.
    Dispatcher.Invoke(() => MessageBox.Show("ASIO driver settings changed. Restart playback."));
    device.Dispose();
};
```

You can also ignore the event entirely. The driver will keep running, but with whatever settings it had before — which may now disagree with the user's expectations. The recommended path is to reinitialize.

## Related driver-message events

`DriverResetRequest` is the event that requires action. Two adjacent driver messages surface separately because they don't:

```c#
device.LatenciesChanged += (_, _) =>
{
    // The driver's reported input/output latency changed (e.g. after SetClockSource).
    // No buffer rebuild needed — re-read the values and refresh any UI that shows them.
    Log.Info($"ASIO latency changed: in={device.InputLatencySamples}, out={device.OutputLatencySamples}");
};

device.ResyncOccurred += (_, _) =>
{
    // The driver detected a clock dropout / xrun. Informational only.
    Log.Warn("ASIO driver reported a resync request (likely a dropout).");
};
```

Both fire on the captured `SynchronizationContext`, same as `DriverResetRequest`. Neither requires you to stop or reinitialize the device — the audio engine keeps running.

## Why the legacy `AsioOut` API didn't have this

`AsioOut` exposed a `DriverResetRequest` event from NAudio 2 onwards but offered no supported recovery path — you'd have to dispose the `AsioOut` instance and rebuild everything from scratch in the user's code. NAudio 3 fixes this by giving `AsioDevice` an internal record of the last-applied options and a `Reinitialize` method that re-applies them.

The `AsioOut` facade in NAudio 3 still has the same limitation — its public surface is preserved bit-for-bit, so there's no supported recovery method on it. To use `Reinitialize`, switch to `AsioDevice` directly. See [AsioMigration](AsioMigration.md) for moving to it.
