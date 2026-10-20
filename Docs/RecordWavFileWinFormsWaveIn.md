# Recording a WAV file in a WinForms app

In this example we'll see how to create a very simple WinForms app that records audio to a WAV File. We'll use `WasapiRecorder`, which is the recommended capture device on Windows. It's created with the fluent `WasapiRecorderBuilder`, and with no configuration it records from the default capture device (your microphone) in shared mode. If you need the legacy WinMM device instead, there's a `WaveIn` version of the same app at the end of this article.

First of all, let's choose where to put the recorded audio. It will go to a file called `recorded.wav` in a `NAudio` folder on your desktop:

```c#
var outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "NAudio");
Directory.CreateDirectory(outputFolder);
var outputFilePath = Path.Combine(outputFolder,"recorded.wav");
```

Next, let's create the recording device. Create it on the UI thread — `WasapiRecorder` captures the `SynchronizationContext` that is current when it is constructed, and raises `RecordingStopped` back on it, which lets us update our buttons directly in that handler.

```c#
var recorder = new WasapiRecorderBuilder().Build();
```

I'll declare a `WaveFileWriter` but it won't get created until we start recording:

```c#
WaveFileWriter writer = null;
```

And let's set up our form. It will have two buttons - one to start and one to stop recording. And we'll declare a `closing` flag to allow us to stop recording when the form is closed.

```c#
bool closing = false;
var f = new Form();
var buttonRecord = new Button() { Text = "Record" };
var buttonStop = new Button() { Text = "Stop", Left = buttonRecord.Right, Enabled = false };
f.Controls.AddRange(new Control[] { buttonRecord, buttonStop });
```

Now we need some event handlers. When we click `Record`, we'll create a new `WaveFileWriter`, specifying the path for the WAV file to create and the format we are recording in. This must be the same as the recording device format as that is the format we'll receive recorded data in. So we use `recorder.WaveFormat`. In shared mode that's the capture device's mix format, which is usually 32 bit IEEE float — `WaveFileWriter` will write a valid WAV file for that. (If you need a specific format instead, ask for it when building with `WithFormat(...)` and WASAPI will convert.)

Then we start recording with `recorder.StartRecording()` and set the button enabled states appropriately.


```c#
buttonRecord.Click += (s, a) => 
{
    writer = new WaveFileWriter(outputFilePath, recorder.WaveFormat); 
    recorder.StartRecording(); 
    buttonRecord.Enabled = false; 
    buttonStop.Enabled = true; 
};
```


We also need a handler for the `DataAvailable` event on our input device. This will start firing periodically after we start recording. `WasapiRecorder` hands us a `ReadOnlySpan<byte>` straight over the WASAPI buffer, so there's no intermediate copy and no byte count to get wrong — we can write the whole span. The span is only valid for the duration of the callback, so if you want to keep the audio for later you must copy it out; writing it to a file like this is fine. The remaining parameters carry the buffer flags and the device/QPC positions for the packet, which we don't need here, so we discard them with `_`.

```c#
recorder.DataAvailable += (buffer, _, _, _) =>
{
    writer.Write(buffer);
};
```

One safety feature I often add when recording WAV is to limit the size of a WAV file. They grow quickly and can't be over 4GB in any case. Here I'll request that recording stops after 30 seconds:

```c#
recorder.DataAvailable += (buffer, _, _, _) =>
{
    writer.Write(buffer);
    if (writer.Position > recorder.WaveFormat.AverageBytesPerSecond * 30)
    {
        recorder.StopRecording();
    }
};
```

Note that unlike `WaveIn`, this event is raised on the capture thread rather than being marshalled back to the UI thread, so don't touch any controls from inside it.

Now we need to handle the stop recording button. This is simple, we just call `recorder.StopRecording()`. However, we might still receive more data in the `DataAvailable` callback, so don't dispose your `WaveFileWriter` just yet.

```c#
buttonStop.Click += (s, a) => recorder.StopRecording();
```

We'll also add a safety measure that if you try to close the form while you're recording, we'll call `StopRecording` and set a flag so we know we can also dispose the input device:

```c#
f.FormClosing += (s, a) => { closing=true; recorder.StopRecording(); };
```

To safely dispose our `WaveFileWriter`, (which we need to do in order to produce a valid WAV file), we should handle the `RecordingStopped` event on our recording device. We `Dispose` the `WaveFileWriter` which fixes up the headers in our WAV file so that it is valid. Then we set the button states. Finally, if we're closing the form, the input device should be disposed.

```c#
recorder.RecordingStopped += (s, a) =>
{
    writer?.Dispose(); 
    writer = null; 
    buttonRecord.Enabled = true;
    buttonStop.Enabled = false;
    if (closing) 
    { 
        recorder.Dispose();
    }
};
```

`WasapiRecorder` also implements `IAsyncDisposable`, so away from an event handler you can `await recorder.DisposeAsync()` instead and avoid blocking while the capture thread is joined.

Now all our handlers are set up, we're ready to show the dialog:

```c#
f.ShowDialog();
```

Here's the full program for reference:

```c#
var outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "NAudio");
Directory.CreateDirectory(outputFolder);
var outputFilePath = Path.Combine(outputFolder,"recorded.wav");

var recorder = new WasapiRecorderBuilder().Build();

WaveFileWriter writer = null;
bool closing = false;
var f = new Form();
var buttonRecord = new Button() { Text = "Record" };
var buttonStop = new Button() { Text = "Stop", Left = buttonRecord.Right, Enabled = false };
f.Controls.AddRange(new Control[] { buttonRecord, buttonStop });

buttonRecord.Click += (s, a) => 
{ 
    writer = new WaveFileWriter(outputFilePath, recorder.WaveFormat); 
    recorder.StartRecording(); 
    buttonRecord.Enabled = false; 
    buttonStop.Enabled = true; 
};

buttonStop.Click += (s, a) => recorder.StopRecording();

recorder.DataAvailable += (buffer, _, _, _) =>
{
    writer.Write(buffer);
    if (writer.Position > recorder.WaveFormat.AverageBytesPerSecond * 30)
    {
        recorder.StopRecording();
    }
};

recorder.RecordingStopped += (s, a) =>
{
    writer?.Dispose(); 
    writer = null; 
    buttonRecord.Enabled = true;
    buttonStop.Enabled = false;
    if (closing) 
    { 
        recorder.Dispose();
    }
};

f.FormClosing += (s, a) => { closing=true; recorder.StopRecording(); };
f.ShowDialog();
```

See [Recording Audio with WasapiRecorder](WasapiRecorder.md) for the rest of what the recorder can do — selecting a specific device, exclusive mode, low-latency capture, loopback capture of system audio, and consuming audio as an `IAsyncEnumerable` with `CaptureAsync`.

## Doing the same thing with WaveIn

`WaveIn` is the legacy WinMM capture device. It's still supported, and it differs from `WasapiRecorder` in a few ways that matter here:

- `DataAvailable` is an ordinary `EventHandler<WaveInEventArgs>` giving you a `byte[]` and a `BytesRecorded` count — write `a.BytesRecorded` bytes, not `a.Buffer.Length`.
- That event is marshalled back onto the synchronization context that was active when `StartRecording` was called, so if you start recording from the UI thread you can update controls from within it.
- Its default recording format is 44.1kHz 16 bit stereo PCM rather than the device mix format.

Only the device creation and the `DataAvailable` handler change:

```c#
var waveIn = new WaveIn();

waveIn.DataAvailable += (s, a) =>
{
    writer.Write(a.Buffer, 0, a.BytesRecorded);
    if (writer.Position > waveIn.WaveFormat.AverageBytesPerSecond * 30)
    {
        waveIn.StopRecording();
    }
};
```

`StartRecording`, `StopRecording`, `RecordingStopped` and `WaveFormat` all work the same way, so the rest of the program above is unchanged.
