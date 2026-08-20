
using NAudio.Wave;
using System.Text;
using NAudio.Utils;
using System.Collections.Generic;

using NAudio.MacOS.CoreAudioTypes;

using NUnit.Framework;

namespace NAudio.MacOS.Tests;

// mdcdi1315: Note that this does not test all the possible
// cases - will create tests for the most common ones for now,
// and some time later on we will cover all the layouts.
[TestFixture]
public class ChannelConversionTests
{
    // Copied from the channel translations class.
    private static bool MatchAudioChannelLayoutTagByConstantValueIgnoringSpeakerCount(
        AudioChannelLayoutTag value,
        AudioChannelLayoutTag expectedTag
    ) => ((uint)value >> 16) == ((uint)expectedTag >> 16);

    [TestCase(Speakers.None)]
    [TestCase(Speakers.Mono)]
    [TestCase(Speakers.Stereo)]
    [TestCase(Speakers.Quad)]
    [TestCase(Speakers.Surround51)]
    [TestCase(Speakers.Surround71)]
    [TestCase(Speakers.BackRight | Speakers.BackLeft | Speakers.BackCenter)]
    [TestCase(Speakers.FrontLeft | Speakers.FrontRight | Speakers.LowFrequency)]
    public void VerifyThatSpeakersToAudioChannelLayoutSucceeds(Speakers valueToTest)
    {
        var l = MacUtils.ConstructAudioChannelLayoutFromSpeakers(valueToTest);

        Assert.IsTrue(
            MatchAudioChannelLayoutTagByConstantValueIgnoringSpeakerCount(
                l.mChannelLayoutTag,
                AudioChannelLayoutTag.kAudioChannelLayoutTag_UseChannelBitmap
            ),
            "The channel layout tag was not an audio channel bitmap!"
        );

        Assert.That(
            (Speakers)l.mChannelBitmap,
            Is.EqualTo(valueToTest),
            "Channel bitmaps must match!"
        );
    }

    [Test]
    public unsafe void VerifyThatStereoConstructedThroughChannelDescriptionsSucceeds()
    {
        var handle = ChannelLayoutHandle.Allocate((uint)(sizeof(AudioChannelLayout) + sizeof(AudioChannelDescription)));

        var layout = handle.DangerousGetHandle();

        AudioChannelLayout.SetNumberOfChannelDescriptions(layout, 2U);

        AudioChannelLayout.SetChannelDescription(layout, 0U, new AudioChannelDescription()
        {
            mChannelFlags = AudioChannelFlags.kAudioChannelFlags_AllOff,
            mChannelLabel = AudioChannelLabel.kAudioChannelLabel_Left
        });

        AudioChannelLayout.SetChannelDescription(layout, 1U, new AudioChannelDescription()
        {
            mChannelFlags = AudioChannelFlags.kAudioChannelFlags_AllOff,
            mChannelLabel = AudioChannelLabel.kAudioChannelLabel_Right
        });

        Assert.That(
            MacUtils.ConstructSpeakersValue(layout, out var needsTranslation, out var needsExtensible),
            Is.EqualTo(Speakers.Stereo),
            "Layouts do not match!"
        );

        Assert.That(needsTranslation, Is.False, "A stereo layout like the one specified should not need translation!");

        Assert.That(needsExtensible, Is.False, "A stereo layout like the one specified should not need a WaveFormatExtensible!");

        Assert.DoesNotThrow(handle.Dispose);
    }

    [Test]
    public unsafe void VerifyThatEvenInconsistentButValidStereoConstructedThroughChannelDescriptionsSucceeds()
    {
        var handle = ChannelLayoutHandle.Allocate((uint)(sizeof(AudioChannelLayout) + sizeof(AudioChannelDescription)));

        var layout = handle.DangerousGetHandle();

        AudioChannelLayout.SetNumberOfChannelDescriptions(layout, 2U);

        AudioChannelLayout.SetChannelDescription(layout, 0U, new AudioChannelDescription()
        {
            mChannelFlags = AudioChannelFlags.kAudioChannelFlags_AllOff,
            mChannelLabel = AudioChannelLabel.kAudioChannelLabel_HeadphonesLeft
        });

        AudioChannelLayout.SetChannelDescription(layout, 1U, new AudioChannelDescription()
        {
            mChannelFlags = AudioChannelFlags.kAudioChannelFlags_AllOff,
            mChannelLabel = AudioChannelLabel.kAudioChannelLabel_Right
        });

        Assert.That(
            MacUtils.ConstructSpeakersValue(layout, out var needsTranslation, out var needsExtensible),
            Is.EqualTo(Speakers.Stereo),
            "Layouts do not match!"
        );

        Assert.That(needsTranslation, Is.False, "A stereo layout like the one specified should not need translation!");

        Assert.That(needsExtensible, Is.False, "A stereo layout like the one specified should not need a WaveFormatExtensible!");

        Assert.DoesNotThrow(handle.Dispose);
    }

    [Test]
    public unsafe void VerifyThatMonoConstructedThroughChannelDescriptionsSucceeds()
    {
        var handle = ChannelLayoutHandle.Allocate((uint)sizeof(AudioChannelLayout));

        var layout = handle.DangerousGetHandle();

        AudioChannelLayout.SetNumberOfChannelDescriptions(layout, 1U);

        AudioChannelLayout.SetChannelDescription(layout, 0U, new AudioChannelDescription()
        {
            mChannelFlags = AudioChannelFlags.kAudioChannelFlags_AllOff,
            mChannelLabel = AudioChannelLabel.kAudioChannelLabel_Mono
        });

        Assert.That(
            MacUtils.ConstructSpeakersValue(layout, out var needsTranslation, out var needsExtensible),
            Is.EqualTo(Speakers.Mono),
            "Layouts do not match!"
        );

        Assert.That(needsTranslation, Is.False, "A mono layout like the one specified should not need translation!");

        Assert.That(needsExtensible, Is.False, "A mono layout like the one specified should not need a WaveFormatExtensible!");

        Assert.DoesNotThrow(handle.Dispose);
    }

    [Test]
    public unsafe void VerifyThatQuadConstructedThroughChannelDescriptionsSucceeds()
    {
        var handle = ChannelLayoutHandle.Allocate((uint)(sizeof(AudioChannelLayout) + (3 * sizeof(AudioChannelDescription))));

        var layout = handle.DangerousGetHandle();

        AudioChannelLayout.SetNumberOfChannelDescriptions(layout, 4U);

        AudioChannelLayout.SetChannelDescription(layout, 0U, new AudioChannelDescription()
        {
            mChannelFlags = AudioChannelFlags.kAudioChannelFlags_AllOff,
            mChannelLabel = AudioChannelLabel.kAudioChannelLabel_Left
        });

        AudioChannelLayout.SetChannelDescription(layout, 1U, new AudioChannelDescription()
        {
            mChannelFlags = AudioChannelFlags.kAudioChannelFlags_AllOff,
            mChannelLabel = AudioChannelLabel.kAudioChannelLabel_Right
        });

        AudioChannelLayout.SetChannelDescription(layout, 2U, new AudioChannelDescription()
        {
            mChannelFlags = AudioChannelFlags.kAudioChannelFlags_AllOff,
            mChannelLabel = AudioChannelLabel.kAudioChannelLabel_LeftSurround
        });

        AudioChannelLayout.SetChannelDescription(layout, 3U, new AudioChannelDescription()
        {
            mChannelFlags = AudioChannelFlags.kAudioChannelFlags_AllOff,
            mChannelLabel = AudioChannelLabel.kAudioChannelLabel_RightSurround
        });

        Assert.That(
            MacUtils.ConstructSpeakersValue(layout, out var needsTranslation, out var needsExtensible),
            Is.EqualTo(Speakers.Quad),
            "Layouts do not match!"
        );

        Assert.That(needsTranslation, Is.False, "A quad layout like the one specified should not need translation!");

        // This requires an extensible format.
        Assert.That(needsExtensible, Is.True, "A quad layout like the one specified should require a WaveFormatExtensible!");

        Assert.DoesNotThrow(handle.Dispose);
    }

    [Test]
    public unsafe void VerifyThatEvenWithInconsistentChannelDescriptionsSucceeds()
    {
        var handle = ChannelLayoutHandle.Allocate((uint)(sizeof(AudioChannelLayout) + (3 * sizeof(AudioChannelDescription))));

        var layout = handle.DangerousGetHandle();

        AudioChannelLayout.SetNumberOfChannelDescriptions(layout, 4U);

        AudioChannelLayout.SetChannelDescription(layout, 0U, new AudioChannelDescription()
        {
            mChannelFlags = AudioChannelFlags.kAudioChannelFlags_AllOff,
            mChannelLabel = AudioChannelLabel.kAudioChannelLabel_Unknown
        });

        AudioChannelLayout.SetChannelDescription(layout, 1U, new AudioChannelDescription()
        {
            mChannelFlags = AudioChannelFlags.kAudioChannelFlags_AllOff,
            mChannelLabel = AudioChannelLabel.kAudioChannelLabel_Unused
        });

        AudioChannelLayout.SetChannelDescription(layout, 2U, new AudioChannelDescription()
        {
            mChannelFlags = AudioChannelFlags.kAudioChannelFlags_AllOff,
            mChannelLabel = AudioChannelLabel.kAudioChannelLabel_Unused
        });

        AudioChannelLayout.SetChannelDescription(layout, 3U, new AudioChannelDescription()
        {
            mChannelFlags = AudioChannelFlags.kAudioChannelFlags_AllOff,
            mChannelLabel = AudioChannelLabel.kAudioChannelLabel_Unknown
        });

        Assert.That(
            MacUtils.ConstructSpeakersValue(layout, out var needsTranslation, out var needsExtensible),
            Is.EqualTo(Speakers.None),
            "Layouts do not match!"
        );

        Assert.That(needsTranslation, Is.False, "A layout like the one specified should not need translation!");

        Assert.That(needsExtensible, Is.False, "A layout like the one specified should not require a WaveFormatExtensible!");

        Assert.DoesNotThrow(handle.Dispose);
    }

    private static Speakers ConvertCommentChannelValueToSpeaker(string commentValue, bool bothSurroundPairsDefined)
        => commentValue switch
        {
            "L" => Speakers.FrontLeft,
            "R" => Speakers.FrontRight,
            "C" => Speakers.FrontCenter,
            "LFE" => Speakers.LowFrequency,
            "Ls" => bothSurroundPairsDefined ? Speakers.SideLeft : Speakers.BackLeft,
            "Rs" => bothSurroundPairsDefined ? Speakers.SideRight : Speakers.BackRight,
            "Rls" => Speakers.BackLeft,
            "Rrs" => Speakers.BackRight,
            "Lsd" => Speakers.SideLeft,
            "Rsd" => Speakers.SideRight,
            "Cs" => Speakers.BackCenter,
            "Lc" => Speakers.FrontLeftOfCenter,
            "Rc" => Speakers.FrontRightOfCenter,
            "Lts" => Speakers.TopBackLeft,
            "Ts" => Speakers.TopBackCenter,
            "Rts" => Speakers.TopBackRight,
            "Vhl" => Speakers.TopFrontLeft,
            "Vhc" => Speakers.TopFrontCenter,
            "Vhr" => Speakers.TopFrontRight,
            _ => throw new System.ArgumentException($"Cannot decode value {commentValue} into a valid Speakers value"),
        };

    private static Speakers DeduceSpeakersValueFromTagComment(string tagComment, out bool needsTranslation)
    {
        needsTranslation = false;
        StringBuilder tempBuilder = new(5);
        List<string> channels = new();

        // Break the tag comment into the channel abbreviations
        foreach (var c in tagComment)
        {
            if (c != ' ')
            {
                _ = tempBuilder.Append(c);
            }
            else if (tempBuilder.Length > 0)
            {
                channels.Add(tempBuilder.ToString());
                _ = tempBuilder.Clear();
            }
        }
        // If enumeration ended and no more characters
        // can be provided, make sure any non-drained
        // characters are put together as another channel.
        if (tempBuilder.Length > 0)
        {
            channels.Add(tempBuilder.ToString());
            _ = tempBuilder.Clear();
        }

        // Determine if both surround pairs are defined.
        bool bothSurroundPairsDefined = channels.Contains("Ls") && channels.Contains("Rs") && channels.Contains("Rls") && channels.Contains("Rrs");

        Speakers finalSpeakers = Speakers.None, previous = Speakers.None;

        foreach (var chLabel in channels)
        {
            var current = ConvertCommentChannelValueToSpeaker(chLabel, bothSurroundPairsDefined);
            if (previous > current) { needsTranslation = true; }
            finalSpeakers |= current;
            previous = current;
        }

        return finalSpeakers;
    }

    // A test that verifies that a given tag is translated to the given Speakers layout
    // and also testing whether correctly flags that tag as needing channel translation or not.
    // If you add any tag to the ConstructSpeakersValue, a corresponding test case must be added here.
    // Note: This test is just used to ensure that any existing tag translation is not broken in future
    // contributions.
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_Mono, "C")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_Stereo, "L R")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_Binaural, "L R")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_StereoHeadphones, "L R")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_AC3_1_0_1, "C LFE")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_3_0_A, "L R C")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_3_0_B, "C L R")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_AC3_3_0, "L C R")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_ITU_2_1, "L R Cs")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_DVD_4, "L R LFE")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_Quadraphonic, "L R Ls Rs")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_ITU_2_2, "L R Ls Rs")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_WAVE_4_0_B, "L R Rls Rrs")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_4_0_A, "L R C Cs")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_4_0_B, "C L R Cs")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_Logic_4_0_C, "L R Cs C")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_AC3_3_1, "L C R Cs")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_DVD_5, "L R LFE Cs")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_AC3_2_1_1, "L R Cs LFE")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_DVD_10, "L R C LFE")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_AC3_3_0_1, "L C R LFE")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_3_1, "C L R LFE")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_0_A, "L R C Ls Rs")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_0_B, "L R Ls Rs C")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_0_C, "L C R Ls Rs")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_0_D, "C L R Ls Rs")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_Pentagonal, "L R Ls Rs C")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_WAVE_5_0_B, "L R C Rls Rrs")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_Ogg_5_0, "L C R Rls Rrs")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_0_E, "L R Rls Rrs C")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_DVD_6, "L R LFE Ls Rs")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_DVD_18, "L R Ls Rs LFE")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_DVD_11, "L R C LFE Cs")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_AC3_3_1_1, "L C R Cs LFE")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_4_1, "C L R Cs LFE")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_6_0_A, "Lc Rc L R Ls Rs")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_6_0_B, "C L R Rls Rrs Ts")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_6_0_C, "C Cs L R Rls Rrs")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_Hexagonal, "L R Ls Rs C Cs")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_AudioUnit_6_0, "L R Ls Rs C Cs")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_AAC_6_0, "C L R Ls Rs Cs")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_EAC_6_0_A, "L C R Ls Rs Cs")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_Logic_6_0_B, "L R Ls Rs Cs C")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_WAVE_5_1_B, "L R C LFE Rls Rrs")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_Ogg_5_1, "L C R Rls Rrs LFE")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_1_E, "L R Rls Rrs C LFE")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_1_A, "L R C LFE Ls Rs")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_1_B, "L R Ls Rs C LFE")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_1_C, "L C R Ls Rs LFE")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_1_D, "C L R Ls Rs LFE")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_WAVE_6_1, "L R C LFE Cs Ls Rs")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_Ogg_6_1, "L C R Ls Rs Cs LFE")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_6_1_B, "L R Ls Rs C Cs LFE")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_AudioUnit_7_0, "L R Ls Rs C Rls Rrs")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_AAC_7_0, "C L R Ls Rs Rls Rrs")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_EAC_7_0_A, "L C R Ls Rs Rls Rrs")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_AudioUnit_7_0_Front, "L R Ls Rs C Lc Rc")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_7_0, "Lc C Rc L R Ls Rs")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_6_1_A, "Lc Rc L R Ls Rs LFE")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_6_1_B, "C L R Rls Rrs Ts LFE")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_6_1_C, "C Cs L R Rls Rrs LFE")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_6_1_A, "L R C LFE Ls Rs Cs")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_EAC3_6_1_A, "L C R Ls Rs LFE Cs")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_AAC_6_1, "C L R Ls Rs Cs LFE")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_6_1_D, "C L R Ls Rs LFE Cs")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_Logic_6_1_B, "L R Ls Rs Cs C LFE")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_Logic_6_1_D, "L C R Ls Cs Rs LFE")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_EAC3_6_1_C, "L C R Ls Rs LFE Vhc")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_EAC3_6_1_B, "L C R Ls Rs LFE Ts")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_8_0_A, "Lc Rc L R Ls Rs Rls Rrs")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_8_0_B, "Lc C Rc L R Ls Cs Rs")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_7_1_A, "L R C LFE Ls Rs Lc Rc")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_7_1_B, "C Lc Rc L R Ls Rs LFE")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_Emagic_Default_7_1, "L R Ls Rs C LFE Lc Rc")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_EAC3_7_1_B, "L C R Ls Rs LFE Lc Rc")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_7_1, "Lc C Rc L R Ls Rs LFE")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_WAVE_7_1, "L R C LFE Rls Rrs Ls Rs")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_7_1_C, "L R C LFE Ls Rs Rls Rrs")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_EAC3_7_1_A, "L C R Ls Rs LFE Rls Rrs")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_Logic_7_1_B, "L R Ls Rs Rls Rrs C LFE")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_7_1_D, "L R Rls Rrs Ls Rs C LFE")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_EAC3_7_1_C, "L C R Ls Rs LFE Lsd Rsd")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_CICP_14, "L R C LFE Ls Rs Vhl Vhr")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_EAC3_7_1_E, "L C R Ls Rs LFE Vhl Vhr")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_EAC3_7_1_F, "L C R Ls Rs LFE Cs Ts")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_EAC3_7_1_G, "L C R Ls Rs LFE Cs Vhc")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_EAC3_7_1_H, "L C R Ls Rs LFE Ts Vhc")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_AAC_Octagonal, "C L R Ls Rs Rls Rrs Cs")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_AAC_7_1_C, "C L R Ls Rs LFE Vhl Vhr")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_Ogg_7_1, "L C R Ls Rs Rls Rrs LFE")]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_AAC_7_1_B, "C L R Ls Rs Rls Rrs LFE")]
    public unsafe void VerifyThatTagValidlyDecodes(
        uint tag, // We can't declare this parameter as AudioChannelLayoutTag because 'AudioChannelLayoutTag' is 'internal'.
        string tagComment // Provided by the tag's comment when provided, deduced from what should be decoded into otherwise.
    )
    {
        AudioChannelLayout layout = new() { mChannelLayoutTag = (AudioChannelLayoutTag)tag };

        var spk = MacUtils.ConstructSpeakersValue(new(&layout), out var needsTranslation, out _);

        Assert.That(
            spk,
            Is.EqualTo(DeduceSpeakersValueFromTagComment(tagComment, out var reallyNeedsTranslation)),
            "Both speaker values must match!"
        );

        Assert.That(
            needsTranslation,
            Is.EqualTo(reallyNeedsTranslation),
            $"The specified translation requirement for tag {tag} does not match!"
        );
    }
}