# Recording a WAV file in a WinForms app with WaveIn

In this example we'll see how to create a very simple WinForms app that records audio to a WAV File.

First of all, let's choose where to put the recorded audio. It will go to a file called `recorded.wav` in a `NAudio` folder on your desktop:

```c#
var outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "NAudio");
Directory.CreateDirectory(outputFolder);
var outputFilePath = Path.Combine(outputFolder,"recorded.wav");
```

Next, let's create the recording device. I'm going to use `WaveInEvent` in this case. We could also use `WaveIn` or indeed `WasapiCapture`.

```c#
var waveIn = new WaveInEvent();
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

Now we need some event handlers. When we click `Record`, we'll create a new `WaveFileWriter`, specifying the path for the WAV file to create and the format we are recording in. This must be the same as the recording device format as that is the format we'll receive recorded data in. So we use `waveIn.WaveFormat`.

Then we start recording with `waveIn.StartRecording()` and set the button enabled states appropriately.


```c#
buttonRecord.Click += (s, a) => 
{
    writer = new WaveFileWriter(outputFilePath, waveIn.WaveFormat); 
    waveIn.StartRecording(); 
    buttonRecord.Enabled = false; 
    buttonStop.Enabled = true; 
};
```


We also need a handler for the `DataAvailable` event on our input device. This will start firing periodically after we start recording. We can just write the buffer in the event args to our writer. Make sure you write `a.BytesRecorded` bytes, not `a.Buffer.Length`

```c#
waveIn.DataAvailable += (s, a) =>
{
    writer.Write(a.Buffer, 0, a.BytesRecorded);
};
```

One safety feature I often add when recording WAV is to limit the size of a WAV file. They grow quickly and can't be over 4GB in any case. Here I'll request that recording stops after 30 seconds:

```c#
waveIn.DataAvailable += (s, a) =>
{
    writer.Write(a.Buffer, 0, a.BytesRecorded);
    if (writer.Position > waveIn.WaveFormat.AverageBytesPerSecond * 30)
    {
        waveIn.StopRecording();
    }
};
```

Now we need to handle the stop recording button. This is simple, we just call `waveIn.StopRecording()`. However, we might still receive more data in the `DataAvailable` callback, so don't dispose you `WaveFileWriter` just yet.

```c#
buttonStop.Click += (s, a) => waveIn.StopRecording();
```

We'll also add a safety measure that if you try to close the form while you're recording, we'll call `StopRecording` and set a flag so we know we can also dispose the input device:

```c#
f.FormClosing += (s, a) => { closing=true; waveIn.StopRecording(); };
```

To safely dispose our `WaveFileWriter`, (which we need to do in order to produce a valid WAV file), we should handle the `RecordingStopped` event on our recording device. We `Dispose` the `WaveFileWriter` which fixes up the headers in our WAV file so that it is valid. Then we set the button states. Finally, if we're closing the form, the input device should be disposed.

```c#
waveIn.RecordingStopped += (s, a) =>
{
    writer?.Dispose(); 
    writer = null; 
    buttonRecord.Enabled = true;
    buttonStop.Enabled = false;
    if (closing) 
    { 
        waveIn.Dispose();
    }
};
```

Now all our handlers are set up, we're ready to show the dialog:

```c#
f.ShowDialog();
```

Here's the full program for reference:

```c#
var outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "NAudio");
Directory.CreateDirectory(outputFolder);
var outputFilePath = Path.Combine(outputFolder,"recorded.wav");

var waveIn = new WaveInEvent();

WaveFileWriter writer = null;
bool closing = false;
var f = new Form();
var buttonRecord = new Button() { Text = "Record" };
var buttonStop = new Button() { Text = "Stop", Left = buttonRecord.Right, Enabled = false };
f.Controls.AddRange(new Control[] { buttonRecord, buttonStop });

buttonRecord.Click += (s, a) => 
{ 
    writer = new WaveFileWriter(outputFilePath, waveIn.WaveFormat); 
    waveIn.StartRecording(); 
    buttonRecord.Enabled = false; 
    buttonStop.Enabled = true; 
};

buttonStop.Click += (s, a) => waveIn.StopRecording();

waveIn.DataAvailable += (s, a) =>
{
    writer.Write(a.Buffer, 0, a.BytesRecorded);
    if (writer.Position > waveIn.WaveFormat.AverageBytesPerSecond * 30)
    {
        waveIn.StopRecording();
    }
};

waveIn.RecordingStopped += (s, a) =>
{
    writer?.Dispose(); 
    writer = null; 
    buttonRecord.Enabled = true;
    buttonStop.Enabled = false;
    if (closing) 
    { 
        waveIn.Dispose();
    }
};

f.FormClosing += (s, a) => { closing=true; waveIn.StopRecording(); };
f.ShowDialog();
```

