# Encode to MP3, WMA and AAC with MediaFoundationEncoder

The `MediaFoundationEncoder` class allows you to use any Media Foundation transforms (MFTs) on your computer to encode files in a variety of common audio formats including MP3, WMA and AAC. However, not all versions of Windows will come with these installed. Media Foundation is available on Windows Vista and above, and Server versions of Windows do not typically have the Media Foundation codecs installed (you can add them by installing the "desktop experience" component. 

To get started, let's create an audio folder on the desktop and also create a simple 20 second WAV file that we can use as an input file. I'll use a combination of the `SignalGenerator` and the `Take` extension method to feed into `WaveFileWriter.CreateWaveFile16` to do that:

```c#
var outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "NAudio");
Directory.CreateDirectory(outputFolder);
var testFilePath = Path.Combine(outputFolder, "test.wav");
// create a test file
WaveFileWriter.CreateWaveFile16(testFilePath, new SignalGenerator(44100,2) 
{   Type = SignalGeneratorType.Sweep, 
    Frequency = 500, 
    FrequencyEnd = 3000, 
    Gain = 0.2f, 
    SweepLengthSecs = 20
}.Take(TimeSpan.FromSeconds(20)));
```
## Initialize Media Foundation

We also need to ensure we've initialized Media Foundation. If we forget this we'll get a `ComException` of `0xC00D36B0` (`MF_E_PLATFORM_NOT_INITIALIZED`)

```c#
MediaFoundationApi.Startup();
```

## Converting WAV to WMA

`MediaFoundationEncoder` includes some static helper methods to make encoding very straightforward. Let's create a WMA file first, as the WMA encoder is available with almost all versions of Windows. We just need to call the `EncodeToWma` method, passing in the source audio (a `WaveFileReader` in our case) and the output file path. We can also specify a desired bitrate and it will automatically try to find the bitrate closest to what we ask for.

```c#
var wmaFilePath = Path.Combine(outputFolder, "test.wma");
using (var reader = new WaveFileReader(testFilePath))
{
    MediaFoundationEncoder.EncodeToWma(reader, wmaFilePath);
}
```

## Converting WAV to AAC
Windows 7 came with an AAC encoder. So we can create MP4 files with AAC encoded audio in them like this:

```c#
var aacFilePath = Path.Combine(outputFolder, "test.mp4");
using (var reader = new WaveFileReader(testFilePath))
{
    MediaFoundationEncoder.EncodeToAac(reader, aacFilePath);
}
```

### Converting WAV to MP3
Windows 8 came with an MP3 encoder. So we can also convert our WAV file to MP3. This time, let's catch the exception if there isn't an available encoder:

```c#

var mp3FilePath = Path.Combine(outputFolder, "test.mp3");
using (var reader = new WaveFileReader(testFilePath))
{
    try
    {
        MediaFoundationEncoder.EncodeToMp3(reader, mp3FilePath);
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine(ex.Message);
    }
}
```

## Converting from other input formats

We've used `WaveFileReader` in all our examples so far. But we can use the same technique using `MediaFoundationReader`. This will allow us to convert files of a whole variety of types MP3, WMA, AAC, etc into anything we have an encoder for. Let's convert our WMA file into AAC

```c'
var aacFilePath2 = Path.Combine(outputFolder, "test2.mp4");
using (var reader = new MediaFoundationReader(wmaFilePath))
{
    MediaFoundationEncoder.EncodeToAac(reader, aacFilePath2);
}
```

## Extracting audio from online video files

As one final example, let's see that we can use `MediaFoundationReader` to read a video file directly from a URL and then convert its audio to an Mp3 file:

```
var videoUrl = "https://sec.ch9.ms/ch9/0334/cf0bd333-9c8a-431e-bc62-8089aea60334/WhatsCoolFallCreators.mp4";
var mp3Path2 = Path.Combine(outputFolder, "test2.mp3");
using (var reader = new MediaFoundationReader(videoUrl))
{
    MediaFoundationEncoder.EncodeToMp3(reader, mp3Path2);
}
```