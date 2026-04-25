# ASIO Channel Mapping

Pro audio interfaces routinely expose 8, 16, 32, or more physical channels. NAudio 3's `AsioDevice` lets you route any subset of them — contiguous or not — through a simple `int[]` of physical channel indices. This article covers the common mapping patterns.

For mode-specific docs see [AsioPlayback](AsioPlayback.md), [AsioRecording](AsioRecording.md), and [AsioDuplex](AsioDuplex.md).

## The mental model

Three options classes carry the same two channel arrays:

| Options class | Input array | Output array |
| --- | --- | --- |
| `AsioPlaybackOptions` | (n/a) | `OutputChannels` |
| `AsioRecordingOptions` | `InputChannels` | (n/a) |
| `AsioDuplexOptions` | `InputChannels` | `OutputChannels` |

Each entry is a **physical channel index**, zero-based, in the range `[0, NbInputChannels)` or `[0, NbOutputChannels)` reported by `device.Capabilities`.

The order matters. In your callback (`AudioCaptured.GetChannel(i)` or `AsioProcessBuffers.GetInput(i)` / `GetOutput(i)`), `i` is an index **into your selected-channels array**, not a physical channel number. That makes channel-handling code portable across mappings.

## Discovering the device

```c#
using var device = AsioDevice.Open(driverName);

Console.WriteLine($"Driver: {device.DriverName}");
Console.WriteLine($"Inputs:  {device.Capabilities.NbInputChannels}");
Console.WriteLine($"Outputs: {device.Capabilities.NbOutputChannels}");

for (int i = 0; i < device.Capabilities.NbInputChannels; i++)
    Console.WriteLine($"  in  {i}: {device.Capabilities.InputChannelInfos[i].name}");

for (int i = 0; i < device.Capabilities.NbOutputChannels; i++)
    Console.WriteLine($"  out {i}: {device.Capabilities.OutputChannelInfos[i].name}");
```

`InputChannelInfos[i].name` and `OutputChannelInfos[i].name` are the driver's human-readable labels (e.g. "Mic 1", "ADAT 3", "Headphone L").

## Common patterns

### Stereo to the first two outputs

```c#
OutputChannels = [0, 1]
// or just leave it null — defaults to the contiguous range matching Source.WaveFormat.Channels
```

### Stereo to a different output pair

```c#
// Route the file's left/right channels to physical outputs 4 and 5.
OutputChannels = [4, 5]
```

This was `AsioOut.ChannelOffset = 4` in NAudio 2; the array form replaces it.

### Stereo to non-adjacent outputs

```c#
// Left channel → output 0, right channel → output 7.
// The legacy ChannelOffset API couldn't express this — the new array can.
OutputChannels = [0, 7]
```

Source channel `n` is routed to physical output `OutputChannels[n]`. Outputs not listed receive silence (the library zeroes them before native conversion).

### Mono to multiple outputs

There's no direct "send mono to N outputs" mode — the source's channel count must match the array length. Use a `MonoToStereoSampleProvider` (or chain custom providers) to expand the source channel count first:

```c#
ISampleProvider mono = new AudioFileReader("voice.wav").ToSampleProvider();
ISampleProvider stereo = new MonoToStereoSampleProvider(mono);

device.InitPlayback(new AsioPlaybackOptions
{
    Source = stereo.ToWaveProvider(),
    OutputChannels = [4, 5]
});
```

### Recording every input

```c#
device.InitRecording(new AsioRecordingOptions
{
    InputChannels = device.Capabilities.AllInputChannels,
    SampleRate    = device.CurrentSampleRate
});
```

`Capabilities.AllInputChannels` is shorthand for `Enumerable.Range(0, NbInputChannels).ToArray()`. There's also `AllOutputChannels`.

### Recording a non-contiguous subset

```c#
// Skip the inputs you don't want to record.
InputChannels = [0, 1, 4, 5, 8, 9]
```

The callback receives them in the order listed: `GetChannel(0)` is physical input 0, `GetChannel(1)` is physical input 1, `GetChannel(2)` is physical input 4, and so on.

### Duplex monitoring with input → output 1:1 routing

```c#
device.InitDuplex(new AsioDuplexOptions
{
    InputChannels  = [0, 1],
    OutputChannels = [0, 1],
    SampleRate     = 48000,
    Processor      = (in AsioProcessBuffers b) =>
    {
        for (int ch = 0; ch < b.InputChannelCount; ch++)
            b.GetInput(ch).CopyTo(b.GetOutput(ch));
    }
});
```

### Duplex with crossed channels

The processor sees inputs and outputs through whatever index scheme you want. For an L/R swap, use the same channel arrays but read/write across:

```c#
Processor = (in AsioProcessBuffers b) =>
{
    b.GetInput(0).CopyTo(b.GetOutput(1));   // physical in 0 → physical out 1
    b.GetInput(1).CopyTo(b.GetOutput(0));   // physical in 1 → physical out 0
};
```

### Duplex with asymmetric channel counts

You can capture from N inputs and drive M outputs where N ≠ M — the inputs and outputs are independent of each other.

```c#
device.InitDuplex(new AsioDuplexOptions
{
    InputChannels  = [0, 1, 2, 3],   // capture 4 mics
    OutputChannels = [0, 1],          // drive a stereo monitor mix
    SampleRate     = 48000,
    Processor      = (in AsioProcessBuffers b) =>
    {
        var outL = b.GetOutput(0);
        var outR = b.GetOutput(1);
        for (int i = 0; i < b.Frames; i++)
        {
            float l = (b.GetInput(0)[i] + b.GetInput(2)[i]) * 0.5f;
            float r = (b.GetInput(1)[i] + b.GetInput(3)[i]) * 0.5f;
            outL[i] = l;
            outR[i] = r;
        }
    }
});
```

## Validation

Each `Init*` call validates the channel arrays before configuring the driver. Bad inputs throw synchronously:

- Duplicate index in the array → `ArgumentException`
- Index out of range for the driver's channel count → `ArgumentOutOfRangeException`
- Empty array → `ArgumentException`
- Mismatched source channel count vs `OutputChannels.Length` (playback only) → `ArgumentException`
- Mixed native sample types across selected channels (some drivers can do this) → `NotSupportedException`

These all fire from the calling thread, before any buffer-switch starts. There's no way for a bad mapping to corrupt audio at runtime.

## Why the library uses arrays of indices

The legacy `AsioOut.ChannelOffset` / `InputChannelOffset` model could only express contiguous ranges starting at the offset. That's a real limitation for studios with patch bays, surround setups, or interfaces that expose sparse useful inputs.

The array model:

- Allows non-contiguous selection — `[0, 3, 5, 7]` works.
- Makes "all channels" trivial: `device.Capabilities.AllInputChannels`.
- Documents the routing in source code where someone reading the call site can see it.
- Maps cleanly to JUCE/PortAudio conventions, which is what experienced cross-platform pro-audio developers expect.
