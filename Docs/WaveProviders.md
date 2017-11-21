# WaveStream, IWaveProvider and ISampleProvider

When you play audio with NAudio or construct a playback graph, you are typically working with either `IWaveProvider` or `ISampleProvider` interface implementations. This article explains the three main base interfaces and classes you will encounter in NAudio and when you might use them.

## WaveStream

`WaveStream` was the first base class in NAudio, and inherits from `System.IO.Stream`. It represents a stream of audio data, and its format can be determined by looking at the `WaveFormat` property.

It supports reporting `Length` and `Position` and these are both measured in terms of bytes, not samples. `WaveStreams` can be repositioned (assuming the underlying implementation supports that), although care must often be taken to reposition to a multiple of the `BlockAlign` of the `WaveFormat`. For example if the wave stream produces 16 bit samples, you should always reposition to an even numbered byte position.

Audio data is from a stream using the `Read` method which has the signature:

```c#
int Read(byte[] destBuffer, int offset, int numBytes)
```

This method is inherited from `System.IO.Stream`, and works in the standard way. The `destBuffer` is the buffer into which audio should be written. The `offset` parameter specifies where in the buffer to write audio to (this parameter is almost always 0), and the `numBytes` parameter is how many bytes of audio should be read.

The `Read` method returns the number for bytes that were read. This should never be more than `numBytes` and can only be less if the end of the audio stream is reached. NAudio playback devices will stop playing when `Read` returns 0.

`WaveStream` is the base class for NAudio file reader classes such as `WaveFileReader`, `Mp3FileReader`, `AiffFileReader` and `MediaFoundationReader`. It is a good choice of base class because these inherently support repositioning. `RawSourceWaveStream` is also a `WaveStream`, and delegates repositioning requests down to its source stream.

For a more detailed look at all the methods on `WaveStream`, see [this article](http://markheath.net/post/naudio-wavestream-in-depth)

## IWaveProvider

Implementing `WaveStream` can be quite a lot of work, and for non-repositionable streams can seem like overkill. Also, streams that simply read from a source and modify or analyse audio as it passes through don't really benefit from inheriting from `WaveStream`.

So the `IWaveProvider` interface provides a much simpler, minimal interface that simply has the `Read` method, and a `WaveFormat` property.

```c#
public interface IWaveProvider
{
    WaveFormat WaveFormat { get; }
    int Read(byte[] buffer, int offset, int count);
}
```

The `IWavePlayer` interface only needs an `IWaveProvider` passed to its `Init` method in order to be able to play audio. `WaveFileWriter.CreateWaveFile` and `MediaFoundationEncoder.EncodeToMp3` also only needs an `IWaveProvider` to dump the audio out to a WAV file. So in many cases you won't need to create a `WaveStream` implementation, just implement `IWavePlayer` and you've got an audio source that can be played or rendered to a file.

`BufferedWaveProvider` is a good example of a `IWaveProvider` as it has no ability to reposition - it simply returns any buffered audio from its `Read` method.

## ISampleProvider

The strength of `IWaveProvider` is that it can be used to represent audio in any format. It can be used for 16,24 or 32 bit PCM audio, and even for compressed audio (MP3, G.711 etc). But if you are performing any kind of signal processing or analysis on the audio, it is very likely that you want the audio to be in 32 bit IEEE floating point format. And it can be a pain to try to read floating point values out of a `byte[]` in C#.

So `ISampleProvider` defines an interface where the samples are all 32 bit floating point:

```c#
public interface ISampleProvider
{
    WaveFormat WaveFormat { get; }
    int Read(float[] buffer, int offset, int count);
}
```

The `WaveFormat` will always be 32 bit floating point, but the number of channels or sample rate may of course vary.

The `Read` method's `count` parameter specifies the number of samples to be read, and the method returns the number of samples written into `buffer`.

`ISampleProvider` is a great base interface to inherit from if you are implementing any kind of audio effects. In the `Read` method you typically read from your source `ISampleProvider`, then modify the floating point samples, before returning them. Here's the implementation of the `Read` method in `VolumeSampleProvider` showing how simple this can be:

```c#
public int Read(float[] buffer, int offset, int sampleCount)
{
    int samplesRead = source.Read(buffer, offset, sampleCount);
    if (volume != 1f)
    {
        for (int n = 0; n < sampleCount; n++)
        {
            buffer[offset + n] *= volume;
        }
    }
    return samplesRead;
}
```

NAudio makes it easy to go from an `IWaveProvider` to an `ISampleProvider` with the `ToSampleProvider` extension method. You can also use `AudioFileReader` which reads a wide variety of file types and implements `ISampleProvider`.

You can get back to an `IWaveProvider` with the `ToWaveProvider` extension method. Or there's the `ToWaveProvider16` extension method if you want to go back to 16 bit integer samples.