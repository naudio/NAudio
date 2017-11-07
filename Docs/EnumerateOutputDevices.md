# Enumerating Audio Devices

The technique you use to enumerate audio devices depends on what audio output (or input) driver type you are using. This article shows the technique for each supported output device.

# WaveOut or WaveOutEvent

To discover the number of output devices you can use `WaveOut.DeviceCount`. Then you can call `WaveOut.GetCapabilities` passing in the index of a device to find out its name (and some basic information about its capabilities).

Note that you can also pass an index of -1 which is the "audio mapper". Use this if you want to keep playing audio even when a device is removed (such as USB headphones being unplugged).

Also note that the `ProductName` retured is limited to 32 characters, resulting in it often being truncated. This is a limitation of the underlying Windows API and there is unfortunately no easy way to fix it in NAudio.

```c#
for (int n = -1; n < WaveOut.DeviceCount; n++)
{
    var caps = WaveOut.GetCapabilities(n);
    Console.WriteLine($"{n}: {caps.ProductName}");
}
```

Once you've selected the device you want, you can open it by passing the device number into the `WaveOut` or `WaveOutEvent` constructor:

```c#
var outputDevice = new WaveOutEvent(deviceNumber);
```

# WaveIn or WaveInEvent

Getting details of audio capture devices for `WaveIn` is very similar to for `WaveOut`: 

```c#
for (int n = -1; n < WaveIn.DeviceCount; n++)
{
    var caps = WaveIn.GetCapabilities(n);
    Console.WriteLine($"{n}: {caps.ProductName}");
}
```

Once you've selected the device you want, you can open it by passing the device number into the `WaveIn` or `WaveInEvent` constructor:

```c#
var recordingDevice = new WaveInEvent(deviceNumber);
```

# DirectSoundOut

`DirectSoundOut` exposes the `Devices` static method allowing you to enumerate through all the output devices. This has the benefit over `WaveOut` of not having truncated device names:

```c#
foreach (var dev in DirectSoundOut.Devices)
{
    Console.WriteLine($"{dev.Guid} {dev.ModuleName} {dev.Description}");
}
```

Each device has a Guid, and that can be used to open a specific device:

```c#
var outputDevice = new DirectSoundOut(deviceGuid);
```

There are also a couple of special device GUIDs you can use to open the default playback device (`DirectSoundOut.DSDEVID_DefaultPlayback`) or default voice playback device (`DirectSoundOut.DSDEVID_DefaultVoicePlayback`)

# WASAPI Devices

WASAPI playback (render) and recording (capture) devices can both be accessed via the `MMDeviceEnumerator` class. This allows you to enumerate only the type of devices you want (`DataFlow.Render` or `DataFlow.Capture` or `DataFlow.All`).

You can also choose whether you want to include devices that are active, or also include disabled, unplugged or otherwise not present devices with the `DeviceState` bitmask. Here we show them all:

```c#
var enumerator = new MMDeviceEnumerator();
foreach (var wasapi in enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.All))
{
    Console.WriteLine($"{wasapi.DataFlow} {wasapi.FriendlyName} {wasapi.DeviceFriendlyName} {wasapi.State}");
}
```

To open the device you want, simply pass the device in to the appropriate WASAPI class depending on if you are playing back or recording...

```c#
var outputDevice = new WasapiOut(mmDevice, ...);
var recordingDevice = new WasapiIn(captureDevice, ...);
var loopbackCapture = new WasapiLoopbackCapture(loopbackDevice);
```

You can also use the MMEnumerator to request what the default device is for a number of different scenarios (playback or record, and voice, multimedia or 'console'):

```c#
enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
```

# ASIO

You can discover the registered ASIO drivers on your system with `AsioOut.GetDriverNames`. There is no guarantee that the associated soundcard is currently connected to the system.

```c#
foreach (var asio in AsioOut.GetDriverNames())
{
    Console.WriteLine(asio);
}
```

You can then use the driver name to open the device:

```c#
new AsioOut(driverName);
```

# Management Objects

Finally you can use Windows Management Objects to get hold of details of the sound devices installed. This doesn't map specifically to any of the NAudio output device types, but can be a source of useful information

```c#
var objSearcher = new ManagementObjectSearcher(
	   "SELECT * FROM Win32_SoundDevice");

var objCollection = objSearcher.Get();
foreach (var d in objCollection)
{
    Console.WriteLine("=====DEVICE====");
    foreach (var p in d.Properties)
    {
        Console.WriteLine($"{p.Name}:{p.Value}");
    }
}
```
