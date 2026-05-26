# NAudio.Dmo

[![Nuget](https://img.shields.io/nuget/v/NAudio.Dmo)](https://www.nuget.org/packages/NAudio.Dmo/)

DirectX Media Object (DMO) and DirectSound support for [NAudio](https://github.com/naudio/NAudio). Windows-only (`net9.0-windows`).

## What's included

- **DMO effects** — `DmoEffectWaveProvider<TEffector, TParam>` with built-in wrappers for the Windows audio effect DMOs (echo, chorus, compressor, distortion, flanger, gargle, I3DL2 reverb, param EQ, waves reverb)
- **DMO MP3 decoder** — `DmoMp3FrameDecompressor` (`IMp3FrameDecompressor` implementation backed by the Windows Media MP3 Decoder DMO)
- **DMO resampler stream** — `ResamplerDmoStream` (wraps the wmcodecdsp resampler as a `WaveStream`)
- **DMO enumeration** — `DmoEnumerator` for discovering installed audio DMOs
- **DirectSound playback** — `DirectSoundOut`, the legacy DirectSound output path

## When to use it

DMO and DirectSound are legacy Windows audio APIs. Reach for them when you need a built-in Windows effect (echo, reverb, chorus, etc.) without pulling a third-party DSP library, or when targeting older code paths that already rely on `DirectSoundOut`. For new code, prefer `MediaFoundationResampler` (in `NAudio.Wasapi`) over `ResamplerDmoStream`, and `WasapiPlayerBuilder` over `DirectSoundOut`.

This package was carved out of `NAudio.Wasapi` for NAudio 3 so consumers who only want WASAPI / MediaFoundation don't transitively pull DMO and DirectSound interop.

See the [NAudio GitHub repository](https://github.com/naudio/NAudio) for full documentation and tutorials.

## License

MIT.
