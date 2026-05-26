# Cross-platform audio file reading and writing with NAudio.SoundFile

The `NAudio.SoundFile` package wraps [libsndfile](https://libsndfile.github.io/libsndfile/) to read and write a wide range of audio formats — WAV, AIFF, FLAC, Ogg/Vorbis, Opus and MP3 — on **Linux, macOS and Windows**. It is the cross-platform counterpart to `MediaFoundationReader` / `MediaFoundationEncoder` (which are Windows-only), and provides the first cross-platform FLAC/Vorbis/Opus *encoder* in NAudio.

`SoundFileReader` is a `WaveStream` **and** an `ISampleProvider`, so it drops straight into any NAudio pipeline. `SoundFileWriter` is a `Stream` sink with the same static helpers as `WaveFileWriter`.

## Installing

`NAudio.SoundFile` is **not** part of the `NAudio` meta-package — reference it explicitly:

```sh
dotnet add package NAudio.SoundFile
```

It P/Invokes a **system libsndfile** (no binary is shipped). Install the runtime library for your platform:

```sh
sudo apt install libsndfile1     # Debian/Ubuntu
brew install libsndfile          # macOS
vcpkg install libsndfile         # Windows (or the official libsndfile release binaries)
```

Which codecs are available depends on how that libsndfile was built; see *Checking codec availability* below.

## Reading any audio file

`SoundFileReader` decodes to 32-bit float. Because it is a `WaveStream`, you can play it through any output device or feed it into any sample pipeline — exactly where you would otherwise reach for `MediaFoundationReader` or `AudioFileReader` on Windows:

```c#
using NAudio.SoundFile;
using NAudio.Wave;

using var reader = new SoundFileReader("song.flac");
Console.WriteLine($"{reader.WaveFormat} {reader.TotalTime}");

using var output = new WasapiPlayerBuilder().Build();   // or AlsaOut on Linux
output.Init(reader);
output.Play();
while (output.PlaybackState == PlaybackState.Playing)
    Thread.Sleep(200);
```

## Encoding to FLAC, Ogg-Vorbis, Opus and MP3

Let's create a 20-second test WAV with `SignalGenerator`, then encode it. `SoundFileWriter` mirrors `WaveFileWriter`'s static helpers — `CreateSoundFile` for an `IWaveProvider` and `CreateSoundFile16` for an `ISampleProvider`:

```c#
var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "NAudio");
Directory.CreateDirectory(dir);
var wavPath = Path.Combine(dir, "test.wav");

WaveFileWriter.CreateWaveFile16(wavPath, new SignalGenerator(44100, 2)
{
    Type = SignalGeneratorType.Sweep,
    Frequency = 500,
    FrequencyEnd = 3000,
    Gain = 0.2f,
    SweepLengthSecs = 20
}.Take(TimeSpan.FromSeconds(20)));
```

FLAC is lossless — there is no bitrate, but you can trade encode speed for size with `CompressionLevel` (0.0 fastest … 1.0 smallest):

```c#
using (var reader = new SoundFileReader(wavPath))
{
    SoundFileWriter.CreateSoundFile(Path.Combine(dir, "test.flac"), reader,
        SoundFileMajorFormat.Flac,
        new SoundFileWriterOptions { CompressionLevel = 0.8 });
}
```

Ogg-Vorbis and Opus are lossy — set `VbrQuality` (0.0 … 1.0):

```c#
using (var reader = new SoundFileReader(wavPath))
{
    SoundFileWriter.CreateSoundFile(Path.Combine(dir, "test.ogg"), reader,
        SoundFileMajorFormat.OggVorbis,
        new SoundFileWriterOptions { VbrQuality = 0.6 });
}

using (var reader = new SoundFileReader(wavPath))
{
    // Opus only supports 8/12/16/24/48 kHz; the writer throws a clear
    // ArgumentException for other rates so resample first if needed.
    SoundFileWriter.CreateSoundFile(Path.Combine(dir, "test.opus"), reader,
        SoundFileMajorFormat.Opus,
        new SoundFileWriterOptions { VbrQuality = 0.7 });
}
```

The output format is also inferred from the file extension, so for the common case you don't even need to name it:

```c#
using (var reader = new SoundFileReader(wavPath))
    SoundFileWriter.CreateSoundFile(Path.Combine(dir, "test.flac"), reader); // → FLAC
```

## Converting between formats

Because the reader decodes anything and the writer encodes anything, conversion is just "read one, write another" — e.g. MP3 → FLAC:

```c#
using (var reader = new SoundFileReader("podcast.mp3"))
{
    SoundFileWriter.CreateSoundFile("podcast.flac", reader, SoundFileMajorFormat.Flac, null);
}
```

The writer accepts **16-bit PCM** or **32-bit IEEE float** input — the two formats NAudio pipelines naturally produce. For anything else, convert with `SampleToWaveProvider16` or `.ToSampleProvider()` first.

## Reading and writing streams

Both ends work over a `System.IO.Stream` (via libsndfile's virtual I/O), so you can encode into a `MemoryStream` or decode from a network/HTTP stream. The stream is **not** disposed by the reader/writer — the caller owns it:

```c#
using var ms = new MemoryStream();
using (var reader = new SoundFileReader("in.wav"))
{
    SoundFileWriter.WriteSoundFileToStream(ms, reader, SoundFileMajorFormat.OggVorbis, null);
}

ms.Position = 0;
using (var back = new SoundFileReader(ms))
{
    // ... decode the in-memory Ogg
}
```

FLAC/Ogg/Opus/MP3 stream fine to a forward-only target; WAV and AIFF back-patch their header at close, so the writer throws early if you pair a non-seekable stream with those.

## Reading and writing tags

Title/artist/album and friends are read on open and can be written via `SoundFileWriterOptions.Tags` (Vorbis comments for FLAC/Ogg/Opus, a limited LIST/INFO set for WAV/AIFF — unsupported fields are simply ignored):

```c#
SoundFileWriter.CreateSoundFile("tagged.flac", source, SoundFileMajorFormat.Flac,
    new SoundFileWriterOptions
    {
        Tags = new SoundFileTags { Title = "Demo", Artist = "NAudio", Album = "Examples" }
    });

using var reader = new SoundFileReader("tagged.flac");
Console.WriteLine($"{reader.Tags.Artist} – {reader.Tags.Title}");
```

## Checking codec availability

FLAC, Vorbis, Opus and MP3 are optional libsndfile build features, so a given install may not have them. Query before encoding rather than catching an exception:

```c#
Console.WriteLine(SoundFileCapabilities.LibraryVersion);   // e.g. "libsndfile-1.2.2"

if (SoundFileCapabilities.IsFormatSupported(SoundFileMajorFormat.Opus))
{
    // safe to encode Opus
}

foreach (var f in SoundFileCapabilities.GetSupportedMajorFormats())
    Console.WriteLine(f);
```

The Debian/Ubuntu `libsndfile1`, Homebrew `libsndfile`, vcpkg `libsndfile`, and the official libsndfile Windows release binaries are all built with the full FLAC/Vorbis/Opus/MP3 set.

## What's out of scope

AAC, M4A, ALAC and WMA are **not** covered — that is the MPEG-4 / FFmpeg family. On Windows those are handled by `MediaFoundationReader` / `MediaFoundationEncoder` (see [Encode to MP3, WMA, AAC and more with MediaFoundationEncoder](MediaFoundationEncoder.md)); there is no cross-platform decoder for them in NAudio.

## See also

- [Play an Audio File on Linux with ALSA](PlayAudioFileLinuxAlsa.md) — `SoundFileReader` is the natural decoder to feed into `AlsaOut`.
- [Encode to MP3, WMA, AAC and more with MediaFoundationEncoder](MediaFoundationEncoder.md) — the Windows-only encoder; `NAudio.SoundFile` is the cross-platform option for FLAC/Ogg/Opus.
