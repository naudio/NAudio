# Recording with SDL2

The `WaveInSdl` class in NAudio allows you to record audio using an SDL2. SDL2 is a cross-platform development library designed to provide low level access to audio, keyboard, mouse, joystick, and graphics hardware via OpenGL and Direct3D.

To use SDL2 you will need native libraries. You can find these libraries on nuget or on official website.

## Opening an SDL2 device for recording

To discover the list of the accessible recording devices on your system you use `WaveInSdl.GetCapabilitiesList()`.

We can use 'DeviceNumber' property from 'WaveInSdlCapabilities' instance and set this number via 'DeviceId' property on the 'WaveInSdl' instance.

We can use 'Frequency', 'Bits', and 'Channels' properies from 'WaveInSdlCapabilities' instance and set 'WaveFormat' property on the 'WaveInSdl' instance.

```c#
var waveInSdl = new WaveInSdl() 
{ 
    DeviceId = waveInSdlCaps.DeviceNumber,
    WaveFormat = new WaveFormat(waveInSdlCaps.Frequency, waveInSdlCaps.Bits, waveInSdlCaps.Channels)
};
```

## Start Recording

We need to subscribe to the `DataAvailable` event in order to process audio received in the SDL2 buffer callback.

And we kick off recording by calling `StartRecording()`.

```c#
waveInSdl.DataAvailable += OnWaveInSdlDataAvailable;
waveInSdl.StartRecording(); // start recording
```

## Handle received audio

When we receive audio we get access to the byte array buffer in an `WaveInEventArgs` object.

Here's the simplest handler for `DataAvailable` that just gets the audio as byte array and writes them to a `WaveFileWriter` that we've set up in advance.

```c#
void OnWaveInSdlDataAvailable(object sender, WaveInEventArgs e)
{
    waveFileWriter.Write(e.Buffer, 0, e.BytesRecorded);
}
```

For a real application, you'd probably want to write your own logic in here.

## Stop Recording

We stop recording by calling `StopRecording()`.

```c#
waveInSdl.StopRecording();
```

As with other NAudio `IWaveIn` implementations, we'll get a `RecordingStopped` event firing when the recording stops.

And of course we should remember to `Dispose` our instance of `WaveInSdl` when we're done with it.

```c#
waveInSdl.Dispose();
```
