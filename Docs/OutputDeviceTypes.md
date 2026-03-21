# Understanding Output Devices

NAudio supplies wrappers for four different audio output APIs. In addition, some of them support several different modes of operation. This can be confusing for those new to NAudio and the various Windows audio APIs, so in this article I will explain what the four main options are and when you should use them.

## IWavePlayer

We'll start off by discussing the common interface for all output devices. In NAudio, each output device implements `IWavePlayer`, which has an `Init` method into which you pass the Wave Provider that will be supplying the audio data. Then you can call `Play`, `Pause` and `Stop` which are pretty self-explanatory, except that you need to know that `Play` only begins playback.

You should only call `Init` once on a given instance of an `IWavePlayer`. If you need to play something else, you should `Dispose` of your output device and create a new one.

You will notice there is no capability to get or set the playback position. That is because the output devices have no concept of position — they just read audio from the `IWaveProvider` supplied until it reaches an end, at which point the `PlaybackStopped` event is fired. Alternatively, you can ignore `PlaybackStopped` and just call `Stop` whenever you decide that playback is no longer required.

You may notice a `Volume` property on the interface that is marked as `[Obsolete]`. This was marked obsolete because it is not supported on all device types, but most of them do.

Finally there is a `PlaybackState` property that can report `Stopped`, `Playing` or `Paused`. Be careful with Stopped though, since if you call the `Stop` method, the `PlaybackState` will immediately go to `Stopped` but it may be a few milliseconds before any background playback threads have actually exited.

## WasapiOut

WASAPI is the recommended audio output API on modern Windows. It is the native audio API from Windows Vista onwards and offers the best combination of features, performance and audio quality.

In shared mode (the default), WASAPI handles sample rate conversion automatically — you can pass in audio at any sample rate and it will be resampled to match the device's configured format. This makes it just as easy to use as `WaveOut`, while offering lower latency and better audio quality.

To select a specific output device, use the `MMDeviceEnumerator` class, which can report the available audio "endpoints" in the system.

WASAPI offers several configuration options. The main one is whether you open in `shared` or `exclusive` mode. In exclusive mode, your application requests exclusive access to the soundcard. This is recommended if you need to work at very low latencies or want bit-perfect output. In exclusive mode, the audio format must exactly match what the hardware supports.

You can also choose whether event callbacks are used. We recommend you do so, since it enables the background thread to get on with filling a new buffer as soon as one is needed.

## WaveOut

`WaveOut` wraps the legacy Windows `waveOut` multimedia APIs using event callbacks. While these APIs have been around since the earliest versions of Windows, they remain a solid and reliable choice for audio playback, and are well suited to applications that don't need the lowest possible latency.

The `WaveOut` object allows you to configure several things before you get round to calling `Init`. Most common would be to change the `DeviceNumber` property. -1 indicates the default output device, while 0 is the first output device (usually the same in my experience). To find out how many `WaveOut` output devices are available, query the static `WaveOut.DeviceCount` property.

You can also set `BufferMilliseconds`, which specifies the duration of each buffer in milliseconds. The default is 100ms, which should ensure a smooth playback experience on most computers. You can also set the `NumberOfBuffers` to something other than its default of 2, although 3 is the only other value that is really worth using. The total latency is `BufferMilliseconds * NumberOfBuffers`.

`WaveOut` uses a dedicated background thread that fills buffers when they become empty. An event handle triggers the background thread whenever a buffer has been returned by the soundcard and is in need of filling again. As with all output audio driver models, it is imperative that buffers are refilled as quickly as possible, or your output sound will stutter.

## DirectSoundOut

DirectSound is another legacy API that can be used as an alternative to `WaveOut`. It is simple and widely supported, but offers no particular advantage over `WaveOut` or `WasapiOut` on modern systems.

To select a specific device with `DirectSoundOut`, you can call the static `DirectSoundOut.Devices` property which will let you get at the GUID for each device, which you can pass into the `DirectSoundOut` constructor. Like `WaveOut`, you can adjust the latency (overall buffer size).

`DirectSoundOut` uses a background thread waiting to fill buffers (same as `WaveOut`). This is a reliable and uncomplicated mechanism, but as with any callback mechanism that uses a background thread, you must take responsibility yourself for ensuring that repositions do not happen at the same time as reads (although some of NAudio's built-in WaveStreams can protect you from getting this wrong).

## AsioOut

ASIO is the de-facto standard for audio interface drivers for recording studios. All professional audio interfaces for Windows will come with ASIO drivers that are designed to operate at the lowest latencies possible. ASIO is a good choice for professional audio applications requiring very low latency and direct hardware access.

ASIO Out devices are selected by name. Use the `AsioOut.GetDriverNames()` to see what devices are available on your system. Note that this will return all installed ASIO drivers. It does not necessarily mean that the soundcard is currently connected in the case of an external audio interface, so `Init` can fail for that reason.

ASIO drivers support their own customised settings GUI. You can access this by calling `ShowControlPanel()`. Latencies are usually set within the control panel and are typically specified in samples. Remember that if you try to work at a really low latency, your input IWaveProvider's `Init` function needs to be really fast.

ASIO drivers can process data in a whole host of native WAV formats (e.g. big endian vs little endian, 16, 24, 32 bit ints, IEEE floats etc), not all of which are currently supported by NAudio. If ASIO Out doesn't work with your soundcard, create an issue on the NAudio GitHub page, as it is fairly easy to add support for another format.
