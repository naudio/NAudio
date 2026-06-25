using System;
using System.Collections.Generic;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudio.Windows.Tests.Wasapi;

/// <summary>
/// Unit tests for the resample-free format adaptation shared by WasapiPlayer and WasapiOut.
/// These exercise pure format logic and the provider-building chain — no audio hardware required,
/// so they are not marked IntegrationTest. (FindSupportedExclusiveFormatAtSampleRate needs a live
/// AudioClient and is covered by integration tests instead.)
/// </summary>
[TestFixture]
public class WasapiFormatAdaptationTests
{
    private static WaveFormat Float32(int rate = 44100, int channels = 2) =>
        WaveFormat.CreateIeeeFloatWaveFormat(rate, channels);

    private static WaveFormat Pcm(int bits, int rate = 44100, int channels = 2) =>
        new(rate, bits, channels);

    // ---- IsByteCompatible ----

    [Test]
    public void IsByteCompatible_IdenticalFormats_True()
    {
        Assert.That(WasapiFormatAdaptation.IsByteCompatible(Pcm(16), Pcm(16)), Is.True);
    }

    [Test]
    public void IsByteCompatible_ExtensibleFloatVsPlainFloat_True()
    {
        // A WaveFormatExtensible IEEE-float and a plain IEEE-float have the same byte layout.
        var extensible = new WaveFormatExtensible(44100, 32, 2);
        Assert.That(WasapiFormatAdaptation.IsByteCompatible(extensible, Float32()), Is.True);
    }

    [Test]
    public void IsByteCompatible_ExtensiblePcmVsPlainPcm_True()
    {
        var extensible = new WaveFormatExtensible(44100, 16, 2);
        Assert.That(WasapiFormatAdaptation.IsByteCompatible(extensible, Pcm(16)), Is.True);
    }

    [Test]
    public void IsByteCompatible_DifferentBitDepth_False()
    {
        Assert.That(WasapiFormatAdaptation.IsByteCompatible(Pcm(16), Float32()), Is.False);
    }

    [Test]
    public void IsByteCompatible_DifferentSampleRate_False()
    {
        Assert.That(WasapiFormatAdaptation.IsByteCompatible(Pcm(16, 44100), Pcm(16, 48000)), Is.False);
    }

    [Test]
    public void IsByteCompatible_DifferentChannels_False()
    {
        Assert.That(WasapiFormatAdaptation.IsByteCompatible(Pcm(16, 44100, 1), Pcm(16, 44100, 2)), Is.False);
    }

    // ---- CanAdaptChannels ----

    [TestCase(1, 1, ExpectedResult = true)]
    [TestCase(2, 2, ExpectedResult = true)]
    [TestCase(1, 2, ExpectedResult = true)]
    [TestCase(2, 1, ExpectedResult = true)]
    [TestCase(1, 6, ExpectedResult = false)]
    [TestCase(6, 2, ExpectedResult = false)]
    [TestCase(2, 6, ExpectedResult = false)]
    public bool CanAdaptChannels_OnlyIdentityAndMonoStereo(int from, int to) =>
        WasapiFormatAdaptation.CanAdaptChannels(from, to);

    // ---- IsSupportedTargetEncoding ----

    [Test]
    public void IsSupportedTargetEncoding_Float32_True() =>
        Assert.That(WasapiFormatAdaptation.IsSupportedTargetEncoding(Float32()), Is.True);

    [Test]
    public void IsSupportedTargetEncoding_Pcm16_True() =>
        Assert.That(WasapiFormatAdaptation.IsSupportedTargetEncoding(Pcm(16)), Is.True);

    [Test]
    public void IsSupportedTargetEncoding_Pcm24_True() =>
        Assert.That(WasapiFormatAdaptation.IsSupportedTargetEncoding(Pcm(24)), Is.True);

    [Test]
    public void IsSupportedTargetEncoding_Pcm32_False() =>
        Assert.That(WasapiFormatAdaptation.IsSupportedTargetEncoding(Pcm(32)), Is.False);

    [Test]
    public void IsSupportedTargetEncoding_Pcm8_False() =>
        Assert.That(WasapiFormatAdaptation.IsSupportedTargetEncoding(Pcm(8)), Is.False);

    // ---- TryDescribeAdaptation ----

    [Test]
    public void TryDescribeAdaptation_SampleRateMismatch_False()
    {
        var ok = WasapiFormatAdaptation.TryDescribeAdaptation(Pcm(16, 44100), Pcm(16, 48000), null);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void TryDescribeAdaptation_ByteCompatible_TrueWithNoConversions()
    {
        var conversions = new List<string>();
        var ok = WasapiFormatAdaptation.TryDescribeAdaptation(Float32(), new WaveFormatExtensible(44100, 32, 2), conversions);
        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.True);
            Assert.That(conversions, Is.Empty);
        });
    }

    [Test]
    public void TryDescribeAdaptation_BitDepthChange_DescribesOneConversion()
    {
        var conversions = new List<string>();
        var ok = WasapiFormatAdaptation.TryDescribeAdaptation(Pcm(16), new WaveFormatExtensible(44100, 32, 2), conversions);
        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.True);
            Assert.That(conversions, Has.Count.EqualTo(1));
            Assert.That(conversions[0], Does.Contain("16-bit PCM").And.Contain("32-bit float"));
        });
    }

    [Test]
    public void TryDescribeAdaptation_MonoToStereo_DescribesOneConversion()
    {
        var conversions = new List<string>();
        var ok = WasapiFormatAdaptation.TryDescribeAdaptation(Pcm(16, 44100, 1), Pcm(16, 44100, 2), conversions);
        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.True);
            Assert.That(conversions, Has.Count.EqualTo(1));
            Assert.That(conversions[0], Does.Contain("mono").And.Contain("stereo"));
        });
    }

    [Test]
    public void TryDescribeAdaptation_ChannelsAndBitDepth_DescribesChannelsThenEncoding()
    {
        var conversions = new List<string>();
        var ok = WasapiFormatAdaptation.TryDescribeAdaptation(Pcm(16, 44100, 1), new WaveFormatExtensible(44100, 32, 2), conversions);
        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.True);
            Assert.That(conversions, Has.Count.EqualTo(2));
            Assert.That(conversions[0], Does.Contain("mono").And.Contain("stereo"));
            Assert.That(conversions[1], Does.Contain("16-bit PCM").And.Contain("32-bit float"));
        });
    }

    [Test]
    public void TryDescribeAdaptation_UnsupportedChannelCount_False()
    {
        // 6-channel source can't be adapted to stereo without a mixing matrix.
        var ok = WasapiFormatAdaptation.TryDescribeAdaptation(Pcm(16, 44100, 6), Pcm(16, 44100, 2), null);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void TryDescribeAdaptation_UnsupportedTargetEncoding_False()
    {
        // float source -> 8-bit PCM target: same rate/channels but no converter for 8-bit output.
        var ok = WasapiFormatAdaptation.TryDescribeAdaptation(Float32(), Pcm(8), null);
        Assert.That(ok, Is.False);
    }

    // ---- AdaptProvider ----

    [Test]
    public void AdaptProvider_ByteCompatible_ReturnsSameInstance()
    {
        var source = new TestWaveProvider(Float32());
        var result = WasapiFormatAdaptation.AdaptProvider(source, new WaveFormatExtensible(44100, 32, 2));
        Assert.That(result, Is.SameAs(source));
    }

    [Test]
    public void AdaptProvider_SampleRateMismatch_ReturnsNull()
    {
        var source = new TestWaveProvider(Pcm(16, 44100));
        var result = WasapiFormatAdaptation.AdaptProvider(source, Pcm(16, 48000));
        Assert.That(result, Is.Null);
    }

    [Test]
    public void AdaptProvider_BitDepthConversion_ProducesTargetFormatAndData()
    {
        var source = new TestWaveProvider(Pcm(16, 44100, 2));
        var result = WasapiFormatAdaptation.AdaptProvider(source, new WaveFormatExtensible(44100, 32, 2));

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.WaveFormat.Encoding, Is.EqualTo(WaveFormatEncoding.IeeeFloat));
            Assert.That(result.WaveFormat.BitsPerSample, Is.EqualTo(32));
            Assert.That(result.WaveFormat.Channels, Is.EqualTo(2));
            Assert.That(result.WaveFormat.SampleRate, Is.EqualTo(44100));
        });
        Assert.That(Read(result), Is.GreaterThan(0), "expected the adapted chain to produce audio");
    }

    [Test]
    public void AdaptProvider_MonoToStereo_ProducesStereoAndData()
    {
        var source = new TestWaveProvider(Pcm(16, 44100, 1));
        var result = WasapiFormatAdaptation.AdaptProvider(source, new WaveFormatExtensible(44100, 16, 2));

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.WaveFormat.Channels, Is.EqualTo(2));
            Assert.That(result.WaveFormat.BitsPerSample, Is.EqualTo(16));
            Assert.That(result.WaveFormat.SampleRate, Is.EqualTo(44100));
        });
        Assert.That(Read(result), Is.GreaterThan(0));
    }

    [Test]
    public void AdaptProvider_UnsupportedChannelCount_ReturnsNull()
    {
        var source = new TestWaveProvider(Pcm(16, 44100, 6));
        var result = WasapiFormatAdaptation.AdaptProvider(source, Pcm(16, 44100, 2));
        Assert.That(result, Is.Null);
    }

    private static int Read(IWaveProvider provider)
    {
        Span<byte> buffer = stackalloc byte[512];
        return provider.Read(buffer);
    }

    /// <summary>Minimal in-memory provider that yields deterministic bytes in a given format.</summary>
    private sealed class TestWaveProvider : IWaveProvider
    {
        private readonly int totalBytes;
        private int position;

        public TestWaveProvider(WaveFormat format, int totalBytes = 1 << 16)
        {
            WaveFormat = format;
            this.totalBytes = totalBytes;
        }

        public WaveFormat WaveFormat { get; }

        public int Read(Span<byte> buffer)
        {
            int n = Math.Min(buffer.Length, totalBytes - position);
            for (int i = 0; i < n; i++)
                buffer[i] = (byte)((position + i) & 0xFF);
            position += n;
            return n;
        }
    }
}
