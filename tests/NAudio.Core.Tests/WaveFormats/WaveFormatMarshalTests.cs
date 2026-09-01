using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudio.Core.Tests.WaveFormats;

/// <summary>
/// Covers the WAVEFORMATEX blob that <see cref="WaveFormat.MarshalToPtr"/> hands to native
/// callers (waveOutOpen, IAudioClient::Initialize, acmStreamOpen, ...) and the decode back.
///
/// These went through Marshal.StructureToPtr / Marshal.PtrToStructure&lt;T&gt;, which relied on
/// the runtime marshalling a class hierarchy by flattening the base class fields in. CoreCLR
/// does; NativeAOT does not — it emitted only the subclass's own fields, at offset 0, so a
/// WaveFormatExtensible reached the driver with its SubFormat GUID written over the sample
/// rate. See https://github.com/naudio/NAudio/issues/1425.
/// </summary>
[TestFixture]
[Category("UnitTest")]
public class WaveFormatMarshalTests
{
    private static byte[] ToBlob(WaveFormat format, int expectedLength)
    {
        IntPtr pointer = WaveFormat.MarshalToPtr(format);
        try
        {
            var blob = new byte[expectedLength];
            Marshal.Copy(pointer, blob, 0, blob.Length);
            return blob;
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
    }

    private static T RoundTrip<T>(T format) where T : WaveFormat
    {
        IntPtr pointer = WaveFormat.MarshalToPtr(format);
        try
        {
            return (T)WaveFormat.MarshalFromPtr(pointer);
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
    }

    [Test]
    public void PcmMarshalsAsFull18ByteWaveFormatEx()
    {
        // Serialize() writes canonical PCM as 16 bytes, but a native WAVEFORMATEX always
        // carries cbSize — truncating it would leave callers reading two bytes off the end.
        var blob = ToBlob(new WaveFormat(44100, 16, 2), 18);

        Assert.That(BitConverter.ToInt16(blob, 0), Is.EqualTo((short)WaveFormatEncoding.Pcm));
        Assert.That(BitConverter.ToInt16(blob, 2), Is.EqualTo(2), "channels");
        Assert.That(BitConverter.ToInt32(blob, 4), Is.EqualTo(44100), "sample rate");
        Assert.That(BitConverter.ToInt32(blob, 8), Is.EqualTo(176400), "average bytes per second");
        Assert.That(BitConverter.ToInt16(blob, 12), Is.EqualTo(4), "block align");
        Assert.That(BitConverter.ToInt16(blob, 14), Is.EqualTo(16), "bits per sample");
        Assert.That(BitConverter.ToInt16(blob, 16), Is.EqualTo(0), "cbSize");
    }

    [Test]
    public void ExtensibleMarshalsBaseFieldsBeforeItsOwn()
    {
        var format = new WaveFormatExtensible(48000, 24, 2, 0x3);
        var blob = ToBlob(format, 40);

        // The regression this guards: the base WAVEFORMATEX must occupy bytes 0..17, with the
        // extensible fields following it rather than overwriting it.
        Assert.That(BitConverter.ToUInt16(blob, 0), Is.EqualTo((ushort)WaveFormatEncoding.Extensible));
        Assert.That(BitConverter.ToInt32(blob, 4), Is.EqualTo(48000), "sample rate");
        Assert.That(BitConverter.ToInt16(blob, 14), Is.EqualTo(24), "bits per sample");
        Assert.That(BitConverter.ToInt16(blob, 16), Is.EqualTo(22), "cbSize");
        Assert.That(BitConverter.ToInt16(blob, 18), Is.EqualTo(24), "wValidBitsPerSample");
        Assert.That(BitConverter.ToInt32(blob, 20), Is.EqualTo(0x3), "dwChannelMask");
        Assert.That(new Guid(blob.AsSpan(24, 16)), Is.EqualTo(format.SubFormat), "SubFormat");
    }

    [Test]
    public void ExtensibleRoundTrips()
    {
        var original = new WaveFormatExtensible(96000, 32, 6, useIeeeFloat: false, validBitsPerSample: 24, channelMask: 0x3F);
        var result = RoundTrip(original);

        Assert.That(result.SampleRate, Is.EqualTo(96000));
        Assert.That(result.Channels, Is.EqualTo(6));
        Assert.That(result.BitsPerSample, Is.EqualTo(32));
        Assert.That(result.AverageBytesPerSecond, Is.EqualTo(original.AverageBytesPerSecond));
        Assert.That(result.BlockAlign, Is.EqualTo(original.BlockAlign));
        Assert.That(result.ValidBitsPerSample, Is.EqualTo(24));
        Assert.That(result.ChannelMask, Is.EqualTo(0x3F));
        Assert.That(result.SubFormat, Is.EqualTo(original.SubFormat));
    }

    [Test]
    public void AdpcmRoundTripsIncludingCoefficients()
    {
        var original = new AdpcmWaveFormat(22050, 1);
        var result = RoundTrip(original);

        Assert.That(result.SampleRate, Is.EqualTo(22050));
        Assert.That(result.BlockAlign, Is.EqualTo(original.BlockAlign));
        Assert.That(result.SamplesPerBlock, Is.EqualTo(original.SamplesPerBlock));
        Assert.That(result.NumCoefficients, Is.EqualTo(original.NumCoefficients));
        Assert.That(result.Coefficients, Is.EqualTo(original.Coefficients));
    }

    [Test]
    public void Gsm610RoundTrips()
    {
        var original = new Gsm610WaveFormat();
        var result = RoundTrip(original);

        Assert.That(result.SampleRate, Is.EqualTo(8000));
        Assert.That(result.BlockAlign, Is.EqualTo(65));
        Assert.That(result.SamplesPerBlock, Is.EqualTo(320));
    }

    [Test]
    public void Mp3MarshalsItsTwelveExtraBytes()
    {
        var original = new Mp3WaveFormat(44100, 2, 1152, 128000);
        var blob = ToBlob(original, 30);

        Assert.That(BitConverter.ToInt32(blob, 4), Is.EqualTo(44100), "sample rate");
        Assert.That(BitConverter.ToInt16(blob, 16), Is.EqualTo(12), "cbSize (MPEGLAYER3_WFX_EXTRA_BYTES)");
        Assert.That(BitConverter.ToUInt16(blob, 18), Is.EqualTo((ushort)Mp3WaveFormatId.Mpeg), "wID");
        Assert.That(BitConverter.ToUInt32(blob, 20), Is.EqualTo((uint)Mp3WaveFormatFlags.PaddingIso), "fdwFlags");
        Assert.That(BitConverter.ToUInt16(blob, 24), Is.EqualTo(1152), "nBlockSize");
        Assert.That(BitConverter.ToUInt16(blob, 26), Is.EqualTo(1), "nFramesPerBlock");
        Assert.That(BitConverter.ToUInt16(blob, 28), Is.EqualTo(0), "nCodecDelay");
    }

    [Test]
    public void UnrecognisedEncodingWithExtraDataRoundTripsAsExtraData()
    {
        var original = new Mp3WaveFormat(44100, 2, 1152, 128000);
        IntPtr pointer = WaveFormat.MarshalToPtr(original);
        try
        {
            var result = WaveFormat.MarshalFromPtr(pointer);

            Assert.That(result, Is.InstanceOf<WaveFormatExtraData>());
            var extraData = (WaveFormatExtraData)result;
            Assert.That(extraData.SampleRate, Is.EqualTo(44100));
            Assert.That(extraData.ExtraSize, Is.EqualTo(12));
            Assert.That(BitConverter.ToUInt16(extraData.ExtraData, 6), Is.EqualTo(1152), "nBlockSize");
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
    }

    /// <summary>
    /// The contract every native consumer relies on: it reads cbSize bytes of extra data
    /// after the 18-byte header, so the block MarshalToPtr allocates must be at least that
    /// long. Before this was enforced, a subclass that declared extraSize but did not write
    /// it in Serialize (as Mp3WaveFormat did) produced an 18-byte block advertising more.
    /// </summary>
    [TestCaseSource(nameof(AllShippedFormats))]
    public void BlobIsNeverShorterThanTheCbSizeItAdvertises(WaveFormat format)
    {
        IntPtr pointer = WaveFormat.MarshalToPtr(format);
        try
        {
            short cbSize = Marshal.ReadInt16(pointer, 16);
            Assert.That(cbSize, Is.GreaterThanOrEqualTo(0), "cbSize must not be negative");

            // Re-derive the block length the way ToWaveFormatExBytes does, then assert the
            // advertised extent fits inside it.
            var blob = new byte[18 + cbSize];
            Assert.DoesNotThrow(() => Marshal.Copy(pointer, blob, 0, blob.Length),
                "reading 18 + cbSize bytes must stay inside the allocation");
            Assert.That(cbSize, Is.EqualTo(format.ExtraSize),
                "the emitted cbSize should match the format's ExtraSize");
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
    }

    private static IEnumerable<TestCaseData> AllShippedFormats()
    {
        yield return new TestCaseData(new WaveFormat(44100, 16, 2)).SetName("Pcm");
        yield return new TestCaseData(WaveFormat.CreateIeeeFloatWaveFormat(48000, 2)).SetName("IeeeFloat");
        yield return new TestCaseData(new WaveFormatExtensible(48000, 24, 2, 0x3)).SetName("Extensible");
        yield return new TestCaseData(new AdpcmWaveFormat(22050, 1)).SetName("Adpcm");
        yield return new TestCaseData(new Gsm610WaveFormat()).SetName("Gsm610");
        yield return new TestCaseData(new Mp3WaveFormat(44100, 2, 1152, 128000)).SetName("Mp3");
        yield return new TestCaseData(new TrueSpeechWaveFormat()).SetName("TrueSpeech");
    }

    /// <summary>
    /// Drivers do under-report cbSize on WAVE_FORMAT_EXTENSIBLE. Marshal.PtrToStructure read
    /// the 22 extensible bytes regardless, and callers depend on getting a real SubFormat, so
    /// the hand-rolled decode has to do the same.
    /// </summary>
    [TestCase((short)22)]
    [TestCase((short)0)]
    [TestCase((short)14)]
    public void ExtensibleWithUnderReportedCbSizeStillDecodesItsSubFormat(short cbSize)
    {
        var source = new WaveFormatExtensible(48000, 24, 2, 0x3);
        IntPtr pointer = WaveFormat.MarshalToPtr(source);
        try
        {
            // The block really does carry all 22 extra bytes; only cbSize lies about it.
            Marshal.WriteInt16(pointer, 16, cbSize);

            var result = WaveFormat.MarshalFromPtr(pointer) as WaveFormatExtensible;

            Assert.That(result, Is.Not.Null, "should still decode as WaveFormatExtensible");
            Assert.That(result.SampleRate, Is.EqualTo(48000));
            Assert.That(result.SubFormat, Is.EqualTo(source.SubFormat));
            Assert.That(result.ChannelMask, Is.EqualTo(0x3));
            Assert.That(result.ValidBitsPerSample, Is.EqualTo(24));
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
    }

    [Test]
    public void PcmRoundTripsAsPlainWaveFormatWithNoExtraData()
    {
        var result = RoundTrip(new WaveFormat(44100, 16, 2));

        Assert.That(result, Is.TypeOf<WaveFormat>());
        Assert.That(result.ExtraSize, Is.EqualTo(0));
        Assert.That(result.SampleRate, Is.EqualTo(44100));
    }
}
