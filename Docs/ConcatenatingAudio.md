# Concatenating Audio

When you play audio or render audio to a file, you create a single `ISampleProvider` or `IWaveProvider` that represents the whole piece of audio to be played. So playback will continue until you reach the end, and then stop. 

But what if you have two pieces of audio you want to play back to back? The `ConcatenatingSampleProvider` enables you to schedule one or more pieces of audio to play one after the other.

Here's a simple example where we have three audio files that are going to play back to back. Note that the three audio files must have exactly the same sample rate, channel count and bit depth, because it's not possible to change those during playback.

```c#
var first = new AudioFileReader("first.mp3");
var second = new AudioFileReader("second.mp3");
var third = new AudioFileReader("third.mp3");

var playlist = new ConcatenatingSampleProvider(new[] { first, second, third });)

// to play:
outputDevice.Init(playlist);
outputDevice.Play();

// ... OR ... to save to file
WaveFileWriter.CreateWaveFile16("playlist.wav", playlist);
```

Note that the `ConcatenatingSampleProvider` does not provide repositioning. If you want that, you can quite simply copy the code for `ConcatenatingSampleProvider` and adjust it to allow you to rewind, or jump to the beginning of one of the inputs, depending on your specific requirements.

# FollowedBy Extension Helpers

There are some helpful extension methods you can make use of to simplify concatenating. For example, to append one `ISampleProvider` onto the end of another, use `FollowedBy`. Under the hood this simply creates a `ConcatenatingSampleProvider`:

```c#
var first = new AudioFileReader("first.mp3");
var second = new AudioFileReader("second.mp3");
var playlist = first.FollowedBy(second);
```

You can also provide a duration of silence that you want after the first sound has finished and before the second begins:

```c#
var first = new AudioFileReader("first.mp3");
var second = new AudioFileReader("second.mp3");
var playlist = first.FollowedBy(TimeSpan.FromSeconds(1), second);
```

This makes use of an `OffsetSampleProvider` in conjunction with a `ConcatenatingSampleProvider`