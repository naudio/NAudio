
using System;
using System.Runtime.CompilerServices;

using NAudio.Wave;
using NAudio.MacOS.CoreAudioTypes;

namespace NAudio.Utils;

// This partial declaration provides the channel layout mapping algorithms from macOS -> Speakers enum.
internal partial class MacUtils
{
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

    // Constructs an AudioChannelLayout instance by specifying a Speakers value.
    // It is directly converted into a AudioChannelBitmap and that is used to 
    // create the layout.
    public static AudioChannelLayout ConstructAudioChannelLayoutFromSpeakers(Speakers speakers)
    {
        AudioChannelLayout layout = new();
        layout.mChannelBitmap = (AudioChannelBitmap)speakers;
        layout.mChannelLayoutTag = AudioChannelLayoutTag.kAudioChannelLayoutTag_UseChannelBitmap;
        return layout;
    }

    // Checks whether a given tag value has as a value
    // the given layout tag, ignoring the number of 
    // channels defined in the 'value' tag.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool MatchAudioChannelLayoutTagByConstantValueIgnoringSpeakerCount(
        AudioChannelLayoutTag value,
        AudioChannelLayoutTag expectedTag
    ) => ((uint)value >> 16) == ((uint)expectedTag >> 16);

    private static Speakers DecodeSpeakersFromChannelDescriptions(IntPtr layout, out bool needsTranslation, out bool needsExtensible)
    {
        // Translate the descriptions into a managed speakers array, 
        // and test the array in common layouts.
        // If a common layout is found and matches, it is returned;
        // otherwise the Speakers value is built from the speakers array and returned.
        uint nDescriptions = AudioChannelLayout.GetNumberOfChannelDescriptions(layout);
        Speakers[] speakers = new Speakers[nDescriptions];
        for (uint I = 0; I < nDescriptions; I++)
        {
            speakers[I] = DecodeAudioChannelLabel(
                AudioChannelLayout.GetChannelDescription(layout, I).mChannelLabel
            );
        }
        bool isUncommonLayout;
        if (speakers.Length == 1)
        {
            needsTranslation = false;
            needsExtensible = false;
            return Speakers.Mono;
        }
        else if (speakers.Length == 2)
        {
            if (speakers[0] == Speakers.FrontLeft && speakers[1] == Speakers.FrontRight)
            {
                needsTranslation = false;
                needsExtensible = false;
                return Speakers.Stereo;
            }
            else
            {
                isUncommonLayout = true;
            }
        }
        else if (speakers.Length == 4)
        {
            if (
                speakers[0] == Speakers.FrontLeft &&
                speakers[1] == Speakers.FrontRight &&
                speakers[2] == Speakers.BackLeft &&
                speakers[3] == Speakers.BackRight
            )
            {
                needsExtensible = true; // Probably.
                needsTranslation = false;
                return Speakers.Quad;
            }
            else
            {
                isUncommonLayout = true;
            }
        }
        else
        {
            isUncommonLayout = true;
        }
        var returnValue = Speakers.None;
        foreach (var spk in speakers) { returnValue |= spk; }
        // Probably it needs translation, because the channels can be in any possible order.
        needsTranslation = returnValue != Speakers.None;
        needsExtensible = isUncommonLayout; // And if we conclude to a result that is an uncommon layout, we will need extensible.
        return returnValue;
    }

    public static Speakers ConstructSpeakersValue(IntPtr layout, out bool needsTranslation, out bool needsExtensible)
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
        // 1. Not all tags are defined today. Define them all and do flag translation as needed.
        // 2. There are many tags that are duplicate of others and each one needs to be found out,
        // to avoid repeating if statements. This has already have a lot of them, let's not
        // make it worse if possible.
        // 3. Several channels in tags are misleading; for example, how does "rear left surround" 
        // translate to Speakers enum, and most important, if there is a 1-1 mapping for it, we should use 
        // that, or we must manufacture it.
        needsExtensible = false;
        needsTranslation = false;
        AudioChannelLayoutTag tag = AudioChannelLayout.GetAudioChannelLayoutTag(layout);
        if (MatchAudioChannelLayoutTagByConstantValueIgnoringSpeakerCount(
            tag, AudioChannelLayoutTag.kAudioChannelLayoutTag_UseChannelBitmap
        ))
        {
            var sp = (Speakers)AudioChannelLayout.GetAudioChannelBitmap(layout);
            needsExtensible = sp != Speakers.Mono && sp != Speakers.Stereo;
            return sp;
        }
        else if (MatchAudioChannelLayoutTagByConstantValueIgnoringSpeakerCount(
            tag, AudioChannelLayoutTag.kAudioChannelLayoutTag_UseChannelDescriptions
        ))
        {
            return DecodeSpeakersFromChannelDescriptions(layout, out needsTranslation, out needsExtensible);
        }
        else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_Mono)
        {
            return Speakers.Mono;
        }
        else if (
            tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_Stereo ||
            tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_StereoHeadphones
        )
        {
            return Speakers.Stereo;
        }
        else
        {
            // All of the below formats are complex; WaveFormatExtensible needs to get in.
            needsExtensible = true;
            if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_Quadraphonic)
            {
                return Speakers.Quad;
            }
            else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_Pentagonal)
            {
                needsTranslation = true;
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.SideLeft | Speakers.SideRight;
            }
            else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_Hexagonal)
            {
                needsTranslation = true;
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter |
                Speakers.SideLeft | Speakers.SideRight | Speakers.BackCenter;
            }
            else if (
                tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_3_0_A ||
                // Same thing, but in different order.
                (needsTranslation = tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_3_0_B)
            )
            {
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter;
            }
            else if (
                tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_4_0_A ||
                (needsTranslation =
                    // Same thing, but in different orders.
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_4_0_B ||
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_Logic_4_0_C
                )
            )
            {
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.BackCenter;
            }
            else if (
                tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_0_A ||
                (needsTranslation = (
                    // Same thing, but in different orders.
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_0_B ||
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_0_C ||
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_0_D
                ))
            )
            {
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.BackLeft | Speakers.BackRight;
            }
            else if (
                tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_1_A ||
                (needsTranslation = (
                    // Same thing, but in different orders.
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_1_B ||
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_1_C ||
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_1_D
                ))
            )
            {
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.LowFrequency
                | Speakers.FrontCenter | Speakers.BackLeft | Speakers.BackRight;
            }
            else if (
                tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_6_1_A
            )
            {
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.LowFrequency
                | Speakers.FrontCenter | Speakers.BackLeft | Speakers.BackRight
                | Speakers.BackCenter;
            }
            else if (
                tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_7_1_A ||
                (needsTranslation = (
                    // Same thing, but in different orders.
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_7_1_B ||
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_7_1_C ||
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_Emagic_Default_7_1
                ))
            )
            {
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.LowFrequency
                | Speakers.FrontCenter | Speakers.BackLeft | Speakers.BackRight
                // mdcdi1315: TODO: Find whether these two below are correct.
                | Speakers.FrontLeftOfCenter | Speakers.FrontRightOfCenter;
            }
            else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_ITU_2_1)
            {
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.BackCenter;
            }
            else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_ITU_2_2)
            {
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.BackLeft | Speakers.BackRight;
            }
            else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_DVD_4)
            {
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.LowFrequency;
            }
            else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_DVD_5)
            {
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.LowFrequency | Speakers.BackCenter;
            }
            else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_DVD_6)
            {
                return Speakers.FrontLeft | Speakers.FrontRight
                | Speakers.LowFrequency | Speakers.BackLeft | Speakers.BackRight;
            }
            else if (
                tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_DVD_10 ||
                tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_AC3_3_0_1
            )
            {
                needsTranslation = true;
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.LowFrequency | Speakers.FrontCenter;
            }
            else if (
                tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_DVD_11 ||
                tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_AC3_3_1_1
            )
            {
                needsTranslation = true;
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.LowFrequency | Speakers.FrontCenter | Speakers.BackCenter;
            }
            else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_DVD_18)
            {
                needsTranslation = true;
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.LowFrequency | Speakers.BackLeft | Speakers.BackRight;
            }
            else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_AudioUnit_6_0)
            {
                needsTranslation = true;
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.BackLeft |
                Speakers.BackRight | Speakers.FrontCenter | Speakers.BackCenter;
            }
            else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_AudioUnit_7_0)
            {
                needsTranslation = true;
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.BackLeft |
                Speakers.BackRight | Speakers.FrontCenter
                // mdcdi1315: TODO: Find whether these two below are correct.
                | Speakers.TopBackLeft | Speakers.TopBackRight;
            }
            else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_AudioUnit_7_0_Front)
            {
                needsTranslation = true;
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.BackLeft |
                Speakers.BackRight | Speakers.FrontCenter | Speakers.FrontLeftOfCenter |
                Speakers.FrontRightOfCenter;
            }
            else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_AC3_1_0_1)
            {
                return Speakers.FrontCenter | Speakers.LowFrequency;
            }
            else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_AC3_3_0)
            {
                needsTranslation = true;
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter;
            }
            else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_AC3_3_1)
            {
                needsTranslation = true;
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.BackCenter;
            }
            else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_AC3_2_1_1)
            {
                needsTranslation = true;
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.BackCenter | Speakers.LowFrequency;
            }
            else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_AC3_3_1_1)
            {
                needsTranslation = true;
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.BackCenter | Speakers.LowFrequency;
            }
            else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_WAVE_4_0_B)
            {
                // mdcdi1315: TODO: The layout describes the last two channels as:
                // "rear left surround"
                // "rear right surround"
                // Check whether the provided constants for them below are correct.
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.BackLeft | Speakers.BackRight;
            }
            else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_WAVE_5_0_B)
            {
                // mdcdi1315: TODO: The layout describes the last two channels as:
                // "rear left surround"
                // "rear right surround"
                // Check whether the provided constants for them below are correct.
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.BackLeft | Speakers.BackRight;
            }
            else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_WAVE_5_1_B)
            {
                // mdcdi1315: TODO: The layout describes the last two channels as:
                // "rear left surround"
                // "rear right surround"
                // Check whether the provided constants for them below are correct.
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.LowFrequency | Speakers.BackLeft | Speakers.BackRight;
            }
            else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_WAVE_6_1)
            {
                needsTranslation = true;
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.LowFrequency | Speakers.BackCenter | Speakers.BackLeft | Speakers.BackRight;
            }
            else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_WAVE_7_1)
            {
                needsTranslation = true;
                return Speakers.Surround71;
            }
            else
            {
                throw new ArgumentException("Could not decompose the specified channel layout tag: " + tag);
            }
        }
    }
}