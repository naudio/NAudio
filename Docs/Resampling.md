# Resampling Audio

Every now and then you'll find you need to resample audio with NAudio. For example, to mix files together of different sample rates, you need to get them all to a common sample rate first. Or if you're playing audio through an API like ASIO, audio must be resampled to match the output device's current sample rate before being sent to the device.

There are also some gotchas you need to be aware of when resampling. In particular there is the danger of "aliasing". The main takeaway is that if you lower the sample rate, you really ought to use a low pass filter first, to get rid of high frequencies that cannot be correctly represented at the lower sample rate.

## Option 1: MediaFoundationResampler

Probably the most powerful resampler available with NAudio is the `MediaFoundationResampler`. This uses the Windows Media Foundation resampler MFT, which is available on all supported versions of Windows (Windows 10+). It has a customisable quality level (60 is the highest quality, down to 1 which is linear interpolation). It's fast enough to run on top quality. It's also quite flexible and is often able to change to a different channel count or bit depth at the same time.

Here's a code sample that resamples an MP3 file (usually 44.1kHz) down to 16kHz. The `MediaFoundationResampler` takes an `IWaveProvider` as input, and a desired output `WaveFormat`:

```c#
int outRate = 16000;
var inFile = @"test.mp3";
var outFile = @"test resampled MF.wav";
using (var reader = new Mp3FileReader(inFile))
{
    var outFormat = new WaveFormat(outRate, reader.WaveFormat.Channels);
    using (var resampler = new MediaFoundationResampler(reader, outFormat))
    {
        // resampler.ResamplerQuality = 60; // default is already 60 (best quality)
        WaveFileWriter.CreateWaveFile(outFile, resampler);
    }
}
```

## Option 2: WdlResamplingSampleProvider

The second option is based on the Cockos WDL resampler for which we were kindly granted permission to use as part of NAudio. It works with floating point samples, so you'll need an `ISampleProvider` to pass in. Here we use `AudioFileReader` to get to floating point and then make a resampled 16 bit WAV file:

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

The big advantage that the WDL resampler brings to the table is that it is fully managed code. This means it can be used in cross-platform scenarios where Windows Media Foundation is not available.

The disadvantage is that performance will not necessarily be as fast as using `MediaFoundationResampler`.

## Option 3: ACM Resampler (legacy)

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

## Option 4: Do it yourself

Of course the fact that NAudio lets you have raw access to the samples means you are able to write your own resampling algorithm, which could be linear interpolation or something more complex. However, you're likely to end up with significant aliasing if you don't also write a low pass filter. Given NAudio now has the WDL resampler, that should be used for all cases where you need a fully managed resampler.
