
using System;
using System.Collections.Generic;
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
                throw new ArgumentException("Cannot decode value " + label + " into a valid Speakers value");
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
        // Translate the descriptions into a list, 
        // and test the list in common layouts.
        // If a common layout is found and matches, it is returned;
        // otherwise the Speakers value is built from the speakers list and returned.
        uint nDescriptions = AudioChannelLayout.GetNumberOfChannelDescriptions(layout);
        List<Speakers> speakers = new((int)nDescriptions);
        for (uint I = 0; I < nDescriptions; I++)
        {
            var desc = AudioChannelLayout.GetChannelDescription(layout, I);
            // Avoid unknown/unused channel labels.
            if (
                desc.mChannelLabel == AudioChannelLabel.kAudioChannelLabel_Unused ||
                desc.mChannelLabel == AudioChannelLabel.kAudioChannelLabel_Unknown
            ) { continue; }
            speakers.Add(DecodeAudioChannelLabel(desc.mChannelLabel));
        }
        if (speakers.Count == 0)
        {
            // We do not need any translation, or the extensible wave format.
            needsTranslation = false;
            needsExtensible = false;
            return Speakers.None;
        }
        else if (speakers.Count == 1)
        {
            // It is a single speaker, resolve to 
            // mono, whatever the channel description is.
            needsTranslation = false;
            needsExtensible = false;
            return Speakers.Mono;
        }
        else
        {
            needsTranslation = false;
            var builtSpeakersValue = Speakers.None;
            // Whatever the layout is (except from stereo), we need WaveFormatExtensible.
            needsExtensible = true;
            // This loop builds the final speakers value
            // and additionally checks whether the current description(s)
            // match the Speakers enum layout semantics.
            // If not, the needsTranslation parameter is set to true.
            Speakers tempSpeaker;
            int length = speakers.Count - 1;
            for (int I = 0; I < length; I++)
            {
                // For the returned speakers value to not need channel layout translation,
                // the current speaker value needs to be less than or equal to the next value.
                if ((tempSpeaker = speakers[I]) > speakers[I + 1])
                {
                    needsTranslation = true;
                }
                builtSpeakersValue |= tempSpeaker;
            }
            // The last speaker value won't be appended by the loop; do that now.
            builtSpeakersValue |= speakers[length];
            if (builtSpeakersValue == Speakers.Stereo)
            {
                // We resolved to a stereo layout, so we do not need WaveFormatExtensible.
                needsExtensible = false;
            }
            return builtSpeakersValue;
        }
    }

    /*
        Several useful channel mappings in tags that are useful to know when porting tags:
        
        L -> Speakers.FrontLeft
        R -> Speakers.FrontRight
        C -> Speakers.FrontCenter
        LFE -> Speakers.LowFrequency

        Ls -> Speakers.BackLeft
        Rs -> Speakers.BackRight

        Rls -> Speakers.SideLeft
        Rrs -> Speakers.SideRight
        Lsd -> Speakers.SideLeft (left surround direct)
        Rsd -> Speakers.SideRight (right surround direct)

        Cs -> Speakers.BackCenter
        Lc -> Speakers.FrontLeftOfCenter
        Rc -> Speakers.FrontRightOfCenter

        Lts -> Speakers.TopBackLeft
        Ts -> Speakers.TopBackCenter
        Rts -> Speakers.TopBackRight

        Vhl -> Speakers.TopFrontLeft
        Vhc -> Speakers.TopFrontCenter
        Vhr -> Speakers.TopFrontRight
    */

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
        needsExtensible = false;
        needsTranslation = false;
        AudioChannelLayoutTag tag = AudioChannelLayout.GetAudioChannelLayoutTag(layout);
        if (MatchAudioChannelLayoutTagByConstantValueIgnoringSpeakerCount(
            tag, AudioChannelLayoutTag.kAudioChannelLayoutTag_UseChannelBitmap
        ))
        {
            var sp = (Speakers)AudioChannelLayout.GetAudioChannelBitmap(layout);
            needsExtensible = sp != Speakers.None && sp != Speakers.Mono && sp != Speakers.Stereo;
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
            tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_StereoHeadphones ||
            tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_Binaural
        )
        {
            return Speakers.Stereo;
        }
        else
        {
            // IMPORTANT: Keep this as clean as possible.
            // Add any tag to it's corresponding region based on
            // the number of channels it provides.
            // If the tag's channel count does not match any of the
            // below provided regions, add it to the if block before the
            // 3-channel region.
            // IMPORTANT: Any new added tag here should have a corresponding
            // test case in ChannelConversionTests.VerifyThatTagValidlyDecodes test. 

            // All of the below channel layouts are complex; WaveFormatExtensible needs to get in.
            needsExtensible = true;

            if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_AC3_1_0_1) // < C LFE
            {
                return Speakers.FrontCenter | Speakers.LowFrequency;
            }

            #region 3 channels
            else if (
                tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_3_0_A || // <  L R C
                (needsTranslation = (
                    // Same thing, but in different order.
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_3_0_B || // <  C L R
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_AC3_3_0 // < L C R
                ))
            )
            {
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter;
            }
            else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_ITU_2_1) // <  L R Cs
            {
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.BackCenter;
            }
            else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_DVD_4) // < L R LFE
            {
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.LowFrequency;
            }
            #endregion

            #region 4 channels
            else if (
                tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_Quadraphonic || // < L R Ls Rs  -- 90 degree speaker separation
                tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_ITU_2_2 // <  L R Ls Rs
            )
            {
                return Speakers.Quad;
            }
            else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_WAVE_4_0_B) // < 4 channels, L R Rls Rrs
            {
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.BackLeft | Speakers.BackRight;
            }
            else if (
                tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_4_0_A || // <  L R C Cs
                (needsTranslation = (
                    // Same thing, but in different orders.
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_4_0_B || // <  C L R Cs
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_Logic_4_0_C || // < L R Cs C
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_AC3_3_1 // < L C R Cs
                ))
            )
            {
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.BackCenter;
            }
            else if (
                tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_DVD_5 || // < L R LFE Cs
                (needsTranslation =
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_AC3_2_1_1 // < L R Cs LFE
                )
            )
            {
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.LowFrequency | Speakers.BackCenter;
            }
            else if (
                tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_DVD_10 || // < L R C LFE
                (needsTranslation = (
                    // Same thing, but in different orders.
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_AC3_3_0_1 || // < L C R LFE
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_3_1 // < C L R LFE
                ))
            )
            {
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.LowFrequency;
            }
            #endregion

            #region 5 channels
            else if (
                tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_0_A || // <  L R C Ls Rs
                (needsTranslation = (
                    // Same thing, but in different orders.
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_0_B || // <  L C R Ls Rs
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_0_C || // <  L C R Ls Rs
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_0_D || // <  C L R Ls Rs
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_Pentagonal // < L R Ls Rs C  -- 72 degree speaker separation
                ))
            )
            {
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter |
                Speakers.BackLeft | Speakers.BackRight;
            }
            else if (
                tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_WAVE_5_0_B || // < 5 channels, L R C Rls Rrs
                (needsTranslation = (
                    // Same thing, but in different orders.
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_Ogg_5_0 || // < 5 channels, L C R Rls Rrs
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_0_E // < 5 channels, L R Rls Rrs C
                ))
            )
            {
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter |
                Speakers.SideLeft | Speakers.SideRight;
            }
            else if (
                tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_DVD_6 || // < L R LFE Ls Rs
                (needsTranslation = (
                    // Same thing, but in different orders.
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_DVD_18 // < L R Ls Rs LFE
                ))
            )
            {
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.LowFrequency |
                Speakers.BackLeft | Speakers.BackRight;
            }
            else if (
                tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_DVD_11 || // < L R C LFE Cs
                (needsTranslation = (
                    // Same thing, but in different orders.
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_AC3_3_1_1 || // < L C R Cs LFE
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_4_1 // < C L R Cs LFE
                ))
            )
            {
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter |
                Speakers.LowFrequency | Speakers.BackCenter;
            }
            #endregion

            #region 6 channels
            else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_6_0_A) // < Lc Rc L R Ls Rs
            {
                needsTranslation = true;
                return Speakers.FrontLeftOfCenter | Speakers.FrontRightOfCenter | Speakers.FrontLeft |
                Speakers.FrontRight | Speakers.BackLeft | Speakers.BackRight;
            }
            else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_6_0_B) // < C L R Rls Rrs Ts
            {
                needsTranslation = true;
                return Speakers.FrontCenter | Speakers.FrontLeft | Speakers.FrontRight |
                Speakers.SideLeft | Speakers.SideRight | Speakers.TopBackCenter;
            }
            else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_6_0_C) // < C Cs L R Rls Rrs
            {
                needsTranslation = true;
                return Speakers.FrontCenter | Speakers.BackCenter | Speakers.FrontLeft |
                Speakers.FrontRight | Speakers.SideLeft | Speakers.SideRight;
            }
            else if (
                tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_Hexagonal || // < L R Ls Rs C Cs  -- 60 degree speaker separation
                tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_AudioUnit_6_0 || // < L R Ls Rs C Cs
                tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_AAC_6_0 || // < C L R Ls Rs Cs
                tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_EAC_6_0_A || // < L C R Ls Rs Cs
                tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_Logic_6_0_B // < L R Ls Rs Cs C
            )
            {
                needsTranslation = true;
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter |
                Speakers.BackLeft | Speakers.BackRight | Speakers.BackCenter;
            }
            else if (
                tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_WAVE_5_1_B || // < 6 channels, L R C LFE Rls Rrs
                (needsTranslation = (
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_Ogg_5_1 || // < 6 channels, L C R Rls Rrs LFE
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_1_E // < 6 channels, L R Rls Rrs C LFE
                ))
            )
            {
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter |
                Speakers.LowFrequency | Speakers.SideLeft | Speakers.SideRight;
            }
            else if (
                tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_1_A || // <  L R C LFE Ls Rs
                (needsTranslation = (
                    // Same thing, but in different orders.
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_1_B || // <  L R Ls Rs C LFE
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_1_C || // <  L C R Ls Rs LFE
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_1_D // <  C L R Ls Rs LFE
                ))
            )
            {
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.LowFrequency
                | Speakers.FrontCenter | Speakers.BackLeft | Speakers.BackRight;
            }
            #endregion

            #region 7 channels
            else if (
                tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_WAVE_6_1 || // < 7 channels, L R C LFE Cs Ls Rs
                (needsTranslation = (
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_Ogg_6_1 || // < 7 channels, L C R Ls Rs Cs LFE
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_6_1_B // < 7 channels, L R Ls Rs C Cs LFE
                ))
            )
            {
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter |
                Speakers.LowFrequency | Speakers.BackCenter | Speakers.SideLeft |
                Speakers.SideRight;
            }
            else if (
                tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_AudioUnit_7_0 || // < L R Ls Rs C Rls Rrs
                tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_AAC_7_0 || // < C L R Ls Rs Rls Rrs
                tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_EAC_7_0_A // < L C R Ls Rs Rls Rrs
            )
            {
                needsTranslation = true;
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter |
                Speakers.SideLeft | Speakers.SideRight | Speakers.BackLeft | Speakers.BackRight;
            }
            else if (
                tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_AudioUnit_7_0_Front || // < L R Ls Rs C Lc Rc
                tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_7_0 // < Lc C Rc L R Ls Rs
            )
            {
                needsTranslation = true;
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.BackLeft |
                Speakers.BackRight | Speakers.FrontCenter | Speakers.FrontLeftOfCenter |
                Speakers.FrontRightOfCenter;
            }
            else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_6_1_A) // < Lc Rc L R Ls Rs LFE
            {
                needsTranslation = true;
                return Speakers.FrontLeftOfCenter | Speakers.FrontRightOfCenter | Speakers.FrontLeft |
                Speakers.FrontRight | Speakers.BackLeft | Speakers.BackRight |
                Speakers.LowFrequency;
            }
            else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_6_1_B) // < C L R Rls Rrs Ts LFE
            {
                needsTranslation = true;
                return Speakers.FrontCenter | Speakers.FrontLeft | Speakers.FrontRight |
                Speakers.SideLeft | Speakers.SideRight | Speakers.TopBackCenter |
                Speakers.LowFrequency;
            }
            else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_6_1_C) // < C Cs L R Rls Rrs LFE
            {
                needsTranslation = true;
                return Speakers.FrontCenter | Speakers.BackCenter | Speakers.FrontLeft |
                Speakers.FrontRight | Speakers.SideLeft | Speakers.SideRight |
                Speakers.LowFrequency;
            }
            else if (
                tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_6_1_A || // <  L R C LFE Ls Rs Cs
                (needsTranslation = (
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_EAC3_6_1_A || // < L C R Ls Rs LFE Cs
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_AAC_6_1 || // < C L R Ls Rs Cs Lfe
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_6_1_D || // < C L R Ls Rs LFE Cs
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_Logic_6_1_B || // < L R Ls Rs Cs C LFE
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_Logic_6_1_D // < L C R Ls Cs Rs LFE
                ))
            )
            {
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter |
                Speakers.LowFrequency | Speakers.BackLeft | Speakers.BackRight | Speakers.BackCenter;
            }
            else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_EAC3_6_1_C) // < L C R Ls Rs LFE Vhc
            {
                needsTranslation = true;
                return Speakers.FrontLeft | Speakers.FrontCenter | Speakers.FrontRight |
                Speakers.SideLeft | Speakers.SideRight | Speakers.LowFrequency |
                Speakers.TopFrontCenter;
            }
            else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_EAC3_6_1_B) // < L C R Ls Rs LFE Ts
            {
                needsTranslation = true;
                return Speakers.FrontLeft | Speakers.FrontCenter | Speakers.FrontRight |
                Speakers.SideLeft | Speakers.SideRight | Speakers.LowFrequency |
                Speakers.TopBackCenter;
            }
            #endregion

            #region 8 channels
            else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_8_0_A) // < Lc Rc L R Ls Rs Rls Rrs
            {
                needsTranslation = true;
                return Speakers.FrontLeftOfCenter | Speakers.FrontRightOfCenter | Speakers.FrontLeft |
                Speakers.FrontRight | Speakers.BackLeft | Speakers.BackRight |
                Speakers.SideLeft | Speakers.SideRight;
            }
            else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_8_0_B) // < Lc C Rc L R Ls Cs Rs
            {
                needsTranslation = true;
                return Speakers.FrontLeftOfCenter | Speakers.FrontCenter | Speakers.FrontRightOfCenter |
                Speakers.FrontLeft | Speakers.FrontRight | Speakers.BackLeft |
                Speakers.BackCenter | Speakers.BackRight;
            }
            else if (
                tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_7_1_A || // <  L R C LFE Ls Rs Lc Rc
                (needsTranslation = (
                    // Same thing, but in different orders.
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_7_1_B || // <  C Lc Rc L R Ls Rs LFE  
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_Emagic_Default_7_1 || // <  L R Ls Rs C LFE Lc Rc
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_EAC3_7_1_B || // < L C R Ls Rs LFE Lc Rc
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_7_1 // < Lc C Rc L R Ls Rs LFE
                ))
            )
            {
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter |
                Speakers.LowFrequency | Speakers.BackLeft | Speakers.BackRight |
                Speakers.FrontLeftOfCenter | Speakers.FrontRightOfCenter;
            }
            else if (
                tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_WAVE_7_1 || // < 8 channels, L R C LFE Rls Rrs Ls Rs
                (needsTranslation = (
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_7_1_C || // <  L R C LFE Ls Rs Rls Rrs
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_EAC3_7_1_A || // < L C R Ls Rs LFE Rls Rrs
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_Logic_7_1_B || // < L R Ls Rs Rls Rrs C LFE
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_7_1_D // < 8 channels, L R Rls Rrs Ls Rs C LFE
                ))
            )
            {
                return Speakers.Surround71;
            }
            else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_EAC3_7_1_C) // < L C R Ls Rs LFE Lsd Rsd
            {
                needsTranslation = true;
                return Speakers.FrontLeft | Speakers.FrontCenter | Speakers.FrontRight |
                Speakers.BackLeft | Speakers.BackRight | Speakers.LowFrequency |
                Speakers.SideLeft | Speakers.SideRight;
            }
            else if (
                tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_CICP_14 || // < L R C LFE Ls Rs Vhl Vhr
                (needsTranslation = (
                    tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_EAC3_7_1_E // < L C R Ls Rs LFE Vhl Vhr
                ))
            )
            {
                return Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter |
                Speakers.LowFrequency | Speakers.BackLeft | Speakers.BackRight |
                Speakers.TopFrontLeft | Speakers.TopFrontRight;
            }
            else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_EAC3_7_1_F) // < L C R Ls Rs LFE Cs Ts
            {
                needsTranslation = true;
                return Speakers.FrontLeft | Speakers.FrontCenter | Speakers.FrontRight |
                Speakers.BackLeft | Speakers.BackRight | Speakers.LowFrequency |
                Speakers.BackCenter | Speakers.TopBackCenter;
            }
            else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_EAC3_7_1_G) // < L C R Ls Rs LFE Cs Vhc
            {
                needsTranslation = true;
                return Speakers.FrontLeft | Speakers.FrontCenter | Speakers.FrontRight |
                Speakers.BackLeft | Speakers.BackRight | Speakers.LowFrequency |
                Speakers.BackCenter | Speakers.TopFrontCenter;
            }
            else if (tag == AudioChannelLayoutTag.kAudioChannelLayoutTag_EAC3_7_1_H) // < L C R Ls Rs LFE Ts Vhc
            {
                needsTranslation = true;
                return Speakers.FrontLeft | Speakers.FrontCenter | Speakers.FrontRight |
                Speakers.BackLeft | Speakers.BackRight | Speakers.LowFrequency |
                Speakers.TopBackCenter | Speakers.TopFrontCenter;
            }
            #endregion

            else
            {
                throw new ArgumentException("Could not decompose the specified channel layout tag: " + tag);
            }
        }
    }
}