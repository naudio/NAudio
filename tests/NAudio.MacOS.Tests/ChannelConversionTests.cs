
using NAudio.Wave;
using NAudio.Utils;

using NAudio.MacOS.CoreAudioTypes;

using NUnit.Framework;

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
}