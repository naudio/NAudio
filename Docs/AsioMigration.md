# Migrating from `AsioOut` to `AsioDevice`

NAudio 3 introduces a redesigned ASIO API, `AsioDevice`. The legacy `AsioOut` class is preserved with a byte-for-byte identical public surface and is now implemented as a thin facade over `AsioDevice` — so existing code keeps working without changes. This article shows how to move new code (and selectively migrate existing code) to `AsioDevice` to gain access to features the old `AsioOut` API can't express.

For mode-specific tutorials see [AsioPlayback](AsioPlayback.md), [AsioRecording](AsioRecording.md), and [AsioDuplex](AsioDuplex.md).

## Should I migrate?

Stay on `AsioOut` if all of these are true:

- You only need stereo (or contiguous-range) playback through `IWaveProvider`.
- You're happy treating ASIO as `IWavePlayer` and don't need per-channel `Span<float>`.
- You don't need to recover from `DriverResetRequest` (it has no supported path on `AsioOut`).

Move to `AsioDevice` if any of these are true:

- You record or do duplex DSP — the new API replaces the awkward `InitRecordAndPlayback` overload.
- You need non-contiguous channel selection like `[0, 3, 5]` — `AsioOut.ChannelOffset` only supports contiguous ranges.
- You want `Span<float>` access to recorded audio without writing `unsafe` code.
- You want a supported `Reinitialize` path for handling sample-rate changes from the driver's control panel.
- You want explicit, fail-fast configuration validation rather than callback-time `NullReferenceException`.

## What the facade gives you for free

Because NAudio 3's `AsioOut` is a facade over `AsioDevice`, two reliability fixes apply even to legacy code:

- The `AutoStop`-on-end-of-stream path no longer hangs the UI when `PlaybackStopped` handlers do work on the same thread that triggered the stop. The auto-stop now defers to a thread-pool worker.
- `Dispose` synchronizes with any in-flight buffer-switch callback before releasing the COM driver.

These were Phase 0 audit findings on the original NAudio 2.x `AsioOut`; the facade inherits the fixes from `AsioDevice` without changing the public surface.

## API translation table

| `AsioOut` (NAudio 2) | `AsioDevice` (NAudio 3) |
| --- | --- |
| `new AsioOut(driverName)` | `AsioDevice.Open(driverName)` |
| `new AsioOut(index)` | `AsioDevice.Open(index)` |
| `AsioOut.GetDriverNames()` | `AsioDevice.GetDriverNames()` |
| `Init(waveProvider)` | `InitPlayback(new AsioPlaybackOptions { Source = waveProvider })` |
| `InitRecordAndPlayback(provider, n, sr)` with non-null provider | `InitDuplex(new AsioDuplexOptions { ... Processor = ... })` |
| `InitRecordAndPlayback(null, n, sr)` | `InitRecording(new AsioRecordingOptions { ... })` |
| `ChannelOffset = x` (with `c`-channel source) | `OutputChannels = [x, x+1, ..., x+c-1]` |
| `InputChannelOffset = y` + `recordChannels = n` | `InputChannels = [y, y+1, ..., y+n-1]` |
| `Play()` | `Start()` |
| `Pause()` | (no direct equivalent — see below) |
| `Stop()` | `Stop()` |
| `Dispose()` | `Dispose()` |
| `AudioAvailable` event (`IntPtr[]` buffers) | `AudioCaptured` event (`Span<float>` per channel) |
| `PlaybackStopped` event | `Stopped` event |
| `DriverResetRequest` event | `DriverResetRequest` event (with supported `Reinitialize` recovery) |
| `DriverInputChannelCount` | `Capabilities.NbInputChannels` |
| `DriverOutputChannelCount` | `Capabilities.NbOutputChannels` |
| `FramesPerBuffer` | `FramesPerBuffer` |
| `PlaybackLatency` | `OutputLatencySamples` |
| `IsSampleRateSupported(rate)` | `IsSampleRateSupported(rate)` |
| `ShowControlPanel()` | `ShowControlPanel()` |
| `AsioInputChannelName(i)` | `Capabilities.InputChannelInfos[i].name` |
| `AsioOutputChannelName(i)` | `Capabilities.OutputChannelInfos[i].name` |
| `OutputWaveFormat` | (none — `AsioDevice` is per-channel `Span<float>`, no aggregated `WaveFormat`) |
| `Volume` (obsolete, set on input stream instead) | (no equivalent — set volume on your `IWaveProvider`) |
| `HasReachedEnd` | (no equivalent — wire up `Stopped` event with `AutoStopOnEndOfStream`) |
| `AutoStop` | `AsioPlaybackOptions.AutoStopOnEndOfStream` (default `true`) |

`AsioDevice` does **not** implement `IWavePlayer`. The interface doesn't fit record-only sessions or arbitrary-channel duplex. If your code paths need `IWavePlayer` polymorphism for playback specifically, keep using `AsioOut` for those.

There's no `Pause` on `AsioDevice` — pause has historically meant "stop the driver but don't raise stop events", which is straightforward to implement on top by calling `Stop` and tracking your own pause flag if you need it.

## Side-by-side examples

### Simple playback — both APIs work, no need to change

```c#
// NAudio 2 (still works in NAudio 3)
using var asioOut = new AsioOut(driverName);
asioOut.Init(audioFileReader);
asioOut.Play();
```

```c#
// NAudio 3 — equivalent
using var device = AsioDevice.Open(driverName);
device.InitPlayback(new AsioPlaybackOptions { Source = audioFileReader });
device.Start();
```

### Recording

```c#
// NAudio 2
var asioOut = new AsioOut(driverName);
asioOut.InputChannelOffset = 4;
asioOut.InitRecordAndPlayback(null, 2, 48000);
asioOut.AudioAvailable += (s, e) =>
{
    var samples = new float[e.SamplesPerBuffer * 2];
    e.GetAsInterleavedSamples(samples);
    writer.WriteSamples(samples, 0, samples.Length);
};
asioOut.Play();
```

```c#
// NAudio 3 — explicit channel array, no IntPtr work, no manual interleaving
using var device = AsioDevice.Open(driverName);
device.InitRecording(new AsioRecordingOptions
{
    InputChannels = [4, 5],
    SampleRate    = 48000
});
device.AudioCaptured += (s, e) =>
{
    for (int i = 0; i < e.Frames; i++)
    {
        writer.WriteSample(e.GetChannel(0)[i]);
        writer.WriteSample(e.GetChannel(1)[i]);
    }
};
device.Start();
```

### Duplex (record + process + play)

This is the largest improvement. The legacy idiom abused the recording event to write outputs:

```c#
// NAudio 2 — abuse of AudioAvailable + WrittenToOutputBuffers + unsafe IntPtr work
var asioOut = new AsioOut(driverName);
asioOut.InitRecordAndPlayback(null, 2, 48000);
asioOut.AudioAvailable += (s, e) =>
{
    unsafe
    {
        float* inL  = (float*)e.InputBuffers[0];
        float* outL = (float*)e.OutputBuffers[0];
        for (int i = 0; i < e.SamplesPerBuffer; i++)
            outL[i] = inL[i] * 0.5f;
        // ... and the right channel ...
    }
    e.WrittenToOutputBuffers = true;
};
asioOut.Play();
```

```c#
// NAudio 3 — purpose-built duplex API, no unsafe, no flags
using var device = AsioDevice.Open(driverName);
device.InitDuplex(new AsioDuplexOptions
{
    InputChannels  = [0, 1],
    OutputChannels = [0, 1],
    SampleRate     = 48000,
    Processor      = (in AsioProcessBuffers b) =>
    {
        var inL = b.GetInput(0); var outL = b.GetOutput(0);
        var inR = b.GetInput(1); var outR = b.GetOutput(1);
        for (int i = 0; i < b.Frames; i++)
        {
            outL[i] = inL[i] * 0.5f;
            outR[i] = inR[i] * 0.5f;
        }
    }
});
device.Start();
```

### Recovering from a sample-rate change in the driver control panel

```c#
// NAudio 2 — no supported recovery; user code has to dispose and rebuild
asioOut.DriverResetRequest += (_, _) => { /* dispose + recreate by hand */ };
```

```c#
// NAudio 3 — the device remembers the last config and re-applies it
device.DriverResetRequest += (_, _) =>
{
    device.Stop();
    device.Reinitialize();
    device.Start();
};
```

See [AsioDriverReset](AsioDriverReset.md) for more.

## Things that no longer work the same way

A small number of behaviors don't have a 1:1 mapping:

- **`OutputWaveFormat`.** `AsioOut` exposed an aggregated `WaveFormat` describing the configured output. `AsioDevice` doesn't aggregate — each channel is per-channel `Span<float>`, and the driver's native format is per-channel via `Capabilities.OutputChannelInfos[i].type`.
- **`HasReachedEnd`.** Replaced by the `Stopped` event combined with `AsioPlaybackOptions.AutoStopOnEndOfStream`. Subscribe to `Stopped` to know when end-of-stream was reached.
- **Init can't be called twice on the same instance.** Same as `AsioOut`. Dispose and create a new device to switch modes. (`Reinitialize` re-applies the *same* configuration; it doesn't switch modes.)
- **Synchronous `PlaybackStopped` from `Stop()`.** Both APIs dispatch via the captured `SynchronizationContext` (asynchronous when there is one). On a UI thread this is the same observable behavior; on a console thread without a sync context, the dispatch goes to the thread pool.

## Breaking changes within `AsioOut` itself

`AsioOut`'s public surface is byte-for-byte identical to NAudio 2.x — there are no breaking changes. A snapshot test in `NAudioTests` ([AsioOutPublicSurfaceTests](../NAudioTests/Asio/AsioOutPublicSurfaceTests.cs)) pins this; if a future change breaks the surface, the test catches it.

The internal implementation routes through `AsioDevice`, picking up the F1 (auto-stop deadlock) and F2 (dispose drain) fixes, but the contract you call against is unchanged.
