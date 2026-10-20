# Guitar amp captures with NAudio.Nam

[Neural Amp Modeler](https://www.neuralampmodeler.com/) (NAM) is an open-source way to capture the sound of a guitar amp, pedal or other gear as a small neural network, saved as a `.nam` file. Thousands of captures are shared on [TONE3000](https://www.tone3000.com/). `NAudio.Nam` loads these files and runs them in real time as an NAudio effect. It is pure managed C# with no native dependencies, so it runs anywhere NAudio does.

```sh
dotnet add package NAudio.Nam
```

## What it supports

`NamModel` loads the WaveNet and LSTM models that NAM's trainer produces:

| Model | Typical size | CPU at 48 kHz* |
| --- | --- | --- |
| **A2-Lite** (Architecture 2, the current standard since June 2026) | 3 channels, 1,871 weights | ~1.5% of one core |
| **A2-Full** | 8 channels, 12,146 weights | ~4% of one core |
| **A1 WaveNet**: standard, lite, feather, nano | up to 16 channels, ~13,800 weights (standard) | ~5% (standard) |
| **LSTM** | hidden size 12–32 | ~2–6% |

\* Measured on a 2.1 GHz Xeon (AVX2). On Arm64 the library uses 128-bit NEON vectors instead.

An A2 file holds A2-Lite and A2-Full versions of the same capture (see [below](#choosing-a2-lite-or-a2-full)), and A2 models run on a dedicated fast path. Other WaveNet configurations made from the same building blocks also load, including gated layers, per-layer kernel sizes, a bottleneck narrower than the channel count, and the activations NAM offers (Tanh, ReLU, LeakyReLU, Sigmoid, SiLU, Hardtanh, Hardswish, Softsign and others). A container file can mix architectures, such as an LSTM for its smallest size and WaveNets above it.

Rarer features are rejected with a `NotSupportedException` that names the feature: FiLM conditioning, grouped convolutions, conditioning models (`condition_dsp`), slimmable layers, post-stack heads, "blended" gating and PReLU. So are the ConvNet and Linear architectures.

The output matches the official C++ implementation ([NeuralAmpModelerCore](https://github.com/sdatkinson/NeuralAmpModelerCore)) to within float rounding: about 1e-6 for most models, and 1e-4 at worst for A1 "standard", whose output also shifts by that order between builds of the C++ core itself.

## Run a capture on an audio file

`NamEffect` is an ordinary `IAudioEffect`, so it goes in an `EffectChain` like any other effect:

```c#
using NAudio.Effects;
using NAudio.Nam;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

var model = NamModel.Load("my-amp.nam");

ISampleProvider guitar = new AudioFileReader("di-guitar.wav");    // a dry DI recording
if (guitar.WaveFormat.Channels == 2)
    guitar = new StereoToMonoSampleProvider(guitar);

var chain = new EffectChain(guitar)
    .Add(new NamEffect(model))                // the amp
    .Add(new GainEffect { GainDb = -6 });     // tame the output level

using var player = new WasapiPlayerBuilder().Build();
player.Init(chain);
player.Play();
```

NAM models are trained at one sample rate, almost always 48 kHz, and must run at it. `NamEffect` takes care of that: at any other rate it converts the audio to the model's rate, runs the model, and converts back, using 64-tap sinc interpolation so that no aliasing reaches the amp model's distortion (where it would come out as audible intermodulation). This adds a fixed latency of about 1.5 ms, which the effect reports in `LatencySamples`; `IsResampling` tells you whether it is happening. At the model's own rate there is no latency.

NAM networks are mono. Given more than one channel, `NamEffect` processes the average of the channels and writes the result to every channel. A guitar recorded on one side of a stereo file would then reach the model at half level and sound less driven, which is why the example converts to mono first.

A capture models only what was captured: amp-only captures need a speaker cabinet after them, or they sound harsh and fizzy. Add a `CabinetEffect` with a cabinet impulse response (IR) file. It adds no latency, accepts IRs at any sample rate, and by default trims the IR's silent tail and normalises its level:

```c#
var cabinet = new CabinetEffect { MonoInput = true };   // NamEffect output is the same on every channel
cabinet.LoadImpulseResponse("4x12-v30-sm57.wav");

var chain = new EffectChain(guitar)
    .Add(new NamEffect(model))
    .Add(cabinet);
```

`MonoInput` convolves once and copies the result to every channel, which halves the CPU cost after a `NamEffect`. A typical 50–200 ms cabinet IR costs well under 1% of a core. Captures whose `Metadata.GearType` is `"amp_cab"` or `"amp_pedal_cab"` already include the cabinet and don't need one.

## Choosing A2-Lite or A2-Full

The second constructor argument picks the network by size, from 0 (smallest) to 1 (largest). This follows NAM's own rule, so for an A2 file anything below 0.5 runs A2-Lite:

```c#
var full = new NamEffect(model);               // size 1.0: A2-Full (the default)
var lite = new NamEffect(model, size: 0.0);    // A2-Lite, for weak hardware or many instances

Console.WriteLine(lite.Submodel.IsA2Lite);     // True
```

A2-Full is the more accurate network and is cheap enough for any desktop, so use it unless you are running many instances or a low-power device. The network is fixed when the effect is created. To switch, swap the effect in the chain (`EffectChain` edits are safe while audio is playing).

## Low-latency live playing

The network is causal and adds **no latency** of its own (`LatencySamples` is 0). Latency is set by your audio device's buffer, so use ASIO or WASAPI exclusive mode with a small buffer. For live input through ASIO, use `NamProcessor` directly in the duplex callback. It processes mono spans in place and never allocates:

```c#
using NAudio.Wave;
using NAudio.Nam;

var processor = NamModel.Load("my-amp.nam").CreateProcessor();

using var device = AsioDevice.Open("Focusrite USB ASIO");
device.InitDuplex(new AsioDuplexOptions
{
    InputChannels  = [0],         // guitar input
    OutputChannels = [0, 1],
    SampleRate     = 48000,       // NamProcessor doesn't resample: use the model's rate
    Processor      = (in AsioProcessBuffers b) =>
    {
        var left = b.GetOutput(0);
        processor.Process(b.GetInput(0), left);
        left.CopyTo(b.GetOutput(1));
    }
});
device.Start();
```

Load the model and create the processor before starting the device. Parsing a `.nam` file allocates and takes tens of milliseconds, so never do it on the audio thread.

Without an ASIO driver, WASAPI works too: capture into a `BufferedWaveProvider` and play the effect chain from it. Shared-mode WASAPI adds more latency than ASIO, and the capture and playback devices run on separate clocks, so the buffer absorbs the difference. Low-latency capture (`WithLowLatency`) only works in the device's own format, which is usually 48 kHz float stereo:

```c#
using NAudio.Effects;
using NAudio.Nam;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

var model = NamModel.Load("my-amp.nam");
var cabinet = new CabinetEffect { MonoInput = true };
cabinet.LoadImpulseResponse("4x12-v30-sm57.wav");

using var recorder = new WasapiRecorderBuilder().WithLowLatency().Build();
var input = new BufferedWaveProvider(recorder.WaveFormat, TimeSpan.FromMilliseconds(100))
{
    DiscardOnBufferOverflow = true,
};
recorder.DataAvailable += (buffer, _, _, _) => input.AddSamples(buffer);

// The guitar is on the interface's first input: take that channel at full level.
ISampleProvider guitar = new StereoToMonoSampleProvider(input.ToSampleProvider()) { LeftVolume = 1f, RightVolume = 0f };
var chain = new EffectChain(guitar)
    .Add(new NamEffect(model))
    .Add(cabinet);

using var player = new WasapiPlayerBuilder().WithLowLatency().Build();
player.Init(new MonoToStereoSampleProvider(chain));
recorder.StartRecording();
player.Play();
```

This assumes a stereo device. If it runs at 44.1 kHz rather than 48 kHz, `NamEffect` resamples internally, adding about 1.5 ms.

`BufferedWaveProvider` counts the glitches it hides. Poll them from a timer or the UI, never from the audio thread:

```c#
Console.WriteLine($"underruns: {input.UnderrunSilenceBytes} bytes, overflow: {input.DiscardedBytes} bytes");
```

A rising `UnderrunSilenceBytes` means playback ran out of input: the capture side delivered late, or processing couldn't keep up (try A2-Lite or a larger buffer). A rising `DiscardedBytes` means input arrived faster than it was played, which is usually clock drift between the two devices.

## Input level and model metadata

A capture responds to how hard it is driven, like real gear. `Metadata.InputLevelDbu`, when the file has it, is the level in dBu that a full-scale sample represented during capture. Setting your interface's input to match reproduces the captured drive; otherwise, adjust the level with a `GainEffect` before the `NamEffect`. `Metadata.Loudness` is the model's output loudness in dB, which is useful for level-matching different captures:

```c#
var m = model.Metadata;
Console.WriteLine($"{m.Name} by {m.ModeledBy}: {m.GearMake} {m.GearModel} ({m.ToneType})");
Console.WriteLine($"Loudness {m.Loudness} dB, input calibration {m.InputLevelDbu} dBu");
```

All metadata fields are optional and are null when the file doesn't supply them.

## Resetting

Each new effect or processor starts in the state the network settles to after silence, so the first samples are clean. `Reset()` returns it to that state, for example after seeking. It is allocation-free and cheap (it fills the history buffers rather than reprocessing silence). One loaded `NamModel` can be shared by any number of effects and processors, each of which keeps its own state.
