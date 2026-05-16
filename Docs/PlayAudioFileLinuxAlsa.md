## Play an Audio File on Linux with ALSA

On Linux there is no WASAPI or WaveOut. The `NAudio.Alsa` package adds
`AlsaOut`, an `IWavePlayer` that plays through ALSA. It is referenced
explicitly (it is not part of the `NAudio` meta-package, and the runtime
needs `libasound` — `sudo apt install libasound2`):

```sh
dotnet add package NAudio.Alsa
```

Playback works exactly like the other NAudio output devices: open the
file with `AudioFileReader`, pass it to `AlsaOut.Init`, then `Play`.
`Play` is non-blocking, so we wait until playback finishes. The `using`
blocks dispose the reader and the device (which stops the streaming
thread and closes the ALSA handle).

```c#
using NAudio.Wave;
using NAudio.Wave.Alsa;

using (var audioFile = new AudioFileReader("test.wav"))
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

To play to a specific device, pass an ALSA PCM name to the constructor
(`new AlsaOut("hw:0")`), or pick one from the enumerator:

```c#
foreach (var device in AlsaDeviceEnumerator.GetPlaybackDevices())
{
    Console.WriteLine($"{device.Name} - {device.Description}");
}
// var outputDevice = new AlsaOut("sysdefault:CARD=PCH");
```

`AlsaOut.Volume` is a software gain (`1.0f` = unity), so you can set it
before or during playback. To be notified when playback ends or fails,
handle `PlaybackStopped` — its `Exception` is `null` on a normal end of
stream or an explicit `Stop()`.
