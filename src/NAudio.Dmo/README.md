# NAudio.Dmo

[![Nuget](https://img.shields.io/nuget/v/NAudio.Dmo)](https://www.nuget.org/packages/NAudio.Dmo/)

DirectX Media Object (DMO) and DirectSound support for [NAudio](https://github.com/naudio/NAudio). Targets `net9.0` and is Windows-only at runtime (guarded by `[SupportedOSPlatform("windows")]`), so it can be referenced from a plain `netX.0` project as well as a `netX.0-windows` one.

## What's included

- **DMO effects** (obsolete as of 3.2; use the managed effects in `NAudio.Effects`) — `DmoEffectWaveProvider<TEffector, TParam>` with built-in wrappers for the Windows audio effect DMOs (echo, chorus, compressor, distortion, flanger, gargle, I3DL2 reverb, param EQ, waves reverb)
- **DMO MP3 decoder** — `DmoMp3FrameDecompressor` (`IMp3FrameDecompressor` implementation backed by the Windows Media MP3 Decoder DMO)
- **DMO resampler stream** (obsolete as of 3.2; use `MediaFoundationResampler` or `WdlResamplingSampleProvider`) — `ResamplerDmoStream` (wraps the wmcodecdsp resampler as a `WaveStream`)
- **DMO enumeration** — `DmoEnumerator` for discovering installed audio DMOs
- **DirectSound playback** — `DirectSoundOut`, the legacy DirectSound output path

## When to use it

DMO and DirectSound are legacy Windows audio APIs. Reach for them when targeting older code paths that already rely on `DirectSoundOut` or the DMO MP3 decoder. For effects, `NAudio.Effects` (in `NAudio.Core`) now provides managed, cross-platform equivalents. For new code, prefer `MediaFoundationResampler` (in `NAudio.Wasapi`) over `ResamplerDmoStream`, and `WasapiPlayerBuilder` over `DirectSoundOut`.

This package was carved out of `NAudio.Wasapi` for NAudio 3 so consumers who only want WASAPI / MediaFoundation don't transitively pull DMO and DirectSound interop.

See the [NAudio documentation site](https://naudio.github.io/NAudio/) for tutorials and the full API reference, or the [GitHub repository](https://github.com/naudio/NAudio) for full documentation and tutorials.
## License

MIT.
