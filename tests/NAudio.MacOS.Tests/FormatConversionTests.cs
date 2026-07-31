
using System;

using NAudio.Dmo;
using NAudio.Wave;
using NAudio.Utils;

using NUnit.Framework;

namespace NAudio.MacOS.Tests;

[TestFixture]
public class FormatConversionTests
{
    [TestCase(44100, 32, 2, 32, true)]
    [TestCase(44100, 16, 2, 14, true)]
    [TestCase(44100, 32, 2, 18, false)]
    [TestCase(48000, 16, 2, 16, false)]
    [TestCase(48000, 32, 2, 32, false)]
    [TestCase(48000, 64, 2, 52, false)]
    public void VerifyWaveFormatToASBDSucceeds(
        int sampleRate,
        int bitRate,
        int channels,
        int validBitsPerSample,
        bool specifyIeeeFloat
    )
    {
        WaveFormat srcFormat;
        if (validBitsPerSample != bitRate)
        {
            srcFormat = new WaveFormatExtensible(
                sampleRate,
                bitRate,
                channels,
                specifyIeeeFloat,
                validBitsPerSample,
                0
            );
        }
        else
        {
            srcFormat = specifyIeeeFloat ?
                WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels)
                : new(sampleRate, bitRate, channels);
        }

        CoreAudioTypes.AudioStreamBasicDescription asbd = default;

        Assert.DoesNotThrow(() => asbd = MacUtils.ConstructASBDFromWaveFormat(srcFormat));

        Assert.AreEqual(asbd.mSampleRate, srcFormat.SampleRate);

        Assert.AreEqual(asbd.mBytesPerFrame, srcFormat.BlockAlign);

        Assert.AreEqual(asbd.mChannelsPerFrame, srcFormat.Channels);

        Assert.AreEqual(asbd.mFramesPerPacket, 1U);

        if (specifyIeeeFloat)
        {
            Assert.That(asbd.mFormatFlags.HasFlag(CoreAudioTypes.AudioFormatFlags.kAudioFormatFlagIsFloat));
        }

        if (srcFormat is WaveFormatExtensible ext)
        {
            Assert.AreEqual(ext.ValidBitsPerSample, asbd.mBitsPerChannel);
            Assert.That(specifyIeeeFloat ?
                ext.SubFormat == AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT :
                ext.SubFormat == AudioMediaSubtypes.MEDIASUBTYPE_PCM
            );
        }
        else
        {
            Assert.AreEqual(srcFormat.BitsPerSample, asbd.mBitsPerChannel);
            Assert.That(specifyIeeeFloat ?
                srcFormat.Encoding == WaveFormatEncoding.IeeeFloat :
                srcFormat.Encoding == WaveFormatEncoding.Pcm
            );
        }
    }

    [TestCase(44100, 32, 2, 32, true)]
    [TestCase(44100, 16, 2, 14, true)]
    [TestCase(44100, 32, 2, 18, false)]
    [TestCase(48000, 16, 2, 16, false)]
    [TestCase(48000, 32, 2, 32, false)]
    [TestCase(48000, 64, 2, 52, false)]
    public void VerifyASBDToWaveFormatSucceeds(
        int sampleRate,
        int bitRate,
        int channels,
        int validBitsPerSample,
        bool specifyIeeeFloat
    )
    {
        var asbd = CoreAudioTypes.AudioStreamBasicDescription.FillOutASBDForLPCM(
            sampleRate,
            (uint)channels,
            (uint)validBitsPerSample,
            (uint)bitRate,
            specifyIeeeFloat,
            !BitConverter.IsLittleEndian
        );

        Assert.AreEqual(asbd.mFramesPerPacket, 1U);

        WaveFormat outFormat = null;

        Assert.DoesNotThrow(() => outFormat = MacUtils.ConstructWaveFormatFromASBD(asbd));

        Assert.NotNull(outFormat);

        Assert.AreEqual(asbd.mSampleRate, outFormat.SampleRate);

        Assert.AreEqual(asbd.mBytesPerFrame, outFormat.BlockAlign);

        Assert.AreEqual(asbd.mChannelsPerFrame, outFormat.Channels);

        if (specifyIeeeFloat)
        {
            if (validBitsPerSample == bitRate)
            {
                Assert.That(
                    outFormat.Encoding == WaveFormatEncoding.IeeeFloat
                );
                Assert.AreEqual(asbd.mBitsPerChannel, outFormat.BitsPerSample);
            }
            else
            {
                Assert.That(
                    outFormat.Encoding == WaveFormatEncoding.Extensible &&
                    ((WaveFormatExtensible)outFormat).SubFormat == AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT
                );
                Assert.AreEqual(asbd.mBitsPerChannel, ((WaveFormatExtensible)outFormat).ValidBitsPerSample);
            }
        }
        else
        {
            if (validBitsPerSample == bitRate)
            {
                Assert.That(
                    outFormat.Encoding == WaveFormatEncoding.Pcm
                );
                Assert.AreEqual(asbd.mBitsPerChannel, outFormat.BitsPerSample);
            }
            else
            {
                Assert.That(
                    outFormat.Encoding == WaveFormatEncoding.Extensible &&
                    ((WaveFormatExtensible)outFormat).SubFormat == AudioMediaSubtypes.MEDIASUBTYPE_PCM
                );
                Assert.AreEqual(asbd.mBitsPerChannel, ((WaveFormatExtensible)outFormat).ValidBitsPerSample);
            }
        }
    }

    [TestCase(WaveFormatEncoding.Acelp)]
    [TestCase(WaveFormatEncoding.Adpcm)]
    [TestCase(WaveFormatEncoding.IbmCvsd)]
    [TestCase(WaveFormatEncoding.DviAdpcm)]
    [TestCase(WaveFormatEncoding.WAVE_FORMAT_BTV_DIGITAL)]
    [TestCase(WaveFormatEncoding.WAVE_FORMAT_CS_IMAADPCM)]
    public void VerifyCustomFormatToASBDFails(WaveFormatEncoding enc)
    {
        // Give a random, nonsense wave format to create as an ASBD.
        var wf = WaveFormat.CreateCustomFormat(
            enc,
            8499,
            34,
            893949,
            300,
            32
        );

        // This should fail because the encoding specified above is not supported
        Assert.Throws<InvalidOperationException>(() => MacUtils.ConstructASBDFromWaveFormat(wf));
    }
}