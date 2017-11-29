# Recording with ASIO

The `AsioOut` class in NAudio allows you to both play back and record audio using an ASIO driver. ASIO is a driver format supported by many popular Digital Audio Workstation (DAW) applications on Windows, and usually offers very low latency for record and playback. 

To use ASIO, you do need a soundcard that has an ASIO driver. Most professional soundcards have ASIO drivers, but you can also try the [ASIO4ALL](http://asio4all.com/) driver which enables ASIO for soundcards that don't have their own native ASIO driver.

Often you'll use `AsioOut` to play audio, or to play and record simultaneously. This article covers the scenario where we just want to record audio.

## Opening an ASIO device for recording

To discover the names of the installed ASIO drivers on your system you use `AsioOut.GetDriverNames()`.

We can use one of those driver names to pass to the constructor of `AsioOut`

```c#
var asioOut = new AsioOut(asioDriverName);
```

## Selecting Recording Channels

We may want to find out how many input channels are available on the device. We can get this with:

```c#
var inputChannels = asioOut.DriverInputChannelCount;
```

By default, ASIO will capture all input channels when you record, but if you have a multi-input soundcard, this may be overkill. If you want to select a sub-range of channels to record from, we can set the `InputChannelOffset` to the first channel to record on. And then here I set up a `recordChannelCount` variable which I will use when I start recording. So in this example, I'm recording on channels 4 and 5 (n.b. channel numbers are zero based).

Finally, I call `InitRecordAndPlayback`. This is a little bit ugly and future versions of NAudio may provide a nicer method, but the first parameter supplies the audio to be played. We're just recording, so this is null. The second argument is the number of channels to record (starting from `InputChannelOffset`). And the third argument is the desired sample rate. When we're playing we don;t need this as the sample rate of the input `IWaveProvider` will be used, but since we're just recording, we do need to specify the desired sample rate.

```c#
asioOut.InputChannelOffset = 4;
var recordChannelCount = 2;
var sampleRate = 44100;
asioOut.InitRecordAndPlayback(null, recordChannelCount, sampleRate);
```

## Start Recording

We need to subscribe to the `AudioAvailable` event in order to process audio received in the ASIO buffer callback.

And we kick off recording by calling `Play()`. Yes, again it's not very intuitively named for the scenario in which we're recording only, but it basically tells the ASIO driver to start capturing audio and call us on each buffer swap.

```c#
asioOut.AudioAvailable += OnAsioOutAudioAvailable;
asioOut.Play(); // start recording
```

## Handle received audio

When we receive audio we get access to the raw ASIO buffers in an `AsioAudioAvailableEventArgs` object.

Because ASIO is all about ultimate low latency, NAudio provides direct access to an `IntPtr` array called `InputBuffers` which contains the recorded buffer for each input channel. It also provides a `SamplesPerBuffer` property to tell you how many

But there's still a lot of work to be done. ASIO supports many different recording formats including 24 bit audio where there are 3 bytes per sample. You need to examine the `AsioSampleType` property of the `AsioAudioAvailableEventArgs` to know what format each sample is in.

So it can be a lot of work to access these samples in a meaningful format. NAudio provides a convenience method called `GetAsInterleavedSamples` to read samples from each input channel, turn them into IEEE floating point samples, and interleave them so they could be written to a WAV file. It supports the most common `AsioSampleType` properties, but not all of them. 

Note that this example uses an overload of `GetAsInterleavedSamples` that always returns a new `float[]`. It's better for memory purposes to create your own `float[]` up front and pass that in instead.

Here's the simplest handler for `AudioAvailable` that just gets the audio as floating point samples and writes them to a `WaveFileReader` that we've set up in advance.

```c#
void OnAsioOutAudioAvailable(object sender, AsioAudioAvailableEventArgs e)
{
    var samples = e.GetAsInterleavedSamples();
    writer.WriteSamples(samples, 0, samples.Length);
}
```

For a real application, you'd probably want to write your own logic in here to access the samples and pass them on to whatever processing logic you need.

## Stop Recording

We stop recording by calling Stop().

```c#
asioOut.Stop();
```

As with other NAudio `IWavePlayer` implementations, we'll get a `PlaybackStopped` event firing when the driver stops.

And of course we should remember to `Dispose` our instance of `AsioOut` when we're done with it.

```c#
asioOut.Dispose();
```