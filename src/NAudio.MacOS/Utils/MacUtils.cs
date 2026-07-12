
using System;
using System.Runtime.CompilerServices;

using NAudio.Dmo;
using NAudio.Wave;
using NAudio.MacOS.CoreAudioTypes;

namespace NAudio.Utils;

/// <summary>
/// Utilities relating to macOS API's. <br />
/// Not meant to be used outside of this assembly.
/// </summary>
internal static class MacUtils
{
    /// <summary>
    /// Constants in macOS can be sneaky; <br />
    /// they are defined as strings but they are differently translated in little and big endian architectures. <br />
    /// This method strives to have support for both of them.
    /// </summary>
    /// <remarks>This method should only be used in <see langword="static"/> <see langword="readonly"/> constants.</remarks>
    /// <param name="str">The 4 character code to convert to an integer value.</param>
    /// <returns>The converted integer value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The length <paramref name="str"/> is not 4 characters.</exception>
    public static int ConstructIntConstantValueFromString(ReadOnlySpan<byte> str)
    {
        if (str.Length != sizeof(int))
        {
            throw new ArgumentOutOfRangeException(nameof(str), "String cannot be less or more than 4 bytes");
        }
        else
        {
            // GetPinnableReference is faster than ref dest[0]. 
            // If deemed necessary in the future however, this should be changed.
            Span<byte> dest = stackalloc byte[sizeof(int)];
            str.CopyTo(dest);
            if (BitConverter.IsLittleEndian) { dest.Reverse(); }
            return Unsafe.ReadUnaligned<int>(ref dest.GetPinnableReference());
        }
    }

    /// <summary>
    /// Constants in macOS can be sneaky; <br />
    /// they are defined as strings but they are differently translated in little and big endian architectures. <br />
    /// This method strives to have support for both of them.
    /// </summary>
    /// <remarks>This method should only be used in <see langword="static"/> <see langword="readonly"/> constants.</remarks>
    /// <param name="str">The 4 character code to convert to an unsigned integer value.</param>
    /// <returns>The converted unsigned integer value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The length <paramref name="str"/> is not 4 characters.</exception>
    public static uint ConstructUIntConstantValueFromString(ReadOnlySpan<byte> str)
    {
        if (str.Length != sizeof(uint))
        {
            throw new ArgumentOutOfRangeException(nameof(str), "String cannot be less or more than 4 bytes");
        }
        else
        {
            // GetPinnableReference is faster than ref dest[0]. 
            // If deemed necessary in the future however, this should be changed.
            Span<byte> dest = stackalloc byte[sizeof(uint)];
            str.CopyTo(dest);
            if (BitConverter.IsLittleEndian) { dest.Reverse(); }
            return Unsafe.ReadUnaligned<uint>(ref dest.GetPinnableReference());
        }
    }

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
            int total_sample_bits = (blkAlign / channels) * 8;
            if (total_sample_bits != description.mBitsPerChannel || channelMaskIfNeeded != Speakers.None)
            {
                return new WaveFormatExtensible(
                    sampleRate,
                    total_sample_bits,
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

    public static Speakers DecodeAudioChannelLabel(AudioChannelLabel label)
    {
        switch (label)
        {
            case AudioChannelLabel.kAudioChannelLabel_Mono:
                return Speakers.Mono;
            case AudioChannelLabel.kAudioChannelLabel_Left:
            case AudioChannelLabel.kAudioChannelLabel_HeadphonesLeft:
                return Speakers.FrontLeft;
            case AudioChannelLabel.kAudioChannelLabel_Right:
            case AudioChannelLabel.kAudioChannelLabel_HeadphonesRight:
                return Speakers.FrontRight;
            case AudioChannelLabel.kAudioChannelLabel_Center:
                return Speakers.FrontCenter;
            case AudioChannelLabel.kAudioChannelLabel_LFEScreen:
                return Speakers.LowFrequency;
            case AudioChannelLabel.kAudioChannelLabel_LeftSurround:
                return Speakers.BackLeft;
            case AudioChannelLabel.kAudioChannelLabel_RightSurround:
                return Speakers.BackRight;
            case AudioChannelLabel.kAudioChannelLabel_LeftCenter:
                return Speakers.FrontLeftOfCenter;
            case AudioChannelLabel.kAudioChannelLabel_RightCenter:
                return Speakers.FrontRightOfCenter;
            case AudioChannelLabel.kAudioChannelLabel_CenterSurround:
            case AudioChannelLabel.kAudioChannelLabel_CenterSurroundDirect:
                return Speakers.BackCenter;
            case AudioChannelLabel.kAudioChannelLabel_LeftSurroundDirect:
                return Speakers.SideLeft;
            case AudioChannelLabel.kAudioChannelLabel_RightSurroundDirect:
                return Speakers.SideRight;
            case AudioChannelLabel.kAudioChannelLabel_TopCenterSurround:
                return Speakers.TopCenter;
            case AudioChannelLabel.kAudioChannelLabel_VerticalHeightLeft:
                return Speakers.TopFrontLeft;
            case AudioChannelLabel.kAudioChannelLabel_VerticalHeightRight:
                return Speakers.TopFrontRight;
            case AudioChannelLabel.kAudioChannelLabel_VerticalHeightCenter:
                return Speakers.TopFrontCenter;
            case AudioChannelLabel.kAudioChannelLabel_TopBackLeft:
                return Speakers.TopBackLeft;
            case AudioChannelLabel.kAudioChannelLabel_TopBackRight:
                return Speakers.TopBackRight;
            case AudioChannelLabel.kAudioChannelLabel_TopBackCenter:
                return Speakers.TopBackCenter;
            default:
                throw new ArgumentException("Cannot decode value" + label + " into a valid Speakers value");
        }
    }

    public static AudioChannelLayout ConstructAudioChannelLayoutFromSpeakers(Speakers speakers)
    {
        AudioChannelLayout layout = new();
        layout.mChannelBitmap = (AudioChannelBitmap)speakers;
        layout.mChannelLayoutTag = AudioChannelLayoutTag.kAudioChannelLayoutTag_UseChannelBitmap;
        return layout;
    }

    public static Speakers ConstructSpeakersValue(IntPtr layout, out bool needs_translation, out bool needs_extensible)
    {
        // mdcdi1315: This method tries at the best effort to decode the AudioChannelLayout and 
        // convert it into a valid Speakers combination, but there are technical challenges around it:
        // 1. Some tags cannot be 'exactly' represented and do need channel translation. 
        // For example, see the kAudioChannelLayoutTag_MPEG_4_0_B, 
        // which is similar to kAudioChannelLayoutTag_MPEG_4_0_A,
        // but with different channel orders. 
        // For that cases, channel translation must kick in.
        // 2. While channel descriptions describe in which order each channel is, 
        // that order may break the Speaker enum semantic which is: 
        // Each defined constant defines the channel's usage and relative position in data.
        // So, we need the channel translation again
        // (even if the channel description order happens to align with the Speaker enum semantic).
        // 3. The only tag that happens to exactly correspond to Speakers enum 
        // is the kAudioChannelLayoutTag_UseChannelBitmap.
        // The AudioChannelBitmap is in fact the Speakers enum,
        // so we just reintepret to it, but this is just a small fraction
        // of the whole image. We of course translate to it when the 
        // translation WaveFormatExtensible -> macOS API's is happening,
        // but the reverse is even harder.
        // TODOs:
        // 1. Not all tags are defined today. Define them all and flag translation as needed.
        // 2. There many tags that are duplicate of others and each one needs to be found out,
        // to avoid repeating if statements. This has already have a lot of them, let's not
        // make it worse if possible.
        // 3. Several channels in tags are misleading; for example, how does "rear left surround" 
        // translate to Speakers enum, and most important, if there is a 1-1 mapping for it we use 
        // that, or we must manufacture it.
        needs_extensible = false;
        needs_translation = false;
        AudioChannelLayoutTag tag = AudioChannelLayout.GetAudioChannelLayoutTag(layout);
        if (tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_UseChannelBitmap))
        {
            needs_extensible = true;
            return (Speakers)AudioChannelLayout.GetAudioChannelBitmap(layout);
        }
        else if (tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_UseChannelDescriptions))
        {
            Speakers returnValue = 0;
            uint nDescriptions = AudioChannelLayout.GetNumberOfChannelDescriptions(layout);
            for (uint I = 0; I < nDescriptions; I++)
            {
                AudioChannelDescription desc = AudioChannelLayout.GetChannelDescription(layout, I);
                returnValue |= DecodeAudioChannelLabel(desc.mChannelLabel);
            }
            // Probably it needs translation, because the channels can be in any possible order.
            needs_translation = true;
            needs_extensible = true; // And we also need a WaveFormatExtensible.
            return returnValue;
        }
        else if (tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_Mono))
        {
            return Speakers.Mono;
        }
        else if (
            tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_Stereo) ||
            tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_StereoHeadphones)
        )
        {
            return Speakers.Stereo;
        }
        else
        {
            // All of the below formats are complex; WaveFormatExtensible needs to get in.
            needs_extensible = true;
            if (tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_Quadraphonic))
            {
                return Speakers.Quad;
            }
            else if (tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_Pentagonal))
            {
                needs_translation = true;
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.SideLeft | Speakers.SideRight;
            }
            else if (tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_Hexagonal))
            {
                needs_translation = true;
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter |
                Speakers.SideLeft | Speakers.SideRight | Speakers.BackCenter;
            }
            else if (
                tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_3_0_A) ||
                // Same thing, but in different order.
                (needs_translation = tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_3_0_B))
            )
            {
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter;
            }
            else if (
                tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_4_0_A) ||
                (needs_translation =
                    // Same thing, but in different orders.
                    tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_4_0_B) ||
                    tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_Logic_4_0_C)
                )
            )
            {
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.BackCenter;
            }
            else if (
                tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_0_A) ||
                (needs_translation = (
                    // Same thing, but in different orders.
                    tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_0_B) ||
                    tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_0_C) ||
                    tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_0_D)
                ))
            )
            {
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.BackLeft | Speakers.BackRight;
            }
            else if (
                tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_1_A) ||
                (needs_translation = (
                    // Same thing, but in different orders.
                    tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_1_B) ||
                    tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_1_C) ||
                    tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_1_D)
                ))
            )
            {
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.LowFrequency
                | Speakers.FrontCenter | Speakers.BackLeft | Speakers.BackRight;
            }
            else if (
                tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_6_1_A)
            )
            {
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.LowFrequency
                | Speakers.FrontCenter | Speakers.BackLeft | Speakers.BackRight
                | Speakers.BackCenter;
            }
            else if (
                tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_7_1_A) ||
                (needs_translation = (
                    // Same thing, but in different orders.
                    tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_7_1_B) ||
                    tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_7_1_C) ||
                    tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_Emagic_Default_7_1)
                ))
            )
            {
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.LowFrequency
                | Speakers.FrontCenter | Speakers.BackLeft | Speakers.BackRight
                // mdcdi1315: TODO: Find whether these two below are correct.
                | Speakers.FrontLeftOfCenter | Speakers.FrontRightOfCenter;
            }
            else if (tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_ITU_2_1))
            {
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.BackCenter;
            }
            else if (tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_ITU_2_2))
            {
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.BackLeft | Speakers.BackRight;
            }
            else if (tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_DVD_4))
            {
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.LowFrequency;
            }
            else if (tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_DVD_5))
            {
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.LowFrequency | Speakers.BackCenter;
            }
            else if (tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_DVD_6))
            {
                return Speakers.FrontLeft | Speakers.FrontRight
                | Speakers.LowFrequency | Speakers.BackLeft | Speakers.BackRight;
            }
            else if (
                tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_DVD_10) ||
                tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_AC3_3_0_1)
            )
            {
                needs_translation = true;
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.LowFrequency | Speakers.FrontCenter;
            }
            else if (
                tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_DVD_11) ||
                tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_AC3_3_1_1)
            )
            {
                needs_translation = true;
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.LowFrequency | Speakers.FrontCenter | Speakers.BackCenter;
            }
            else if (tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_DVD_18))
            {
                needs_translation = true;
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.LowFrequency | Speakers.BackLeft | Speakers.BackRight;
            }
            else if (tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_AudioUnit_6_0))
            {
                needs_translation = true;
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.BackLeft |
                Speakers.BackRight | Speakers.FrontCenter | Speakers.BackCenter;
            }
            else if (tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_AudioUnit_7_0))
            {
                needs_translation = true;
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.BackLeft |
                Speakers.BackRight | Speakers.FrontCenter
                // mdcdi1315: TODO: Find whether these two below are correct.
                | Speakers.TopBackLeft | Speakers.TopBackRight;
            }
            else if (tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_AudioUnit_7_0_Front))
            {
                needs_translation = true;
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.BackLeft |
                Speakers.BackRight | Speakers.FrontCenter | Speakers.FrontLeftOfCenter |
                Speakers.FrontRightOfCenter;
            }
            else if (tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_AC3_1_0_1))
            {
                return Speakers.FrontCenter | Speakers.LowFrequency;
            }
            else if (tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_AC3_3_0))
            {
                needs_translation = true;
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter;
            }
            else if (tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_AC3_3_1))
            {
                needs_translation = true;
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.BackCenter;
            }
            else if (tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_AC3_2_1_1))
            {
                needs_translation = true;
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.BackCenter | Speakers.LowFrequency;
            }
            else if (tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_AC3_3_1_1))
            {
                needs_translation = true;
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.BackCenter | Speakers.LowFrequency;
            }
            else if (tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_WAVE_4_0_B))
            {
                // mdcdi1315: TODO: The layout describes the last two channels as:
                // "rear left surround"
                // "rear right surround"
                // Check whether the provided constants for them below are correct.
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.BackLeft | Speakers.BackRight;
            }
            else if (tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_WAVE_5_0_B))
            {
                // mdcdi1315: TODO: The layout describes the last two channels as:
                // "rear left surround"
                // "rear right surround"
                // Check whether the provided constants for them below are correct.
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.BackLeft | Speakers.BackRight;
            }
            else if (tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_WAVE_5_1_B))
            {
                // mdcdi1315: TODO: The layout describes the last two channels as:
                // "rear left surround"
                // "rear right surround"
                // Check whether the provided constants for them below are correct.
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.LowFrequency | Speakers.BackLeft | Speakers.BackRight;
            }
            else if (tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_WAVE_6_1))
            {
                needs_translation = true;
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.LowFrequency | Speakers.BackCenter | Speakers.BackLeft | Speakers.BackRight;
            }
            else if (tag.HasFlag(AudioChannelLayoutTag.kAudioChannelLayoutTag_WAVE_7_1))
            {
                needs_translation = true;
                return Speakers.Surround71;
            }
            else
            {
                throw new ArgumentException("Could not decompose the specified channel layout tag: " + tag);
            }
        }
    }
}