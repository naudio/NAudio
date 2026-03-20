# Convert an MP3 File to a WAV File

In this article I will show a few ways you can convert an MP3 file into a WAV file with NAudio.

To start with we'll need a couple of file paths, one to the input MP3 file, and one to where we want to put the converted WAV file.

```c#
var infile = @"C:\Users\Mark\Desktop\example.mp3";
var outfile = @"C:\Users\Mark\Desktop\converted.wav";
```

## MediaFoundationReader (recommended)

`MediaFoundationReader` is the recommended approach for reading MP3 files (and many other formats) in NAudio. It uses Media Foundation which is available on all supported versions of Windows. It can read MP3, WMA, AAC, FLAC, Opus and many other formats.

```c#
using(var reader = new MediaFoundationReader(infile))
{
    WaveFileWriter.CreateWaveFile(outfile, reader);
}
```

## Mp3FileReader

The `Mp3FileReader` class uses the ACM MP3 codec that is present on most versions of Windows. The conversion is straightforward. Open the MP3 file with `Mp3FileReader` and then pass it to `WaveFileWriter.CreateWaveFile` to write the converted PCM audio to a WAV file. This will usually be 44.1kHz 16 bit stereo, but uses whatever format the MP3 decoder emits.

```c#
using(var reader = new Mp3FileReader(infile))
{
    WaveFileWriter.CreateWaveFile(outfile, reader);
}
```

## DirectX Media Object

`Mp3FileReaderBase` allows us to plug in alternative MP3 frame decoders. One option that comes in the box with NAudio is the DirectX Media Object MP3 codec.

Here's how to use the `DmoMp3FrameDecompressor` as a custom frame decompressor:

```c#
using(var reader = new Mp3FileReaderBase(infile, wf => new DmoMp3FrameDecompressor(wf)))
{
    WaveFileWriter.CreateWaveFile(outfile, reader);
}
```

## NLayer

The final option is to use [NLayer](https://github.com/naudio/NLayer) as the decoder for `Mp3FileReader`. NLayer is a fully managed MP3 decoder, meaning it can run on any .NET platform including cross-platform scenarios where Windows codecs are not available. You'll need the [NLayer.NAudioSupport NuGet package](https://www.nuget.org/packages/NLayer.NAudioSupport/). Then you can plug in a fully managed MP3 frame decoder:

```c#
using (var reader = new Mp3FileReaderBase(infile, wf => new Mp3FrameDecompressor(wf)))
{
    WaveFileWriter.CreateWaveFile(outfile, reader);
}
```
