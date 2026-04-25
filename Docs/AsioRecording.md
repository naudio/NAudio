# Recording with ASIO

NAudio 3's `AsioDevice` exposes a clean recording mode that delivers per-channel `Span<float>` to your event handler — no `IntPtr` arithmetic, no manual sample-format decoding, no interleaving math. This article covers recording only — see [AsioPlayback](AsioPlayback.md), [AsioDuplex](AsioDuplex.md), and [AsioMigration](AsioMigration.md) for related modes and migration guidance.

You need a soundcard with an ASIO driver installed. [ASIO4ALL](http://asio4all.com/) is a free fallback for hardware that doesn't ship one.

## Open the device

```c#
foreach (var name in AsioDevice.GetDriverNames())
    Console.WriteLine(name);

using var device = AsioDevice.Open("Focusrite USB ASIO");
```

## Pick input channels

`AsioRecordingOptions.InputChannels` is an `int[]` of physical channel indices. Entries can be **non-contiguous** — record from channels `[0, 1, 4, 5]` directly without recording the channels in between.

```c#
device.InitRecording(new AsioRecordingOptions
{
    InputChannels = [0, 1, 4, 5],
    SampleRate    = device.CurrentSampleRate
});
```

To record every available input:

```c#
InputChannels = device.Capabilities.AllInputChannels
```

`device.Capabilities.NbInputChannels` reports how many physical inputs the driver exposes. `device.Capabilities.InputChannelInfos[i].name` gives the driver's name for each (e.g. "Mic 1", "Line 3").

If you don't pass `SampleRate`, the device runs at whatever rate the driver is currently set to — which `device.CurrentSampleRate` reports. If you pass a specific rate, the driver must support it; check first with `device.IsSampleRateSupported(rate)`.

See [AsioChannelMapping](AsioChannelMapping.md) for the rationale and patterns around non-contiguous channel selection.

## Subscribe to AudioCaptured

Each ASIO buffer-switch raises `AudioCaptured` on the real-time driver thread. The event args expose one `ReadOnlySpan<float>` per selected input, in the same order as `InputChannels`. NAudio handles the native `AsioSampleType` → float conversion (`Int16LSB`, `Int24LSB`, `Int32LSB`, `Float32LSB` are all supported transparently).

```c#
device.AudioCaptured += (sender, e) =>
{
    // e.GetChannel(i) returns a ReadOnlySpan<float> for the i'th selected input.
    var ch0 = e.GetChannel(0);   // physical input InputChannels[0]
    var ch1 = e.GetChannel(1);   // physical input InputChannels[1]

    // Compute RMS, write to a file, push to a ring buffer — anything quick.
    // The spans are valid only for the duration of this handler.
};

device.Start();
```

The index passed to `GetChannel` is into the **selected-channels array**, not the physical channel number. If `InputChannels = [4, 5]`, then `GetChannel(0)` returns physical input 4 and `GetChannel(1)` returns physical input 5. This matches JUCE/PortAudio conventions and keeps your handler portable across channel selections.

## Real-time thread constraints

The handler runs on the ASIO callback thread. To avoid glitches:

- Don't allocate. Pre-allocate any buffers you need before calling `Start`.
- Don't perform blocking I/O. Hand the data off to a worker thread or a lock-free queue.
- Don't call `device.Stop`, `device.Dispose`, or `device.Reinitialize` from inside the handler — `Stop` actively throws `InvalidOperationException` if you try, because the same-thread call would self-deadlock waiting for the callback to return.

The spans returned by `GetChannel` point into library-owned buffers that the device reuses across callbacks. Copy out anything you need to keep beyond the handler's return.

## Save each input to its own WAV file

```c#
using var device = AsioDevice.Open(driverName);
int[] channels = [0, 1, 4, 5];
int sampleRate = device.CurrentSampleRate;

device.InitRecording(new AsioRecordingOptions
{
    InputChannels = channels,
    SampleRate    = sampleRate
});

var writers = channels
    .Select(phys => new WaveFileWriter(
        $"input-{phys}.wav",
        WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1)))
    .ToArray();

device.AudioCaptured += (s, e) =>
{
    for (int i = 0; i < e.ChannelCount; i++)
        writers[i].WriteSamples(e.GetChannel(i));
};

device.Stopped += (s, e) =>
{
    foreach (var w in writers) w.Dispose();
};

device.Start();
Console.ReadLine();
device.Stop();
```

## Save selected inputs as a single multi-channel WAV

If you want to interleave the selected inputs into one WAV file:

```c#
using var writer = new WaveFileWriter(
    "multi.wav",
    WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels.Length));

device.AudioCaptured += (s, e) =>
{
    for (int frame = 0; frame < e.Frames; frame++)
        for (int ch = 0; ch < e.ChannelCount; ch++)
            writer.WriteSample(e.GetChannel(ch)[frame]);
};
```

## The raw escape hatch

If you genuinely need zero-copy access to the driver's native bytes — for example, to memcpy them into a fixed buffer for a downstream codec — `AsioAudioCapturedEventArgs.RawInput(i)` returns an `AsioRawInputBuffer`:

```c#
device.AudioCaptured += (s, e) =>
{
    var raw = e.RawInput(0);            // ref struct
    ReadOnlySpan<byte> bytes = raw.Bytes;
    AsioSampleType format = raw.Format; // Int16LSB / Int24LSB / Int32LSB / Float32LSB
    int frames = raw.Frames;
    // ... your zero-copy logic ...
};
```

The bytes span is valid only for the duration of the handler.

## Stop and dispose

```c#
device.Stop();
device.Dispose();   // or wrap the device in `using`
```

The `Stopped` event fires on the captured `SynchronizationContext` after the callback thread has fully drained, so it's safe for handlers to dispose the device.
