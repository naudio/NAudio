# NAudio.WinMM

[![Nuget](https://img.shields.io/nuget/v/NAudio.WinMM)](https://www.nuget.org/packages/NAudio.WinMM/)

Windows Multimedia (WinMM / `winmm.dll`) bindings for [NAudio](https://github.com/naudio/NAudio). Windows-only (`net8.0-windows`).

## What's included

- **Playback** — `WaveOut`, `WaveOutEvent` for playing audio via the legacy WinMM API
- **Recording** — `WaveIn`, `WaveInEvent` for capturing audio from input devices
- **MIDI I/O** — `MidiIn` / `MidiOut` for sending and receiving live MIDI messages
- **ACM** — Audio Compression Manager wrappers: `AcmStream`, `AcmMp3FrameDecompressor`, driver enumeration
- Device enumeration and capability queries for WinMM audio and MIDI devices

## When to use it

Use this package when you need the classic `WaveOut`/`WaveIn` APIs — useful for broad compatibility with older Windows versions, for very simple playback/capture, or when you specifically want ACM codecs. For lower-latency playback, system loopback capture, or modern device enumeration prefer [NAudio.Wasapi](https://www.nuget.org/packages/NAudio.Wasapi/).

See the [NAudio GitHub repository](https://github.com/naudio/NAudio) for full documentation and tutorials.

## License

MIT.
