# Mix the Microphone and System Audio into One Stream

One of the most frequently asked NAudio questions is some variant of *"how do I record (or live‑mix) the microphone and the system/loopback audio at the same time into a single stream?"* — usually to save a single WAV file, or to feed the mixed bytes into a video muxer. People reach for `MixingSampleProvider`, wire up `WasapiLoopbackCapture` and `WaveInEvent`, and hit an exception like:

```
ArgumentException: All mixer inputs must have the same WaveFormat
```

This tutorial explains *why* that happens and shows a complete, modern (NAudio 3) recipe that works.

## Why you can't just mix the two capture devices

`MixingSampleProvider` sums its inputs sample‑for‑sample. For that to be meaningful, **every input must have the same wave format**: the same sample rate, the same channel count, and 32‑bit IEEE float samples. Two capture devices almost never agree on this:

- **System audio** via `WasapiLoopbackCapture` (or [`WasapiRecorder`](WasapiRecorder.md) with `WithLoopbackCapture()`) is delivered as **IEEE float at the device mix format** — commonly 48 kHz, stereo.
- **The microphone** via `WaveInEvent` is typically **16‑bit PCM** at whatever you (or the driver) chose — commonly 44.1 kHz, mono.

So before mixing you have to bring both sources to a **common format**. The pipeline for each source is:

1. Buffer the incoming bytes in a `BufferedWaveProvider` (the capture callbacks and the mixer read on different threads).
2. Convert to `ISampleProvider` with `.ToSampleProvider()` — this normalises 8/16/24/32‑bit PCM **and** IEEE float to 32‑bit float for you.
3. Make the **channel count** match (e.g. `MonoToStereoSampleProvider` for a mono mic feeding a stereo mix).
4. Make the **sample rate** match with `WdlResamplingSampleProvider` (cross‑platform, no Media Foundation dependency).

Then add both adapted sources to a single `MixingSampleProvider` and read from it.

## A reusable capture-to-mixer adapter

This helper takes any `IWaveIn` (microphone, loopback, WASAPI) and exposes it as an `ISampleProvider` in a target format, ready to drop into a mixer:

```c#
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

/// Buffers an IWaveIn capture source and resamples/rechannels it to a target format.
class CaptureMixerInput
{
    private readonly BufferedWaveProvider buffer;

    public ISampleProvider SampleProvider { get; }

    public CaptureMixerInput(IWaveIn capture, WaveFormat targetFormat)
    {
        buffer = new BufferedWaveProvider(capture.WaveFormat)
        {
            // capture callbacks may briefly outrun the mixer; drop rather than throw
            DiscardOnBufferOverflow = true,
            // hand back silence when empty so the mixer never starves
            ReadFully = true,
        };
        capture.DataAvailable += (s, a) => buffer.AddSamples(a.Buffer, 0, a.BytesRecorded);

        // normalise bit depth -> float, then channels, then sample rate
        ISampleProvider provider = buffer.ToSampleProvider();
        provider = MatchChannels(provider, targetFormat.Channels);
        if (provider.WaveFormat.SampleRate != targetFormat.SampleRate)
            provider = new WdlResamplingSampleProvider(provider, targetFormat.SampleRate);

        SampleProvider = provider;
    }

    private static ISampleProvider MatchChannels(ISampleProvider provider, int channels)
    {
        if (provider.WaveFormat.Channels == channels) return provider;
        if (provider.WaveFormat.Channels == 1 && channels == 2)
            return new MonoToStereoSampleProvider(provider);
        if (provider.WaveFormat.Channels == 2 && channels == 1)
            return new StereoToMonoSampleProvider(provider);
        throw new NotSupportedException(
            $"No channel conversion from {provider.WaveFormat.Channels} to {channels} channels");
    }
}
```

## Wiring it up

Pick a target mix format (IEEE float is required by the mixer), create the two capture devices, wrap each in the adapter, and add them to a `MixingSampleProvider`:

```c#
// the format everything is mixed into
var mixFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);

var systemCapture = new WasapiLoopbackCapture();          // system audio (float, e.g. 48k stereo)
var micCapture = new WaveInEvent { WaveFormat = new WaveFormat(44100, 16, 1) }; // mic (16-bit mono)

var system = new CaptureMixerInput(systemCapture, mixFormat);
var mic = new CaptureMixerInput(micCapture, mixFormat);

var mixer = new MixingSampleProvider(new[] { system.SampleProvider, mic.SampleProvider })
{
    // keep producing audio (silence) even when one source has no data,
    // e.g. loopback delivers nothing while no system audio is playing
    ReadFully = true,
};

systemCapture.StartRecording();
micCapture.StartRecording();
```

### Writing the mix to a WAV file

The mixer is just an `ISampleProvider`, so you pull from it on a background thread and write to a `WaveFileWriter`. Reading from the mixer is what *drives* the whole pipeline:

```c#
var writer = new WaveFileWriter("mixed.wav", mixer.WaveFormat);
var buffer = new float[mixFormat.SampleRate * mixFormat.Channels]; // ~1 second
var stop = false;

var pump = Task.Run(() =>
{
    while (!stop)
    {
        int read = mixer.Read(buffer, 0, buffer.Length);
        if (read > 0) writer.WriteSamples(buffer, 0, read);
        else Thread.Sleep(5); // shouldn't happen with ReadFully, but be safe
    }
});

// ... record for as long as you want ...

stop = true;
pump.Wait();
systemCapture.StopRecording();
micCapture.StopRecording();
systemCapture.Dispose();
micCapture.Dispose();
writer.Dispose();
```

### Getting the mixed bytes instead of a file

If you're feeding a video muxer (the AVI scenario in [#761](https://github.com/naudio/NAudio/issues/761)) you want raw bytes rather than a WAV file. Wrap the mixer in a `SampleToWaveProvider16` (16‑bit PCM) or `SampleToWaveProvider` (32‑bit float) and read into a `byte[]`:

```c#
IWaveProvider output = new SampleToWaveProvider16(mixer); // mixer.WaveFormat -> 16-bit PCM
var bytes = new byte[output.WaveFormat.AverageBytesPerSecond / 10]; // 100ms chunk

int bytesRead = output.Read(bytes, 0, bytes.Length);
// hand bytes[0..bytesRead] to your encoder / muxer
```

`output.WaveFormat` is the format your muxer should be told about.

## Things to watch out for

- **Clipping.** Summing two loud sources can exceed `±1.0f`. Reduce the inputs before mixing — `MonoToStereoSampleProvider` exposes `LeftVolume`/`RightVolume`, or insert a `VolumeSampleProvider` per source.
- **Loopback silence.** `WasapiLoopbackCapture` only raises `DataAvailable` while audio is actually playing. `ReadFully = true` on both the `BufferedWaveProvider` and the `MixingSampleProvider` keeps the output flowing (as silence) through those gaps so the two sources stay roughly aligned.
- **Clock drift.** The microphone and the soundcard are driven by independent clocks, so over long recordings they drift apart by fractions of a percent. `WdlResamplingSampleProvider` does a fixed‑ratio resample; it doesn't dynamically track drift. For short clips this is inaudible, but for long sessions where A/V sync matters you may need to monitor each `BufferedWaveProvider`'s `BufferedDuration` and adjust.
- **Disposal order.** Stop both captures, let the pump loop finish, *then* dispose the capture devices and the writer. Disposing a `WasapiCapture`/`WaveOut` while another thread is still touching it has caused crashes (see [#1183](https://github.com/naudio/NAudio/issues/1183) and [#1184](https://github.com/naudio/NAudio/issues/1184)).
- **Modern capture.** You can use [`WasapiRecorder`](WasapiRecorder.md) for both sources — one builder with `WithLoopbackCapture()` for system audio and one for the microphone — instead of `WasapiLoopbackCapture` + `WaveInEvent`. The mixing pipeline above is identical; only the capture objects change.

## Mixing vs. separate channels

If your goal is not to *sum* the two sources but to keep them **separate** — for example microphone on the left channel and system audio on the right ([#1220](https://github.com/naudio/NAudio/issues/1220)) — use `MultiplexingSampleProvider` (or `MultiplexingWaveProvider`) instead of `MixingSampleProvider`. You still bring both sources to a common sample rate first, but you map input channels to output channels rather than adding them together.

## Related questions

This question comes up regularly:

- [#761 – Get mixed byte-stream from WasapiLoopbackCapture and microphone](https://github.com/naudio/NAudio/issues/761)
- [#405 – How to record system sound and microphone input sound at the same time?](https://github.com/naudio/NAudio/issues/405)
- [#1169 – Can this library record the microphone and computer audio at the same time?](https://github.com/naudio/NAudio/issues/1169)
- [#1220 – Record microphone and system sound into the left/right channels of one WAV](https://github.com/naudio/NAudio/issues/1220) (use multiplexing rather than mixing)
