# Recording Level Meter

In this article we'll see how you can represent the current audio input level coming from a recording device.

## Start Capturing Audio

In NAudio, the method you call to start capturing audio from an input device is called `StartRecording`. This method name can cause confusion. All that it means is that you are asking the input device to provide you with samples audio. It doesn't mean you are actually recording to an audio file. 

So if you want to allow the user to set up their volume levels before they start "recording", you'll actually need to call `StartRecording` to start capturing the audio simply for the purposes of updating the level meter.

We won't go into great detail in this article on how to record audio as that's [covered elsewhere](RecordWavFileWinFormsWaveIn.md), but here we'll create a new recording device, subscribe to the data available event, and start capturing audio by calling `StartRecording`. We'll use `WasapiRecorder`, the recommended capture device on Windows, built with `WasapiRecorderBuilder`. Omit `WithDevice` if you just want the default microphone; see [enumerating audio devices](EnumerateOutputDevices.md) for how to get an `MMDevice` for a specific one.

```c#
using NAudio.CoreAudioApi;
using NAudio.Wave;

var recorder = new WasapiRecorderBuilder()
    .WithDevice(captureDevice)
    .WithLowLatency()       // optional: smaller, more frequent buffers for a responsive meter
    .Build();
recorder.DataAvailable += OnDataAvailable;
recorder.StartRecording();
```

## Handling Captured Audio

`WasapiRecorder` gives the `DataAvailable` handler a `ReadOnlySpan<byte>` over the captured audio, along with the buffer flags and the packet's device and QPC positions. The span is only valid for the duration of the callback, so anything you want to keep must be copied out — writing it straight to a `WaveFileWriter` is fine. If we were simply recording audio, the handler would look like this:

```c#
private void OnDataAvailable(ReadOnlySpan<byte> buffer, AudioClientBufferFlags flags,
                             long devicePosition, long qpcPosition)
{
    writer.Write(buffer);
}
```

But if we're just letting the user get their levels set up, we'd only write to the file if the user had actually begun recording. So we might have a boolean flag that says whether we're recording or not. So when we get the `DataAvailable` event we don't necessarily write to a file.

```c#
private void OnDataAvailable(ReadOnlySpan<byte> buffer, AudioClientBufferFlags flags,
                             long devicePosition, long qpcPosition)
{
    if (isRecording) 
    {
        writer.Write(buffer);
    }
}
```

## Calculating Peak Values

The captured audio arrives as bytes. This means that we must convert to samples.

The way this works depends on the bit depth being recorded at. The two most common options are 32 bit IEEE floating point numbers (`float`'s in C#), which is what `WasapiRecorder` supplies by default since it captures at the device's mix format, and 16 bit signed integers (`short`'s in C#), which is what the legacy `WaveIn` supplies by default, and what you'll get from `WasapiRecorder` if you asked for it with `WithFormat`. Check `recorder.WaveFormat` if you're not sure which you have.

`MemoryMarshal.Cast` (from `System.Runtime.InteropServices`) lets us reinterpret the byte span as a span of samples with no copying. Here's how we might discover the maximum sample value for the default floating point case. Notice that we are simply taking the absolute value of each sample, and we are calculating one maximum value irrespective of whether it is mono or stereo audio. If you wanted, you could calculate the maximum values for each channel separately, by maintaining separate max values for each channel (the samples are interleaved):

```c#
void OnDataAvailable(ReadOnlySpan<byte> buffer, AudioClientBufferFlags flags,
                     long devicePosition, long qpcPosition)
{
    if (isRecording) 
    {
        writer.Write(buffer);
    }

    float max = 0;
    // interpret as 32 bit floating point audio
    foreach (var sample in MemoryMarshal.Cast<byte, float>(buffer))
    {
        // absolute value 
        var abs = sample < 0 ? -sample : sample;
        // is this the max value?
        if (abs > max) max = abs;
    }
}
```

For 16 bit audio, cast to `short` instead and divide by 32768 to get back to the same 0.0f to 1.0f range:

```c#
    float max = 0;
    // interpret as 16 bit audio
    foreach (var sample in MemoryMarshal.Cast<byte, short>(buffer))
    {
        var sample32 = sample / 32768f;
        if (sample32 < 0) sample32 = -sample32;
        if (sample32 > max) max = sample32;
    }
```

If you're working with a legacy `IWaveIn` device such as `WaveIn`, the handler is an `EventHandler<WaveInEventArgs>` and you get a `byte[]` plus a `BytesRecorded` count instead. The same casts work on `args.Buffer.AsSpan(0, args.BytesRecorded)`, or you can use NAudio's `WaveBuffer` class, which can 'cast' a `byte[]` to a `short[]` or `float[]` — something that is not normally possible in C#:

```c#
void OnDataAvailable(object sender, WaveInEventArgs args)
{
    float max = 0;
    var buffer = new WaveBuffer(args.Buffer);
    // interpret as 16 bit audio via ShortBuffer (FloatBuffer for 32 bit float)
    for (int index = 0; index < args.BytesRecorded / 2; index++)
    {
        var sample32 = buffer.ShortBuffer[index] / 32768f;
        if (sample32 < 0) sample32 = -sample32;
        if (sample32 > max) max = sample32;
    }
}
```

## Updating the Volume Meter

A very simple way to implement a volume meter in WinForms or WPF is to use a progressbar. You can set it up with a minimum value of 0 and a maximum value of 100. 

In both our examples, we calulated `max` as a floating point value between 0.0f and 1.0f, so setting the progressBar value is as simple as:

```c#
progressBar.Value = 100 * max;
```

Note that you are updating the UI in the `OnDataAvailable` callback. `WasapiRecorder` raises `DataAvailable` on its capture thread, so you must marshal to the UI thread yourself (e.g. `Control.Invoke` in WinForms or `Dispatcher.Invoke` in WPF) before touching a control. Do the peak calculation in the callback and marshal only the resulting number — you can't pass the span itself to another thread. The legacy `WaveIn` is the exception here: it marshals `DataAvailable` back onto the synchronization context that was active when `StartRecording` was called, so if you start recording from the UI thread you can update controls directly.

Also, this approach means that the frequency of meter updates will match the size of recording buffers. This is the simplest approach, and normally works just fine as there will usually be at least 10 buffers per second which is usually adequate for a volume meter.
