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