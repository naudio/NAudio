# Playback with SDL2

The `WaveOutSdl` class in NAudio allows you to playback audio using an SDL2. SDL2 is a cross-platform development library designed to provide low level access to audio, keyboard, mouse, joystick, and graphics hardware via OpenGL and Direct3D.

To use SDL2 you will need native libraries. You can find these libraries on nuget or on official website.

## Opening an SDL2 device for playback

To discover the list of the accessible playback devices on your system you use `WaveOutSdl.GetCapabilitiesList()`.

We can use 'DeviceNumber' property from 'WaveOutSdlCapabilities' instance and set this number via 'DeviceId' property on the 'WaveOutSdl' instance.

```c#
var waveOutSdl = new WaveOutSdl() 
{ 
    DeviceId = waveOutSdlCaps.DeviceNumber
};
```

## Inititializing wave provider

Call `Init`, this lets us pass the `IWaveProvider` we want to play. Note that the sample rate of the `WaveFormat` of the input provider must be one supported by the SDL2.

```c#
waveOutSdl.Init(myWaveProvider);
```

## Stop Playback

We stop playing by calling Stop().

```c#
waveOutSdl.Stop(); // stop playing
```

As with other NAudio `IWavePlayer` implementations, we'll get a `PlaybackStopped` event firing when the playback stops.

And of course we should remember to `Dispose` our instance of `WaveOutSdl` when we're done with it.

```c#
waveOutSdl.Dispose();
```
