# NAudio.Nam

[![Nuget](https://img.shields.io/nuget/v/NAudio.Nam)](https://www.nuget.org/packages/NAudio.Nam/)

Run [Neural Amp Modeler](https://www.neuralampmodeler.com/) (NAM) captures of guitar amps and pedals in real time with [NAudio](https://github.com/naudio/NAudio). Pure managed C#, `net9.0`, no native dependencies: it runs on Windows, Linux and macOS, x64 and Arm64, and is Native-AOT compatible.

## What's included

- **`NamModel`** loads `.nam` files: NAM Architecture 2 (A2) models, as the A2-Lite/A2-Full container TONE3000 distributes or as a single A2 WaveNet. It exposes the capture's metadata (gear, loudness, input calibration).
- **`NamEffect`** is an `IAudioEffect` for `EffectChain` / `EffectSampleProvider`. It adds no latency, does not allocate while processing, and has bypass and dry/wet mix from the `AudioEffect` base.
- **`NamProcessor`** is the mono engine underneath, for driving from your own buffers (e.g. an ASIO duplex callback).

A2-Full uses about 4% of one desktop core at 48 kHz and A2-Lite about 1.5%. Output matches the official C++ implementation to within float rounding (about 1e-6). Older A1 and LSTM models are not supported yet.

```c#
var model = NamModel.Load("my-amp.nam");
var chain = new EffectChain(guitarAt48kHz).Add(new NamEffect(model));
```

## When to use it

Use this package to build guitar amp simulators, re-amp DI recordings, or add captured amp and pedal tones to an NAudio app, without shipping a native library. It is not pulled in by the `NAudio` meta-package; reference it explicitly:

```sh
dotnet add package NAudio.Nam
```

## Tutorial

For a worked walkthrough see [the NAudio.Nam tutorial](https://github.com/naudio/NAudio/blob/main/Docs/NeuralAmpModeler.md).

See the [NAudio documentation site](https://naudio.github.io/NAudio/) for tutorials and the full API reference, or the [GitHub repository](https://github.com/naudio/NAudio) for full documentation and tutorials.

## License

MIT. The A2 network implementation is a port of [NeuralAmpModelerCore](https://github.com/sdatkinson/NeuralAmpModelerCore), Copyright (c) 2023 Steven Atkinson, also MIT licensed.
