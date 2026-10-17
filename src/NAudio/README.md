# NAudio

[![Nuget](https://img.shields.io/nuget/v/NAudio)](https://www.nuget.org/packages/NAudio/)

NAudio is an open source .NET audio library written by [Mark Heath](https://markheath.net) and contributors.

This is the main NAudio meta-package. Installing it pulls in what you need to play, record and manipulate audio on Windows, and adds `AudioFileReader` — the one-line "just open this audio file" reader that picks the right decoder for you — along with `Mp3FileReader`.

Requires `net9.0` or later. If you need .NET Framework or .NET Standard 2.0, use NAudio 2.x.

## What it pulls in

On any target framework (`net9.0` or later, with or without `-windows`):

- **NAudio.Core** — the format-independent core: `WaveStream`, `IWaveProvider` / `ISampleProvider`, WAV and AIFF readers and writers, mixing, resampling, DSP, and the `NAudio.Effects` framework
- **NAudio.Midi** — the MIDI event model and Standard MIDI File reading and writing (plus WinRT live MIDI I/O when your project targets `net9.0-windows10.0.19041.0` or later)
- **NAudio.Wasapi** — WASAPI playback, capture and loopback (`WasapiPlayer` / `WasapiRecorder`), plus Media Foundation codecs
- **NAudio.WinMM** — `WaveOut` / `WaveIn`, classic MIDI I/O, ACM codecs and mixer controls via `winmm.dll`

`NAudio.Wasapi` and `NAudio.WinMM` are Windows-only at runtime. They're marked `[SupportedOSPlatform("windows")]`, so the platform compatibility analyzer (`CA1416`) flags unguarded calls from code that isn't Windows-specific. `AudioFileReader` reads PCM/float WAV and AIFF everywhere, and uses Media Foundation (MP3, AAC, WMA, FLAC…) and ACM (compressed WAV) when running on Windows.

If you're targeting Linux or macOS, you probably want the individual packages (`NAudio.Core` plus `NAudio.Alsa`, `NAudio.MacOS` or `NAudio.SoundFile`) rather than this meta-package.

## Optional packages

These are not pulled in by the meta-package — add them explicitly if you want them:

| Package | Platform | What it gives you |
| --- | --- | --- |
| [NAudio.Asio](https://www.nuget.org/packages/NAudio.Asio/) | Windows | Low-latency multichannel playback and capture through ASIO drivers (`AsioDevice`, `AsioOut`) |
| [NAudio.Dmo](https://www.nuget.org/packages/NAudio.Dmo/) | Windows | `DirectSoundOut`, DMO effects, and the DMO MP3 decoder and resampler |
| [NAudio.WinForms](https://www.nuget.org/packages/NAudio.WinForms/) | Windows (`netX.0-windows`) | Windows Forms controls, and the window-callback `WaveOutWindow` / `WaveInWindow` |
| [NAudio.Sampler](https://www.nuget.org/packages/NAudio.Sampler/) | cross-platform | Polyphonic software sampler — SoundFont (`.sf2`), SFZ and single-sample instruments |
| [NAudio.SoundFile](https://www.nuget.org/packages/NAudio.SoundFile/) | cross-platform | Read *and write* WAV/AIFF/FLAC/Ogg-Vorbis/Opus/MP3 via libsndfile |
| [NAudio.Alsa](https://www.nuget.org/packages/NAudio.Alsa/) | Linux | `AlsaOut` / `AlsaIn` playback and capture via libasound |
| [NAudio.Vst3](https://www.nuget.org/packages/NAudio.Vst3/) | Windows | VST 3 plug-in hosting (preview) — effects and instruments |
| [NAudio.Extras](https://www.nuget.org/packages/NAudio.Extras/) | cross-platform (playback engine Windows-only) | Opinionated helpers — playback engine, capture mixing, ID3 tags |

NAudio 3.0–3.2 included `NAudio.Asio`, `NAudio.Dmo` and `NAudio.WinForms` in this meta-package on Windows target frameworks. If you're upgrading and use any of them, add the package reference.

## Getting started

```csharp
using NAudio.Wave;

using var audioFile = new AudioFileReader("myfile.mp3");
using var player = new WasapiPlayerBuilder().Build();
player.Init(audioFile);
player.Play();
while (player.PlaybackState == PlaybackState.Playing)
{
    Thread.Sleep(500);
}
```

- **[Documentation site](https://naudio.github.io/NAudio/)** — tutorials and the full API reference
- **[GitHub repository](https://github.com/naudio/NAudio)** — source, issues, and the demo applications (`NAudioDemo`, `NAudioWpfDemo`)
- **[Migrating from NAudio 2 to NAudio 3](https://github.com/naudio/NAudio/blob/main/Docs/MigratingFromNAudio2.md)** — every breaking change, with before/after code

## License

MIT. See the [project site](https://github.com/naudio/NAudio) for source, issues, and contribution guidelines.
