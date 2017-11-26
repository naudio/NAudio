# Recording Level Meter

In this article we'll see how you can represent the current audio input level coming from a recording device.

## Start Capturing Audio

In NAudio, the method you call to start capturing audio from an input device is called `StartRecording`. This method name can cause confusion. All that it means is that you are asking the input device to provide you with samples audio. It doesn't mean you are actually recording to an audio file. 

So if you want to allow the user to set up their volume levels before they start "recording", you'll actually need to call `StartRecording` to start capturing the audio simply for the purposes of updating the level meter.

We won't go into great detail in this article on how to record audio as that's [covered elsewhere](RecordWavFileWinFormsWaveIn.md), but here we'll create a new recording device, subscribe to the data available event, and start capturing audio by calling `StartRecording`.

```c#
var waveIn = new WaveInEvent(deviceNumber);
waveIn.DataAvailable += OnDataAvailable;
waveIn.StartRecording();
```

## Handling Captured Audio

In the `DataAvailable` event handler, if we were simply recording audio, we'd write to a `WaveFileWriter` like this:

```c#
private void OnDataAvailable(object sender, WaveInEventArgs args)
{
    writer.Write(args.Buffer, 0, args.BytesRecorded);
};
```

But if we're just letting the user get their levels set up, we'd only write to the file if the user had actually begun recording. So we might have a boolean flag that says whether we're recording or not. So when we get the `DataAvailable` event we don't necessarily write to a file.

```c#
private void OnDataAvailable(object sender, WaveInEventArgs args)
{
    if (isRecording) 
    {
        writer.Write(args.Buffer, 0, args.BytesRecorded);
    }
};
```

## Calculating Peak Values

The `WaveInEventArgs.Buffer` property contains the captured audio. Unfortunately this is represented as a byte array. This means that we must convert to samples.

The way this works depends on the bit depth being recorded at. The two most common options are 16 bit signed integers (`short`'s in C#), which is what `WaveIn` and `WaveInEvent` will supply by default. And 32 bit IEEE floating point numbers (`float`'s in C#) which is what `WasapiIn` or `WasapiLoopbackCapture` will supply by default.

Here's how we might discover the maximum sample value if the incoming audio is 16 bit. Notice that we are simply taking the absolute value of each sample, and we are calculating one maximum value irrespective of whether it is mono or stereo audio. If you wanted, you could calculate the maximum values for each channel separately, by maintaining separate max values for each channel (the samples are interleaved):

```c#
void OnDataAvailable(object sender, WaveInEventArgs args)
{
    if (isRecording) 
    {
        writer.Write(args.Buffer, 0, args.BytesRecorded);
    }

    float max = 0;
    // interpret as 16 bit audio
    for (int index = 0; index < args.BytesRecorded; index += 2)
    {
        short sample = (short)((args.Buffer[index + 1] << 8) |
                                args.Buffer[index + 0]);
        // to floating point
        var sample32 = sample/32768f;
        // absolute value 
        if (sample32 < 0) sample32 = -sample32;
        // is this the max value?
        if (sample32 > max) max = sample32;
    }
}
```

The previous example showed using bit manipulation, but NAudio also has a clever trick up its sleeve called `WaveBuffer`. This allows us to 'cast' from a `byte[]` to a `short[]` or `float[]`, something that is not normally possible in C#.

Here's it working for floating point audio:

```c#
void OnDataAvailable(object sender, WaveInEventArgs args)
{
    if (isRecording) 
    {
        writer.Write(args.Buffer, 0, args.BytesRecorded);
    }

    float max = 0;
    var buffer = new WaveBuffer(args.Buffer);
    // interpret as 32 bit floating point audio
    for (int index = 0; index < args.BytesRecorded / 4; index++)
    {
        var sample = buffer.FloatBuffer[index];

        // absolute value 
        if (sample < 0) sample = -sample;
        // is this the max value?
        if (sample > max) max = sample;
    }
}
```

The same approach can be used for 16 bit audio, by accessing `ShortBuffer` instead of `FloatBuffer`.


## Updating the Volume Meter

A very simple way to implement a volume meter in WinForms or WPF is to use a progressbar. You can set it up with a minimum value of 0 and a maximum value of 100. 

In both our examples, we calulated `max` as a floating point value between 0.0f and 1.0f, so setting the progressBar value is as simple as:

```c#
progressBar.Value = 100 * max;
```

Note that you are updating the UI in the `OnDataAvailable` callback. NAudio will attempt to call this on the UI context if there is one. 

Also, this approach means that the frequency of meter updates will match the size of recording buffers. This is the simplest approach, and normally works just fine as there will usually be at least 10 buffers per second which is usually adequate for a volume meter.