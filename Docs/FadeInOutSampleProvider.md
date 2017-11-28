# Fading Audio in and out with FadeInOutSampleProvider

The `FadeInOutSampleProvider` offers a simple, basic way to fade audio in and out.

It follows the decorator pattern common to many `ISampleProvider` implementations. You pass in the `ISampleProvider` that you want to fade in and out. 

In this example, we'll construct a `FadeInOutSampleProvider` taking its source from an `AudioFileReader`, and passing the `true` flag to specify that we want to start with silence, ready for a fade in.

We'll also immediately trigger a fade in over 2 seconds (2000 milliseconds) by calling `BeginFadeIn`.

```c#
var audio = new AudioFileReader("example.mp3");
var fade = new FadeInOutSampleProvider(audio, true);
fade.BeginFadeIn(2000);
```

Now we can pass our `FadeInOutSampleProvider` to an output device and start playing. We'll hear the audio fading in over the first two seconds.

```c#
var waveOutDevice = new WaveOutEvent();
waveOutDevice.Init(fade);
waveOutDevice.Play();
```

At some point in the future, we might want to fade out, and we can trigger that with `BeginFadeOut`, again specifying a 2 second fadeout. 

```c#
fade.BeginFadeOut(2000);
```

Once the audio has faded out, the `FadeInOutSampleProvider` continues to read from its source but emits silence until it reaches its end, or until you call `BeginFadeIn` again.

### Taking it further

The `FadeInOutSampleProvider` is a very basic fade provider, and you may want additional features like:

- automatically fading out when you reach the end of the source
- automatically stopping at the end of a fade out
- cross-fading into another input. 

You can do this by taking the code for `FadeInOutSampleProvider` and adapting it. 

For example, to automatically fade out at the end of the source, you'd actually need to read ahead by the duration of the fade (or know in advance where you want the fade to begin)

These features may be added in the future to NAudio, but don't be afraid to create your own custom `ISampleProvider` implementations that behave just how you want.