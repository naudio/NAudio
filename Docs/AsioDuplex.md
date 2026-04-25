# Duplex Processing with ASIO

Duplex is the mode where you read input and write output in a single buffer-switch callback — the foundation for low-latency monitoring, software effects, software-defined I/O, and live DSP. NAudio 3's `AsioDevice.InitDuplex` makes this a first-class API: a per-callback `AsioProcessBuffers` view over per-channel `Span<float>` for both directions, no `IntPtr` arithmetic, no flag dance.

For playback-only or recording-only use cases see [AsioPlayback](AsioPlayback.md) and [AsioRecording](AsioRecording.md). For migration from the old `InitRecordAndPlayback` pattern see [AsioMigration](AsioMigration.md).

## The processor callback

A duplex session is configured with three things: which inputs to capture, which outputs to drive, and a `Processor` delegate that does the work. The delegate runs on the ASIO real-time thread once per buffer-switch.

```c#
using var device = AsioDevice.Open("Focusrite USB ASIO");

device.InitDuplex(new AsioDuplexOptions
{
    InputChannels  = [0, 1],
    OutputChannels = [0, 1],
    SampleRate     = 48000,
    Processor      = (in AsioProcessBuffers b) =>
    {
        var inL  = b.GetInput(0);
        var inR  = b.GetInput(1);
        var outL = b.GetOutput(0);
        var outR = b.GetOutput(1);
        for (int i = 0; i < b.Frames; i++)
        {
            outL[i] = inL[i] * 0.5f;
            outR[i] = inR[i] * 0.5f;
        }
    }
});

device.Start();
Console.ReadLine();
device.Stop();
```

That's a complete monitoring passthrough with -6dB attenuation.

## How channel indexing works

`GetInput(i)` and `GetOutput(i)` are zero-based **into the selected-channels arrays**, not into the physical channel space. If `InputChannels = [4, 5]` and `OutputChannels = [0, 1]`, then `GetInput(0)` is physical input 4 and `GetOutput(0)` is physical output 0. This makes processor code reusable across channel configurations and is consistent with the recording-mode `GetChannel` semantics.

You can recover the physical channel number from `options.InputChannels[i]` if you need it.

## What `AsioProcessBuffers` exposes

`AsioProcessBuffers` is a `ref struct` — it lives on the stack and cannot escape the processor callback. That's enforced by the compiler.

| Member | Description |
| --- | --- |
| `int Frames` | Number of frames in this buffer (constant per session, equals `device.FramesPerBuffer`). |
| `int SampleRate` | Sample rate in Hz. |
| `int InputChannelCount` / `OutputChannelCount` | Counts of the selected channel arrays. |
| `long SamplePosition` | Position of the first frame in this buffer, in frames since `Start`. |
| `ReadOnlySpan<float> GetInput(int)` | Per-channel float input. Length = `Frames`. |
| `Span<float> GetOutput(int)` | Per-channel float output. Length = `Frames`. |
| `AsioRawInputBuffer RawInput(int)` | Zero-copy native bytes + format. |
| `AsioRawOutputBuffer RawOutput(int)` | Zero-copy writable native bytes + format (bypasses float conversion). |

All spans point into library-owned or driver-owned memory and are invalidated when the callback returns. Don't store them, don't pass them to another thread.

## Output channel zero-fill

The library zeroes every selected output channel's float buffer **before** invoking your processor. So if you don't write to a particular `GetOutput(i)`, that physical output goes silent for this buffer. You don't need to explicitly clear it.

After your processor returns, the library converts each output float buffer to the driver's native format. The exception is channels you accessed via `RawOutput(i)` — those are taken to be raw passthrough and the float-conversion step is skipped (see [The raw escape hatch](#the-raw-escape-hatch) below).

## Real-time thread constraints

The processor runs on the ASIO callback thread. The time budget is the buffer duration — typically 1ms to 20ms. To stay in budget:

- **Don't allocate.** Allocate every buffer, queue, FFT plan, etc. before calling `Start`. The processor must be allocation-free; this is a documented and benchmark-gated property of the path itself.
- **Don't perform blocking I/O.** Disk reads, network calls, locks contended with non-realtime threads — all forbidden.
- **Don't call `device.Stop`, `device.Dispose`, or `device.Reinitialize`** from inside the processor. `Stop` actively throws `InvalidOperationException` if you try, because calling `driver.Stop()` from the same thread the driver is using to invoke the callback would self-deadlock. If you need to stop in response to something the processor observed, set a flag and stop from another thread.

## A simple effect: stereo gain + L/R swap

```c#
device.InitDuplex(new AsioDuplexOptions
{
    InputChannels  = [0, 1],
    OutputChannels = [0, 1],
    SampleRate     = 48000,
    Processor      = (in AsioProcessBuffers b) =>
    {
        var inL  = b.GetInput(0);
        var inR  = b.GetInput(1);
        var outL = b.GetOutput(0);
        var outR = b.GetOutput(1);
        const float gain = 0.7f;
        for (int i = 0; i < b.Frames; i++)
        {
            outL[i] = inR[i] * gain;   // right input → left output
            outR[i] = inL[i] * gain;   // left input → right output
        }
    }
});
```

## Many-input, one-output mix

```c#
int[] inputs = [0, 1, 2, 3, 4, 5, 6, 7];

device.InitDuplex(new AsioDuplexOptions
{
    InputChannels  = inputs,
    OutputChannels = [0],
    SampleRate     = 48000,
    Processor      = (in AsioProcessBuffers b) =>
    {
        var mix = b.GetOutput(0);
        for (int ch = 0; ch < b.InputChannelCount; ch++)
        {
            var src = b.GetInput(ch);
            for (int i = 0; i < b.Frames; i++)
                mix[i] += src[i];
        }
        // Optional: scale by 1/N to avoid clipping.
        float invN = 1f / b.InputChannelCount;
        for (int i = 0; i < b.Frames; i++) mix[i] *= invN;
    }
});
```

(Note that `mix` starts zeroed — the library zeros output buffers before each call.)

## The raw escape hatch

For zero-copy passthrough — say you want to memcpy native bytes from one channel to another without the float round-trip — call `RawInput` / `RawOutput`:

```c#
Processor = (in AsioProcessBuffers b) =>
{
    var src = b.RawInput(0);    // ReadOnlySpan<byte>, AsioSampleType.Float32LSB say
    var dst = b.RawOutput(0);   // Span<byte>, same format

    if (src.Format == dst.Format)
        src.Bytes.CopyTo(dst.Bytes);
    else
        // formats differ — fall back to the float path or convert manually
        ...
};
```

Calling `RawOutput(i)` marks that output channel as "raw-handled" — the library skips its float→native conversion, trusting you to write every frame yourself. Mixing modes is allowed: handle some channels via `GetOutput` (float path) and others via `RawOutput` (raw path) in the same callback.

## Latency

`device.InputLatencySamples` and `device.OutputLatencySamples` report the driver's reported latencies in frames after `InitDuplex` succeeds. The round-trip latency for monitoring is the sum, plus your processor's compute time.

## Buffer size

`AsioDuplexOptions.BufferSize` is a frame count, or `null` to use the driver's preferred size. The driver typically supports a power-of-two range — `device.Capabilities.BufferMinSize`, `BufferMaxSize`, `BufferGranularity` describe the constraints.

Smaller buffers = lower latency = more callback overhead. 64–256 frames is typical for live monitoring; 512–1024 frames is typical for software DAW work where a few extra ms is fine.

## Recovering from driver resets

If the user changes the sample rate in the driver's control panel, the device fires `DriverResetRequest`. The recommended response:

```c#
device.DriverResetRequest += (_, _) =>
{
    device.Stop();
    device.Reinitialize();
    device.Start();
};
```

`Reinitialize()` re-applies the cached duplex options, including the same `Processor` delegate, against the new driver state. See [AsioDriverReset](AsioDriverReset.md) for the full pattern.
