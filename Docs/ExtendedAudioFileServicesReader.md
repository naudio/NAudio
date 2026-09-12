
### Reading audio files with Extended Audio File Services (macOS)

Along with the new `CoreAudio` HAL backend, the new `NAudio.MacOS` package 
also ships wrappers for the Extended Audio File Services API (Audio Toolbox), 
which is equivalent to `MediaFoundationReader`. Let's see what it offers.

First of all, you need to reference the `NAudio.MacOS` package into your project.
Note that it ships pre-release only while its API settles, so `--prerelease` is also required:

~~~C#
dotnet add package NAudio.MacOS --prerelease
~~~

### Reading an audio file

Let's assume that you want to read an MP3 file.
With the reader of Extended Audio File Services, you can do it this way:

~~~C#
using NAudio.Wave;

using var reader = ExtendedAudioFileReaderFromURL.CreateFromFile(
    "/Users/user_name/a_file.mp3"
);
~~~

You can of course specify any file path; the path provided here is purely illustrative.

It is possible to open a file from a .NET data stream as well:

~~~C#
using NAudio.Wave;

using var reader = new ExtendedAudioFileReaderFromStream(
    dataStream
);
~~~

> [!NOTE]
The reader extends the `WaveStream` class, so you have a seekable
(repositionable) source of data for any file supported through the reader.

> [!NOTE]
The reader does the best effort to decode to PCM, as such you will only ever
recieve PCM audio data through the reader, or it will fail fast during initialization.

### Supported formats

The reader has been tested and verified to work with:

- MP3
- MP4/AAC
- CAF
- WAV
- AIFF
- FLAC
- OGG Vorbis

Which of those formats is actually supported and which not depends on 
the macOS version you use.
You are able to query the supported file formats as MIME types at run-time:

~~~C#
using System;
using NAudio.MacOS.AudioToolbox;

foreach (var mimeType in AudioFileLibraryInformation.SupportedMimeTypes)
{
    Console.WriteLine(mimeType);
}
~~~

### Modifying the initialization behavior of the reader.

Both of these reader classes do accept a settings object that modifies 
how the reader should initialize. That settings class is the `ExtendedAudioFileReaderSettings`
class, and it is located into `NAudio.MacOS.AudioToolbox`:

~~~C#
using NAudio.Wave;

namespace NAudio.MacOS.AudioToolbox;

public class ExtendedAudioFileReaderSettings
{
    public bool RequestIeeeFloat { get; set; }

    public bool AllowNonPowerOfTwoBitDepths { get; set; }

    public WaveFormat OutputFormat { get; set; }
}
~~~

#### The `RequestIeeeFloat` property

When you assign this property to `true`, you instruct the reader 
to return 32-bit IEEE floating-point audio samples. It is useful 
for injecting the reader into an `ISampleProvider` chain,
and you can also very easily manipulate the samples.

Even though that the reader does not implement `ISampleProvider`
inherently, you can use the `ToSampleProvider` extension method
to get an `ISampleProvider` that will give you the floating-point samples.

The default value of this property is `false`, which indicates to the reader
to use whatever format the file is into, and convert it to it's closest PCM format.

#### The `AllowNonPowerOfTwoBitDepths` property

When you assign this property to `true`, you instruct the reader to use whatever
bit depth the audio format is into. Most of the times, the samples of the audio files
are subject to be modified in a way, so this assures that you can use a numeric
type that you can easily handle, if you are doing the processing yourself.

If, however, you do not intend to somehow modify the samples, you can keep this to `false`.

> [!WARNING]
When the `RequestIeeeFloat` property is defined to `true`, that will take precendence
over this one as the reader will go ahead and directly specify a 32-bit IEEE floating-point format.

#### The `OutputFormat` property

While the internal algorithm does a decent job defining a good PCM or IEEE floating-point format,
you might need a very specific format that you need to read the file into.
With this property, you can specify a completely specified PCM or IEEE floating-point format
to use. It also supports specifying the format with `WaveFormatExtensible` and honours the provided `ChannelMask`.

The default value of this property is `null`, which instructs 
the reader to use the internal algorithm to define the output format.

> [!WARNING]
When this property is appropriately defined, it takes precedence over the `RequestIeeeFloat`
and the `AllowNonPowerOfTwoBitDepths` properties as these properties are solely existing
for modifying the behavior of the internal algorithm.

There is also an extended variant of this settings object, provided only
for when you work with streams. This is the `ExtendedAudioFileReaderFromStreamSettings` class,
and derives from `ExtendedAudioFileReaderSettings`, so it inherits all the properties from it:

~~~C#
using NAudio.MacOS.AudioToolbox;

namespace NAudio.Wave;

public sealed class ExtendedAudioFileReaderFromStream : ExtendedAudioFileServicesReader
{
    public sealed class ExtendedAudioFileReaderFromStreamSettings : ExtendedAudioFileReaderSettings
    {
        public string MimeType { get; set; }

        public string FileName { get; set; }
    }
}
~~~

#### The `MimeType` property

A MIME type string treated as a hint to the reader that will better help to identify the type
of the audio file being decoded. 

It is optional, and the reader does a decent job finding the audio file type of the provided stream.

The MIME types can be queried at run-time using this code:

~~~C#
using System;
using NAudio.MacOS.AudioToolbox;

foreach (var mimeType in AudioFileLibraryInformation.SupportedMimeTypes)
{
    Console.WriteLine(mimeType);
}
~~~

> [!CAUTION]
Try not to hardcode the MIME type given to the property as it subject to change
in different macOS versions. Make sure to call first the `SupportedMimeTypes` property
and use one of those values.

#### The `FileName` property

There might be cases that you cannot know the MIME type of the stream, but
you know it's name. So, you can provide it to this property.
You may also just provide a bare file extension, such as `.m4a`.

Just like the `MimeType` property, this is just a hint and the reader
does a decent job finding the audio file type of the provided stream.

> [!CAUTION]
If you specify both `FileName` and `MimeType` properties, the reader
will prefer to use the `MimeType` property as the MIME type property
is a formal description of the data stream.

The supported file types (by extension) can be also queried with this snippet:

~~~C#
using System;
using NAudio.MacOS.AudioToolbox;

foreach (var extension in AudioFileLibraryInformation.RecognizedFileExtensions)
{
    Console.WriteLine(mimeType);
}
~~~

> [!CAUTION]
The file extensions returned through the `RecognizedFileExtensions` property
do *not* have a dot prepended to them. Make sure to prepend a dot if you want
to use them with the `System.IO` API's.

### Play back a MP3 file.

This rather simple code example illustrates how to read an MP3 file
using the Extended Audio File Services reader and use the `CoreAudioPlayer` API to play it back.

Make sure to read the [`CoreAudioPlayer`](PlayAudioFileMacOS.md) document 
first to understand what the below does, if you have not.


~~~C#
using NAudio.Wave;

using var reader = ExtendedAudioFileReaderFromURL.CreateFromFile(
    "/Users/user_name/a_second_file.mp3"
);

using var player = new CoreAudioPlayer();

player.Init(reader);
player.Play();

while (player.PlaybackState == PlaybackState.Playing)
{
    System.Threading.Thread.Sleep(400);
}
~~~
