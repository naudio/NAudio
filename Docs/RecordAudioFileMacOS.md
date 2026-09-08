## Record an Audio File on macOS with Core Audio

The `NAudio.MacOS` package provides `CoreAudioRecorder`, which records
from a Core Audio input device. It ships pre-release only while its API
settles, so `--prerelease` is required:

```sh
dotnet add package NAudio.MacOS --prerelease
```

`CoreAudioRecorder` is **not** an `IWaveIn`, so the pattern differs from
`WaveIn` and `AlsaIn` in two ways. There is an explicit
`InitializeRecording()` step before `StartRecording()`, and the capture
format is not something you request — the HAL decides it, and you read
it back from `CaptureFormat` once initialized:

```c#
using NAudio.Wave;

using var input = new CoreAudioRecorder();   // default input device
input.InitializeRecording();

var writer = new WaveFileWriter("recorded.wav", input.CaptureFormat);

input.DataAvailable += (audioData, currentTime, firstByteTime) =>
{
    writer.Write(audioData);
};

input.RecordingStopped += (sender, a) =>
{
    writer.Dispose();
    writer = null;
};

input.StartRecording();
Thread.Sleep(5000);                          // record for 5 seconds
input.StopRecording();
```

`audioData` is a `ReadOnlySpan<byte>` pointing at a buffer owned by the
HAL, so write or copy it before the handler returns — storing the span
itself will later read memory that has been reused.

Alternatively, `CaptureAsync` yields `CoreAudioCaptureBuffer` objects,
each with its own `byte[] Buffer`, and initializes the recorder for you:

```c#
using var input = new CoreAudioRecorder();
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

await foreach (var buffer in input.CaptureAsync(cts.Token))
{
    writer.Write(buffer.Buffer, 0, buffer.Buffer.Length);
}
```

Both forms also hand you HAL timestamps (in seconds) for the start of
the I/O cycle and for the first byte captured, which is what you need to
line a recording up against another clock.

### Recording straight to a compressed file

`ExtendedAudioFileWriter` encodes as it writes, using the codecs built
into macOS — so you can record to AAC/M4A, FLAC or CAF with no
third-party encoder. It is a `Stream`, and the target container is
chosen by MIME type:

```c#
using NAudio.Wave;
using NAudio.MacOS.AudioToolbox;

using var writer = ExtendedAudioFileWriter.CreateFromFilePath(
    "recorded.m4a",
    new ExtendedAudioFileWriterSettings
    {
        FileType = "audio/mp4",             // one of SupportedMimeTypes, see below
        ProvidingFormat = input.CaptureFormat,
    },
    overwriteIfExists: true);
```

`ProvidingFormat` describes what you feed in, and must be PCM or IEEE
float. The accepted `FileType` strings vary by macOS version — read them
from `AudioFileLibraryInformation.SupportedMimeTypes` rather than
hard-coding one.

For plain uncompressed output, prefer `WaveFileWriter`: WAV is what
every other tool reads. `AiffFileWriter` in `NAudio.Core` is there if
you want AIFF, and handles the byte-order swap to AIFF's big-endian
layout for you.

### Choosing an input device, and permissions

Enumerate `Devices` and pass one to the constructor:

```c#
using NAudio.MacOS.CoreAudio;

foreach (var device in AudioSystemObject.Instance.Devices)
{
    if (device.GetStreams(AudioObjectPropertyScopeConstants.Input).Length > 0)
    {
        Console.WriteLine($"{device.Name} - {device.Manufacturer}");
    }
}
// using var input = new CoreAudioRecorder(chosenDevice);
```

macOS gates microphone access, and the prompt is attributed to the host
application — for a command-line tool that is the terminal, not your
program. If access has been denied, capture typically yields silence
rather than an error, so check System Settings › Privacy & Security ›
Microphone if your recording comes out empty.
