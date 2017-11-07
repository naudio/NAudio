# Understanding Output Devices

NAudio supplies wrappers for four different audio output APIs. In addition, some of them support several different modes of operation. This can be confusing for those new to NAudio and the various Windows audio APIs, so in this article I will explain what the four main options are and when you should use them.

## IWavePlayer

We’ll start off by discussing the common interface for all output devices. In NAudio, each output device implements `IWavePlayer`, which has an `Init` method into which you pass the Wave Provider that will be supplying the audio data. Then you can call `Play`, `Pause` and `Stop` which are pretty self-explanatory, except that you need to know that `Play` only begins playback. 

You should only call `Init` once on a given instance of an `IWavePlayer`. If you need to play something else, you should `Dispose` of your output device and create a new one.

You will notice there is no capability to get or set the playback position. That is because the output devices have no concept of position – they just read audio from the `IWaveProvider` supplied until it reaches an end, at which point the `PlaybackStopped` event is fired. Alternatively, you can ignore `PlaybackStopped` and just call `Stop` whenever you decide that playback is no longer required.

You may notice a `Volume` property on the interface that is marked as `[Obsolete]`. This was marked obsolete because it is not supported on all device types, but most of them do.

Finally there is a `PlaybackState` property that can report `Stopped`, `Playing` or `Paused`. Be careful with Stopped though, since if you call the `Stop` method, the `PlaybackState` will immediately go to `Stopped` but it may be a few milliseconds before any background playback threads have actually exited.

## WaveOutEvent & WaveOut
`WaveOutEvent` should be thought of as the default audio output device in NAudio. If you don’t know what to use, choose `WaveOutEvent`. It essentially wraps the Windows `waveOut` APIs, and is the most universally supported of all the APIs.

The `WaveOutEvent` (or `WaveOut`) object allows you to configure several things before you get round to calling `Init`. Most common would be to change the `DeviceNumber` property. –1 indicates the default output device, while 0 is the first output device (usually the same in my experience). To find out how many `WaveOut` output devices are available, query the static `WaveOut.DeviceCount` property.

You can also set `DesiredLatency`, which is measured in milliseconds. This figure actually sets the total duration of all the buffers. So in fact, you could argue that the real latency is shorter. In a future NAudio, I might reduce confusion by replacing this with a `BufferDuration` property. By default the `DesiredLatency` is 300ms, which should ensure a smooth playback experience on most computers. You can also set the `NumberOfBuffers` to something other than its default of 2 although 3 is the only other value that is really worth using.

One complication with `WaveOut` is that there are several different "callback models" available. Understanding which one to use is important. Callbacks are used whenever `WaveOut` has finished playing one of its buffers and wants more data. In the callback we read from the source wave provider and fill a new buffer with the audio. It then queues it up for playback, assuming there is still more data to play. As with all output audio driver models, it is imperative that this happens as quickly as possible, or your output sound will stutter.

### Event Callback
Event callback is the default and recommended approach if you are using waveOut APIs, and this is implemented in the `WaveOutEvent` class unlike the other callback options which are accessed via the `WaveOut` class.

The implementation of event callback is similar to WASAPI and DirectSound. A background thread simply sits around filling up buffers when they become empty. To help it respond at the right time, an event handle is set to trigger the background thread that a buffer has been returned by the soundcard and is in need of filling again.

### New Window Callback
This is a good approach if you are creating a `WaveOut` object from the GUI thread of a Windows Forms or WPF application. Whenever `WaveOut` wants more data it posts a message that is handled by the Windows message pump of an invisible new window. You get this callback model by default when you call the empty `WaveOut` constructor. However, it will not work on a background thread, since there is no message pump.

One of the big benefits of using this model (or the Existing Window model) is that everything happens on the same thread. This protects you from threading race conditions where a reposition happens at the same time as a read.

note: The reason for using a new window instead of an existing window is to eliminate bugs that can happen if you start one playback before a previous one has finished. It can result in WaveOut picking up messages it shouldn’t.

### Existing Window

Existing Window is essentially the same callback mechanism as New Window, but you have to pass in the handle of an existing window. This is passed in as an IntPtr to make it compatible with WPF as well as WinForms. The only thing to be careful of with this model is using multiple concurrent instances of WaveOut as they will intercept each other’s messages (I may fix this in a future version of NAudio).

note: with both New and Existing Window callback methods, audio playback will deteriorate if your windows message pump on the GUI thread has too much other work to do.

### Function Callback
Function callback was the first callback method I attempted to implement for NAudio, and has proved the most problematic of all callback methods. Essentially you can give it a function to callback, which seems very convenient, these callbacks come from a thread within the operating system.

To complicate matters, some soundcards really don’t like two threads calling waveOut functions at the same time (particularly one calling waveOutWrite while another calls waveOutReset). This in theory would be easily fixed with locks around all waveOut calls, but some audio drivers call the callbacks from another thread while you are calling waveOutReset, resulting in deadlocks.

Function callbacks should be considered as obsolete now in NAudio, with `WaveOutEvent` a much better choice.

## DirectSoundOut

DirectSound is a good alternative if for some reason you don’t want to use `WaveOut` since it is simple and widely supported.

To select a specific device with `DirectSoundOut`, you can call the static `DirectSoundOut.Devices` property which will let you get at the GUID for each device, which you can pass into the `DirectSoundOut` constructor. Like `WaveOut`, you can adjust the latency (overall buffer size).

`DirectSoundOut` uses a background thread waiting to fill buffers (same as `WaveOutEvent`). This is a reliable and uncomplicated mechanism, but as with any callback mechanism that uses a background thread, you must take responsibility yourself for ensuring that repositions do not happen at the same time as reads (although some of NAudio’s built-in WaveStreams can protect you from getting this wrong).

## WasapiOut

WASAPI is the latest and greatest Windows audio API, introduced with Windows Vista. But just because it is newer doesn’t mean you should use it. In fact, it can be a real pain to use, since it is much more fussy about the format of the `IWaveProvider` passed to its `Init` function and will not perform resampling for you.

To select a specific output device, you need to make use of the `MMDeviceEnumerator` class, which can report the available audio "endpoints" in the system.

WASAPI out offers you a couple of configuration options. The main one is whether you open in `shared` or `exclusive` mode. In exclusive mode, your application requests exclusive access to the soundcard. This is only recommended if you need to work at very low latencies. 

You can also choose whether event callbacks are used. I recommend you do so, since it enables the background thread to get on with filling a new buffer as soon as one is needed.

Why would you use WASAPI? I would only recommend it if you want to work at low latencies or are wanting exclusive use of the soundcard. Remember that WASAPI is not supported on Windows XP. However, in situations where WASAPI would be a good choice, ASIO out is often a better one…

## AsioOut

ASIO is the de-facto standard for audio interface drivers for recording studios. All professional audio interfaces for Windows will come with ASIO drivers that are designed to operate at the lowest latencies possible. ASIO is probably a better choice than WASAPI for low latency applications since it is more widely supported (you can use ASIO on XP for example).

ASIO Out devices are selected by name. Use the AsioOut.GetDriverNames() to see what devices are available on your system. Note that this will return all installed ASIO drivers. It does not necessarily mean that the soundcard is currently connected in the case of an external audio interface, so `Init` can fail for that reason.

ASIO drivers support their own customised settings GUI. You can access this by calling `ShowControlPanel()`. Latencies are usually set within the control panel and are typically specified in samples. Remember that if you try to work at a really low latency, your input IWaveProvider’s `Init` function needs to be really fast.

ASIO drivers can process data in a whole host of native WAV formats (e.g. big endian vs little endian, 16, 24, 32 bit ints, IEEE floats etc), not all of which are currently supported by NAudio. If ASIO Out doesn’t work with your soundcard, create an issue on the NAudio GitHub page, as it is fairly easy to add support for another format.