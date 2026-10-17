# Guitar amp captures with NAudio.Nam

[Neural Amp Modeler](https://www.neuralampmodeler.com/) (NAM) is an open-source way to capture the sound of a guitar amp, pedal or other gear as a small neural network, saved as a `.nam` file. Thousands of captures are shared on [TONE3000](https://www.tone3000.com/). `NAudio.Nam` loads these files and runs them in real time as an NAudio effect. It is pure managed C# with no native dependencies, so it runs anywhere NAudio does.

```sh
dotnet add package NAudio.Nam
```

## What it supports

This version runs NAM's **Architecture 2 (A2)** models, the current standard since June 2026. An A2 file holds two versions of the same capture:

| Network | Channels | Weights | CPU at 48 kHz* |
| --- | --- | --- | --- |
| A2-Lite | 3 | 1,871 | ~1.5% of one core |
| A2-Full | 8 | 12,146 | ~4% of one core |

\* Measured on a 2.1 GHz Xeon (AVX2). On Arm64 the library uses 128-bit NEON vectors instead.

Older A1 WaveNet, LSTM and other architectures are rejected with a `NotSupportedException` that names the architecture. The output matches the official C++ implementation ([NeuralAmpModelerCore](https://github.com/sdatkinson/NeuralAmpModelerCore)) to within about 1e-6.

## Run a capture on an audio file

`NamEffect` is an ordinary `IAudioEffect`, so it goes in an `EffectChain` like any other effect. NAM models are trained at 48 kHz and must be run at that rate, so resample first if your audio is at another rate:

```c#
using NAudio.Effects;
using NAudio.Nam;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

var model = NamModel.Load("my-amp.nam");

ISampleProvider guitar = new AudioFileReader("di-guitar.wav");    // a dry DI recording
if (guitar.WaveFormat.Channels == 2)
    guitar = new StereoToMonoSampleProvider(guitar);
if (guitar.WaveFormat.SampleRate != 48000)
    guitar = new WdlResamplingSampleProvider(guitar, 48000);

var chain = new EffectChain(guitar)
    .Add(new NamEffect(model))                // the amp
    .Add(new GainEffect { GainDb = -6 });     // tame the output level

using var player = new WasapiPlayerBuilder().Build();
player.Init(chain);
player.Play();
```

NAM networks are mono. Given more than one channel, `NamEffect` processes the average of the channels and writes the result to every channel. A guitar recorded on one side of a stereo file would then reach the model at half level and sound less driven, which is why the example converts to mono first.

A capture models only what was captured: amp-only captures usually need a cabinet impulse response after them. `ConvolutionReverbEffect` with a cabinet IR and `Mix = 1` does this, at the cost of its partition latency (`LatencySamples`).

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
    SampleRate     = 48000,       // the model's training rate
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
