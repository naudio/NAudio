# Resampling Audio

Every now and then you’ll find you need to resample audio with NAudio. For example, to mix files together of different sample rates, you need to get them all to a common sample rate first. Or if you’re playing audio through an API like ASIO or WASAPI, audio must be resampled to match the output device's current sample rate before being to the device. Note that `WasapiOut` in NAudio does include a resampling step on your behalf if needed.

There are also some gotchas you need to be aware of when resampling. In particular there is the danger of "aliasing". I explain what this is in my Pluralsight ["Digital Audio Fundamentals"](https://www.shareasale.com/r.cfm?u=1036405&b=611266&m=53701&afftrack=&urllink=www%2Epluralsight%2Ecom%2Fcourses%2Fdigital%2Daudio%2Dfundamentals) course. The main takeaway is that if you lower the sample rate, you really ought to use a low pass filter first, to get rid of high frequencies that cannot correctly.

### Option 1: MediaFoundationResampler

Probably the most powerful resampler available with NAudio is the `MediaFoundationResampler`. This is not available for XP users, but desktop versions of Windows from Vista onwards include it. If you are using a Windows Server, you’ll need to make sure the "desktop experience" is installed. It has a customisable quality level (60 is the highest quality, down to 1 which is linear interpolation). I’ve found it’s fast enough to run on top quality. It also is quite flexible and is often able to change to a different channel count or bit depth at the same time.

Here's a code sample that resamples an MP3 file (usually 44.1kHz) down to 16kHz. `The MediaFoundationResampler` takes an `IWaveProvider` as input, and a desired output `WaveFormat`:

```c#
int outRate = 16000;
var inFile = @"test.mp3";
var outFile = @"test resampled MF.wav";
using (var reader = new Mp3FileReader(inFile))
{
    var outFormat = new WaveFormat(outRate,    reader.WaveFormat.Channels);
    using (var resampler = new MediaFoundationResampler(reader, outFormat))
    {
        // resampler.ResamplerQuality = 60;
        WaveFileWriter.CreateWaveFile(outFile, resampler);
    }
}
```

### Option 2: WdlResamplingSampleProvider

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

The big advantage that the WDL resampler brings to the table is that it is fully managed. This means it can be used within UWP Windows Store apps (as I’m still finding it very difficult to work out how to create the `MediaFoundationResampler` in a way that passes WACK), or in cross-platform scenarios. 

The disadvantage is of course that performance will not necessarily be as fast as using `MediaFoundationResampler`.

### Option 3: ACM Resampler

You can also use `WaveFormatConversionStream` which is an ACM based Resampler, which has been in NAudio since the beginning and works back to Windows XP. It resamples 16 bit only and you can’t change the channel count at the same time. It predates `IWaveProvider` so you need to pass in a `WaveStream` based. Here’s it being used to resample an MP3 file:

```c#
int outRate = 16000;
var inFile = @"test.mp3";
var outFile = @"test resampled ACM.wav";
using (var reader = new Mp3FileReader(inFile))
{
    var outFormat = new WaveFormat(outRate,    reader.WaveFormat.Channels);
    using (var resampler = new WaveFormatConversionStream(outFormat, reader))
    {
        WaveFileWriter.CreateWaveFile(outFile, resampler);
    }
}
```

### Option 4: Do it yourself

Of course the fact that NAudio lets you have raw access to the samples means you are able to write your own resampling algorithm, which could be Linear Interpolation or something more complex. I’d recommend against doing this unless you really understand audio DSP. If you want to see some spectograms showing what happens when you write your own naive resampling algorithm, have a look at [this article](http://www.codeproject.com/Articles/501521/How-to-convert-between-most-audio-formats-in-NET) I wrote on CodeProject. Basically, you’re likely to end up with significant aliasing if you don’t also write a low pass filter. Given NAudio now has the WDL resampler, that should probably be used for all cases where you need a fully managed resampler.

