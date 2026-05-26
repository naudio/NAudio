# Recording Audio with WasapiRecorder

`WasapiRecorder` is NAudio 3's modern WASAPI capture device. It is the recommended way to capture audio through WASAPI, superseding the older [`WasapiCapture` and `WasapiLoopbackCapture`](WasapiLoopbackCapture.md). It adds:

- **Zero-copy capture** — the `DataAvailable` event hands you a `ReadOnlySpan<byte>` directly over the WASAPI buffer.
- **MMCSS thread priority** — register the capture thread with the Multimedia Class Scheduler Service.
- **`IAsyncEnumerable` support** — consume captured audio with `await foreach` via `CaptureAsync`.
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

The zero-copy path uses the `DataAvailable` event. The span is **only valid for the duration of the callback** — if you need to keep the data (e.g. to write to a file), copy it out. Here we record to a WAV file:

```c#
using NAudio.Wave;

var recorder = new WasapiRecorderBuilder().Build();
var writer = new WaveFileWriter("recorded.wav", recorder.WaveFormat);

recorder.DataAvailable += (buffer, flags) =>
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

> **Note:** per-process loopback capture (`WithProcessLoopback`) is not yet implemented and currently throws `NotImplementedException`. Use `WithLoopbackCapture()` for system-wide loopback for now.
