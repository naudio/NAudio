## Play an Audio File on macOS with Audio Queue Services

On macOS there is no WASAPI or WaveOut. The `NAudio.MacOS` package adds
`AudioQueuePlayer`, an `IWavePlayer` that plays through Apple's Audio
Queue Services. It is referenced explicitly (it is not part of the `NAudio`
meta-package), and it ships pre-release only while its API settles, so
`--prerelease` is required:

```sh
dotnet add package NAudio.MacOS --prerelease
```

> [!IMPORTANT]
For older users who still want to use the older low-level HAL API, they can use
the older versions of the package, although that no fixes will probably be 
provided for that API. If you want to explore the source for that API or even modify it,
you can always clone the repo containing that old code from this URL: https://github.com/mdcdi1315/NAudio/tree/macos-wrappers-old-coreaudioplayer-impl

The `AudioQueuePlayer` can play any `IWaveProvider` you give to it. 
For a WAV file, you can use `WaveFileReader` from the Core library:

```c#
using NAudio.Wave;

using (var audioFile = new WaveFileReader("any.wav"))
using (var outputDevice = new AudioQueuePlayerBuilder().Build())   // default output device
{
    outputDevice.Init(audioFile);
    outputDevice.Play();
    while (outputDevice.PlaybackState == PlaybackState.Playing)
    {
        Thread.Sleep(200);
    }
}
```

> [!NOTE]
The `AudioQueuePlayer` does not require any device format negotiation to be done
by your side. The Audio Queue converts the format of your wave provider to the format of the device.

> [!NOTE]
The player queues audio ahead of the device, so a garbage collection or a slow
provider does not interrupt playback unless it takes longer than the queued audio.
Set `DesiredLatency` (milliseconds, 100 by default) and `NumberOfBuffers` (4 by default) before calling `Init`
to trade latency against the risk of dropouts.

### Building player instances

The Audio Queue provides a handful of settings that you can use to configure
the player before creating an instance of it. For this reason, a builder class
is provided, called `AudioQueuePlayerBuilder` and it is located into the `NAudio.Wave` namespace:

```c#

using System.Threading.Tasks;

using NAudio.MacOS.CoreAudio;
using NAudio.MacOS.AudioToolbox;

namespace NAudio.Wave;

public sealed class AudioQueuePlayerBuilder
{
    public AudioQueuePlayerBuilder WithLatency(int latencyMs);

    public AudioQueuePlayerBuilder WithNumberOfBuffers(int bufferCount);

    public AudioQueuePlayerBuilder WithDevice(AudioDevice device);

    public AudioQueuePlayerBuilder WithDefaultDeviceStreamRouting();

    public AudioQueuePlayerBuilder WithTimePitchAlgorithm(TimePitchAlgorithm a);

    public AudioQueuePlayer Build();

    public Task<AudioQueuePlayer> BuildAsync();
}
```

> [!NOTE]
You may also build an Audio Queue Player asynchronously, with the `BuildAsync` method.
Note that the method captures the synchornization context of the thread where
`BuildAsync` is called, and the playback stopped events are dispatched on that
synchronization context, if one is set.

#### The `WithLatency` method

For the audio queue to play audio, the class internally allocates buffers
and sets their size based on this value. So, this method specifies the size,
in milliseconds, that each of those buffers will be able to hold data.

Note that the greater this value is, the buffer size becomes larger,
and so the time required to fetch new data from the audio provider
that you give during the player's initialization.

The default latency value assigned by the builder is 0.1 seconds (100 milliseconds).

> [!NOTE]
This method throws `ArgumentOutOfRangeException` if the provided latency value is less than 1.

#### The `WithNumberOfBuffers` method

This can be used to further fine-tune the desired latency that you want
to have on the player. As described above, the Audio Queue player allocates
bytes of the specified latency that are subsequently used to provide data
to the Audio Queue.

This is useful if, for example, you are working in a constrained I/O environment 
where access to data is limited and can take a large time to complete a single I/O
operation. So, you set, for example, 8 buffers to avoid hearing discontinuities
(sound is 'glitching').

On the other side, you set this value to 1 or 2 buffers if you want the lowest latency as possible,
but this incurrs more calls to your audio provider read method.

> [!NOTE]
This method throws `ArgumentOutOfRangeException` if the provided latency value is less than 1.

#### The `WithDevice` method

The default for an Audio Queue player is to follow the default playback device
that macOS uses. With this method, you can specify a Core Audio HAL `AudioDevice`
that indicates the audio device to use for doing playback.

> [!NOTE]
This method throws `ArgumentNullException` if the device you provide to it
is `null`.

##### Choosing an output device

If you do not want to follow the default playback device to perform the playback,
you can enumerate the available devices on the system by getting the `Devices`
property on the HAL's audio system object:

```c#
using NAudio.MacOS.CoreAudio;

// Enumerate all the devices that provide output.
foreach (var device in AudioSystemObject.Instance.Devices)
{
    if (device.GetStreams(AudioObjectPropertyScopeConstants.Output).Length > 0)
    {
        Console.WriteLine($"{device.Name} - {device.Manufacturer}");
    }
}
// Once you have selected an audio device, you can give it to the player:
// using var createdPlayer = new AudioQueuePlayerBuilder().WithDevice(chosenDevice).Build();
```

> [!NOTE]
The `Devices` property returns all the devices installed to the system at the time of calling it.
That includes input and output devices.

> [!NOTE]
You can also retrieve a hidden device by using the `ConvertUIDToDevice` method,
that is provided in the instance of the audio system object.
You have to know it's UID to retrieve such device.
You can also always retrieve the UID of the device, 
by calling the `DeviceUID` property on the device object. 
For the `CoreAudioPlayer`, you can retrieve the device object 
that it uses to do playback by invoking the `Device` property.

> [!NOTE]
Building a `AudioQueuePlayer` instance with the default device routing
may result in a `InvalidOperationException` exception, if there is not a proper
output device that macOS can use to render your audio.

#### The `WithDefaultDeviceStreamRouting` method

This method instructs the audio queue to follow the default playback device.
So, this is the exact opposite of what the `WithDevice` method does.
It is useful if you finally settled on following the default device and you want
to undo a previous `WithDevice` call.

#### The `WithTimePitchAlgorithm` method

This method enables the use of time-pitch during playback. The Audio Queue provides
several different algorithms for you to use, and there's also the default option which
uses whatever best algorithm macOS has determined.

Once you enable the time-pitch, you can modify several properties on the created
Audio Queue Player:

```c#
createdPlayer.PlayRate = 0.8f;

// Or, if you have specified the Spectral time-pitch algorithm:
createdPlayer.Pitch = 1400;
```

### Changing the gain (volume) of the device

The device's volume can be changed by setting the `Volume` property on the `AudioQueuePlayer` instance:

```c#
createdPlayer.Volume = 0.8f;
```

In some cases, you might be able to modify the volume of the device per-channel.
To do this, retrieve the device's control list (if possible) and enumerate through the controls:

```c#
foreach (var control in createdPlayer.Device.ControlList)
{
    if (control is AudioLevelControl lc && lc.Kind == AudioControlKind.VolumeControl)
    {
        System.Console.WriteLine("Modifying volume of channel {0}.", lc.Element);
        lc.ScalarValue = 0.8f;
    }
}
```

> [!WARNING]
The above code snippet directly modifies the hardware volume control of the device. 
It is not recommended to modify this value as the user of the OS sets this to a desired value. 
If you want to modify the gain without disrupting the user's value, use the Audio Queue's `Volume` property instead.
