# Record Soundcard Output with WasapiLoopbackCapture

Lots of people ask how they can use NAudio to record the audio being played by another program. The answer is that unfortunately Windows does not provide an API that lets you target the output of one specific program to record. However, with WASAPI loopback capture, you can record all the audio that is being played out of a specific output device.

The audio has to be captured at the `WaveFormat` the device is already using. This will typically be stereo 44.1kHz (sometimes 48kHz) IEEE floating point. Obviously you can manually manipulate the audio after capturing it into another format, but for this example, we'll pass it straight onto the WAV file unchanged.

Let's start off by selecting a path to record to, creating an instance of `WasapiLoopbackCapture` (uses the default system device, but we can pass any rendering `MMDevice` that we want which we can find with `MMDeviceEnumerator`). We'll also create a `WaveFileWriter` using the capture `WaveFormat`.

```c#
var outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "NAudio");
Directory.CreateDirectory(outputFolder);
var outputFilePath = Path.Combine(outputFolder, "recorded.wav");
var capture = new WasapiLoopbackCapture();
var writer = new WaveFileWriter(outputFilePath, capture.WaveFormat);
```

We need to handle the `DataAvailable` event, and it's very much the same approach here as recording to a WAV file from a regular `WaveIn` device. We just write `BytesRecorded` bytes from the `Buffer` into the `WaveFileWriter`. And in this example, I am stopping recording when we've captured 20 seconds worth of audio, by calling `StopRecording`.

```c#
capture.DataAvailable += (s, a) =>
{
    writer.Write(a.Buffer, 0, a.BytesRecorded);
    if (writer.Position > capture.WaveFormat.AverageBytesPerSecond * 20)
    {
        capture.StopRecording();
    }
};
```

When the `RecordingStopped` event fires, we `Dispose` our `WaveFileWriter` so we create a valid WAV file, and we're done recording so we'll `Dispose` our capture device as well.

```c#
capture.RecordingStopped += (s, a) =>
{
    writer.Dispose();
    writer = null;
    capture.Dispose();
};
```

All that remains is for us to start recording with `StartRecording` and wait for recording to finish by monitoring the `CaptureState`.

```c#
capture.StartRecording();
while (capture.CaptureState != NAudio.CoreAudioApi.CaptureState.Stopped)
{
    Thread.Sleep(500);
}
```

Now there is one gotcha with `WasapiLoopbackCapture`. If no audio is playing whatsoever, then the `DataAvailable` event won't fire. So if you want to record "silence", one simple trick is to simply use an NAudio playback device to play silence through that device for the duration of time you're recording. Alternatively, you could insert silence yourself when you detect gaps in the incoming audio.