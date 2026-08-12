
using NAudio.Wave;
using NAudio.Utils;

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

        Assert.AreEqual((Speakers)l.mChannelBitmap, valueToTest);
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

        Assert.AreEqual(
            MacUtils.ConstructSpeakersValue(layout, out var needsTranslation, out var needsExtensible),
            Speakers.Stereo,
            "Layouts do not match!"
        );

        Assert.IsFalse(needsTranslation, "A stereo layout like the one specified should not need translation!");

        Assert.IsFalse(needsExtensible, "A stereo layout like the one specified should not need a WaveFormatExtensible!");

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

        Assert.AreEqual(
            MacUtils.ConstructSpeakersValue(layout, out var needsTranslation, out var needsExtensible),
            Speakers.Stereo,
            "Layouts do not match!"
        );

        Assert.IsFalse(needsTranslation, "A stereo layout like the one specified should not need translation!");

        Assert.IsFalse(needsExtensible, "A stereo layout like the one specified should not need a WaveFormatExtensible!");

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

        Assert.AreEqual(
            MacUtils.ConstructSpeakersValue(layout, out var needsTranslation, out var needsExtensible),
            Speakers.Mono,
            "Layouts do not match!"
        );

        Assert.IsFalse(needsTranslation, "A mono layout like the one specified should not need translation!");

        Assert.IsFalse(needsExtensible, "A mono layout like the one specified should not need a WaveFormatExtensible!");

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

        Assert.AreEqual(
            MacUtils.ConstructSpeakersValue(layout, out var needsTranslation, out var needsExtensible),
            Speakers.Quad,
            "Layouts do not match!"
        );

        Assert.IsFalse(needsTranslation, "A quad layout like the one specified should not need translation!");

        // This requires an extensible format.
        Assert.IsTrue(needsExtensible, "A quad layout like the one specified should require a WaveFormatExtensible!");

        Assert.DoesNotThrow(handle.Dispose);
    }

    // A test that verifies that a given tag is translated to the given Speakers layout
    // and also testing whether correctly flags that tag as needing channel translation or not.
    // If you add any tag to the ConstructSpeakersValue, a corresponding test case must be added here.
    // Note: This test is just used to ensure that any existing tag translation is not broken in future
    // contributions.
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_Mono, Speakers.Mono, false)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_Stereo, Speakers.Stereo, false)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_Binaural, Speakers.Stereo, false)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_StereoHeadphones, Speakers.Stereo, false)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_AC3_1_0_1, Speakers.FrontCenter | Speakers.LowFrequency, false)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_3_0_A, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter, false)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_3_0_B, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_AC3_3_0, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_ITU_2_1, Speakers.FrontLeft | Speakers.FrontRight | Speakers.BackCenter, false)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_DVD_4, Speakers.FrontLeft | Speakers.FrontRight | Speakers.LowFrequency, false)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_Quadraphonic, Speakers.Quad, false)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_ITU_2_2, Speakers.Quad, false)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_WAVE_4_0_B, Speakers.FrontLeft | Speakers.FrontRight | Speakers.BackLeft | Speakers.BackRight, false)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_4_0_A, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.BackCenter, false)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_4_0_B, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.BackCenter, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_Logic_4_0_C, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.BackCenter, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_AC3_3_1, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.BackCenter, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_DVD_5, Speakers.FrontLeft | Speakers.FrontRight | Speakers.LowFrequency | Speakers.BackCenter, false)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_AC3_2_1_1, Speakers.FrontLeft | Speakers.FrontRight | Speakers.LowFrequency | Speakers.BackCenter, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_DVD_10, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.LowFrequency, false)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_AC3_3_0_1, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.LowFrequency, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_3_1, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.LowFrequency, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_0_A, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.BackLeft | Speakers.BackRight, false)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_0_B, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.BackLeft | Speakers.BackRight, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_0_C, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.BackLeft | Speakers.BackRight, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_0_D, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.BackLeft | Speakers.BackRight, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_Pentagonal, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.BackLeft | Speakers.BackRight, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_WAVE_5_0_B, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.SideLeft | Speakers.SideRight, false)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_Ogg_5_0, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.SideLeft | Speakers.SideRight, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_0_E, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.SideLeft | Speakers.SideRight, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_DVD_6, Speakers.FrontLeft | Speakers.FrontRight | Speakers.LowFrequency | Speakers.BackLeft | Speakers.BackRight, false)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_DVD_18, Speakers.FrontLeft | Speakers.FrontRight | Speakers.LowFrequency | Speakers.BackLeft | Speakers.BackRight, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_DVD_11, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.LowFrequency | Speakers.BackCenter, false)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_AC3_3_1_1, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.LowFrequency | Speakers.BackCenter, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_4_1, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.LowFrequency | Speakers.BackCenter, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_6_0_A, Speakers.FrontLeftOfCenter | Speakers.FrontRightOfCenter | Speakers.FrontLeft | Speakers.FrontRight | Speakers.BackLeft | Speakers.BackRight, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_6_0_B, Speakers.FrontCenter | Speakers.FrontLeft | Speakers.FrontRight | Speakers.SideLeft | Speakers.SideRight | Speakers.TopBackCenter, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_6_0_C, Speakers.FrontCenter | Speakers.BackCenter | Speakers.FrontLeft | Speakers.FrontRight | Speakers.SideLeft | Speakers.SideRight, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_Hexagonal, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.BackLeft | Speakers.BackRight | Speakers.BackCenter, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_AudioUnit_6_0, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.BackLeft | Speakers.BackRight | Speakers.BackCenter, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_AAC_6_0, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.BackLeft | Speakers.BackRight | Speakers.BackCenter, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_EAC_6_0_A, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.BackLeft | Speakers.BackRight | Speakers.BackCenter, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_Logic_6_0_B, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.BackLeft | Speakers.BackRight | Speakers.BackCenter, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_WAVE_5_1_B, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.LowFrequency | Speakers.SideLeft | Speakers.SideRight, false)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_Ogg_5_1, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.LowFrequency | Speakers.SideLeft | Speakers.SideRight, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_1_E, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.LowFrequency | Speakers.SideLeft | Speakers.SideRight, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_1_A, Speakers.FrontLeft | Speakers.FrontRight | Speakers.LowFrequency | Speakers.FrontCenter | Speakers.BackLeft | Speakers.BackRight, false)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_1_B, Speakers.FrontLeft | Speakers.FrontRight | Speakers.LowFrequency | Speakers.FrontCenter | Speakers.BackLeft | Speakers.BackRight, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_1_C, Speakers.FrontLeft | Speakers.FrontRight | Speakers.LowFrequency | Speakers.FrontCenter | Speakers.BackLeft | Speakers.BackRight, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_5_1_D, Speakers.FrontLeft | Speakers.FrontRight | Speakers.LowFrequency | Speakers.FrontCenter | Speakers.BackLeft | Speakers.BackRight, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_WAVE_6_1, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.LowFrequency | Speakers.BackCenter | Speakers.SideLeft | Speakers.SideRight, false)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_Ogg_6_1, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.LowFrequency | Speakers.BackCenter | Speakers.SideLeft | Speakers.SideRight, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_6_1_B, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.LowFrequency | Speakers.BackCenter | Speakers.SideLeft | Speakers.SideRight, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_AudioUnit_7_0, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.SideLeft | Speakers.SideRight | Speakers.BackLeft | Speakers.BackRight, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_AAC_7_0, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.SideLeft | Speakers.SideRight | Speakers.BackLeft | Speakers.BackRight, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_EAC_7_0_A, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.SideLeft | Speakers.SideRight | Speakers.BackLeft | Speakers.BackRight, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_AudioUnit_7_0_Front, Speakers.FrontLeft | Speakers.FrontRight | Speakers.BackLeft | Speakers.BackRight | Speakers.FrontCenter | Speakers.FrontLeftOfCenter | Speakers.FrontRightOfCenter, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_7_0, Speakers.FrontLeft | Speakers.FrontRight | Speakers.BackLeft | Speakers.BackRight | Speakers.FrontCenter | Speakers.FrontLeftOfCenter | Speakers.FrontRightOfCenter, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_6_1_A, Speakers.FrontLeftOfCenter | Speakers.FrontRightOfCenter | Speakers.FrontLeft | Speakers.FrontRight | Speakers.BackLeft | Speakers.BackRight | Speakers.LowFrequency, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_6_1_B, Speakers.FrontCenter | Speakers.FrontLeft | Speakers.FrontRight | Speakers.SideLeft | Speakers.SideRight | Speakers.TopBackCenter | Speakers.LowFrequency, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_6_1_C, Speakers.FrontCenter | Speakers.BackCenter | Speakers.FrontLeft | Speakers.FrontRight | Speakers.SideLeft | Speakers.SideRight | Speakers.LowFrequency, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_6_1_A, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.LowFrequency | Speakers.BackLeft | Speakers.BackRight | Speakers.BackCenter, false)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_EAC3_6_1_A, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.LowFrequency | Speakers.BackLeft | Speakers.BackRight | Speakers.BackCenter, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_AAC_6_1, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.LowFrequency | Speakers.BackLeft | Speakers.BackRight | Speakers.BackCenter, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_6_1_D, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.LowFrequency | Speakers.BackLeft | Speakers.BackRight | Speakers.BackCenter, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_Logic_6_1_B, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.LowFrequency | Speakers.BackLeft | Speakers.BackRight | Speakers.BackCenter, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_Logic_6_1_D, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.LowFrequency | Speakers.BackLeft | Speakers.BackRight | Speakers.BackCenter, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_EAC3_6_1_C, Speakers.FrontLeft | Speakers.FrontCenter | Speakers.FrontRight | Speakers.SideLeft | Speakers.SideRight | Speakers.LowFrequency | Speakers.TopFrontCenter, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_EAC3_6_1_B, Speakers.FrontLeft | Speakers.FrontCenter | Speakers.FrontRight | Speakers.SideLeft | Speakers.SideRight | Speakers.LowFrequency | Speakers.TopBackCenter, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_8_0_A, Speakers.FrontLeftOfCenter | Speakers.FrontRightOfCenter | Speakers.FrontLeft | Speakers.FrontRight | Speakers.BackLeft | Speakers.BackRight | Speakers.SideLeft | Speakers.SideRight, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_8_0_B, Speakers.FrontLeftOfCenter | Speakers.FrontCenter | Speakers.FrontRightOfCenter | Speakers.FrontLeft | Speakers.FrontRight | Speakers.BackLeft | Speakers.BackCenter | Speakers.BackRight, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_7_1_A, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.LowFrequency | Speakers.BackLeft | Speakers.BackRight | Speakers.FrontLeftOfCenter | Speakers.FrontRightOfCenter, false)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_7_1_B, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.LowFrequency | Speakers.BackLeft | Speakers.BackRight | Speakers.FrontLeftOfCenter | Speakers.FrontRightOfCenter, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_Emagic_Default_7_1, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.LowFrequency | Speakers.BackLeft | Speakers.BackRight | Speakers.FrontLeftOfCenter | Speakers.FrontRightOfCenter, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_EAC3_7_1_B, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.LowFrequency | Speakers.BackLeft | Speakers.BackRight | Speakers.FrontLeftOfCenter | Speakers.FrontRightOfCenter, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_DTS_7_1, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.LowFrequency | Speakers.BackLeft | Speakers.BackRight | Speakers.FrontLeftOfCenter | Speakers.FrontRightOfCenter, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_WAVE_7_1, Speakers.Surround71, false)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_7_1_C, Speakers.Surround71, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_EAC3_7_1_A, Speakers.Surround71, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_Logic_7_1_B, Speakers.Surround71, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_MPEG_7_1_D, Speakers.Surround71, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_EAC3_7_1_C, Speakers.FrontLeft | Speakers.FrontCenter | Speakers.FrontRight | Speakers.BackLeft | Speakers.BackRight | Speakers.LowFrequency | Speakers.SideLeft | Speakers.SideRight, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_CICP_14, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.LowFrequency | Speakers.BackLeft | Speakers.BackRight | Speakers.TopFrontLeft | Speakers.TopFrontRight, false)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_EAC3_7_1_E, Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.LowFrequency | Speakers.BackLeft | Speakers.BackRight | Speakers.TopFrontLeft | Speakers.TopFrontRight, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_EAC3_7_1_F, Speakers.FrontLeft | Speakers.FrontCenter | Speakers.FrontRight | Speakers.BackLeft | Speakers.BackRight | Speakers.LowFrequency | Speakers.BackCenter | Speakers.TopBackCenter, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_EAC3_7_1_G, Speakers.FrontLeft | Speakers.FrontCenter | Speakers.FrontRight | Speakers.BackLeft | Speakers.BackRight | Speakers.LowFrequency | Speakers.BackCenter | Speakers.TopFrontCenter, true)]
    [TestCase(AudioChannelLayoutTag.kAudioChannelLayoutTag_EAC3_7_1_H, Speakers.FrontLeft | Speakers.FrontCenter | Speakers.FrontRight | Speakers.BackLeft | Speakers.BackRight | Speakers.LowFrequency | Speakers.TopBackCenter | Speakers.TopFrontCenter, true)]
    public unsafe void VerifyThatTagValidlyDecodes(
        uint tag, // We can't declare this parameter as AudioChannelLayoutTag because 'AudioChannelLayoutTag' is 'internal'.
        Speakers expectedValue,
        bool reallyNeedsTranslation
    )
    {
        AudioChannelLayout layout = new() { mChannelLayoutTag = (AudioChannelLayoutTag)tag };

        var spk = MacUtils.ConstructSpeakersValue(new(&layout), out var needsTranslation, out _);

        Assert.That(
            spk,
            Is.EqualTo(expectedValue),
            "Both speaker values must match!"
        );

        Assert.That(
            needsTranslation,
            Is.EqualTo(reallyNeedsTranslation),
            $"The specified translation requirement for tag {tag} does not match!"
        );
    }
}