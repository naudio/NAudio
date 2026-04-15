# NAudio

[![Nuget](https://img.shields.io/nuget/v/NAudio)](https://www.nuget.org/packages/NAudio/)

NAudio is an open source .NET audio library written by [Mark Heath](https://markheath.net) and contributors.

This is the main NAudio meta-package. Installing it pulls in everything you need to play, record, and manipulate audio on .NET, including:

- **NAudio.Core** — the format-independent core (`WaveStream`, `ISampleProvider`, mixers, resamplers, signal generators, file readers, etc.)
- **NAudio.Midi** — MIDI file reading/writing and event model
- **NAudio.WinMM** — WaveIn/WaveOut via the Windows Multimedia API (Windows only)
- **NAudio.WinForms** — Windows Forms controls (fader, pan slider, wave viewer) (Windows only)
- **NAudio.Asio** — ASIO playback and recording (Windows only)
- **NAudio.Wasapi** — WASAPI playback, capture, and loopback, plus Media Foundation, ACM, and DMO (Windows only)

On non-Windows target frameworks only the cross-platform pieces (Core + MIDI) are referenced. If you only need a subset, reference the individual packages directly rather than this meta-package.

## Getting Started

See the [NAudio GitHub repository](https://github.com/naudio/NAudio) for documentation, tutorials, and the demo applications (`NAudioDemo`, `NAudioWpfDemo`) that show how to use the main features.

## License

MIT. See the [project site](https://github.com/naudio/NAudio) for source, issues, and contribution guidelines.
