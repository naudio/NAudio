# Play a Sine Wave

To play a sine wave we can use the `SignalGenerator` class. This can produce a variety of signal types including sawtooth, pink noise and triangle waves. We will specify that we want a frequency of 500Hz, and set the gain to 0.2 (20%). This will help protect us from hurting our ears.

The `SignalGenerator` will produce a never-ending stream of sound, so for it to finish, we'd either just call Stop on our output device when we are happy, or we can se the `Take` extension method, to specify that we want just the first 20 seconds of sound.

Here's some sample code

```c#
var sine20Seconds = new SignalGenerator() { 
    Gain = 0.2, 
    Frequency = 500,
    Type = SignalGeneratorType.Sin}
    .Take(TimeSpan.FromSeconds(20));
using (var wo = new WaveOutEvent())
{
    wo.Init(sine20Seconds);
    wo.Play();
    while (wo.PlaybackState == PlaybackState.Playing)
    {
        Thread.Sleep(500);
    }
}
```

# Explore other Signal Types

Signal Generator can produe several other signal types. There are three other simple repeating signal patterns, for which you can adjust the gain and signal frequency.

triangle:

```
Gain = 0.2, 
Frequency = 500,
Type = SignalGeneratorType.Triangle
```

square:

```c#
Gain = 0.2, 
Frequency = 500,
Type = SignalGeneratorType.Square
```

and sawtooth:
```c#
Gain = 0.2, 
Frequency = 500,
Type = SignalGeneratorType.SawTooth
```

There are also two types of noise - pink and white noise. The Frequency property has no effect:

pink noise

```c#
Gain = 0.2, 
Type = SignalGeneratorType.PinkNoise
```
white noise:

```c#
Gain = 0.2, 
Type = SignalGeneratorType.WhiteNoise
```

The final type is the frequency sweep (or 'chirp'). This is a sine wave that starts at `Frequency` and smoothly ramps up to `FrequencyEnd` over the period defined in `SweepLengthSecs`. It then returns to the start frequency and repeats indefinitely

```c#
Gain = 0.2, 
Frequency = 500, // start frequency of the sweep
FrequencyEnd = 2000, 
Type = SignalGeneratorType.Sweep, 
SweepLengthSecs = 2
```
