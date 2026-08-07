
using System;

using NAudio.Dmo;
using NAudio.Wave;
using NAudio.MacOS.CoreAudioTypes;

namespace NAudio.Utils;

// This partial declaration provides the mapping algorithms
// that convert structures of AudioStreamBasicDescription to wave formats,
// and wave formats to AudioStreamBasicDescription structures.
internal partial class MacUtils
{
    public static AudioStreamBasicDescription ConstructASBDFromWaveFormat(WaveFormat waveFormat)
    {
        switch (waveFormat.Encoding)
        {
            case WaveFormatEncoding.Pcm:
            case WaveFormatEncoding.IeeeFloat:
                return AudioStreamBasicDescription.FillOutASBDForLPCM(
                    waveFormat.SampleRate,
                    (uint)waveFormat.Channels,
                    (uint)waveFormat.BitsPerSample,
                    (uint)waveFormat.BitsPerSample,
                    waveFormat.Encoding == WaveFormatEncoding.IeeeFloat,
                    !BitConverter.IsLittleEndian
                );
            case WaveFormatEncoding.Extensible:
                if (waveFormat is not WaveFormatExtensible extensible)
                {
                    throw new ArgumentException(
                        "Incomplete layout of the audio format passed in; there is an extensible format but it cannot be decoded because it is not of type WaveFormatExtensible.",
                        "waveFormat"
                    );
                }
                return AudioStreamBasicDescription.FillOutASBDForLPCM(
                    waveFormat.SampleRate,
                    (uint)waveFormat.Channels,
                    (uint)extensible.ValidBitsPerSample,
                    (uint)waveFormat.BitsPerSample,
                    extensible.SubFormat == AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT,
                    !BitConverter.IsLittleEndian
                );
            case WaveFormatEncoding.ALaw:
                var asbd = new AudioStreamBasicDescription()
                {
                    mFormatID = AudioFormatIDs.kAudioFormatALaw,
                    mBitsPerChannel = (uint)waveFormat.BitsPerSample,
                    mFramesPerPacket = 1U,
                    mSampleRate = waveFormat.SampleRate,
                    mChannelsPerFrame = (uint)waveFormat.Channels,
                    mFormatFlags = !BitConverter.IsLittleEndian ? AudioFormatFlags.kAudioFormatFlagIsBigEndian : 0
                };
                asbd.mBytesPerPacket = asbd.mBytesPerFrame = (uint)(waveFormat.Channels * (waveFormat.BitsPerSample / 8L));
                return asbd;
            default:
                throw new InvalidOperationException("Audio format cannot be other than PCM, IEEE Float or A-law.");
        }
    }

    public static WaveFormat ConstructWaveFormatFromASBD(AudioStreamBasicDescription description, Speakers channelMaskIfNeeded = Speakers.None)
    {
        // mdcdi1315: Check whether the format ID kAudioFormatULaw is the Mu-law audio format.
        int sampleRate = (int)description.mSampleRate;
        int channels = (int)description.mChannelsPerFrame;
        if (description.mFormatID == AudioFormatIDs.kAudioFormatALaw)
        {
            return WaveFormat.CreateALawFormat(sampleRate, channels);
        }
        else if (description.mFormatID == AudioFormatIDs.kAudioFormatLinearPCM)
        {
            bool IeeeFloat = description.mFormatFlags.HasFlag(AudioFormatFlags.kAudioFormatFlagIsFloat);
            if (
                !IeeeFloat &&
                !description.mFormatFlags.HasFlag(AudioFormatFlags.kAudioFormatFlagIsSignedInteger)
            )
            {
                throw new NotSupportedException("Unsigned integer formats are not supported by NAudio.");
            }
            int blkAlign = (int)description.mBytesPerFrame;
            int totalSampleBits = (blkAlign / channels) * 8;
            if (totalSampleBits != description.mBitsPerChannel || channelMaskIfNeeded != Speakers.None)
            {
                return new WaveFormatExtensible(
                    sampleRate,
                    totalSampleBits,
                    channels,
                    IeeeFloat,
                    (int)description.mBitsPerChannel,
                    channelMaskIfNeeded
                );
            }
            else
            {
                return WaveFormat.CreateCustomFormat(
                    IeeeFloat ? WaveFormatEncoding.IeeeFloat : WaveFormatEncoding.Pcm,
                    sampleRate,
                    channels,
                    blkAlign * sampleRate,
                    blkAlign,
                    (int)description.mBitsPerChannel
                );
            }
        }
        else
        {
            throw new InvalidOperationException("Unknown audio format ID " + description.mFormatID);
        }
    }
}