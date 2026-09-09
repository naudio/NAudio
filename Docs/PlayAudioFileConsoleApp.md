## Play an Audio File from a Console application

To play a file from a console application, we will use `AudioFileReader` as a simple way of opening our audio file, and `WasapiPlayer` as the output device. `WasapiPlayer` is the recommended output device on Windows — it's built with the fluent `WasapiPlayerBuilder`, and with no configuration at all it opens the default playback device in shared mode.

We simply need to pass the `audioFile` into the `outputDevice` with the `Init` method, and then call `Play`. 

Since `Play` only means "start playing" and isn't blocking, we can wait in a loop until playback finishes.

Afterwards, we need to `Dispose` our `audioFile` and `outputDevice`, which in this example we do by virtue of putting them inside `using` blocks.

```c#
using NAudio.Wave;

using(var audioFile = new AudioFileReader(audioFilePath))
using(var outputDevice = new WasapiPlayerBuilder().Build())
{
    outputDevice.Init(audioFile);
    outputDevice.Play();
    while (outputDevice.PlaybackState == PlaybackState.Playing)
    {
        Thread.Sleep(1000);
    }
}
```

`WasapiPlayer` also implements `IAsyncDisposable`, so in an async `Main` you can `await using` it instead and avoid blocking while the playback thread is joined. See [Playing Audio with WasapiPlayer](WasapiPlayer.md) for the full set of builder options (device selection, exclusive mode, latency, low latency and raw mode).

## Other output devices

Every output device in NAudio implements `IWavePlayer`, so the code above works unchanged whichever one you pick — only the line that constructs the device changes:

- **`WaveOut`** — the legacy WinMM device (`new WaveOut()`). Still supported, but has higher latency than WASAPI and no reason to be preferred in new code.
- **`AsioDevice`** — for professional low-latency audio interfaces, see [Play audio with ASIO](AsioPlayback.md).
- **`AlsaOut`** (`NAudio.Alsa`) on Linux, and **`CoreAudioPlayer`** (`NAudio.MacOS`) on macOS.

[Choose an output device type](OutputDeviceTypes.md) compares them in more detail.
