# Playing Audio with WasapiPlayer

`WasapiPlayer` is NAudio 3's modern WASAPI playback device. It is the recommended way to play audio through WASAPI, superseding the older [`WasapiOut`](WasapiOut.md). It still implements `IWavePlayer`, so it works with the rest of the NAudio playback pipeline, but adds:

- **Zero-copy buffering** — audio is read directly into the WASAPI render buffer via `Span<byte>`, avoiding an intermediate copy.
- **MMCSS thread priority** — the playback thread can be registered with the Multimedia Class Scheduler Service for glitch-resistant low-latency playback.
- **`IAudioClient3` low-latency shared mode** — when the OS and driver support it, shared-mode playback can run at the engine's minimum period.
- **A fluent builder** (`WasapiPlayerBuilder`) instead of a long constructor.
- **`IAsyncDisposable`** for non-blocking teardown in async/UI code.

## Creating a WasapiPlayer

You create a `WasapiPlayer` through `WasapiPlayerBuilder`. With no configuration, it uses the default render device in shared mode with event synchronization:

```c#
using NAudio.Wave;

var player = new WasapiPlayerBuilder().Build();
```

The builder methods are chainable, so you can configure exactly what you need:

```c#
var player = new WasapiPlayerBuilder()
    .WithDevice(someMMDevice)        // default: system default render device
    .WithExclusiveMode()             // default: shared mode
    .WithEventSync()                 // default: event sync (vs WithPollingSync)
    .WithLatency(50)                 // default: 200ms
    .WithLowLatency()                // try IAudioClient3 shared-mode low latency
    .WithMmcssThreadPriority("Pro Audio")
    .WithCategory(AudioStreamCategory.Media)
    .WithRawMode()                   // bypass system audio enhancements
    .Build();
```

To select a specific device, enumerate render endpoints with `MMDeviceEnumerator` (see [enumerating output devices](EnumerateOutputDevices.md)) and pass the `MMDevice` to `WithDevice`.

### Share modes

`WithSharedMode()` (the default) mixes your audio with other applications. In shared mode the engine resamples/converts your audio to the device mix format automatically, so any reasonable `WaveFormat` will play.

`WithExclusiveMode()` takes sole ownership of the device, allowing the exact sample rate and lower latency, but no other application can play through it while you hold it. In exclusive mode the format must be natively supported by the device — check first with `IsFormatSupported` or discover one with `GetSupportedExclusiveFormat`:

```c#
var preferred = new WaveFormat(48000, 24, 2);
var format = player.GetSupportedExclusiveFormat(preferred);
if (format == null)
{
    // no supported exclusive format found for this device
}
```

### Raw mode (bypassing audio enhancements)

By default Windows runs your audio through a signal-processing pipeline — the "audio enhancements" / APO effects configured for the endpoint (loudness equalization, bass boost, virtual surround, downmixing, and so on). These can alter your signal in ways you don't want; a common surprise is stereo content being mixed toward mono so the left and right channels are no longer isolated.

`WithRawMode()` opens a *raw* stream (`AUDCLNT_STREAMOPTIONS_RAW`) that bypasses that processing, leaving only endpoint-specific, always-on processing in the APO, driver, and hardware. Your samples reach the device essentially unaltered:

```c#
var player = new WasapiPlayerBuilder()
    .WithRawMode()
    .Build();
```

Raw mode requires `IAudioClient2` (Windows 8.1+); `Init` throws `InvalidOperationException` if the device doesn't support it. It composes with the other options — shared or exclusive mode, low latency, event/polling sync, a stream category, and default-device stream routing. In exclusive mode the audio engine is already bypassed, so raw mode there only affects any remaining driver/APO processing.

## Following the default device (automatic stream routing)

By default a player is bound to one endpoint: if the user switches the default playback device (or unplugs the current one) mid-playback, the stream stops. `WithDefaultDeviceStreamRouting()` opts into Windows' *automatic stream routing* instead — playback follows whatever the default render device currently is, and Windows transfers the stream to the new default seamlessly with no application code. Requires Windows 10 version 1607 or later.

Activation is asynchronous (it uses `ActivateAudioInterfaceAsync` under the hood), so build with `BuildAsync()` rather than `Build()` — calling `Build()` throws:

```c#
await using var player = await new WasapiPlayerBuilder()
    .WithDefaultDeviceStreamRouting()
    .BuildAsync();

player.Init(audioFile);
player.Play();
```

Routing is standard shared mode only, so don't combine it with `WithDevice`, `WithExclusiveMode`, or `WithLowLatency` (each throws from `BuildAsync`). Because there is no fixed endpoint, `DeviceVolume` (endpoint-wide volume) is unavailable — use `Volume`/`SessionVolume` for per-application volume instead.

## Playing audio

Usage mirrors any other `IWavePlayer`: call `Init` with your source, `Play` to start, `Stop` to stop, and subscribe to `PlaybackStopped` to know when playback ends. If a `SynchronizationContext` is present when the player is constructed (e.g. on a UI thread), `PlaybackStopped` is raised on that context.

```c#
using NAudio.Wave;

using var audioFile = new AudioFileReader("example.mp3");
using var player = new WasapiPlayerBuilder().Build();

player.Init(audioFile);
player.Play();
while (player.PlaybackState == PlaybackState.Playing)
{
    Thread.Sleep(500);
}
```

## Volume control

`WasapiPlayer` exposes several levels of volume control:

- `Volume` / `IsMuted` — your application's slider in the Windows volume mixer (delegates to `SessionVolume`). This is the one most apps want.
- `SessionVolume` — the full `SimpleAudioVolume` for the session.
- `StreamVolume` — per-channel volume for this stream (shared mode only; throws in exclusive mode).
- `DeviceVolume` — the device endpoint master volume, affecting **all** applications on that device. Use with care.

```c#
player.Volume = 0.5f;   // 50% for this application only
player.IsMuted = true;  // mute just this application
```

## Async disposal

In async or UI code, prefer `DisposeAsync` over `Dispose` so the calling thread isn't blocked while the playback thread is joined:

```c#
await using var player = new WasapiPlayerBuilder().Build();
player.Init(reader);
player.Play();
// ...
// disposal happens asynchronously at end of scope
```

## Getting playback position

`WasapiPlayer` implements `IWavePosition`. `GetPosition()` returns the number of bytes the device has actually rendered (driven by the audio clock), which is not the same as the read position of your source stream.
