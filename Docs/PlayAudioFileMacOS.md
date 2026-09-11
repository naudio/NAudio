## Play an Audio File on macOS with Core Audio

On macOS there is no WASAPI or WaveOut. The `NAudio.MacOS` package adds
`CoreAudioPlayer`, an `IWavePlayer` that plays through the Core Audio
HAL. It is referenced explicitly (it is not part of the `NAudio`
meta-package), and it ships pre-release only while its API settles, so
`--prerelease` is required:

~~~sh
dotnet add package NAudio.MacOS --prerelease
~~~

`CoreAudioPlayer` can play any `IWaveProvider` you give to it. 
For a WAV file, you can use `WaveFileReader` from the Core library:

~~~C#
using NAudio.Wave;

using (var audioFile = new WaveFileReader("any.wav"))
using (var outputDevice = new CoreAudioPlayer())   // default output device
{
    outputDevice.Init(audioFile);
    outputDevice.Play();
    while (outputDevice.PlaybackState == PlaybackState.Playing)
    {
        Thread.Sleep(200);
    }
}
~~~

> [!NOTE]
The `CoreAudioPlayer` does not require any device format negotiation to be done
by your side. That is managed by the internal algorithms of the class. When the format
of your wave provider does not match the HAL's current format, it resamples internally
to match it. Because the HAL format changes frequently, 
the class handles these changes for you appropriately.

### Selecting the output device to perform playback

If you do not want to use the default output device to perform the playback,
you can enumerate the available devices on the system by getting the `Devices`
property on the HAL's audio system object:

~~~C#
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
// using var createdPlayer = new CoreAudioPlayer(chosenDevice);
~~~

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

### Changing the gain (volume) of the device

The device's volume can be changed by setting the `Volume` property on the `CoreAudioPlayer` instance:

~~~C#
createdPlayer.Volume = 0.8f;
~~~

> [!WARNING]
Unlike many other `IWavePlayer` implementations, this implementation directly modifies the 
hardware volume control of the device. It is not recommended to modify this value as the
user of the OS sets this to a desired value. If you want to modify the gain without 
disrupting the user's value, use a `VolumeSampleProvider`.

In some cases, you might be able to modify the volume of the device per-channel.
To do this, retrieve the device's control list and enumerate through the controls:

~~~C#
foreach (var control in createdPlayer.Device.ControlList)
{
    if (control is AudioLevelControl lc && lc.Kind == AudioControlKind.VolumeControl)
    {
        System.Console.WriteLine("Modifying volume of channel {0}.", lc.Element);
        lc.ScalarValue = 0.8f;
    }
}
~~~
