
## Creating new audio files with Extended Audio File Services (macOS)

Along with the new `CoreAudio` HAL backend, the new `NAudio.MacOS` package 
also ships wrappers for the Extended Audio File Services API (Audio Toolbox), 
which is equivalent to `MediaFoundationEncoder`. Let's see what it offers.

First of all, you need to reference the `NAudio.MacOS` package into your project.
Note that it ships pre-release only while its API settles, so `--prerelease` is also required:

```c#
dotnet add package NAudio.MacOS --prerelease
```

### Writing an audio file

Let's assume that we want to write a new MP4 AAC file (MP3 is not supported).

Let's also assume that we provide the data to write in 16 bit 44.1 kHz PCM with 2 channels:

Doing so is pretty much straightforward:

```c#
using NAudio.Wave;
using NAudio.MacOS.AudioToolbox;

var writer = ExtendedAudioFileWriter.CreateToFilePath(
    "/Users/your_name/a_file.m4a",
    new ExtendedAudioFileWriterSettings()
    {
        FileType = "audio/mp4",
        ProvidingFormat = new WaveFormat(44100, 16, 2) // The format of the audio data you provide to the writer.
    },
    true // Overwrites the file, if /Users/your_name/a_file.m4a exists. If false, it throws an exception if the file exists.
);

// Write the data you want to write. Use writer.Write(Span<byte>) to do so.
// Close the writer with Dispose() so that the writer can patch and finalize the file for consumption.
```

> [!NOTE]
The writer also supports creating a file into a .NET data stream.
The same settings apply to that case, except for the last parameter,
which it does not exist. See the `CreateToStream` static method
in `ExtendedAudioFileWriter` for more information.

> [!CAUTION]
For the writer to write files into a .NET `System.IO.Stream`, the object
must support all the operations, that is, reading, writing and seeking.
If your object is not that capable, use a `MemoryStream` so that
the writer can handle the stream, then copy it's data to your 
target stream.

### Settings supported by the writer

The writer requires a settings object, which you must fill in before creating the writer.
That settings object is the `ExtendedAudioFileWriterSettings` class,
located into the `NAudio.MacOS.AudioToolbox` namespace:

```c#
using NAudio.Wave;

namespace NAudio.MacOS.AudioToolbox;

public class ExtendedAudioFileWriterSettings
{
    public string? FileType { get; set; }

    public WaveFormat? ProvidingFormat { get; set; }

    public bool EmitIEEEFloatSamples { get; set; } = false;

    public int FramesPerPacket { get; set; } = 1024;
}
```

#### The `FileType` property

This property controls the final container and audio data format of 
the file. With this property you define the format of the resulting 
file and as such it is a required property.

> [!NOTE]
Although the property is marked as a nullable property, the writer
requires this property to have been defined so that it can pick 
the format you want to save your audio data into.

> [!CAUTION]
Do not try to hardcode this property as it's acceptable values
may vary by macOS version. You can read the supported values at
run-time from the `AudioFileLibraryInformation.SupportedMimeTypes` property.
Note too that the `SupportedMimeTypes` property advertises the readable
formats as well, so several types reported by the property may not be
supported. The writer will reject the value of the property if it is not supported.

#### The `ProvidingFormat` property

This property controls the audio data format you provide your audio as.
The writer requires that this property contains a valid PCM or an IEEE floating-point
format. Anything else will be rejected by the writer right away.

> [!NOTE]
Although the property is marked as a nullable property, the writer
requires this property to have been defined so that it can choose
the audio data format under which you provide data to it. The file's
data format is decided by the writer's algorithms based on the format
that you provide in this property and you cannot modify it however you like.

#### The `EmitIEEEFloatSamples` property.

When you write a file into an uncompressed format, this property 
controls whether the samples in the file should be saved as 32-bit IEEE 
floating-point samples. This property is only supported for uncompressed
formats, and only if the uncompressed format supports using it.

> [!NOTE]
This property is not effective on compressed formats, as they define
their own data formats to store the files.

> [!NOTE]
For plain uncompressed output, prefer `WaveFileWriter`: WAV is what
every other tool reads. `AiffFileWriter` in the Core library is there if
you want AIFF, and handles the byte-order swap to AIFF's big-endian
layout for you.

#### The `FramesPerPacket` property.

This property controls the number of sample frames to store in each compressed
packet in the format under which the writer is writing data into.
It might be helpful in some cases to modify this property to achieve better
compression of your audio data in the target file. Note that this property
is just a hint to the writer and whether this property will be used or not
depends on the active encoder.

> [!NOTE]
This property is not effective on uncompressed formats, as they are 
simply storing the samples as-is into the target file.
