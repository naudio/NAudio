# NAudio.Asio

[![Nuget](https://img.shields.io/nuget/v/NAudio.Asio)](https://www.nuget.org/packages/NAudio.Asio/)

ASIO driver support for [NAudio](https://github.com/naudio/NAudio). Targets `net9.0` and is Windows-only at runtime (guarded by `[SupportedOSPlatform("windows")]`), so it can be referenced from a plain `netX.0` project as well as a `netX.0-windows` one. It is Native AOT compatible.

## What's included

- `AsioOut` — low-latency playback and/or recording through any installed ASIO driver
- ASIO driver enumeration (`AsioOut.GetDriverNames()`)
- Channel routing, per-channel input metering, and access to the driver's control panel
- Interop types for building your own ASIO host code

## When to use it

ASIO is the right choice when you need very low round-trip latency — typical in pro-audio, virtual instruments, and multi-channel recording scenarios — and the user has a working ASIO driver installed (manufacturer-supplied, ASIO4ALL, FlexASIO, etc.). For general-purpose playback/capture prefer [NAudio.Wasapi](https://www.nuget.org/packages/NAudio.Wasapi/) or [NAudio.WinMM](https://www.nuget.org/packages/NAudio.WinMM/).

See the [NAudio documentation site](https://naudio.github.io/NAudio/) for tutorials and the full API reference, or the [GitHub repository](https://github.com/naudio/NAudio) for full documentation and tutorials, including `AsioPlayback.md` and `AsioRecording.md`.
## License

MIT. ASIO is a trademark of Steinberg Media Technologies GmbH — this package is a driver-consumer only; you are responsible for any agreement required to redistribute the ASIO SDK in your own shipping product.
