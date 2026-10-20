# Resampling Audio

Every now and then you'll find you need to resample audio with NAudio. For example, to mix files together of different sample rates, you need to get them all to a common sample rate first. Or if you're playing audio through an API like ASIO, audio must be resampled to match the output device's current sample rate before being sent to the device.

There are also some gotchas you need to be aware of when resampling. In particular there is the danger of "aliasing". The main takeaway is that if you lower the sample rate, you really ought to use a low pass filter first, to get rid of high frequencies that cannot be correctly represented at the lower sample rate.

## Option 1: MediaFoundationResampler (Windows)

Probably the most powerful resampler available with NAudio is the `MediaFoundationResampler`. This uses the Windows Media Foundation resampler MFT, which is available on all supported versions of Windows (Windows 10+). It has a customisable quality level (60 is the highest quality, down to 1 which is linear interpolation). It's fast enough to run on top quality. It's also quite flexible and is often able to change to a different channel count or bit depth at the same time.

Here's a code sample that resamples an MP3 file (usually 44.1kHz) down to 16kHz. The `MediaFoundationResampler` takes an `IWaveProvider` as input, and a desired output `WaveFormat`:

```c#
using NAudio.Wave;

int outRate = 16000;

var inFile = @"test.mp3";
var outFile = @"test resampled MF.wav";

using var reader = new MediaFoundationReader(inFile);

var outFormat = new WaveFormat(outRate, reader.WaveFormat.Channels);
using var resampler = new MediaFoundationResampler(reader, outFormat);

// resampler.ResamplerQuality = 60; // default is already 60 (best quality)
WaveFileWriter.CreateWaveFile(outFile, resampler);
```

## Option 2: AudioConverter (macOS)

An equally powerful resampler as the `MediaFoundationResampler` that is provided with macOS
in the new macOS wrappers package.
Note that it ships pre-release only while its API settles, so `--prerelease` is also required:

```c#
dotnet add package NAudio.MacOS --prerelease
```

This resampler provides top-notch quality as the Windows Media resampler,
but has a caveat: You cannot change any of the input stream format properties at any time.

However, it allows us to select a resampling algorithm, quality and dithering algorithm if we want to.
Also, it supports mapping the input channels to different outputs, or even disable input channels:

```c#
using NAudio.Wave;
using NAudio.MacOS.AudioToolbox;

int outRate = 16000;

var inFile = @"test.mp3";
var outFile = @"test resampled AudioConverter.wav";

using var reader = ExtendedAudioFileReaderFromURL.CreateFromFile(inFile);

var outFormat = new WaveFormat(outRate, reader.WaveFormat.Channels);
using var resampler = new MacAudioConverter(reader, outFormat);

// The default is Normal, minimum is Min, and Max is the maximum.
// Resampling quality also depends on the resampling algorithm you use.
// resampler.Quality = AudioConverterQuality.Max; 

WaveFileWriter.CreateWaveFile(outFile, resampler);
```

As mentioned above, this requires running on macOS to be able to use this.

## Option 3: WdlResamplingSampleProvider (cross-platform)

The third option is based on the Cockos WDL resampler for which we were kindly granted permission to use as part of NAudio. It works with floating point samples, so you'll need an `ISampleProvider` to pass in. Here we use `AudioFileReader` to get to floating point and then make a resampled 16 bit WAV file:

```c#
int outRate = 16000;
var inFile = @"test.mp3";
var outFile = @"test resampled WDL.wav";
using (var reader = new AudioFileReader(inFile))
{
    var resampler = new WdlResamplingSampleProvider(reader, outRate);
    WaveFileWriter.CreateWaveFile16(outFile, resampler);
}
```

### Choosing the quality

By default `WdlResamplingSampleProvider` uses 64-tap windowed sinc interpolation (`WdlResamplerQuality.Sinc64`), which is flat across the audio band and keeps aliasing and imaging well down. That makes it suitable for quality-sensitive work such as music, mastering, or feeding a non-linear effect like an amp model, where aliasing products would turn into audible intermodulation. You can pick a different algorithm with the `quality` constructor argument:

```c#
// the pre-3.4 algorithm: linear interpolation plus two IIR low-pass filters
var resampler = new WdlResamplingSampleProvider(reader, 48000, WdlResamplerQuality.Linear);
```

Measured on a 0.5 amplitude sine, read in 64-frame blocks:

| Conversion | Tone | `Linear`: level / signal-to-error | `Sinc64` (default): level / signal-to-error |
|---|---|---|---|
| 44.1k→48k | 1 kHz | -0.01 dB / 62.5 dB | 0.00 dB / 121 dB |
| 44.1k→48k | 10 kHz | -1.97 dB / 22.7 dB | 0.00 dB / 81 dB |
| 44.1k→48k | 18 kHz | -20.9 dB / 7.1 dB | 0.00 dB / 71 dB |
| 48k→44.1k | 10 kHz | -1.73 dB / 22.2 dB | 0.00 dB / 84 dB |
| 48k→44.1k | 18 kHz | -20.3 dB / 8.6 dB | 0.00 dB / 74 dB |

`Sinc64` costs a little more CPU (about 0.7% of one core for 44.1k→48k mono, against 0.5% for `Linear`) and adds about 0.8 ms of latency. Neither mode allocates per `Read`. `WdlResamplerQuality.Sinc16` is a cheaper middle ground: clean up to around 10 kHz (81 dB signal-to-error) but about -0.8 dB at 18 kHz. `Linear` is the cheapest and adds no latency, but it audibly dulls the top end.

The sinc modes need a little input from ahead of each output sample. Like other NAudio providers, `WdlResamplingSampleProvider` expects its source to be well-behaved: it fills every read until the end of the stream, returns at most one short read, and then returns 0. That short read is treated as the end, so the resampler flushes its tail on it and is well-behaved itself. A source that returns short reads mid-stream, such as a `BufferedWaveProvider` with `ReadFully = false` that is read faster than it is filled, makes the last few output samples of each read come out slightly wrong in the sinc modes, because silence stands in for input that hasn't arrived yet. Leave `ReadFully` on for real-time use, or, for a consumer that polls for whatever is available, set `HoldBackOnShortReads = true`:

```c#
var resampler = new WdlResamplingSampleProvider(buffered.ToSampleProvider(), 16000)
{
    HoldBackOnShortReads = true
};
```

The resampler then holds back the output that still needs input the source hasn't delivered and returns it on a later read, flushing only when the source returns 0. Its own short reads can then be followed by more samples, so don't use this setting for an input to a `MixingSampleProvider`, which removes an input on its first short read.

The big advantage that the WDL resampler brings to the table is that it is fully managed code. This means it can be used in cross-platform scenarios where Windows Media Foundation is not available.

The disadvantage is that performance will not necessarily be as fast as using `MediaFoundationResampler` (or `MacAudioConverter` for macOS).

## Option 4: ACM Resampler (Windows, legacy)

You can also use `WaveFormatConversionStream` which is an ACM based resampler that has been in NAudio since the beginning. This is a legacy option - for new code, prefer `MediaFoundationResampler` or `WdlResamplingSampleProvider`. ACM resamples 16 bit audio only and you can't change the channel count at the same time. It requires a `WaveStream` as input. Here's it being used to resample an MP3 file:

```c#
int outRate = 16000;
var inFile = @"test.mp3";
var outFile = @"test resampled ACM.wav";
using (var reader = new Mp3FileReader(inFile))
{
    var outFormat = new WaveFormat(outRate, reader.WaveFormat.Channels);
    using (var resampler = new WaveFormatConversionStream(outFormat, reader))
    {
        WaveFileWriter.CreateWaveFile(outFile, resampler);
    }
}
```

## Option 5: Do it yourself

Of course the fact that NAudio lets you have raw access to the samples means you are able to write your own resampling algorithm, which could be linear interpolation or something more complex. However, you're likely to end up with significant aliasing if you don't also write a low pass filter. Given NAudio now has the WDL resampler, that should be used for all cases where you need a fully managed resampler.
