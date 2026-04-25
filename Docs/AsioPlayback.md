# Playback with ASIO

NAudio 3 introduces `AsioDevice`, a redesigned ASIO API that handles playback, recording, and duplex I/O through three explicit configuration modes. This article covers playback only — see [AsioRecording](AsioRecording.md) and [AsioDuplex](AsioDuplex.md) for the other modes, and [AsioMigration](AsioMigration.md) if you're moving from the legacy `AsioOut` class.

ASIO is the low-latency driver format supported by most professional Windows audio interfaces and many DAW applications. To use it you need a soundcard with an ASIO driver installed. If your hardware doesn't ship one, [ASIO4ALL](http://asio4all.com/) is a free WDM-to-ASIO shim that works with most consumer soundcards.

## Open the device

Enumerate the installed ASIO drivers and open one by name:

```c#
foreach (var name in AsioDevice.GetDriverNames())
    Console.WriteLine(name);

using var device = AsioDevice.Open("Focusrite USB ASIO");
```

`AsioDevice` implements `IDisposable`. Always wrap it in a `using` statement (or call `Dispose` explicitly) — the underlying COM driver doesn't release until you do.

## Configure for playback

Pass an `IWaveProvider` (or wrap an `ISampleProvider` via `.ToWaveProvider()`) to `InitPlayback`:

```c#
using var reader = new AudioFileReader("music.wav");

device.InitPlayback(new AsioPlaybackOptions
{
    Source = reader
});
```

The source's sample rate must be one the driver supports — `device.IsSampleRateSupported(rate)` answers that. The source's channel count must equal the number of output channels you select (defaults to a contiguous range starting at channel 0).

## Select output channels

`AsioPlaybackOptions.OutputChannels` is an `int[]` of physical channel indices. Source channel `n` is routed to physical output `OutputChannels[n]`. The array can be **non-contiguous** — there's no `ChannelOffset` style restriction.

```c#
// Stereo source → physical outputs 4 and 5 (zero-based).
device.InitPlayback(new AsioPlaybackOptions
{
    Source = reader,
    OutputChannels = [4, 5]
});
```

To send to every available output:

```c#
OutputChannels = device.Capabilities.AllOutputChannels
```

`device.Capabilities.NbOutputChannels` tells you how many physical outputs the driver exposes; `device.Capabilities.OutputChannelInfos[i].name` gives a human-readable name for each.

See [AsioChannelMapping](AsioChannelMapping.md) for more channel-routing patterns.

## Start and stop

```c#
device.Start();
// ...
device.Stop();
```

`Stop()` raises the `Stopped` event on the captured `SynchronizationContext` (the thread you constructed the device on, typically the UI thread). The handler may safely call `Dispose()` — the device is fully off the ASIO callback thread by the time `Stopped` fires.

By default the device auto-stops when the source reaches end-of-stream. Set `AutoStopOnEndOfStream = false` in the options if you want the device to keep running on silent buffers after the source runs dry (e.g. so you can swap providers).

## Handle errors and end-of-stream

```c#
device.Stopped += (sender, e) =>
{
    if (e.Exception is not null)
        Console.WriteLine($"ASIO faulted: {e.Exception.Message}");
    else
        Console.WriteLine("Playback complete.");
};
```

`Stopped` fires exactly once per `Start`/`Stop` cycle, with `e.Exception` populated if the source threw or the driver reported an unrecoverable fault.

## Recover from driver settings changes

If the user opens the driver's control panel and changes the sample rate (or any other setting), the driver fires a reset request. The recommended response:

```c#
device.DriverResetRequest += (_, _) =>
{
    device.Stop();
    device.Reinitialize();
    device.Start();
};
```

`Reinitialize()` re-applies the most recent `InitPlayback` options against the (possibly changed) driver state — see [AsioDriverReset](AsioDriverReset.md) for the full pattern.

## Buffer size and latency

`AsioPlaybackOptions.BufferSize` accepts a frame count, or `null` to use the driver's preferred size. Smaller buffers mean lower latency but more callback overhead. The actual latencies in frames are reported by `device.OutputLatencySamples` after `InitPlayback` succeeds.

The buffer-switch callback runs on the ASIO driver's real-time thread, so any `IWaveProvider` in your chain must produce samples within the buffer duration. Allocations and slow I/O on that thread cause glitches.

## Full example

```c#
using NAudio.Wave;

using var reader = new AudioFileReader("music.wav");
using var device = AsioDevice.Open(AsioDevice.GetDriverNames()[0]);

device.InitPlayback(new AsioPlaybackOptions
{
    Source = reader,
    OutputChannels = [0, 1]
});

var done = new ManualResetEventSlim();
device.Stopped += (_, _) => done.Set();
device.Start();
done.Wait();
```
