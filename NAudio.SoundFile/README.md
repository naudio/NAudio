# NAudio.SoundFile

Cross-platform audio file reading and writing for NAudio, backed by
[libsndfile](https://libsndfile.github.io/libsndfile/).

| Type | Role |
|---|---|
| `SoundFileReader` | `WaveStream` + `ISampleProvider` — decode WAV/AIFF/FLAC/Ogg/Opus/MP3 |
| `SoundFileWriter` | `Stream` sink — **encode** WAV/AIFF/FLAC/Ogg-Vorbis/Opus/MP3 |
| `SoundFileCapabilities` | query which codecs the installed libsndfile supports |
| `SoundFileException` | thrown on libsndfile errors (exposes `ErrorCode`) |

This is the first cross-platform FLAC/Vorbis/Opus **encoder** in NAudio,
and on Linux/macOS the first general-purpose decoder (there is no Media
Foundation off Windows).

## Platform

Truly cross-platform — `net9.0`, no `[SupportedOSPlatform]`. It
P/Invokes a **system libsndfile**, resolved automatically per OS
(`libsndfile.so.1` / `libsndfile.1.dylib` / `sndfile.dll` /
`libsndfile-1.dll`). You must provide it:

```sh
sudo apt install libsndfile1     # Debian/Ubuntu
brew install libsndfile          # macOS
vcpkg install libsndfile         # Windows (or the official binaries)
```

Not pulled in by the `NAudio` meta-package — reference it explicitly:

```sh
dotnet add package NAudio.SoundFile
```

## Supported formats

| Format | Read | Write | Requires |
|---|---|---|---|
| WAV, AIFF, AU, CAF, W64, RAW | ✅ | ✅ | always |
| FLAC | ✅ | ✅ | libsndfile built with libFLAC (typical) |
| Ogg/Vorbis | ✅ | ✅ | libvorbis (typical) |
| Ogg/Opus | ✅ | ✅ | libsndfile ≥ 1.0.29 + libopus |
| MP3 | ✅ | ✅ | libsndfile ≥ 1.1.0 |

Codec availability depends on how libsndfile was built — query it:

```c#
foreach (var f in SoundFileCapabilities.GetSupportedMajorFormats())
    Console.WriteLine(f);
bool canFlac = SoundFileCapabilities.IsFormatSupported(SoundFileMajorFormat.Flac);
```

AAC / M4A / ALAC / WMA are out of scope (the MPEG-4 family is FFmpeg
territory).

## Read any file

`SoundFileReader` decodes to 32-bit float, so it is both a `WaveStream`
and an `ISampleProvider`:

```c#
using NAudio.SoundFile;
using NAudio.Wave;

using var reader = new SoundFileReader("song.flac");
Console.WriteLine($"{reader.WaveFormat} {reader.TotalTime}");
// feed it into any NAudio output, mixer or sample pipeline
```

## Write FLAC / Ogg / Opus

```c#
using (var source = new SoundFileReader("in.wav"))
    SoundFileWriter.CreateSoundFile("out.flac", source,
        SoundFileMajorFormat.Flac,
        new SoundFileWriterOptions { CompressionLevel = 0.8 });

// Ogg Vorbis at VBR quality 0.6, with tags
using (var source = new SoundFileReader("in.wav"))
    SoundFileWriter.CreateSoundFile("out.ogg", source,
        SoundFileMajorFormat.OggVorbis,
        new SoundFileWriterOptions
        {
            VbrQuality = 0.6,
            Tags = new SoundFileTags { Title = "Demo", Artist = "NAudio" }
        });
```

Read embedded metadata back:

```c#
using var reader = new SoundFileReader("song.flac");
Console.WriteLine($"{reader.Tags.Artist} – {reader.Tags.Title}");
Console.WriteLine(SoundFileCapabilities.LibraryVersion);
```

The output format is also inferred from the extension:

```c#
SoundFileWriter.CreateSoundFile("out.flac", source); // → FLAC
```

## Streams

Both ends work over a `System.IO.Stream` (via libsndfile virtual I/O):

```c#
using var ms = new MemoryStream();
SoundFileWriter.WriteSoundFileToStream(ms, source, SoundFileMajorFormat.OggVorbis, null);
ms.Position = 0;
using var reader = new SoundFileReader(ms);   // stream not disposed by the reader
```

FLAC/Ogg/Opus/MP3 stream fine to a forward-only target; WAV/AIFF
back-patch their header at close and require a seekable stream (the
writer throws early if you pair a non-seekable stream with such a
format).

## Notes

- The writer accepts **16-bit PCM** or **32-bit IEEE float** input — the
  two container types NAudio pipelines naturally produce. Convert other
  formats with `SampleToWaveProvider16` or `.ToSampleProvider()` first.
- AOT-compatible: source-generated `[LibraryImport]`, `SafeHandle`
  lifetime, and `[UnmanagedCallersOnly]` virtual-I/O callbacks.
- The wrapper is MIT; libsndfile itself is LGPL-2.1+ and supplied by the
  user as a system library (no binary is shipped) — the same model as
  `NAudio.Alsa`.
