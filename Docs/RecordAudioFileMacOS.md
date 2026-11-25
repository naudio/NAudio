## Record from a device on macOS with Audio Queue Services

The `NAudio.MacOS` package provides `CoreAudioRecorder`, which records
from a Core Audio input device through Apple's Audio Queue Services. It ships pre-release only while its API
settles, so `--prerelease` is required:

```sh
dotnet add package NAudio.MacOS --prerelease
```

It has to be noted down that the `AudioQueueRecorder` class does *not* implement
`IWaveIn`. As such, it diverges from the classic recording pattern:

- The recording format is not something that you can request.

The sample rate and channel count come from the device. The format does not
change for the duration of the recording, but you cannot
specify it or change it in any way.
The only guarantee that the recorder gives you is that the recording format
will always be either linear PCM or IEEE-floating point (or the initialization will fail).

An example creating a new wave file directly from recording:

```c#
using System.Threading;

using NAudio.Wave;

using var input = new AudioQueueRecorderBuilder().Build();   // default input device

// The CaptureFormat property can be only
// queried after the recorder is constructed!
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

> [!CAUTION]
`audioData` is a `ReadOnlySpan<byte>` pointing at a buffer owned by the
Audio Queue, so write or copy it to your own buffer before the handler returns 
— storing the span itself will later read memory that has been reused.

Alternatively, `CaptureAsync` yields `AudioQueueCaptureBuffer` objects,
each with its own `byte[] Buffer`, and initializes the recorder for you:

```c#
using var input = new AudioQueueRecorderBuilder().Build();
// Record 5 seconds.
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

await foreach (var buffer in input.CaptureAsync(cts.Token))
{
    writer.Write(buffer.Buffer, 0, buffer.Buffer.Length);
}
```

> [!NOTE]
Both recording forms hand you timestamps (in seconds) for the end of
the delivered buffer and for the first byte captured, which is what you need to
line a recording up against another clock.

> [!NOTE]
Creating a `AudioQueueRecorder` instance with the parameterless constructor
may result in a `InvalidOperationException` exception, if there is not a proper
input device that macOS can use to capture audio.

> [!NOTE]
Set `WithLatency` (milliseconds, 100 by default) and `NumberOfBuffers` (4 by default)
in the builder before initializing to trade latency against the risk of dropouts.

### Building recorder instances

The Audio Queue provides a handful of settings that you can use to configure
the recorder before creating an instance of it. For this reason, a builder class
is provided, called `AudioQueueRecorderBuilder` and it is located into the `NAudio.Wave` namespace:

```c#

using System.Threading.Tasks;

using NAudio.MacOS.CoreAudio;

namespace NAudio.Wave;

public sealed class AudioQueueRecorderBuilder
{
    public AudioQueueRecorderBuilder WithLatency(int latencyMs);

    public AudioQueueRecorderBuilder WithNumberOfBuffers(int bufferCount);

    public AudioQueueRecorderBuilder WithDevice(AudioDevice device);

    public AudioQueueRecorderBuilder WithDefaultDeviceStreamRouting();

    public AudioQueueRecorder Build();

    public Task<AudioQueueRecorder> BuildAsync();
}
```

> [!NOTE]
You may also build an Audio Queue Recorder asynchronously, with the `BuildAsync` method.
Note that the method captures the synchornization context of the thread where
`BuildAsync` is called, and the recording stopped events are dispatched on that
synchronization context, if one is set.

#### The `WithLatency` method

For the audio queue to record audio, the class internally allocates buffers
and sets their size based on this value. So, this method specifies the size,
in milliseconds, that each of those buffers will be able to hold data.

Note that the greater this value is, the buffer size becomes larger,
and so the time required to fetch new data from the underlying audio device.

The default latency value assigned by the builder is 0.1 seconds (100 milliseconds).

> [!NOTE]
This method throws `ArgumentOutOfRangeException` if the provided latency value is less than 1.

#### The `WithNumberOfBuffers` method

This can be used to further fine-tune the desired latency that you want
to have on the recorder. As described above, the Audio Queue recorder allocates
bytes of the specified latency that are subsequently used to provide data
to the Audio Queue.

This is useful if, for example, you are working in a constrained I/O environment 
where access to data is limited and can take a large time to complete a single I/O
operation.

On the other side, you set this value to 1 or 2 buffers if you want the lowest latency as possible,
but this incurrs more calls to the `DataAvailable` event.

> [!NOTE]
This method throws `ArgumentOutOfRangeException` if the provided latency value is less than 1.

#### The `WithDevice` method

The default for an Audio Queue player is to follow the default recording device
that macOS uses. With this method, you can specify a Core Audio HAL `AudioDevice`
that indicates the audio device to use for doing recording.

> [!NOTE]
This method throws `ArgumentNullException` if the device you provide to it
is `null`.

##### Choosing a capture device

If you do not want to use the default input device to perform the capture,
you can enumerate the available devices on the system by getting the `Devices`
property on the HAL's audio system object:

```c#
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
// using var createdRecorder = new AudioQueueRecorderBuilder().WithDevice(chosenDevice);
```

#### The `WithDefaultDeviceStreamRouting` method

This method instructs the audio queue to follow the default recording device.
So, this is the exact opposite of what the `WithDevice` method does.
It is useful if you finally settled on following the default device and you want
to undo a previous `WithDevice` call.

### Recording permissions

If the application you are using this API has not explicitly been given permission to capture,
macOS will prompt at the first time to capture from the device.
Even if the user denies access to the device, the HAL does automatically write silence to the provided
buffers for the duration of the recording.

If you want to check that your application has access to the capture device, check
in your macOS System Settings app -&gt; find the Privacy &amp; Security tab on the left and click it -&gt;
scroll down to Microphone section and click it -&gt; find your app.

> [!NOTE]
If you cannot find your app in the list, and you are using the terminal to run your app,
make sure that the Terminal is granted access to the microphone. 
The prompt is attributed to the app that macOS users can easily identify.


