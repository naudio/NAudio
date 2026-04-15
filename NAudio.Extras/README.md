# NAudio.Extras

[![Nuget](https://img.shields.io/nuget/v/NAudio.Extras)](https://www.nuget.org/packages/NAudio.Extras/)

Extra, opinionated helpers built on top of [NAudio](https://github.com/naudio/NAudio). Targets `net8.0` and `net8.0-windows10.0.19041.0`.

## What's included

Utilities that don't belong in the core library but are still useful across projects — for example playlist helpers, ID3 tag readers, Equalizer sample providers, and other community-contributed extras.

## When to use it

Install `NAudio.Extras` when you want these convenience helpers in addition to the main NAudio APIs. It depends on both [NAudio](https://www.nuget.org/packages/NAudio/) and (on Windows) [NAudio.Wasapi](https://www.nuget.org/packages/NAudio.Wasapi/), so it pulls in the full playback/capture stack.

This package is not required for typical NAudio usage — start with the main [NAudio](https://www.nuget.org/packages/NAudio/) package and add `NAudio.Extras` only if you need one of the helpers it contains.

See the [NAudio GitHub repository](https://github.com/naudio/NAudio) for source and documentation.

## License

MIT.
