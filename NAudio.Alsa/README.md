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
sudo apt install libasound2     # Debian/Ubuntu
```

This package is **not** pulled in by the `NAudio` meta-package (there is
no `net9.0-linux` TFM). Reference it explicitly:

```sh
dotnet add package NAudio.Alsa
```

## Play an audio file

```c#
using NAudio.Wave;
using NAudio.Wave.Alsa;

using var audioFile = new AudioFileReader("test.wav");
using var output = new AlsaOut();          // or new AlsaOut("hw:0")
output.Init(audioFile);
output.Play();
while (output.PlaybackState == PlaybackState.Playing)
    Thread.Sleep(200);
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
