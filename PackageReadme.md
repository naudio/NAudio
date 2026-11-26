# NAudio 2.x

NAudio is an open source .NET audio library for playing, recording, reading, writing, converting and processing audio, and for working with MIDI.

This package is part of **NAudio 2**, which is in maintenance: it receives bug fixes but no new features. New development happens in **NAudio 3**, available from the same package IDs on NuGet. See [Migrating from NAudio 2 to NAudio 3](https://naudio.github.io/NAudio/docs/MigratingFromNAudio2.html) for what changed.

The NAudio 2 packages:

| Package | Contents |
|---|---|
| `NAudio` | Meta-package referencing the packages below, for Windows desktop apps |
| `NAudio.Core` | Cross-platform core: wave streams and providers, file readers and writers, DSP |
| `NAudio.WinMM` | WaveOut, WaveIn and ACM (winmm.dll) |
| `NAudio.Wasapi` | WASAPI playback and capture, Media Foundation |
| `NAudio.Asio` | ASIO playback and recording |
| `NAudio.Midi` | MIDI files and MIDI in/out |
| `NAudio.WinForms` | Windows Forms controls and WaveOut with window callbacks |
| `NAudio.Uap` | Windows Runtime (UWP) support |
| `NAudio.Extras` | Higher-level helpers such as an audio playback engine and equalizer |

## Links

- Source and issues: [github.com/naudio/NAudio](https://github.com/naudio/NAudio) (NAudio 2 lives on the [`release/2.x`](https://github.com/naudio/NAudio/tree/release/2.x) branch)
- NAudio 2 tutorials: [Docs on the release/2.x branch](https://github.com/naudio/NAudio/tree/release/2.x/Docs)
- Release notes: [RELEASE_NOTES.md](https://github.com/naudio/NAudio/blob/release/2.x/RELEASE_NOTES.md)

NAudio is licensed under the MIT license.
