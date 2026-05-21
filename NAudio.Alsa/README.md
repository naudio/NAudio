# NAudio.Alsa

ALSA playback and capture for NAudio on **Linux**.

This package provides:

| Type | Role |
|---|---|
| `AlsaOut` | `IWavePlayer` — play any NAudio `IWaveProvider` through ALSA |
| `AlsaIn` | `IWaveIn` — record from an ALSA capture device |
| `AlsaDeviceEnumerator` | list playback / capture devices (`AlsaDeviceInfo`) |
| `AlsaException` | thrown on libasound errors (exposes `ErrorCode`) |

## Platform

Linux only — the assembly is marked `[SupportedOSPlatform("linux")]`. It
P/Invokes `libasound`; the runtime SONAME `libasound.so.2` is resolved
automatically (you do **not** need the `-dev` package). Install the ALSA
runtime if it is missing:

```sh
sudo apt install libasound2t64  # Ubuntu 24.04+ (renamed from libasound2 in the t64 transition)
sudo apt install libasound2     # older Debian/Ubuntu
```

This package is **not** pulled in by the `NAudio` meta-package (there is
no `net9.0-linux` TFM). Reference it explicitly:

```sh
dotnet add package NAudio.Linux.Alsa
```

> **Package id note:** this ships as `NAudio.Linux.Alsa` because the
> `NAudio.Alsa` name on nuget.org is held by a third party. We intend (but
> cannot guarantee) to move to `NAudio.Alsa` once that name is reclaimed.
> The assembly name and namespaces are `NAudio.Alsa` regardless, so your
> code does not change.

## Supported input formats

`AlsaOut` plays any `IWaveProvider`, so it is format-agnostic — what you
can play depends on which **decoder** you feed it. `MediaFoundationReader`
and the bundled `Mp3FileReader` / `AudioFileReader` are Windows-only, but
the cross-platform [`NAudio.SoundFile`](../NAudio.SoundFile/README.md)
package (libsndfile) decodes the compressed/free codecs on Linux:

| Format | Reader | Status |
|---|---|---|
| WAV (PCM / IEEE float) | `WaveFileReader` | ✅ supported |
| AIFF (uncompressed PCM) | `AiffFileReader` | ✅ supported |
| Raw PCM | `RawSourceWaveStream` | ✅ supported |
| FLAC / Ogg-Vorbis / Opus | `SoundFileReader` (`NAudio.SoundFile`) | ✅ supported (needs system libsndfile) |
| MP3 | `SoundFileReader` (libsndfile ≥ 1.1), or `Mp3FileReaderBase` + a managed `IMp3FrameDecompressor` (e.g. [NLayer](https://github.com/naudio/NLayer)) | ✅ / ⚠️ |
| AAC / M4A / ALAC / WMA | — | ❌ no cross-platform decoder (FFmpeg territory) |

`SoundFileReader` is the cross-platform `MediaFoundationReader`-style
decoder (it is a `WaveStream` / `ISampleProvider`); add the
`NAudio.SoundFile` package and a system libsndfile to play FLAC/Ogg/Opus/
MP3 through `AlsaOut`. `NAudio.SoundFile` also **encodes** those formats,
so it pairs with `AlsaIn` to capture straight to FLAC/Ogg/Opus.

## Play a WAV file

```c#
using NAudio.Wave;
using NAudio.Wave.Alsa;

using (var audioFile = new WaveFileReader("test.wav"))
using (var outputDevice = new AlsaOut())          // default device
{
    outputDevice.Init(audioFile);
    outputDevice.Play();
    while (outputDevice.PlaybackState == PlaybackState.Playing)
        Thread.Sleep(200);
}
```

Play FLAC/Ogg/Opus/MP3 with `SoundFileReader` (cross-platform, needs a
system libsndfile — `dotnet add package NAudio.SoundFile`):

```c#
using NAudio.SoundFile;

using var audioFile = new SoundFileReader("test.flac");
using var output = new AlsaOut();
output.Init(audioFile);
output.Play();
```

Or play MP3 by wiring a managed decompressor into `Mp3FileReaderBase`
(no native dependency):

```c#
// using NLayer.NAudioSupport;  // dotnet add package NLayer.NAudioSupport
using var mp3 = new Mp3FileReaderBase("test.mp3",
    waveFormat => new Mp3FrameDecompressor(waveFormat));
using var output = new AlsaOut();
output.Init(mp3);
output.Play();
```

## Record to a WAV file

```c#
using NAudio.Wave;
using NAudio.Wave.Alsa;

using var writer = new WaveFileWriter("captured.wav", new WaveFormat(44100, 16, 2));
using var input = new AlsaIn { WaveFormat = writer.WaveFormat };
input.DataAvailable += (s, a) => writer.Write(a.Buffer, 0, a.BytesRecorded);
input.StartRecording();
Thread.Sleep(5000);
input.StopRecording();
```

Swap `WaveFileWriter` for `NAudio.SoundFile`'s `SoundFileWriter` to
capture straight to FLAC/Ogg-Vorbis/Opus instead of WAV.

## Enumerate devices

```c#
foreach (var d in AlsaDeviceEnumerator.GetPlaybackDevices())
    Console.WriteLine($"{d.Name} - {d.Description}");
```

`AlsaDeviceInfo.Name` is passed straight to `new AlsaOut(name)` /
`new AlsaIn(name)`.

## Notes

- Output is taken through a `SampleChannel`, so `AlsaOut.Volume` is a
  real software gain and mono sources are handled. The device format is
  negotiated as IEEE float, else 16-bit, else 24-bit PCM.
- `Pause()` uses hardware pause where the driver supports it, otherwise
  it drops and re-prepares the stream on resume.
- A dedicated streaming thread does blocking I/O and recovers from
  xruns; it is always joined before the device handle is closed.
