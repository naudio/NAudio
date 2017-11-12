# Working with WasapiOut

`WasapiOut` is an implementation of `IWavePlayer` that uses the WASAPI audio API under the hood. WASAPI was introduced with Windows Vista, meaning it will be supported on most versions of Windows, but not XP.

## WasapiOut vs WaveOut

Although it may seem like the obvious replacement to `WaveOut`, it does have one important limitation. `WaveOut` is much more accomodating to the `WaveFormat` you attempt to play. So if the soundcard is operating at 48kHz and you try to play a 44.1kHz or 16kHz file, the underlying `waveOut` Windows APIs will resample on your behalf.

WASAPI is less forgiving. It requires you to supply audio as IEEE floating point samples, and you should only attempt to play audio that matches the channel count (usually 2 for stereo) and sample rate (usually 44.1kHz but sometimes 48kHz) that the soundcard is operating at.

To work around this limitation, the NAudio `WasapiOut` class will attempt to use the `ResamplerDmoStream` class to seamlessly convert your audio into an appropriate format. So most of the time it will just seem like it works seamlessly, but it is helpful to know that this might be happening, as it can have latency implications since the resampler needs to read ahead.

## Configuring WasapiOut

When you create an instance of `WasapiOut` you can choose an output device. This is discussed in the [enumerating output devices article](EnumerateOutputDevices.md).

There are a number of other options you can specify with WASAPI.

First of all, you can choose the "share mode". This is normally set to `AudioClientShareMode.Shared` which means you are happy to share the sound card with other audio applications in Windows. This however does mean that the sound card will continue to operate at whatever sample rate it is currently set to, irrespective of the sample rate of audio you want to play, and this is why the `ResamplerDmoStream` may be required.

If you choose `AudioClientShareMode.Exclusive` then you are requesting exclusive access to the sound card. The benefits of this approach are that you can specify the exact sample rate you want (has to be supported by the sound card and usually cannot be less than 44.1kHz), and you can often work at lower latencies. Obviously this mode impacts on other programs wanting to use the soundcard.

You can choose whether to use `eventSync` or not. This governs the behaviour of the background thread that is supplying audio to WASAPI. With event sync, you listen on an event for when WASAPI wants more audio. Without, you simply sleep for a short period of time and then provide more audio. Event sync is the default and generally is fine for most use cases.

You can also request the latency you want. This is only a request, and depending on the share mode may not have any effect. The lower the latency, the shorter the period of time between supplying audio to the soundcard and hearing it. This can be very useful for real-time monitoring effects, but comes at the cost of higher CPU usage and potential for dropouts causing pops and clicks. So take care when adjusting this setting. The default is currently set to a fairly conservative 200ms.

## Playing Audio with WasapiOut

Once you've created an instance of `WasapiOut`, you use it exactly the same as any other `IWavePlayer` device in NAudio. You call `Init` to pass it the audio to be played, `Stop` to stop playback. You can use the `Volume` property to adjust the volume and subscribe to `PlaybackStopped` to determine when playback has stopped. And you should call `Dispose` when you are finished with it.

Here's a simple example of playing audio with the default `WasapiOut` device in shared mode with event sync and the default latency:

```c#
using(var audioFile = new AudioFileReader(audioFile))
using(var outputDevice = new WasapiOut())
{
    outputDevice.Init(audioFile);
    outputDevice.Play();
    while (outputDevice.PlaybackState == PlaybackState.Playing)
    {
        Thread.Sleep(1000);
    }
}
```

