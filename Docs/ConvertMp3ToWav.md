# Convert an MP3 File to a WAV FIle

In this article I will show a few ways you can convert an MP3 file into a WAV file with NAudio.

To start with we'll need a couple of file paths, one to the input MP3 file, and one to where we want to put the converted WAV file.

```c#
var infile = @"C:\Users\Mark\Desktop\example.mp3";
var outfile = @"C:\Users\Mark\Desktop\converted.wav";
```

## Mp3FileReader

The `Mp3FileReader` class uses the ACM MP3 codec that is present on almost all consumer versions of Windows. However, it is important to note that some versions of Windows Server do not have this codec installed without installing the "Desktop Experience" component.

The conversion is straightforward. Open the MP3 file with `Mp3FileReader` and then pass it to `WaveFileWriter.CreateWaveFile` to write the converted PCM audio to a WAV file. This will usually be 44.1kHz 16 bit stereo, but uses whatever format the MP3 decoder emits.

```c#
using(var reader = new Mp3FileReader(infile))
{
    WaveFileWriter.CreateWaveFile(outfile, reader);
}
```

## MediaFoundationReader

`MediaFoundationReader` is a flexible class that allows you to read any audio file formats that Media Foundation supports. This typically includes MP3 on most consumer versions of Windows, but also usually supports WMA, AAC, MP4 and others. So unless you need to support Windows XP or are on a version of Windows without any Media Foundation condecs installed, this is a great choice. Usage is very similar to `Mp3FileReader`:

```c#
using(var reader = new MediaFoundationReader(infile))
{
    WaveFileWriter.CreateWaveFile(outfile, reader);
}
```

## DirectX Media Object
`Mp3FileReader` allows us to plug in alternative MP3 frame decoders. One option that comes in the box with NAudio is the DirectX Media Object MP3 codec. Again, this can only be used if you have that codec installed on Windows, but it comes with most consumer versions of Windows.

Here's how to use the `DmoMp3FrameDecompressor` as a custom frame decompressor

```c#
using(var reader = new Mp3FileReader(infile, wf => new DmoMp3FrameDecompressor(wf)))
{
    WaveFileWriter.CreateWaveFile(outfile, reader);
}
```

## NLayer
The final option is to use [NLayer](https://github.com/naudio/NLayer) as the decoder for `Mp3FileReader`. NLayer is a fully managed MP3 decoder, meaning it can run on any version of Windows (or indeed any .NET platform). You'll need the [NLayer.NAudioSupport nuget package](https://www.nuget.org/packages/NLayer.NAudioSupport/). But then you can plug in a fully managed MP3 frame decoder:

```c#
using (var reader = new Mp3FileReader(infile, wf => new Mp3FrameDecompressor(wf)))
{
    WaveFileWriter.CreateWaveFile(outfile, reader);
}
```

