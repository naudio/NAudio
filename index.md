---
_layout: landing
---

# NAudio

NAudio is an open source .NET audio library written by [Mark Heath](https://markheath.net). It provides a comprehensive set of classes for playing, recording, converting and manipulating audio in .NET applications.

## Get started

- **[Tutorials](Docs/PlayAudioFileConsoleApp.md)** — task-focused how-to guides covering playback, recording, codecs, MIDI, visualisation and more.
- **[API Reference](api/index.md)** — the full class library reference, generated from the source XML documentation.
- **[NAudio on NuGet](https://www.nuget.org/packages/NAudio/)** — install the package into your project.
- **[Source on GitHub](https://github.com/naudio/NAudio)** — browse the code, demos and issues.

## Installation

```
dotnet add package NAudio
```

## A quick example

Playing an audio file is just a few lines:

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

See the [tutorials](Docs/PlayAudioFileConsoleApp.md) for many more worked examples.
