
## Creating new audio files with Extended Audio File Services (macOS)

Along with the new `CoreAudio` HAL backend, the new `NAudio.MacOS` package 
also ships wrappers for the Extended Audio File Services API (Audio Toolbox), 
which is equivalent to `MediaFoundationEncoder`. Let's see what it offers.

First of all, you need to reference the `NAudio.MacOS` package into your project.
Note that it ships pre-release only while its API settles, so `--prerelease` is also required:

~~~C#
dotnet add package NAudio.MacOS --prerelease
~~~

### Writing an audio file

Let's assume that we want to write a new MP4 AAC file (MP3 is not supported).

Let's also assume that we provide the data to write in 16 bit 44.1 kHz PCM with 2 channels:

Doing so is pretty much straightforward:

~~~C#
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
~~~

> [!NOTE]
For plain uncompressed output, prefer `WaveFileWriter`: WAV is what
every other tool reads. `AiffFileWriter` in the Core library is there if
you want AIFF, and handles the byte-order swap to AIFF's big-endian
layout for you.

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

> [!NOTE]
The `ProvidingFormat` setting must be a PCM or IEEE floating-point format.

> [!CAUTION]
Do not try to hardcode the `FileType` property as it's acceptable values
may vary by macOS version. You can read the supported values at
run-time from the `AudioFileLibraryInformation.SupportedMimeTypes` property.


