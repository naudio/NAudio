## Play an Audio File on macOS with Core Audio

On macOS there is no WASAPI or WaveOut. The `NAudio.MacOS` package adds
`CoreAudioPlayer`, an `IWavePlayer` that plays through the Core Audio
HAL. It is referenced explicitly (it is not part of the `NAudio`
meta-package), and it ships pre-release only while its API settles, so
`--prerelease` is required:

```sh
dotnet add package NAudio.MacOS --prerelease
```

`CoreAudioPlayer` plays any `IWaveProvider`. For a WAV file, use
`WaveFileReader` from `NAudio.Core`:

```c#
using NAudio.Wave;

using (var audioFile = new WaveFileReader("test.wav"))
using (var outputDevice = new CoreAudioPlayer())   // default output device
{
    outputDevice.Init(audioFile);
    outputDevice.Play();
    while (outputDevice.PlaybackState == PlaybackState.Playing)
    {
        Thread.Sleep(200);
    }
}
```

There is no device-format negotiation to worry about: when the
provider's format does not match the device's, `CoreAudioPlayer`
configures a Core Audio converter internally and resamples for you.

### Playing compressed formats

`AudioFileReader` is not the answer here — its cross-platform build
handles only PCM WAV and AIFF, and throws `NotSupportedException` for
MP3 and everything else.

Use `ExtendedAudioFileReaderFromURL` instead. It is a `WaveStream` over
macOS Extended Audio File Services — the macOS equivalent of
`MediaFoundationReader` — decoding to PCM with no extra install, because
the codecs are part of the OS:

```c#
using NAudio.Wave;

using var audioFile = ExtendedAudioFileReaderFromURL.CreateFromFile("test.m4a");
using var outputDevice = new CoreAudioPlayer();
outputDevice.Init(audioFile);
outputDevice.Play();
```

That covers MP3, AAC/M4A, ALAC, FLAC and AIFF among others. The exact
set depends on the macOS version, and can be queried at runtime:

```c#
using NAudio.MacOS.AudioToolbox;

foreach (var extension in AudioFileLibraryInformation.RecognizedFileExtensions)
{
    Console.WriteLine(extension);
}
```

Pass an `ExtendedAudioFileReaderSettings` to control what comes back —
`RequestIeeeFloat` for 32-bit float, or `OutputFormat` for a specific
`WaveFormat`, converted as the file is read.
`ExtendedAudioFileReaderFromStream` does the same over a `Stream`.

### Choosing an output device

The default device comes from the system object; enumerate `Devices` to
pick another one and pass it to the constructor:

```c#
using NAudio.MacOS.CoreAudio;

foreach (var device in AudioSystemObject.Instance.Devices)
{
    if (device.GetStreams(AudioObjectPropertyScopeConstants.Output).Length > 0)
    {
        Console.WriteLine($"{device.Name} - {device.Manufacturer}");
    }
}
// using var outputDevice = new CoreAudioPlayer(chosenDevice);
```

Note that `CoreAudioPlayer.Volume` is the *device's* volume, not a
software gain like `AlsaOut.Volume` — setting it moves the system
output level for that device. Handle `PlaybackStopped` to be notified
when playback ends or fails; its `Exception` is `null` on a normal end
of stream or an explicit `Stop()`.
