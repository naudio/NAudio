# Playback with ASIO

The `AsioOut` class in NAudio allows you to both play back and record audio using an ASIO driver. ASIO is a driver format supported by many popular Digital Audio Workstation (DAW) applications on Windows, and usually offers very low latency for record and playback. 

To use ASIO, you do need a soundcard that has an ASIO driver. Most professional soundcards have ASIO drivers, but you can also try the [ASIO4ALL](http://asio4all.com/) driver which enables ASIO for soundcards that don't have their own native ASIO driver.

The `AsioOut` class is able to play, record or do both simultaneously. This article covers the scenario where we just want to play audio.

## Opening an ASIO device for playback

To discover the names of the installed ASIO drivers on your system you use `AsioOut.GetDriverNames()`.

We can use one of those driver names to pass to the constructor of `AsioOut`

```c#
var asioOut = new AsioOut(asioDriverName);
```

## Selecting Output Channels

Pro Audio soundcards often support multiple inputs and outputs. We may want to find out how many output channels are available on the device. We can get this with:

```c#
var outputChannels = asioOut.DriverOutputChannelCount;
```

By default, `AsioOut` will send the audio to the first output channels on your soundcard. So if you play stereo audio through a four channel soundcard, the samples will come out of the first two channels. If you wanted it to come out of different channels you can adjust the `OutputChannelOffset` parameter.

Next, I call `Init`. This lets us pass the `IWaveProvider` or `ISampleProvider` we want to play. Note that the sample rate of the `WaveFormat` of the input provider must be one supported by the ASIO driver. Usually this means 44.1kHz or higher.


```c#
// optionally, change the starting channel for outputting audio:
asioOut.OutputChannelOffset = 2;  
asioOut.Init(mySampleProvider);
```

## Start Playback

As `AsioOut` is an implementation of `IWavePlayer` we just need to call `Play` to start playing.

```c#
asioOut.Play(); // start playing
```

Note that since ASIO typically works at very low latencies, it's important that the components that make up your signal chain are able to provide audio fast enough. If the ASIO buffer size is say 10ms, that means that every 10ms you need to generate the next 10ms of audio. If you miss this window, the audio will gitch.

## Stop Playback

We stop recording by calling Stop().

```c#
asioOut.Stop();
```

As with other NAudio `IWavePlayer` implementations, we'll get a `PlaybackStopped` event firing when the driver stops.

And of course we should remember to `Dispose` our instance of `AsioOut` when we're done with it.

```c#
asioOut.Dispose();
```