# Using RawSourceWaveStream

`RawSourceWaveStream` is useful when you have some raw audio, which might be PCM or compressed, but it is not contained within a file format. `RawSourceWaveStream` allows you to specify the `WaveFormat` associated with the raw audio. Let's see some examples.

## Playing from a Byte array

Suppose we have a byte array containing raw 16 bit mono PCM, and want to play it.

For demo purposes, let's create a 5 second sawtooth wave into the `raw`. Obviously `SignalGenerator` would be a better way to do this, but we are simulating getting a byte array from somewhere else, maybe received over the network.

```c#
var sampleRate = 16000;
var frequency = 500;
var amplitude = 0.2;
var seconds = 5;

var raw = new byte[sampleRate * seconds * 2];

var multiple = 2.0*frequency/sampleRate;
for (int n = 0; n < sampleRate * seconds; n++)
{
    var sampleSaw = ((n*multiple)%2) - 1;
    var sampleValue = sampleSaw > 0 ? amplitude : -amplitude;
    var sample = (short)(sampleValue * Int16.MaxValue);
    var bytes = BitConverter.GetBytes(sample);
    raw[n*2] = bytes[0];
    raw[n*2 + 1] = bytes[1];
}
```

`RawSourceWaveStream` takes a `Stream` and a `WaveFormat`. The `WaveFormat` in this instance is 16 bit mono PCM. The stream we can use `MemoryStream` for, passing in our byte array.

```c#
var ms = new MemoryStream(raw);
var rs = new RawSourceWaveStream(ms, new WaveFormat(sampleRate, 16, 1));
```

And now we can play the `RawSourceWaveStream` just like it was any other `WaveStream`:

```c#
var wo = new WaveOutEvent();
wo.Init(rs);
wo.Play();
while (wo.PlaybackState == PlaybackState.Playing)
{
    Thread.Sleep(500);
}
wo.Dispose();
```

## Turning a raw file into WAV

Suppose we have a raw audio file and we know the wave format of the audio in it. Let's say its 8kHz 16 bit mono. We can just open the file with `File.OpenRead` and pass it into a `RawSourceWaveStream`. Then we can convert it to a WAV file with `WaveFileWriter.CreateWaveFile`.

```c#
var path = "example.pcm";
var s = new RawSourceWaveStream(File.OpenRead(path), new WaveFormat(8000,1));
var outpath = "example.wav";
WaveFileWriter.CreateWaveFile(outpath, s);
```

Note that WAV files can contain compressed audio, so as long as you know the correct `WaveFormat` structure you can use that. Let's look at a compressed audio example next.

## Converting G.729 audio into a PCM WAV

Suppose we have a .g729 file containing raw audio compressed with G.729. G.729 isn't actually a built-in `WaveFormat` in NAudio (some other common ones like mu and a-law are). But we can use `WaveFormat.CreateCustomFormat` or even derive from `WaveFormat` to define the correct format.

Now in the previous example we saw how we could create a WAV file that contains the G.729 audio still encoded. But if we wanted it to be PCM, we'd need to use `WaveFormatConversionStream.CreatePcmStream` to look for an  ACM codec that understands the incoming `WaveFormat` and can turn it into PCM. 

Please note that this won't always be possible. If your version of Windows doesn't have a suitable decoder, this will fail.

But here's how we would convert that raw G.729 file into a PCM WAV file if we did have a suitable decoder:

```c#
var inFile = @"c:\users\mheath\desktop\chirpg729.g729";
var outFile = @"c:\users\mheath\desktop\chirpg729.wav";
var inFileFormat = WaveFormat.CreateCustomFormat(
            WaveFormatEncoding.G729,
            8000, // sample rate
            1, // channels
            1000, // average bytes per second
            10, // block align
            1); // bits per sample
using(var inStream = File.OpenRead(inFile))
using(var reader = new RawSourceWaveStream(inStream, inFileFormat))
using(var converter = WaveFormatConversionStream.CreatePcmStream(reader))
{
    WaveFileWriter.CreateWaveFile(outFile, converter);
}
```

If it was a format that NAUdio has built-in support for like G.711 a-law, then we'd do it like this:

```c#
var inFile = @"c:\users\mheath\desktop\alaw.bin";
var outFile = @"c:\users\mheath\desktop\alaw.wav";
var inFileFormat = WaveFormat.CreateALawFormat(8000,1);
using(var inStream = File.OpenRead(inFile))
using(var reader = new RawSourceWaveStream(inStream, inFileFormat))
using(var converter = WaveFormatConversionStream.CreatePcmStream(reader))
{
    WaveFileWriter.CreateWaveFile(outFile, converter);
}
```