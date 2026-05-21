## Play an Audio File on Linux with ALSA

On Linux there is no WASAPI or WaveOut. The `NAudio.Alsa` package adds
`AlsaOut`, an `IWavePlayer` that plays through ALSA. It is referenced
explicitly (it is not part of the `NAudio` meta-package, and the runtime
needs `libasound` — `sudo apt install libasound2`):

```sh
dotnet add package NAudio.Linux.Alsa
```

> The package id is currently `NAudio.Linux.Alsa` because the `NAudio.Alsa`
> name on nuget.org is held by a third party. We intend (but cannot
> guarantee) to move to `NAudio.Alsa` once that name is reclaimed. The
> assembly name and namespaces are `NAudio.Alsa` either way, so your code
> does not change.

`AlsaOut` plays any `IWaveProvider`, so the format you can play depends
on the **reader** you give it. `AudioFileReader`, `Mp3FileReader` and
`MediaFoundationReader` are Windows-only; on Linux use the cross-platform
`NAudio.Core` readers (`WaveFileReader`, `AiffFileReader`,
`RawSourceWaveStream`) for PCM, and the `NAudio.SoundFile` package's
`SoundFileReader` for FLAC/Ogg/Opus/MP3 (see below). So for a WAV file,
use `WaveFileReader`:

```c#
using NAudio.Wave;
using NAudio.Wave.Alsa;

using (var audioFile = new WaveFileReader("test.wav"))
using (var outputDevice = new AlsaOut())          // default device
{
    outputDevice.Init(audioFile);
    outputDevice.Play();
    while (outputDevice.PlaybackState == PlaybackState.Playing)
    {
        Thread.Sleep(200);
    }
}
```

To play to a specific device, pass an ALSA PCM name to the constructor,
or pick one from the enumerator. Prefer `default` or a `plughw:` name
for arbitrary sample rates/formats — those go through ALSA's plug layer,
which converts; a bare `hw:` device only accepts rates and formats it
supports natively (the backend negotiates the nearest rate but will not
resample):

```c#
foreach (var device in AlsaDeviceEnumerator.GetPlaybackDevices())
{
    Console.WriteLine($"{device.Name} - {device.Description}");
}
// var outputDevice = new AlsaOut("sysdefault:CARD=PCH");
```

### Playing compressed formats

The cross-platform [`NAudio.SoundFile`](../NAudio.SoundFile/README.md)
package is the Linux equivalent of `MediaFoundationReader`: its
`SoundFileReader` (a `WaveStream` / `ISampleProvider` over libsndfile)
decodes FLAC, Ogg-Vorbis, Opus and MP3. It needs a system libsndfile
(`sudo apt install libsndfile1`); `SoundFileCapabilities` reports which
codecs the installed build supports:

```c#
// dotnet add package NAudio.SoundFile
using NAudio.SoundFile;

using var audioFile = new SoundFileReader("test.flac");
using var outputDevice = new AlsaOut();
outputDevice.Init(audioFile);
outputDevice.Play();
```

(AAC/M4A/ALAC/WMA remain out of scope — that is FFmpeg territory.)

Alternatively, MP3 alone can be played with no native dependency by
plugging a fully-managed frame decompressor such as
[NLayer](https://github.com/naudio/NLayer) into `Mp3FileReaderBase`:

```c#
// dotnet add package NLayer.NAudioSupport
using NLayer.NAudioSupport;

using var mp3 = new Mp3FileReaderBase("test.mp3",
    waveFormat => new Mp3FrameDecompressor(waveFormat));
using var outputDevice = new AlsaOut();
outputDevice.Init(mp3);
outputDevice.Play();
```

`AlsaOut.Volume` is a software gain (`1.0f` = unity), so you can set it
before or during playback. Handle `PlaybackStopped` to be notified when
playback ends or fails — its `Exception` is `null` on a normal end of
stream or an explicit `Stop()`.
