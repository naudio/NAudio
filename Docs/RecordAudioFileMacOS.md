## Record from a device on macOS with Core Audio

The `NAudio.MacOS` package provides `CoreAudioRecorder`, which records
from a Core Audio (HAL) input device. It ships pre-release only while its API
settles, so `--prerelease` is required:

```sh
dotnet add package NAudio.MacOS --prerelease
```

It has to be noted down that the `CoreAudioRecorder` class does *not* implement
`IWaveIn`. As such, it diverges from the classic recording pattern in two ways:

1. Recording requires an explicit initialization.

This happens because the HAL decides the audio format to record, not us.
Because we have to do several things to ensure that it is possible to record,
this preparation takes some time.

2. The recording format is not something that you can request.

The format is decided by the HAL and may be changed at any time.
It is not something you decide.

An example creating a new wave file directly from recording:

~~~C#
using System.Threading;

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
~~~

> [!CAUTION]
`audioData` is a `ReadOnlySpan<byte>` pointing at a buffer owned by the
HAL, so write or copy it to your own buffer before the handler returns 
— storing the span itself will later read memory that has been reused.

Alternatively, `CaptureAsync` yields `CoreAudioCaptureBuffer` objects,
each with its own `byte[] Buffer`, and initializes the recorder for you:

~~~c#
using var input = new CoreAudioRecorder();
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

await foreach (var buffer in input.CaptureAsync(cts.Token))
{
    writer.Write(buffer.Buffer, 0, buffer.Buffer.Length);
}
~~~

> [!NOTE]
Both recording forms hand you HAL timestamps (in seconds) for the start of
the I/O cycle and for the first byte captured, which is what you need to
line a recording up against another clock.

> [!NOTE]
As it was mentioned before, the capture format may be changed multiple
times during capture. Once that happens, the recording is stopped,
and the `CaptureFormatChanged` event dispatches.
You can restart the recording, or completely abort it.

### Recording permissions

If the application you are using this API has not explicitly been given permission to capture,
macOS will prompt at the first time to capture from the device.
Even if the user denies access to the device, HAL does automatically write silence to the provided
buffers for the duration of the recording.

If you want to check that your application has access to the capture device, check
in your macOS System Settings app -> go to Privacy &amp; Security -> scroll down to Microphone section and click it ->
find your app.

> [!NOTE]
If you cannot find your app in the list, and you are using the terminal to run your app,
make sure that the Terminal is granted access to the microphone. 
The prompt is attributed to the app that macOS users can easily identify.

### Selecting a different capture device than the default

If you do not want to use the default input device to perform the capture,
you can enumerate the available devices on the system by getting the `Devices`
property on the HAL's audio system object:

~~~C#
using NAudio.MacOS.CoreAudio;

// Enumerate all the devices that provide input.
foreach (var device in AudioSystemObject.Instance.Devices)
{
    if (device.GetStreams(AudioObjectPropertyScopeConstants.Input).Length > 0)
    {
        Console.WriteLine($"{device.Name} - {device.Manufacturer}");
    }
}
// Once you have selected an audio device, you can give it to the recorder:
// using var createdRecorder = new CoreAudioRecorder(chosenDevice);
~~~
