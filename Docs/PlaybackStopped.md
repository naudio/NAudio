# Handling Playback Stopped

In NAudio, you use an implementation of the `IWavePlayer` class to play audio. Examples include `WaveOut`, `WaveOutEvent`, `WasapiOut`, `AsioOut` etc. To specify the audio to be played, you call the `Init` method passing in an `IWaveProvider`. And to start playing you call `Play`.

## Manually Stopping Playback

You can stop audio playback any time by simply calling `Stop`. Depending on the implementation of `IWavePlayer`, playback may not stop instantaneously, but finish playing the currently queued buffer (usually no more than 100ms). So even when you call `Stop`, you should wait for the `PlaybackStopped` event to be sure that playback has actually stopped.

## Reaching the end of the input audio

In NAudio, the `Read` method on `IWaveProvider` is called every time the output device needs more audio to play. The `Read` method should normally return the requested number of bytes of audio (the `count` parameter). If `Read` returns less than `count` this means this is the last piece of audio in the input stream. If `Read` returns 0, the end has been reached.

NAudio playback devices will stop playing when the `IWaveProvider`'s `Read` method returns 0. This will cause the `PlaybackStopped` event to get raised.

## Output device error

If there is any kind of audio error during playback, the `PlaybackStopped` event will be fired, and the `Exception` property set to whatever exception caused playback to stop. A very common cause of this would be playing to a USB device that has been removed during playback.

## Disposing resources

Often when playback ends, you want to clean up some resources, such as disposing the output device, and closing any input files such as `AudioFileReader`. It is strongly recommended that you do this when you receive the `PlaybackStopped` event and not immediately after calling `Stop`. This is because in many `IWavePlayer` implementations, the audio playback code is on another thread, and you may be disposing resources that will still be used.

Note that NAudio attempts to fire the `PlaybackStopped` event on the `SynchronizationContext` the device was created on. This means in a WinForms or WPF application it is safe to access the GUI in the handler.