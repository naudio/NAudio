# Pitch Shifting with SmbPitchShiftingSampleProvider

The `SmbPitchShiftingSampleProvider` class provides a fully managed pitch shifter effect.

You pass in the source audio to the constructor, and then use the `PitchFactor` to set the amount of pitch shift. 1.0f means no pitch change, 2.0f means an octave up, and 0.5f means an octave down. To move up one semitone, use the twelfth root of two.

In this simple example, we calculate pitch factors to transpose an audio file up and down a whole tone (two semitones). This demo just plays the first 10 seconds of the audio file.

Note that pitch shifting algorithms do introduce artifacts. It may sound slightly metalic, and the bigger the shift the bigger the effect. But for practicing along to a backing track that's in the wrong key, this can be a great benefit.

```c#
var inPath = @"C:\Users\markh\example.mp3";
var semitone = Math.Pow(2, 1.0/12);
var upOneTone = semitone * semitone;
var downOneTone = 1.0/upOneTone;
using (var reader = new MediaFoundationReader(inPath))
{
    var pitch = new SmbPitchShiftingSampleProvider(reader.ToSampleProvider());
    using(var device = new WaveOutEvent())
    {
        pitch.PitchFactor = (float)upOneTone; // or downOneTone
        // just playing the first 10 seconds of the file
        device.Init(pitch.Take(TimeSpan.FromSeconds(10)));
        device.Play();
        while(device.PlaybackState == PlaybackState.Playing)
        {
            Thread.Sleep(500);
        }
    }
}
```

For an alternative approach to pitch shifting, look at creating a managed wrapper for the SoundTouch library, as explained in [this article](http://markheath.net/post/varispeed-naudio-soundtouch)