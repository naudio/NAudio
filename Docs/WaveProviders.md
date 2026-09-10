# WaveStream, IWaveProvider and ISampleProvider

When you play audio with NAudio or construct a playback graph, you are typically working with either `IWaveProvider` or `ISampleProvider` interface implementations. 
This article explains the three main base interfaces and classes you will encounter in NAudio and when you might use them.

## `WaveStream`

`WaveStream` was the first base class in NAudio, inherits from `System.IO.Stream`,
and implements `IWaveProvider` in newer versions.
It represents a stream of audio data, and its format can be determined by querying the `WaveFormat` property.
Typically, it is also seekable (repositionable), which it means that it is possible to get any sample of the stream at will.

Seeking (reposition) is implemented through the `System.IO.Stream` `Length` and `Position` properties. 
These properties are both measured in terms of bytes, not samples.
When a `WaveStream` is seekable (repositionable), care must often be taken to seek (reposition) to a multiple of the `BlockAlign` of the `WaveFormat`.
If, for example, the stream produces 16 bit samples, you should always seek (reposition) to an even numbered byte position.

Audio data are retrieved from a stream using the `Read` method which has the signature:

```c#
int Read(Span<byte> buffer);
```

This method is also inherited from `System.IO.Stream`, and works in the standard way. 
The `buffer` parameter is the buffer into which audio is expected to be written. 
It's `Length` property denotes how many bytes are expected to be written to it.

The return value of the `Read` method must provide the number of bytes that were read. 
This should never be more than `buffer.Length` bytes and can only be less if the end of the audio stream is reached. 
By convention, NAudio playback devices will stop playing when `Read` returns 0.

`WaveStream` is the base class for NAudio file reader classes such as `WaveFileReader`, `Mp3FileReader`, `AiffFileReader` and `MediaFoundationReader`.
It is a good choice of base class because these inherently support repositioning. 
`RawSourceWaveStream` is also a `WaveStream`, and delegates repositioning requests down to its source stream.

For a more detailed look at all the methods on `WaveStream`, see [this article](http://markheath.net/post/naudio-wavestream-in-depth).

## `IWaveProvider`

The `WaveStream` class is not suitable for all possible audio cases, and implementing all the properties/methods it requires can seem like overkill.
Additionally, not every audio primitive provides immutable samples (samples that are not changed during the lifetime of the stream).
It is also not necessary to implement when the source is non-seekable (not repositionable).
Other audio primitives are often wrapping other primitives to implement an effect or analyze the incoming samples.

For all these reasons described above, the `IWaveProvider` interface was created.
It provides a much simpler and straightforward interface that simply
has the `Read` method, and a `WaveFormat` property:

```c#
public interface IWaveProvider
{
    WaveFormat WaveFormat { get; }
    int Read(Span<byte> buffer);
}
```

The `IWavePlayer` interface only needs an `IWaveProvider` passed to its `Init` method in order to be able to play audio. 
`WaveFileWriter.CreateWaveFile` and `MediaFoundationEncoder.EncodeToMp3` also only need an `IWaveProvider` to dump the audio out to a WAV file.
So, in many cases you won't need to create a `WaveStream` implementation, just implement `IWaveProvider` and you have an audio source 
that can be played or rendered to a file.

`BufferedWaveProvider` is a good example of a `IWaveProvider` as it has no ability to reposition - it simply returns any buffered audio from its `Read` method.

## `ISampleProvider`

While implementing `IWaveProvider` is a useful approach to implement an audio effect, it has the caveat that
decoding the samples depends of the bit rate (number of bits per sample) declared on the `WaveFormat`. 
And, it can be difficult to get the samples as numbers and cover all the possible bit rates.

While the `IWaveProvider` is able to represent a lot of audio formats, 
just doing any signal processing suddenly becomes one of the hardest tasks to do.

For these reasons, the `ISampleProvider` interface was created.

This particular interface makes it easy to process the samples because it's `Read` 
method provides 32-bit IEEE floating-point samples:

```c#
public interface ISampleProvider
{
    WaveFormat WaveFormat { get; }
    int Read(float[] buffer, int offset, int count);
}
```

The `WaveFormat` will always be 32 bit floating point,
but the number of channels or sample rate may of course vary by implementation.

The `Read` method's `buffer` parameter specifies the buffer to place the processed samples into.
The buffer's `Length` property denotes how many bytes are expected to be written to it.

Finally, the return value indicates how many samples were written to the buffer.
You should return 0 to signal that you do not have any other data to provide.

To summarize, the `ISampleProvider` is a great base interface to use when you
are implementing any kind of an audio effect.

In the `Read` method you typically read from your source `ISampleProvider`, then modify the floating point samples, before returning them.
Here's the implementation of the `Read` method in `VolumeSampleProvider` showing how simple this can be:

```c#
public int Read(Span<byte> buffer)
{
    int samplesRead = source.Read(buffer);
    if (volume != 1f)
    {
        for (int n = 0; n < samplesRead; n++)
        {
            buffer[n] *= volume;
        }
    }
    return samplesRead;
}
```

To make the development of effects easier, NAudio provides built-in converters so that the transition
from an `IWaveProvider` to an `ISampleProvider` (and vice-versa) is easy.
For example, to convert any `IWaveProvider` to an `ISampleProvider` instance, you use the `ToSampleProvider` extension method.
You can also get back to an `IWaveProvider` at any time you want by using the `ToWaveProvider` extension method.
There is the `ToWaveProvider16` extension method too if you want specifically to get PCM 16 bit integer samples.

You can also use `AudioFileReader` which reads a wide variety of file types and implements `ISampleProvider`.

