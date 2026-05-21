# NAudio.Wasapi

[![Nuget](https://img.shields.io/nuget/v/NAudio.Wasapi)](https://www.nuget.org/packages/NAudio.Wasapi/)

WASAPI and Media Foundation support for [NAudio](https://github.com/naudio/NAudio). Windows-only (`net9.0-windows10.0.19041.0`).

## What's included

- **WASAPI playback** — `WasapiOut` (shared and exclusive mode, event-driven callbacks), plus the modern `WasapiPlayer` / `WasapiPlayerBuilder`
- **WASAPI capture** — `WasapiCapture` for microphone/line-in capture, `WasapiLoopbackCapture` for system audio, plus the modern `WasapiRecorder` / `WasapiRecorderBuilder`
- **Device enumeration** — `MMDeviceEnumerator`, `MMDevice`, per-device volume, mute, peak/RMS metering, notification callbacks
- **Media Foundation** — `MediaFoundationReader` and `MediaFoundationEncoder` for MP3, AAC/MP4, WMA and any installed MFT codec; `MediaFoundationResampler`
- **Audio Session API** — per-session volume, mute, and metering

For DMO effects (echo, chorus, reverb, etc.), the DMO MP3 decoder, the DMO resampler, and `DirectSoundOut`, see the separate [`NAudio.Dmo`](https://www.nuget.org/packages/NAudio.Dmo/) package.

## When to use it

Use this package whenever you want modern Windows audio APIs: low-latency playback, loopback capture of the system mixer, per-application volume, or codec support via Media Foundation. The package is being actively modernized to use `GeneratedComInterface` and source-generated COM interop.

See the [NAudio GitHub repository](https://github.com/naudio/NAudio) for full documentation and tutorials.

## License

MIT.
