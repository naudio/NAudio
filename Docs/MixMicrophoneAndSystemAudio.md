# Mix the Microphone and System Audio into One Stream

One of the most frequently asked NAudio questions is some variant of *"how do I record (or live‑mix) the microphone and the system/loopback audio at the same time into a single stream?"* — usually to save a single WAV file, or to feed the mixed bytes into a video muxer. People reach for `MixingSampleProvider`, wire up `WasapiLoopbackCapture` and `WaveInEvent`, and hit an exception like:

```
ArgumentException: All mixer inputs must have the same WaveFormat
```

This tutorial explains *why* that happens and shows a complete, modern (NAudio 3) recipe that works.

## Why you can't just mix the two capture devices

`MixingSampleProvider` sums its inputs sample‑for‑sample. For that to be meaningful, **every input must have the same wave format**: the same sample rate, the same channel count, and 32‑bit IEEE float samples. Two capture devices almost never agree on this:

- **System audio** — captured with [`WasapiRecorder`](WasapiRecorder.md) and `WithLoopbackCapture()` (or the legacy `WasapiLoopbackCapture`) — is delivered as **IEEE float at the render device's mix format**, commonly 48 kHz, stereo.
- **The microphone** — captured with `WasapiRecorder` (or a legacy `WaveInEvent`) — has the **capture device's mix format**, often a different sample rate and channel count (e.g. 44.1 kHz, mono). A legacy `WaveInEvent` mic is usually **16‑bit PCM** as well.

So before mixing you have to bring both sources to a **common format**. The pipeline for each source is:

1. Buffer the incoming bytes in a `BufferedWaveProvider` (the capture callbacks and the mixer read on different threads).
2. Convert to `ISampleProvider` with `.ToSampleProvider()` — this normalises 8/16/24/32‑bit PCM **and** IEEE float to 32‑bit float for you.
3. Make the **channel count** match (e.g. `MonoToStereoSampleProvider` for a mono mic feeding a stereo mix).
4. Make the **sample rate** match with `WdlResamplingSampleProvider` (cross‑platform, no Media Foundation dependency).

Then add both adapted sources to a single `MixingSampleProvider` and read from it.

## Use the packaged helper (NAudio.Extras)

You don't have to write any of this yourself. **`NAudio.Extras` ships `CaptureMixerInput` and `RealtimeCaptureMixer`**, which implement exactly this pipeline — including timestamp-based alignment so independently-clocked sources don't drift apart (see [Keeping the sources aligned](#keeping-the-sources-aligned)). The runnable **"Mixing Capture" panel in `NAudioDemo`** wires them to two or three WASAPI sources at once (microphone and/or loopback), with a level meter per source and a maximum recording length.

`CaptureMixerInput` adapts one source to the common format; `RealtimeCaptureMixer` bundles the inputs, their shared timeline, and a wall-clock-paced output:

```c#
using NAudio.Extras;
using NAudio.Wave;

var mixer = new RealtimeCaptureMixer(WaveFormat.CreateIeeeFloatWaveFormat(48000, 2));

// system audio (loopback) and the microphone, each captured with WasapiRecorder
var systemRecorder = new WasapiRecorderBuilder().WithLoopbackCapture().WithPollingSync().Build();
var micRecorder = new WasapiRecorderBuilder().Build();

// add each source in its native format; the input resamples/rechannels to the mixer format
var systemInput = mixer.AddInput(systemRecorder.WaveFormat);
var micInput = mixer.AddInput(micRecorder.WaveFormat);

// feed each recorder's zero-copy packets — with their QPC and device timestamps — into its input
systemRecorder.DataAvailable += (data, flags, dev, qpc) => systemInput.AddSamples(data, qpc, dev);
micRecorder.DataAvailable += (data, flags, dev, qpc) => micInput.AddSamples(data, qpc, dev);

mixer.Start();
systemRecorder.StartRecording();
micRecorder.StartRecording();
```

> A plain `AddSamples(data)` overload (no timestamps) also exists for sources that don't provide them — e.g. a legacy `IWaveIn` device: `waveIn.DataAvailable += (s, a) => input.AddSamples(a.Buffer.AsSpan(0, a.BytesRecorded));`.

### Writing the mix to a WAV file

`RealtimeCaptureMixer.Read` returns only as much audio as the wall clock says should exist by now, so a background pump stays real-time:

```c#
var writer = new WaveFileWriter("mixed.wav", mixer.WaveFormat);
var buffer = new float[mixer.WaveFormat.SampleRate * mixer.WaveFormat.Channels / 5]; // ~200ms
var stop = false;

var pump = Task.Run(() =>
{
    while (!stop)
    {
        int read = mixer.Read(buffer, 0, buffer.Length);
        if (read > 0) writer.WriteSamples(buffer, 0, read);
        else Thread.Sleep(5); // caught up with the wall clock (or still in pre-roll) — wait
    }
});

// ... record for as long as you want ...

stop = true;
pump.Wait();
systemRecorder.StopRecording();
micRecorder.StopRecording();
systemRecorder.Dispose();   // or 'await DisposeAsync()' off the UI thread
micRecorder.Dispose();
writer.Dispose();
```

> **Why the paced `Read`, and not a plain `MixingSampleProvider`?** A mixer with `ReadFully = true` never blocks — it zero-fills any input that has no data yet. Read it flat out and you race ahead of real time, producing a file padded with silence that is *longer* than the actual recording. `RealtimeCaptureMixer.Read` throttles the output to the wall clock so the file length matches elapsed time (which also absorbs the tiny clock-rate differences between devices). If you assemble the mixer by hand, pace the output yourself — don't free-run a `ReadFully` mixer.

### Getting the mixed bytes instead of a file

If you're feeding a video muxer (the AVI scenario in [#761](https://github.com/naudio/NAudio/issues/761)) you want raw bytes rather than a WAV file. Read paced float samples from the mixer and convert each chunk to 16‑bit PCM:

```c#
var floats = new float[mixer.WaveFormat.SampleRate * mixer.WaveFormat.Channels / 10]; // 100ms
var pcm = new byte[floats.Length * 2];

int samples = mixer.Read(floats, 0, floats.Length);
for (int i = 0; i < samples; i++)
{
    short s = (short)(Math.Clamp(floats[i], -1f, 1f) * short.MaxValue);
    pcm[i * 2] = (byte)(s & 0xFF);
    pcm[i * 2 + 1] = (byte)(s >> 8);
}
// hand pcm[0 .. samples * 2] to your encoder / muxer
```

## Keeping the sources aligned

The heavy lifting is done by **pacing the output to the wall clock**, not by per-packet correction. `RealtimeCaptureMixer.Read` hands back only as much audio as real time says should exist, and the mixer zero-fills any input whose buffer is momentarily empty. That single mechanism handles the two things that would otherwise pull sources apart:

- **Loopback gaps.** WASAPI loopback only raises `DataAvailable` while audio is actually playing. While the system is quiet that input's buffer simply drains and the mixer pads silence for it; when playback resumes the buffered audio plays at the right moment. No `devicePosition` gap-filling is needed — and, crucially, none is done, because real capture drivers report device positions inconsistently (many report a static or zero value) and acting on them corrupts the stream.
- **Clock-rate drift.** The microphone and the soundcard run off independent clocks, so one delivers marginally more or fewer frames per second than the other. Because output is clocked to the wall clock, a slightly slow source is padded and a slightly fast one is drained, keeping both locked to real time.

The output is anchored to the **first captured sample** (not to when you called `Start`), so a recording begins at the first real audio with only a small pre-roll cushion of latency — a device that is slow to spin up doesn't add a long leading gap or swallow the start.

On top of this, `CaptureMixerInput` applies one *optional* refinement when you feed it the timestamped overload, `AddSamples(data, qpcPosition, devicePosition)`: it uses the shared-origin **`qpcPosition`** to nudge a source's start so sources that began within ~100ms of each other line up precisely. The nudge is bounded (≤100ms) and non-destructive — a larger apparent offset is left to the pacing above, captured audio is never dropped, and if the driver reports a zero/garbage QPC nothing happens. `devicePosition` is captured for diagnostics only.

For diagnostics, `CaptureMixerInput` exposes `PacketsReceived` / `FramesReceived` / `SilenceFramesInserted` / `BufferedFrames` and the raw `FirstQpcPosition` / `FirstDevicePosition` / `LastQpcPosition` / `LastDevicePosition` (plus `RealtimeCaptureMixer.OutputFrames`). The demo's **Align sources** toggle switches the timestamped overload on and off so you can compare — on most hardware the two sound identical, which tells you the timestamps aren't adding anything.

## Things to watch out for

- **Clipping.** Summing two loud sources can exceed `±1.0f`. Reduce the inputs before mixing — `MonoToStereoSampleProvider` exposes `LeftVolume`/`RightVolume`, or insert a `VolumeSampleProvider` per source.
- **Loopback silence.** WASAPI loopback capture only raises `DataAvailable` while audio is actually playing, so `RealtimeCaptureMixer` fills those gaps with silence (the output stays paced to the wall clock) — a loopback source with nothing playing simply contributes silence.
- **Clock drift.** The microphone and the soundcard are driven by independent clocks, but the wall-clock-paced output keeps both locked to real time (see above). This isn't sample-accurate; if you need tighter long-run A/V sync you'd add per-source adaptive resampling driven by the measured QPC-vs-samples error.
- **Disposal order.** Stop both captures, let the pump loop finish, *then* dispose the recorders and the writer. `WasapiRecorder` also implements `IAsyncDisposable`, so prefer `await recorder.DisposeAsync()` (or `await using`) off a UI thread. Disposing a capture device while another thread is still touching it has caused crashes (see [#1183](https://github.com/naudio/NAudio/issues/1183) and [#1184](https://github.com/naudio/NAudio/issues/1184)).
- **Legacy capture devices.** `CaptureMixerInput` is device-agnostic: to mix a classic `IWaveIn` device (`WaveInEvent`, `WasapiLoopbackCapture`) add it with `mixer.AddInput(waveIn.WaveFormat)` and feed it via the untimestamped overload, `waveIn.DataAvailable += (s, a) => input.AddSamples(a.Buffer.AsSpan(0, a.BytesRecorded));`. Those devices don't provide QPC/device timestamps, so such a source is appended in arrival order rather than timeline-aligned.

## Mixing vs. separate channels

If your goal is not to *sum* the two sources but to keep them **separate** — for example microphone on the left channel and system audio on the right ([#1220](https://github.com/naudio/NAudio/issues/1220)) — use `MultiplexingSampleProvider` (or `MultiplexingWaveProvider`) instead of `MixingSampleProvider`. You still bring both sources to a common sample rate first, but you map input channels to output channels rather than adding them together.

## Related questions

This question comes up regularly:

- [#761 – Get mixed byte-stream from WasapiLoopbackCapture and microphone](https://github.com/naudio/NAudio/issues/761)
- [#405 – How to record system sound and microphone input sound at the same time?](https://github.com/naudio/NAudio/issues/405)
- [#1169 – Can this library record the microphone and computer audio at the same time?](https://github.com/naudio/NAudio/issues/1169)
- [#1220 – Record microphone and system sound into the left/right channels of one WAV](https://github.com/naudio/NAudio/issues/1220) (use multiplexing rather than mixing)
