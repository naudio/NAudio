## Play an Audio File from a WinForms application

In this demo, we'll see how to play an audio file from a WinForms application. This technique will also work

To start with, we'll create a very simple form with a start and a stop button. And we'll also declare two private members, one to hold the audio output device (that's the soundcard we're playing out of), and one to hold the audio file (that's the audio file we're playing).

```c#
using NAudio.Wave;
using NAudio.Wave.SampleProviders

public class MainForm : Form
{
    private WaveOutEvent outputDevice;
    private AudioFileReader audioFile;

    public MainForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var flowPanel = new FlowLayoutPanel();
        flowPanel.FlowDirection = FlowDirection.LeftToRight;
        flowPanel.Margin = new Padding(10);

        var buttonPlay = new Button();
        buttonPlay.Text = "Play";
        buttonPlay.Click += OnButtonPlayClick;
        flowPanel.Controls.Add(buttonPlay);

        var buttonStop = new Button();
        buttonStop.Text = "Stop";
        buttonStop.Click += OnButtonStopClick;
        flowPanel.Controls.Add(buttonStop);

        this.Controls.Add(flowPanel);

        this.FormClosing += OnButtonStopClick;
    }
}
```

Now we've not defined the button handlers yet, so let's do that. First of all the Play button. The first time we click this, we won't have opened our output device or audio file.

So we'll create an output device of type `WaveOutEvent`. This is only one of several options for sending audio to the soundcard, but its a good choice in many scenarios, due to its ease of use and broad platform support.

We'll also subscribe to the `PlaybackStopped` event, which we can use to do some cleaning up.

Then if we haven't opened an audio file, we'll use `AudioFileReader` to load an audio file. This is a good choice as it supports several common audio file formats including WAV and MP3.

We then tell the output device to play audio from the audio file by using the `Init` method. 

Finally, if all that is done, we can call `Play` on the output device. This method starts playback but won't wait for it to stop.

```c#
private void OnButtonPlayClick(object sender, EventArgs args)
{
    if (outputDevice == null)
    {
        outputDevice = new WaveOutEvent();
        outputDevice.PlaybackStopped += OnPlaybackStopped;
    }
    if (audioFile == null)
    {
        audioFile = new AudioFileReader(@"D:\example.mp3");
        outputDevice.Init(audioFile);
    }
    outputDevice.Play();
}
```

We also need a way to request playback to stop. That's in the stop button click handler, and that's nice and easy. Just call `Stop` on the output device (if we have one).

```c#
private void OnButtonStopClick(object sender, EventArgs args)
{
    outputDevice?.Stop();
}
```

Finally, we need to clean up, and the best place to do that is in the `PlaybackStopped` event handler. Playback can stop for three reasons: 

1. you requested it to stop with `Stop()`
2. you reached the end of the input file
3. there was an error (e.g. you removed the USB headphones you were listening on)

In the handler for `PlaybackStopped` we'll dispose of both the output device and the audio file. Of course, you might not want to do this. Maybe you want the user to carry on playing from where they left off. In which case you'd not dispose of either. But you would probably want to reset the `Position` of the `audioFile` to 0, if it had got to the end, so they could listen again.

```c#
private void OnPlaybackStopped(object sender, StoppedEventArgs args)
{
    outputDevice.Dispose();
    outputDevice = null;
    audioFile.Dispose();
    audioFile = null;
}
```

And that's it. Congratulations, you've played your first audio file with NAudio.

## Example 2 - Supporting Rewind and Resume

In this example, we'll use a similar approach, but this time, when we stop, we won't dispose either the output device or the reader. This means that next time we press play, we'll resume from where we were when we stopped.

I've also added a rewind button. This sets the position of the `AudioFileReader` back to the start by simply setting `Position = 0` 

Obviously it is important that when the form is closed we do properly stop playback and dispose our resources, so we set a `closing` flag to true when the user shuts down the form. This means that when the `PlaybackStopped` event fires, we can dispose of the output device and `AudioFileReader`

Here's the code

```c#
var wo = new WaveOutEvent();
var af = new AudioFileReader(@"example.mp3");
var closing = false;
wo.PlaybackStopped += (s, a) => { if (closing) { wo.Dispose(); af.Dispose(); } };
wo.Init(af);
var f = new Form();
var b = new Button() { Text = "Play" };
b.Click += (s, a) => wo.Play();
var b2 = new Button() { Text = "Stop", Left=b.Right };
b2.Click += (s, a) => wo.Stop();
var b3 = new Button { Text="Rewind", Left = b2.Right };
b3.Click += (s, a) => af.Position = 0;
f.Controls.Add(b);
f.Controls.Add(b2);
f.Controls.Add(b3);
f.FormClosing += (s, a) => { closing = true; wo.Stop(); };
f.ShowDialog();
```

## Example 3 - Adjusting Volume

In this example, we'll build on the previous one by adding in a volume slider. We'll use a WinForms `TrackBar` with value between 0 and 100. 

When the user moves the trackbar, the `Scroll` event fires and we can adjust the volume in one of two ways.

First, we can simply change the volume of our output device. It's important to note that this is a floating point value where 0.0f is silence and 1.0f is the maximum value. So we'll need to divide the value of our `TrackBar` by 100.

```c#
t.Scroll += (s, a) => wo.Volume = t.Value / 100f;
```

Alternatively, the `AudioFileReader` class has a convenient `Volume` property. This adjusts the value of each sample before it even reaches the soundcard. This is more work for the code to do, but is very convenient when you are mixing together multiple files and want to control their volume individually. The `Volume` property on the `AudioFileReader` works just the same, going between 0.0 and 1.0. You can actually provide values greater than 1.0f to this property, to amplify the audio, but this does result in the potential for clipping.

```c#
t.Scroll += (s, a) => af.Volume = t.Value / 100f;
```

Let's see the revised version of our form:

```c#
var wo = new WaveOutEvent();
var af = new AudioFileReader(inputFilePath);
var closing = false;
wo.PlaybackStopped += (s, a) => { if (closing) { wo.Dispose(); af.Dispose(); } };
wo.Init(af);
var f = new Form();
var b = new Button() { Text = "Play" };
b.Click += (s, a) => wo.Play();
var b2 = new Button() { Text = "Stop", Left=b.Right };
b2.Click += (s, a) => wo.Stop();
var b3 = new Button { Text="Rewind", Left = b2.Right };
b3.Click += (s, a) => af.Position = 0;
var t = new TrackBar() { Minimum = 0, Maximum = 100, Value = 100, Top = b.Bottom, TickFrequency = 10 };
t.Scroll += (s, a) => wo.Volume = t.Value / 100f;
// Alternative: t.Scroll += (s, a) => af.Volume = t.Value / 100f;
f.Controls.AddRange(new Control[] { b, b2, b3, t });
f.FormClosing += (s, a) => { closing = true; wo.Stop(); };
f.ShowDialog();
```
