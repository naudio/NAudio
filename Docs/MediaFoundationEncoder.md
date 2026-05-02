# Encode to MP3, WMA, AAC and more with MediaFoundationEncoder

The `MediaFoundationEncoder` class allows you to use any Media Foundation transforms (MFTs) on your computer to encode files in a variety of common audio formats including MP3, WMA, AAC, FLAC and ALAC. The available encoders depend on your version of Windows - Windows 10 and above includes encoders for all of these formats.

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

## Converting WAV to WMA

`MediaFoundationEncoder` includes some static helper methods to make encoding very straightforward. Let's create a WMA file first. We just need to call the `EncodeToWma` method, passing in the source audio (a `WaveFileReader` in our case) and the output file path. We can also specify a desired bitrate and it will automatically try to find the bitrate closest to what we ask for.

```c#
var wmaFilePath = Path.Combine(outputFolder, "test.wma");
using (var reader = new WaveFileReader(testFilePath))
{
    MediaFoundationEncoder.EncodeToWma(reader, wmaFilePath);
}
```

## Converting WAV to AAC

Creating MP4 files with AAC encoded audio is just as simple:

```c#
var aacFilePath = Path.Combine(outputFolder, "test.mp4");
using (var reader = new WaveFileReader(testFilePath))
{
    MediaFoundationEncoder.EncodeToAac(reader, aacFilePath);
}
```

## Converting WAV to MP3

Converting to MP3 is equally straightforward. Let's also catch the exception if there isn't an available encoder:

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

## Encoding to FLAC

Windows 10 and later include a FLAC (Free Lossless Audio Codec) encoder. FLAC is lossless, so there is no bitrate to choose — `EncodeToFlac` is a simple helper analogous to `EncodeToMp3` / `EncodeToAac` / `EncodeToWma`:

```c#
var flacFilePath = Path.Combine(outputFolder, "test.flac");
using (var reader = new WaveFileReader(testFilePath))
{
    MediaFoundationEncoder.EncodeToFlac(reader, flacFilePath);
}
```

Some Windows SKUs only advertise 24-bit output media types for FLAC at certain sample rates. The helper prefers a media type whose bit depth matches the input but falls back to the first rate/channels match if no exact bps match exists. For best results, supply PCM input at 16- or 24-bit depending on what the encoder offers for your sample rate.

## Apple Lossless (ALAC) is not supported

Windows 10+ ships an ALAC encoder MFT, but using it via the MP4 sink writer is impractical. Microsoft does not document the encoder, and the MP4 sink rejects every codec-private layout we've tried (bare 24-byte `ALACSpecificConfig`, FFmpeg's 36-byte `'alac'`-FullBox wrapper) with `MF_E_SINK_HEADERS_NOT_FOUND` (`0xC00D4A45`) at the first `WriteSample`. NAudio therefore does not provide an `EncodeToAlac` helper. ALAC playback through `MediaFoundationReader` works fine; it's only encode that's blocked. If you need ALAC encoding, look at FFmpeg or a dedicated ALAC encoder library.

## Opus is read-only on Windows

Windows 10 1903+ and Windows 11 ship a Media Foundation **decoder** for Opus, but no encoder. `MediaFoundationEncoder.GetOutputMediaTypes(AudioSubtypes.MFAudioFormat_Opus)` returns an empty array on every stock Windows install, and there is no `MFTranscodeContainerType_Opus` in the platform headers. As a result NAudio does not provide an `EncodeToOpus` helper — it would always throw "no suitable encoders".

Opus **playback** works through `MediaFoundationReader` for `.mka` (Matroska) and `.webm` containers carrying Opus tracks, and is exposed through `AudioFileReader` and the demo apps just like any other format.

If you need Opus **encoding** in .NET, the recommended option is the [Concentus](https://github.com/lostromb/concentus) pure-managed port of `libopus` (NuGet: `Concentus`). It supports both encoding and decoding, and the companion [Concentus.OggFile](https://github.com/lostromb/concentus.oggfile) package handles the Ogg container around encoded frames. NAudio integration is straightforward — feed PCM samples from any `IWaveProvider` / `ISampleProvider` into a `Concentus.OpusEncoder` and write the encoded packets out via `OpusOggWriteStream`.

## Converting from other input formats

We've used `WaveFileReader` in all our examples so far. But we can use the same technique using `MediaFoundationReader`. This will allow us to convert files of a whole variety of types (MP3, WMA, AAC, FLAC, Opus, etc.) into anything we have an encoder for. Let's convert our WMA file into AAC:

```c#
var aacFilePath2 = Path.Combine(outputFolder, "test2.mp4");
using (var reader = new MediaFoundationReader(wmaFilePath))
{
    MediaFoundationEncoder.EncodeToAac(reader, aacFilePath2);
}
```

## Encoding to a Stream

You can also encode to a `Stream` instead of a file path. This is useful when you want to write to a `MemoryStream` or network stream. All three static helpers (`EncodeToMp3`, `EncodeToAac`, `EncodeToWma`) have overloads that accept a `Stream`:

```c#
using (var reader = new WaveFileReader(testFilePath))
using (var outputStream = new MemoryStream())
{
    MediaFoundationEncoder.EncodeToMp3(reader, outputStream);
}
```

## Querying available bitrates

You can query the available bitrates for a given encoder format, sample rate and channel count:

```c#
var bitrates = MediaFoundationEncoder.GetEncodeBitrates(AudioSubtypes.MFAudioFormat_AAC, 44100, 2);
foreach (var bitrate in bitrates)
{
    Console.WriteLine($"{bitrate / 1000} kbps");
}
```

## Extracting audio from video files

As one final example, `MediaFoundationReader` can read video files and extract the audio track. This works with both local files and URLs:

```c#
var mp3Path2 = Path.Combine(outputFolder, "extracted.mp3");
using (var reader = new MediaFoundationReader("video.mp4"))
{
    MediaFoundationEncoder.EncodeToMp3(reader, mp3Path2);
}
```
