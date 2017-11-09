# Convert Between Stereo and Mono

NAudio includes a number of utility classes that can help you to convert between mono and stereo audio. You can use these whether you are playing audio live, or whether you are simply converting from one file format to another.

# Mono to Stereo

If you have a mono input file, and want to convert to stereo, the `MonoToStereoSampleProvider` allows you to do this. It takes a `SampleProvider` as input, and has two floating point `LeftVolume` and `RightVolume` properties, which default to `1.0f`. This means that the mono input will be copied at 100% volume into both left and right channels.

If you wanted to route it just to the left channel, you could set `LeftVolume` to `1.0f` and `RightVolume` to `0.0f`. And if you wanted it more to the right than the left you might set `LeftVolume` to `0.25f` and `RightVolume` to `1.0f`.

```c#
using(var inputReader = new AudioFileReader(monoFilePath))
{
    // convert our mono ISampleProvider to stereo
    var stereo = new MonoToStereoSampleProvider(inputReader);
    stereo.LeftVolume = 0.0f; // silence in left channel
    stereo.RightVolume = 1.0f; // full volume in right channel

    // can either use this for playback:
    myOutputDevice.Init(stereo);
    myOutputDevice.Play();
    // ...

    // ... OR ... could write the stereo audio out to a WAV file
    WaveFileWriter.CreateWaveFile16(outputFilePath, stereo);
}
```

There's also a `MonoToStereoProvider16` that works with 16 bit PCM `IWaveProvider` inputs and outputs 16 bit PCM. It works very similarly to `MonoToStereoSampleProvider` otherwise.

# Stereo to Mono

If you have a stereo input file and want to collapse to mono, then the `StereoToMonoSampleProvider` is what you want. It takes a stereo `ISampleProvider` as input, and also has a `LeftVolume` and `RightVolume` property, although the defaults are `0.5f` for each. This means the left sample will be multiplied by `0.5f` and the right by `0.5f` and the two are then summed together.

If you want to just keep the left channel and throw away the right, you'd set `LeftVolume` to 1.0f and `RightVolume` to 0.0f. You could even sort out an out of phase issue by setting `LeftVolume` to `0.5f` and `RightVolume` to -0.5f.

Usage is almost exactly the same. Note that some output devices won't let you play a mono file directly, so this would be more common if you were creating a mono output file, or if the mono audio was going to be passed on as a mixer input to `MixingSampleProvider`.

```c#
using(var inputReader = new AudioFileReader(stereoFilePath))
{
    // convert our stereo ISampleProvider to mono
    var mono = new StereoToMonoSampleProvider(inputReader);
    stereo.LeftVolume = 0.0f; // discard the left channel
    stereo.RightVolume = 1.0f; // keep the right channel

    // can either use this for playback:
    myOutputDevice.Init(mono);
    myOutputDevice.Play();
    // ...

    // ... OR ... could write the mono audio out to a WAV file
    WaveFileWriter.CreateWaveFile16(outputFilePath, mono);
}
```

There is also a `StereoToMonoProvider16` that works with 16 bit PCM stereo `IWaveProvider` inputs and emits 16 bit PCM.

# Panning Mono to Stereo

Finally, NAudio offers a `PanningSampleProvider` which allows you to use customisable panning laws to govern how a mono input signal is placed into a stereo output signal.

It has a `Pan` property which can be configured between `-1.0f` (fully left) and `1.0f` (fully right), with `0.0f` being central.

The `PanningStrategy` can be overridden. By default is uses the `SinPanStrategy`. There is also `SquareRootPanStrategy`, `LinearPanStrategy` and `StereoBalanceStrategy`, each one operating slightly differently with regards to how loud central panning is, and how the sound tapers off as it is panned to each side. You can experiment to discover which one fits your needs the best.

Usage is very similar to the `MonoToStereoSampleProvider`

```c#
using(var inputReader = new AudioFileReader(monoFilePath))
{
    // convert our mono ISampleProvider to stereo
    var panner = new PanningSampleProvider(inputReader);
    // override the default pan strategy
    panner.PanStrategy = new SquareRootPanStrategy();
    panner.Pan = -0.5f; // pan 50% left

    // can either use this for playback:
    myOutputDevice.Init(panner);
    myOutputDevice.Play();
    // ...

    // ... OR ... could write the stereo audio out to a WAV file
    WaveFileWriter.CreateWaveFile16(outputFilePath, panner);
}
```
