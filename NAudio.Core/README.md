# NAudio.Core

[![Nuget](https://img.shields.io/nuget/v/NAudio.Core)](https://www.nuget.org/packages/NAudio.Core/)

The cross-platform core of [NAudio](https://github.com/naudio/NAudio). Contains everything that is independent of the underlying audio API, so it targets plain `net8.0` and can be used on Windows, Linux, and macOS.

## What's included

- `WaveStream`, `IWaveProvider`, `ISampleProvider` and associated base classes
- `WaveFormat` and format-conversion helpers
- WAV, AIFF, and raw file readers and writers (`WaveFileReader`, `WaveFileWriter`, `AiffFileReader`, …)
- Sample providers for mixing, panning, volume, fade in/out, offset/skip/take, mono↔stereo conversion
- Resampling (`WdlResamplingSampleProvider`) and pitch shifting (`SmbPitchShiftingSampleProvider`)
- Signal generators, envelope generators, a BiQuad filter, and an FFT
- G.711 (µ-law / a-law) codecs

## When to use it

Reference `NAudio.Core` directly when you want to read, write, or manipulate audio on non-Windows targets, or when you are assembling your own combination of platform packages (`NAudio.Wasapi`, `NAudio.WinMM`, `NAudio.Asio`, …) and don't want the full [NAudio](https://www.nuget.org/packages/NAudio/) meta-package.

See the [NAudio GitHub repository](https://github.com/naudio/NAudio) for full documentation.

## License

MIT.
