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

Two capture devices run off independent clocks and start delivering at slightly different moments, so naively appending their packets lets them drift apart over a long recording. `WasapiRecorder.DataAvailable` tags every packet with two timestamps that `CaptureMixerInput.AddSamples(data, qpc, devicePosition)` uses to counter this:

- **`qpcPosition`** — a system-wide QueryPerformanceCounter value (100‑nanosecond units) marking when the packet was captured. All inputs on one `RealtimeCaptureMixer` share a single origin (the first packet seen), so each source is placed at its true capture time relative to that origin. A source that starts later — or a loopback that only begins delivering once audio plays — is offset with leading silence so it lines up instead of being pulled to the front.
- **`devicePosition`** — the device's own running frame count. If it jumps further than the previous packet's length, the device counted frames it never delivered (a glitch); the hole is back-filled with exactly that many silence frames so everything downstream keeps its timing.

This is best-effort alignment aimed at stopping sources drifting apart, not a sample-accurate resampling clock; the residual per-device clock-rate difference is absorbed by pacing the output to the wall clock (above). Corrections are **bounded and non-destructive** — captured audio is never dropped, each correction is capped at one second, and if a driver reports unreliable timestamps (some report a static or zero device position) the input just appends packets in arrival order. `CaptureMixerInput` exposes `PacketsReceived` / `FramesReceived` / `SilenceFramesInserted` / `BufferedFrames` (and `RealtimeCaptureMixer.OutputFrames`) for diagnostics, and the demo's **Align sources** toggle lets you compare aligned against raw capture.

## Things to watch out for

- **Clipping.** Summing two loud sources can exceed `±1.0f`. Reduce the inputs before mixing — `MonoToStereoSampleProvider` exposes `LeftVolume`/`RightVolume`, or insert a `VolumeSampleProvider` per source.
- **Loopback silence.** WASAPI loopback capture only raises `DataAvailable` while audio is actually playing, so `RealtimeCaptureMixer` fills those gaps with silence (the output stays paced to the wall clock) — a loopback source with nothing playing simply contributes silence.
- **Clock drift.** The microphone and the soundcard are driven by independent clocks. Start offsets and glitches are handled by the timestamp alignment above, and the residual clock-*rate* difference is absorbed by the wall-clock-paced output. This keeps sources from drifting apart but is not sample-accurate; if you need tighter long-run A/V sync you'd add per-source adaptive resampling driven by the measured QPC-vs-samples error.
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
