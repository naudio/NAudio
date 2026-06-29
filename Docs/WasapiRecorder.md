# Recording Audio with WasapiRecorder

`WasapiRecorder` is NAudio 3's modern WASAPI capture device. It is the recommended way to capture audio through WASAPI, superseding the older [`WasapiCapture` and `WasapiLoopbackCapture`](WasapiLoopbackCapture.md). It adds:

- **Zero-copy capture** — the `DataAvailable` event hands you a `ReadOnlySpan<byte>` directly over the WASAPI buffer.
- **MMCSS thread priority** — register the capture thread with the Multimedia Class Scheduler Service.
- **`IAsyncEnumerable` support** — consume captured audio with `await foreach` via `CaptureAsync`.
- **`IAudioClient3` low-latency capture** — open the stream at the engine's minimum period via `WithLowLatency()`.
- **A fluent builder** (`WasapiRecorderBuilder`) covering microphone capture and loopback capture.
- **`IAsyncDisposable`** for non-blocking teardown.

## Creating a WasapiRecorder

You create a `WasapiRecorder` through `WasapiRecorderBuilder`. With no configuration, it captures from the default capture device (microphone) in shared mode with event synchronization:

```c#
using NAudio.Wave;

var recorder = new WasapiRecorderBuilder().Build();
```

The builder methods are chainable:

```c#
var recorder = new WasapiRecorderBuilder()
    .WithDevice(someMMDevice)        // default: system default capture device
    .WithExclusiveMode()             // default: shared mode
    .WithEventSync()                 // default: event sync (vs WithPollingSync)
    .WithBufferLength(50)            // default: 100ms
    .WithFormat(new WaveFormat(44100, 16, 2))  // default: device mix format
    .WithMmcssThreadPriority("Pro Audio")
    .Build();
```

In shared mode the engine converts to the format you request via `WithFormat`. If you don't request one, the device's mix format is used and exposed on the `WaveFormat` property after building.

## Recording with the DataAvailable event

The zero-copy path uses the `DataAvailable` event. The span is **only valid for the duration of the callback** — if you need to keep the data (e.g. to write to a file), copy it out. The callback also receives the WASAPI `devicePosition` (frame count at the start of the packet) and `qpcPosition` (QueryPerformanceCounter value in 100-nanosecond units) for that packet, which you can use to detect gaps or time-align the audio against a wall clock; ignore them with discards (`_`) if you don't need them. Here we record to a WAV file:

```c#
using NAudio.Wave;

var recorder = new WasapiRecorderBuilder().Build();
var writer = new WaveFileWriter("recorded.wav", recorder.WaveFormat);

recorder.DataAvailable += (buffer, flags, devicePosition, qpcPosition) =>
{
    writer.Write(buffer);   // WaveFileWriter has a ReadOnlySpan<byte> overload
};

recorder.RecordingStopped += (s, a) =>
{
    writer.Dispose();
    writer = null;
    recorder.Dispose();
};

recorder.StartRecording();
// ... record for a while ...
recorder.StopRecording();
```

As with the playback device, if a `SynchronizationContext` was present when the recorder was constructed, `RecordingStopped` is raised on that context.

## Low-latency capture

For real-time scenarios — live visualization, level metering, monitoring — `WithLowLatency()` opens
the capture stream through `IAudioClient3` at the audio engine's *minimum* supported period instead of
the configured buffer length. This typically drops capture latency to a few milliseconds at the cost
of a higher wake-up frequency (more, smaller `DataAvailable` callbacks).

```c#
var recorder = new WasapiRecorderBuilder()
    .WithLowLatency()
    .Build();

recorder.StartRecording();
// recorder.LowLatencyActive    -> true when low latency actually engaged
// recorder.LatencyMilliseconds -> the period the engine granted (e.g. 10ms)
```

Low-latency shared capture does **no** format conversion, so it has preconditions: shared mode,
event-driven sync (both defaults), no loopback, `IAudioClient3` support (Windows 10 version 1607 or
later), and a capture format matching the device mix format — so don't combine it with `WithFormat`
requesting a different format, `WithLoopbackCapture`, `WithExclusiveMode`, `WithPollingSync`, or
`WithProcessLoopback`.

By default, if any precondition isn't met capture **silently falls back** to standard shared mode;
inspect `LowLatencyActive` to see what you got, and `LowLatencyUnavailableReason` for a short
explanation of why low latency was declined. Pass `WithLowLatency(required: true)` to instead throw an
`InvalidOperationException` from `StartRecording`/`CaptureAsync` when low latency can't be honoured.

This mirrors `WasapiPlayer`'s low-latency support — see [WasapiPlayer.md](WasapiPlayer.md).

## Loopback capture

To record what an output device is *playing* (rather than a microphone), call `WithLoopbackCapture()`. This replaces the older `WasapiLoopbackCapture` class. Pass a render endpoint via `WithDevice`, or omit it to use the default render device:

```c#
var recorder = new WasapiRecorderBuilder()
    .WithLoopbackCapture()
    .Build();
```

As with the legacy loopback class, the `DataAvailable` event only fires while audio is actually playing through the device. If you need to capture continuous silence, play silence through the device for the duration.

## Async capture with CaptureAsync

For async pipelines you can consume audio as an `IAsyncEnumerable<AudioBuffer>`. Unlike the `DataAvailable` span, each `AudioBuffer` contains a heap-allocated **copy** of the data, so it is safe to store or process asynchronously. It also carries the device and QPC positions for the captured packet.

```c#
await using var recorder = new WasapiRecorderBuilder().Build();

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
await foreach (var buffer in recorder.CaptureAsync(cts.Token))
{
    // buffer.Data is a ReadOnlyMemory<byte>
    ProcessAudio(buffer.Data.Span);
}
```

Cancelling the token (or letting it time out) stops capture and ends the enumeration.

## Async disposal

In async or UI code, prefer `DisposeAsync` so the calling thread isn't blocked while the capture thread is joined:

```c#
await using var recorder = new WasapiRecorderBuilder().Build();
```

## Following the default device (automatic stream routing)

By default a recorder is bound to one endpoint: if the user switches the default recording device (or unplugs the current one) mid-capture, the stream stops. `WithDefaultDeviceStreamRouting()` opts into Windows' *automatic stream routing* instead — capture follows whatever the default capture device currently is, and Windows transfers the stream to the new default seamlessly with no application code. Requires Windows 10 version 1607 or later.

Activation is asynchronous (it uses `ActivateAudioInterfaceAsync` under the hood), so build with `BuildAsync()` rather than `Build()` — calling `Build()` throws:

```c#
await using var recorder = await new WasapiRecorderBuilder()
    .WithDefaultDeviceStreamRouting()
    .BuildAsync();

recorder.DataAvailable += (buffer, flags, _, _) => { /* captured from the current default device */ };
recorder.StartRecording();
```

Routing is standard shared mode only, so don't combine it with `WithDevice`, `WithExclusiveMode`, `WithLowLatency`, `WithLoopbackCapture`, or `WithProcessLoopback` (each throws from `BuildAsync`). Unlike the process-loopback virtual device, the routing endpoint exposes a mix format, so capture defaults to the current default device's mix format (or use `WithFormat(...)`).

## Per-process loopback capture

`WithProcessLoopback` captures only the audio rendered by a specific process (and, with `ProcessLoopbackMode.IncludeTargetProcessTree`, its child processes), rather than the whole device. It uses `ActivateAudioInterfaceAsync`, so it is activated asynchronously — build it with `BuildAsync()` rather than `Build()` (calling `Build()` throws). Requires Windows 10 version 2004 (build 19041) or later.

```c#
await using var recorder = await new WasapiRecorderBuilder()
    .WithProcessLoopback((uint)targetProcessId, ProcessLoopbackMode.IncludeTargetProcessTree)
    .BuildAsync();

recorder.DataAvailable += (buffer, flags, _, _) => { /* buffer is the process's rendered audio */ };
recorder.StartRecording();
```

The virtual loopback device does not expose a mix format, so the recorder captures at the format you request via `WithFormat(...)`, defaulting to 44.1 kHz stereo IEEE float. Use `ProcessLoopbackMode.ExcludeTargetProcessTree` to capture everything *except* the target process. As with all WASAPI loopback, no buffers are delivered while the target renders no audio.

For system-wide loopback (everything the device is playing) use `WithLoopbackCapture()` instead.

## Acoustic echo cancellation reference

On a microphone capture stream, Windows can run acoustic echo cancellation (AEC) to subtract the
audio coming out of your speakers from the captured signal — useful for calling and conferencing
apps. The cancellation itself is performed by an audio processing object (APO) in the capture
pipeline supplied by the device/driver or by Windows; NAudio does not implement echo cancellation.
What it exposes is control over **which render endpoint** provides the loopback *reference* signal
that the AEC effect cancels out.

Configure the reference endpoint up front with `WithEchoCancellationReferenceEndpoint`. Pass the
render device whose output should be cancelled, or call it with no argument to let Windows pick the
reference automatically:

```c#
await using var recorder = new WasapiRecorderBuilder()
    .WithDevice(microphone)                              // a capture endpoint
    .WithEchoCancellationReferenceEndpoint(speakers)     // the render endpoint to cancel out
    .Build();

recorder.StartRecording();
```

This requires **Windows 11 build 22621 or later** and a capture endpoint whose AEC effect supports
controlling the reference endpoint. If it does not, `StartRecording` throws `NotSupportedException`.
Note that an endpoint may apply AEC but not allow choosing the reference — in that case the control
is unavailable and Windows uses its own reference selection.

The AEC effect is only inserted into the capture pipeline when the stream is opened in the
**communications signal-processing mode**, so `WithEchoCancellationReferenceEndpoint` enables that
mode automatically. On most microphones (laptop built-in mics, USB webcams) the AEC control is
*absent* in the default mode and only appears in communications mode — this is why selecting a
reference endpoint implies it. If you want the system's communications audio pipeline (AEC, noise
suppression, automatic gain control) **without** selecting a specific reference endpoint, call
`WithCommunicationsMode()` on its own:

```c#
var recorder = new WasapiRecorderBuilder()
    .WithDevice(microphone)
    .WithCommunicationsMode()   // request the AEC/NS/AGC capture pipeline
    .Build();
```

Communications mode requires `IAudioClient2` (Windows 8+) and is not available for process-loopback
capture. The exact effects applied depend on the capture endpoint and its audio processing objects.

You can also change the reference endpoint while recording via the
`AcousticEchoCancellationControl` property (available once recording has started, otherwise null):

```c#
recorder.StartRecording();
recorder.AcousticEchoCancellationControl?.SetReferenceEndpoint(otherSpeakers);
recorder.AcousticEchoCancellationControl?.UseDefaultReferenceEndpoint(); // let Windows choose
```

At the lower level, `AudioClient.TryGetAcousticEchoCancellationControl()` returns the same control
(or null when unsupported) for an initialized capture client.
