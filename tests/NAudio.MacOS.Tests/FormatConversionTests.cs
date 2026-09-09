
using System;

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

        Assert.That(
            asbd.mSampleRate,
            Is.EqualTo((double)srcFormat.SampleRate),
            "Constructed sample rate must match with the source format!"
        );

        Assert.That(
            asbd.mBytesPerFrame,
            Is.EqualTo((uint)srcFormat.BlockAlign),
            "Constructed block align must match with the source format!"
        );

        Assert.That(
            asbd.mChannelsPerFrame,
            Is.EqualTo((uint)srcFormat.Channels),
            "Constructed # of channels must match with the source format!"
        );

        Assert.That(
            asbd.mFramesPerPacket,
            Is.EqualTo(1U),
            "Constructed # of frames per packet must be the value 1!"
        );

        if (specifyIeeeFloat)
        {
            Assert.That(
                asbd.mFormatFlags.HasFlag(CoreAudioTypes.AudioFormatFlags.kAudioFormatFlagIsFloat),
                Is.True,
                "Expected the target format to be IEEE float while it wasn't!"
            );
        }

        if (srcFormat is WaveFormatExtensible ext)
        {
            Assert.That(
                (uint)ext.ValidBitsPerSample,
                Is.EqualTo(asbd.mBitsPerChannel),
                "Constructed bits per sample must match with the source format!"
            );
            Assert.That(specifyIeeeFloat ?
                ext.SubFormat == AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT :
                ext.SubFormat == AudioMediaSubtypes.MEDIASUBTYPE_PCM
            );
        }
        else
        {
            Assert.That(
                (uint)srcFormat.BitsPerSample,
                Is.EqualTo(asbd.mBitsPerChannel),
                "Constructed bits per sample must match with the source format!"
            );
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

        Assert.That(
            asbd.mFramesPerPacket,
            Is.EqualTo(1U),
            "Constructed # of frames per packet must be the value 1!"
        );

        WaveFormat outFormat = null;

        Assert.DoesNotThrow(() => outFormat = MacUtils.ConstructWaveFormatFromASBD(asbd));

        Assert.NotNull(outFormat);

        Assert.That(
            asbd.mSampleRate,
            Is.EqualTo((double)outFormat.SampleRate),
            "Constructed sample rate must match with the source format!"
        );

        Assert.That(
            asbd.mBytesPerFrame,
            Is.EqualTo((uint)outFormat.BlockAlign),
            "Constructed block align must match with the source format!"
        );

        Assert.That(
            asbd.mChannelsPerFrame,
            Is.EqualTo((uint)outFormat.Channels),
            "Constructed # of channels must match with the source format!"
        );

        if (specifyIeeeFloat)
        {
            if (validBitsPerSample == bitRate)
            {
                Assert.That(
                    outFormat.Encoding,
                    Is.EqualTo(WaveFormatEncoding.IeeeFloat),
                    "The wave format encoding was not IeeeFloat!"
                );
                Assert.That(
                    asbd.mBitsPerChannel,
                    Is.EqualTo((uint)outFormat.BitsPerSample),
                    "Constructed bits per sample must match with the source format!"
                );
            }
            else
            {
                Assert.That(
                    outFormat.Encoding,
                    Is.EqualTo(WaveFormatEncoding.Extensible),
                    "The wave format encoding was not Extensible!"
                );
                Assert.That(
                    ((WaveFormatExtensible)outFormat).SubFormat,
                    Is.EqualTo(AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT),
                    "The wave format sub-format was not IeeeFloat!"
                );
                Assert.That(
                    asbd.mBitsPerChannel,
                    Is.EqualTo((uint)((WaveFormatExtensible)outFormat).ValidBitsPerSample),
                    "The number of valid bits per sample must match with the source format!"
                );
            }
        }
        else
        {
            if (validBitsPerSample == bitRate)
            {
                Assert.That(
                    outFormat.Encoding == WaveFormatEncoding.Pcm
                );
                Assert.That(
                    asbd.mBitsPerChannel,
                    Is.EqualTo((uint)outFormat.BitsPerSample),
                    "Constructed bits per sample must match with the source format!"
                );
            }
            else
            {
                Assert.That(
                    outFormat.Encoding,
                    Is.EqualTo(WaveFormatEncoding.Extensible),
                    "The wave format encoding was not Extensible!"
                );
                Assert.That(
                    ((WaveFormatExtensible)outFormat).SubFormat,
                    Is.EqualTo(AudioMediaSubtypes.MEDIASUBTYPE_PCM),
                    "The wave format sub-format was not PCM!"
                );
                Assert.That(
                    asbd.mBitsPerChannel,
                    Is.EqualTo((uint)((WaveFormatExtensible)outFormat).ValidBitsPerSample),
                    "Constructed valid bits per sample must match with the source format!"
                );
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