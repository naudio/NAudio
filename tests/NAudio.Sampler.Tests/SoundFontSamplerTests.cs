using System;
using NAudio.Midi;
using NUnit.Framework;

namespace NAudio.Sampler.Tests;

[TestFixture]
[Category("UnitTest")]
public class SoundFontSamplerTests
{
    private const int SampleRate = 44100;

    // A short, fully-sustaining looped instrument: a constant-amplitude
    // "DC" sample looped forever with an instant attack and full sustain,
    // so the rendered output is a predictable non-zero constant while held.
    private static NAudio.SoundFont.SoundFont BuildConstantInstrument(
        byte rootKey = 60, short value = 16000, bool loop = true)
    {
        // 4 sample points all equal to `value` (16-bit little-endian)
        var data = new byte[8];
        for (int i = 0; i < 4; i++)
        {
            data[i * 2] = (byte)(value & 0xFF);
            data[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
        }

        // igen: sampleModes(54)=1 (loop), then overridingRootKey(58),
        // then SampleID(53). Generators apply in order; index/range last.
        ushort loopMode = (ushort)(loop ? 1 : 0);
        var igen = SoundFontTestBuilder.Chunk("igen", SoundFontTestBuilder.Concat(
            SoundFontTestBuilder.Gen(54, loopMode),
            SoundFontTestBuilder.Gen(58, rootKey),
            SoundFontTestBuilder.Gen(53, 0)));

        return SoundFontTestBuilder.BuildSingleRegion(data, igen,
            sampleStart: 0, sampleEnd: 4, loopStart: 0, loopEnd: 4,
            sampleRate: SampleRate, originalPitch: rootKey);
    }

    private static SoundFontSampler NewSampler(NAudio.SoundFont.SoundFont sf, int maxVoices = 16)
        => new(sf, SampleRate, maxVoices);

    /// <summary>Renders a block of stereo frames and returns the interleaved buffer.</summary>
    private static float[] Render(SoundFontSampler sampler, int frames)
    {
        var buffer = new float[frames * 2];
        int read = sampler.Read(buffer);
        Assert.That(read, Is.EqualTo(buffer.Length));
        return buffer;
    }

    private static float Peak(float[] interleaved)
    {
        float peak = 0;
        foreach (var s in interleaved) peak = Math.Max(peak, Math.Abs(s));
        return peak;
    }

    [Test]
    public void SilentBeforeAnyNote()
    {
        var sampler = NewSampler(BuildConstantInstrument());
        var output = Render(sampler, 256);
        Assert.That(Peak(output), Is.EqualTo(0f));
        Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(0));
    }

    [Test]
    public void RomBasedFontWithEmptySamplesPlaysSilentlyWithoutThrowing()
    {
        // A ROM-based SoundFont has an empty smpl chunk but sample headers
        // whose ranges point into the (absent) ROM. Loading must succeed, and
        // playing a note must produce silence rather than throwing on an
        // out-of-range sample slice.
        var igen = SoundFontTestBuilder.Chunk("igen", SoundFontTestBuilder.Concat(
            SoundFontTestBuilder.Gen(58, 60), // overridingRootKey
            SoundFontTestBuilder.Gen(53, 0))); // SampleID
        var sf = SoundFontTestBuilder.BuildSingleRegion(new byte[0], igen,
            sampleStart: 0, sampleEnd: 1000, loopStart: 0, loopEnd: 1000,
            sampleRate: SampleRate, originalPitch: 60);
        var sampler = NewSampler(sf);

        Assert.DoesNotThrow(() => sampler.NoteOn(0, 60, 127));
        Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(0));
        Assert.That(Peak(Render(sampler, 256)), Is.EqualTo(0f));
    }

    [Test]
    public void NoteOnProducesAudio()
    {
        var sampler = NewSampler(BuildConstantInstrument());
        sampler.NoteOn(0, 60, 127);
        Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(1));
        var output = Render(sampler, 512);
        Assert.That(Peak(output), Is.GreaterThan(0.1f));
    }

    [Test]
    public void NoteAtRootKeyPlaysSampleValueAtFullVelocity()
    {
        // root key, full velocity, instant attack, looped constant sample.
        // After the first sample the envelope is at full; output should be
        // close to the sample's normalised amplitude on both channels.
        var sampler = NewSampler(BuildConstantInstrument(value: 16384)); // 0.5 fs
        sampler.NoteOn(0, 60, 127);
        var output = Render(sampler, 512);

        // centre pan => equal-power -0.707 on each channel of a 0.5 signal
        float expected = 0.5f * 0.70710677f;
        // sample the steady-state tail (envelope settled)
        float left = output[1000];
        float right = output[1001];
        Assert.That(left, Is.EqualTo(expected).Within(0.02f));
        Assert.That(right, Is.EqualTo(expected).Within(0.02f));
    }

    [Test]
    public void TwentyFourBitSamplesRenderWithTheLowByteApplied()
    {
        // A constant sample whose 16-bit word is tiny (1) and whose sm24 low
        // byte is 0xFF: the 24-bit value ((1 << 8) | 255 = 511) is nearly
        // double the 16-bit-only interpretation (256), so the rendered level
        // audibly reflects the low byte end-to-end through the sampler.
        var igen = SoundFontTestBuilder.Chunk("igen", SoundFontTestBuilder.Concat(
            SoundFontTestBuilder.Gen(54, 1),   // loop
            SoundFontTestBuilder.Gen(58, 60),  // root key
            SoundFontTestBuilder.Gen(53, 0))); // sampleID
        var data = new byte[8];
        for (int i = 0; i < 4; i++) { data[i * 2] = 0x01; data[i * 2 + 1] = 0x00; } // 16-bit word = 1
        var sm24 = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

        var with24 = NewSampler(SoundFontTestBuilder.BuildSingleRegion(
            data, igen, 0, 4, 0, 4, SampleRate, 60, sm24: sm24));
        var only16 = NewSampler(SoundFontTestBuilder.BuildSingleRegion(
            data, igen, 0, 4, 0, 4, SampleRate, 60));

        with24.NoteOn(0, 60, 127);
        only16.NoteOn(0, 60, 127);
        float peak24 = Peak(Render(with24, 512));
        float peak16 = Peak(Render(only16, 512));

        Assert.That(peak16, Is.GreaterThan(0f));
        Assert.That(peak24 / peak16, Is.EqualTo(511f / 256f).Within(0.02),
            "the sm24 low byte should raise the rendered level by its exact contribution");
    }

    [Test]
    public void PercussionChannelResolvesAgainstBank128EvenAfterBankSelect()
    {
        // GM channel 10 (index 9) is percussion: a kit lives in bank 128, and
        // note numbers pick the drum. A stray bank-select on the drum track
        // (sequencers commonly send CC0=0) must not drop it to a melodic bank.
        var igen = SoundFontTestBuilder.Chunk("igen", SoundFontTestBuilder.Concat(
            SoundFontTestBuilder.Gen(54, 1),  // sampleModes = loop (so it sustains audibly)
            SoundFontTestBuilder.Gen(58, 60), // overridingRootKey
            SoundFontTestBuilder.Gen(53, 0))); // SampleID
        var data = new byte[8];
        for (int i = 0; i < 4; i++) { data[i * 2] = 0x00; data[i * 2 + 1] = 0x40; } // ~0.5
        var sf = SoundFontTestBuilder.BuildSingleRegion(data, igen,
            sampleStart: 0, sampleEnd: 4, loopStart: 0, loopEnd: 4,
            sampleRate: SampleRate, originalPitch: 60, bank: 128);
        var sampler = NewSampler(sf);

        sampler.ProcessMidiEvent(new ControlChangeEvent(0, 10, MidiController.BankSelect, 0));
        sampler.NoteOn(9, 60, 127);

        Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(1));
        Assert.That(Peak(Render(sampler, 256)), Is.GreaterThan(0.1f));
    }

    [Test]
    public void PercussionChannelDoesNotPlayMelodicPresets()
    {
        // a SoundFont with only a melodic (bank 0) preset: the drum channel must
        // stay silent rather than play the melodic preset chromatically
        var sampler = NewSampler(BuildConstantInstrument()); // bank 0 preset
        sampler.NoteOn(9, 60, 127);
        Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(0));

        // ...while the same note on a melodic channel does play
        sampler.NoteOn(0, 60, 127);
        Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(1));
    }

    // ---- GS/XG drum-bank heuristic (bank MSB 120 / 127 selects percussion) ----

    // a font whose ONLY preset is a bank-128 percussion kit: a melodic
    // channel can play it only through a percussion-bank resolution
    private static NAudio.SoundFont.SoundFont BuildPercussionOnlyFont()
    {
        var igen = SoundFontTestBuilder.Chunk("igen", SoundFontTestBuilder.Concat(
            SoundFontTestBuilder.Gen(54, 1),   // loop (sustains audibly)
            SoundFontTestBuilder.Gen(58, 60),  // overridingRootKey
            SoundFontTestBuilder.Gen(53, 0))); // SampleID
        var data = new byte[8];
        for (int i = 0; i < 4; i++) { data[i * 2] = 0x00; data[i * 2 + 1] = 0x40; } // ~0.5
        return SoundFontTestBuilder.BuildSingleRegion(data, igen,
            sampleStart: 0, sampleEnd: 4, loopStart: 0, loopEnd: 4,
            sampleRate: SampleRate, originalPitch: 60, bank: 128);
    }

    [TestCase(120, TestName = "GsRhythmBankMsb120PlaysThePercussionKitOnAMelodicChannel")]
    [TestCase(127, TestName = "XgDrumBankMsb127PlaysThePercussionKitOnAMelodicChannel")]
    public void GsXgDrumBankMsbPlaysThePercussionKitOnAMelodicChannel(int bankMsb)
    {
        // GS selects rhythm parts with CC0 (bank MSB) = 120 on any channel;
        // XG uses MSB 127 — both must resolve against the percussion bank
        var sampler = NewSampler(BuildPercussionOnlyFont());
        sampler.ProcessMidiEvent(new ControlChangeEvent(0, 1, MidiController.BankSelect, bankMsb));
        sampler.NoteOn(0, 60, 127); // channel 0: NOT the GM percussion channel

        Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(1), "the kit must play via the drum-bank heuristic");
        Assert.That(Peak(Render(sampler, 256)), Is.GreaterThan(0.1f));
    }

    [Test]
    public void GsXgDrumBankHeuristicCanBeDisabled()
    {
        // with the heuristic off, CC0=120 follows the normal melodic bank
        // rules (bank 120 missing, no bank-0/melodic fallback exists, and a
        // melodic lookup never falls back to the percussion bank) -> silence
        var sampler = NewSampler(BuildPercussionOnlyFont());
        sampler.TreatGsXgDrumBanksAsPercussion = false;
        sampler.ProcessMidiEvent(new ControlChangeEvent(0, 1, MidiController.BankSelect, 120));
        sampler.NoteOn(0, 60, 127);
        Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(0),
            "with the heuristic disabled the melodic bank rules apply");
    }

    [Test]
    public void NormalVariationBankIsNotTreatedAsPercussion()
    {
        // CC0=8 is an ordinary GS variation bank: it must NOT trip the
        // drum-bank heuristic (this font has no melodic preset to fall
        // back to, so a percussion resolution would be the only way to sound)
        var sampler = NewSampler(BuildPercussionOnlyFont());
        sampler.ProcessMidiEvent(new ControlChangeEvent(0, 1, MidiController.BankSelect, 8));
        sampler.NoteOn(0, 60, 127);
        Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(0),
            "a normal variation bank must not select the percussion bank");
    }

    [Test]
    public void HigherNotePlaysFasterThanRoot()
    {
        // A non-looping ramp sample played at root vs an octave up: the
        // octave-up voice should reach the end of the sample (go silent)
        // in roughly half the frames. The sample is long so the fixed (pitch-
        // independent) de-click tail at the end is small relative to the body.
        var rampSf = BuildRampInstrument(rootKey: 60, points: 2000, loop: false);

        int FramesUntilSilent(int note)
        {
            var s = NewSampler(rampSf);
            s.NoteOn(0, note, 127);
            int frames = 0;
            var buf = new float[2];
            while (s.ActiveVoiceCount > 0 && frames < 10000)
            {
                s.Read(buf);
                frames++;
            }
            return frames;
        }

        int atRoot = FramesUntilSilent(60);
        int octaveUp = FramesUntilSilent(72);
        Assert.That(octaveUp, Is.LessThan(atRoot));
        // octave up = 2x rate, so ~half the frames (allow tolerance for envelope tail)
        Assert.That(octaveUp, Is.EqualTo(atRoot / 2).Within(atRoot / 4));
    }

    [Test]
    public void LoopedNoteSustainsIndefinitely()
    {
        var sampler = NewSampler(BuildConstantInstrument(loop: true));
        sampler.NoteOn(0, 60, 127);
        // render far longer than the 4-sample loop
        var output = Render(sampler, 44100); // 1 second
        // last frame should still be sounding
        Assert.That(Math.Abs(output[^1]), Is.GreaterThan(0.05f));
        Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(1));
    }

    [Test]
    public void VeryShortOneShotIsAudible()
    {
        // Regression for the "very short one-shot samples" bug: a
        // 4-frame non-looped one-shot rendered as pure silence because the
        // volume-envelope delay (the SF2 default -12000 tc ≈ 1 ms ≈ 43
        // samples) consumed the whole waveform and the final read sample was
        // dropped. The delay must postpone the waveform, not eat it.
        var sampler = NewSampler(BuildConstantInstrument(loop: false));
        sampler.NoteOn(0, 60, 127);
        var output = Render(sampler, 512);
        Assert.That(Peak(output), Is.GreaterThan(0.01f), "a 4-frame one-shot must produce audio");
    }

    [Test]
    public void EnvelopeDelayPostponesTheWaveformInsteadOfConsumingIt()
    {
        // SF2.04 §8.1.2 gen 33: during the volume-envelope delay the voice is
        // silent but the waveform must not advance, so an N-frame one-shot at
        // unity pitch still yields ~N non-silent frames (just shifted by the
        // delay). Consuming the delay from the waveform loses the first ~43
        // samples — the attack transient — of every note.
        const int points = 500;
        var sampler = NewSampler(BuildDescendingRampInstrument(points));
        sampler.NoteOn(0, 60, 127);
        var output = Render(sampler, 2048);

        int nonSilent = 0;
        for (int f = 0; f < 2048; f++)
            if (Math.Abs(output[f * 2]) > 0.002f) nonSilent++;
        Assert.That(nonSilent, Is.EqualTo(points).Within(10),
            "the rendered body of a one-shot must be the whole waveform");
    }

    [Test]
    public void OneShotEmitsItsFinalSample()
    {
        // The Read that discovers a one-shot's end returns the last real
        // sample; it must be emitted, not discarded (the voice was dropping
        // the final output sample of every one-shot). This waveform's only
        // non-zero value IS its final sample, so dropping it renders silence.
        var data = new byte[6]; // 3 points: 0, 0, ~0.49
        data[4] = 16000 & 0xFF;
        data[5] = 16000 >> 8;
        var igen = SoundFontTestBuilder.Chunk("igen", SoundFontTestBuilder.Concat(
            SoundFontTestBuilder.Gen(58, 60), // overridingRootKey (no loop)
            SoundFontTestBuilder.Gen(53, 0))); // SampleID
        var sf = SoundFontTestBuilder.BuildSingleRegion(data, igen,
            sampleStart: 0, sampleEnd: 3, loopStart: 0, loopEnd: 3,
            sampleRate: SampleRate, originalPitch: 60);
        var sampler = NewSampler(sf);
        sampler.NoteOn(0, 60, 127);
        Assert.That(Peak(Render(sampler, 512)), Is.GreaterThan(0.01f),
            "the final sample of a one-shot must be emitted");
    }

    [Test]
    public void NonLoopedNoteStopsAtSampleEnd()
    {
        var sampler = NewSampler(BuildConstantInstrument(loop: false));
        sampler.NoteOn(0, 60, 127);
        // 4 samples at root pitch -> the voice ends quickly, after the brief
        // (~5 ms) de-click ramp that smooths the cut at the sample's end
        Render(sampler, 512);
        Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(0));
    }

    [Test]
    public void NoteOffReleasesVoiceToSilence()
    {
        // instant attack/short release on a looped sample
        var sampler = NewSampler(BuildReleasingInstrument(releaseTimecents: -7200)); // ~6 ms
        sampler.NoteOn(0, 60, 127);
        Render(sampler, 256);
        Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(1));

        sampler.NoteOff(0, 60);
        // render well past the release time
        Render(sampler, 4096);
        Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(0));
    }

    [Test]
    public void SustainPedalHoldsNoteAfterNoteOff()
    {
        var sampler = NewSampler(BuildReleasingInstrument(releaseTimecents: -7200));
        sampler.NoteOn(0, 60, 127);
        Render(sampler, 128);

        // pedal down, then note off -> still held
        sampler.ProcessMidiEvent(new ControlChangeEvent(0, 1, MidiController.Sustain, 127));
        sampler.NoteOff(0, 60);
        Render(sampler, 2048);
        Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(1), "note should be sustained by the pedal");

        // pedal up -> releases
        sampler.ProcessMidiEvent(new ControlChangeEvent(0, 1, MidiController.Sustain, 0));
        Render(sampler, 4096);
        Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(0));
    }

    [Test]
    public void PolyphonyPlaysMultipleNotes()
    {
        var sampler = NewSampler(BuildConstantInstrument());
        sampler.NoteOn(0, 60, 127);
        sampler.NoteOn(0, 64, 127);
        sampler.NoteOn(0, 67, 127);
        Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(3));
    }

    [Test]
    public void VoiceStealingCapsActiveVoices()
    {
        var sampler = NewSampler(BuildConstantInstrument(), maxVoices: 4);
        for (int n = 60; n < 70; n++) sampler.NoteOn(0, n, 127);
        Assert.That(sampler.ActiveVoiceCount, Is.LessThanOrEqualTo(4));
    }

    [Test]
    public void ExclusiveClassChokesPreviousNote()
    {
        // two regions in the same exclusive class (e.g. open/closed hi-hat):
        // starting the second should choke the first
        var sf = BuildExclusiveClassInstrument();
        var sampler = NewSampler(sf);

        sampler.NoteOn(0, 42, 127); // closed hat region key
        Render(sampler, 64);
        int after1 = sampler.ActiveVoiceCount;

        sampler.NoteOn(0, 46, 127); // open hat region key, same exclusive class
        Render(sampler, 512); // let the choke fade complete
        Assert.That(after1, Is.EqualTo(1));
        Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(1), "previous exclusive-class voice should be choked");
    }

    [Test]
    public void ExclusiveClassDoesNotChokeSiblingLayersOfTheSameNoteOn()
    {
        // two zones on the same key range in the same exclusive class — the
        // stereo-linked / layered-drum shape. One note-on starts both layers
        // and must not choke its own siblings; the next note chokes them all.
        var data = new byte[8];
        for (int i = 0; i < 4; i++) { data[i * 2] = 0x00; data[i * 2 + 1] = 0x40; } // ~0.5
        var sf = SoundFontTestBuilder.BuildTwoLayerExclusiveClass(data, SampleRate);
        var sampler = NewSampler(sf);

        sampler.NoteOn(0, 44, 127);
        Render(sampler, 512); // past the choke fade, had a sibling been wrongly choked
        Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(2), "both layers of one note-on should sound");

        sampler.NoteOn(0, 46, 127);
        Render(sampler, 512);
        Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(2), "the previous note's layers should both be choked");
    }

    [Test]
    public void BankSelectMsbSelectsTheSf2Bank()
    {
        // SF2 wBank is the bank-select MSB: CC0=8 must reach the bank-8
        // preset (attenuated in this font), not be shifted to an out-of-range
        // bank number that falls back to bank 0
        var data = new byte[8];
        for (int i = 0; i < 4; i++) { data[i * 2] = 0x00; data[i * 2 + 1] = 0x40; } // ~0.5
        var sf = SoundFontTestBuilder.BuildTwoBankFont(data, SampleRate, bank8AttenuationCb: 200);

        var bank0 = NewSampler(sf);
        bank0.NoteOn(0, 60, 127);
        float loud = Peak(Render(bank0, 256));

        var bank8 = NewSampler(sf);
        bank8.ProcessMidiEvent(new ControlChangeEvent(0, 1, MidiController.BankSelect, 8));
        bank8.NoteOn(0, 60, 127);
        float quiet = Peak(Render(bank8, 256));

        Assert.That(loud, Is.GreaterThan(0.1f));
        Assert.That(quiet, Is.LessThan(loud * 0.5f), "CC0=8 should select the attenuated bank-8 preset");
    }

    [Test]
    public void MelodicProgramDoesNotFallBackToThePercussionBank()
    {
        // a font whose only preset is a bank-128 drum kit: a melodic channel
        // must stay silent rather than play the kit, while the percussion
        // channel still resolves it
        var igen = SoundFontTestBuilder.Chunk("igen", SoundFontTestBuilder.Concat(
            SoundFontTestBuilder.Gen(54, 1),
            SoundFontTestBuilder.Gen(58, 60),
            SoundFontTestBuilder.Gen(53, 0)));
        var data = new byte[8];
        for (int i = 0; i < 4; i++) { data[i * 2] = 0x00; data[i * 2 + 1] = 0x40; }
        var sf = SoundFontTestBuilder.BuildSingleRegion(data, igen,
            sampleStart: 0, sampleEnd: 4, loopStart: 0, loopEnd: 4,
            sampleRate: SampleRate, originalPitch: 60, bank: 128);
        var sampler = NewSampler(sf);

        sampler.NoteOn(0, 60, 127); // melodic channel: no melodic preset exists
        Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(0), "a melodic program must not fall back to a drum kit");

        sampler.NoteOn(9, 60, 127); // percussion channel resolves the kit
        Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(1));
    }

    [Test]
    public void NoteOnVelocityZeroIsNoteOff()
    {
        var sampler = NewSampler(BuildReleasingInstrument(releaseTimecents: -7200));
        sampler.ProcessMidiEvent(new NoteOnEvent(0, 1, 60, 127, 0));
        Render(sampler, 128);
        Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(1));

        sampler.ProcessMidiEvent(new NoteOnEvent(0, 1, 60, 0, 0)); // velocity 0
        Render(sampler, 4096);
        Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(0));
    }

    [Test]
    public void PanGeneratorRoutesToCorrectChannel()
    {
        // hard-left pan => left channel loud, right channel near silent
        var sf = BuildPannedInstrument(pan: -500);
        var sampler = NewSampler(sf);
        sampler.NoteOn(0, 60, 127);
        var output = Render(sampler, 512);

        float leftPeak = 0, rightPeak = 0;
        for (int i = 0; i < output.Length; i += 2)
        {
            leftPeak = Math.Max(leftPeak, Math.Abs(output[i]));
            rightPeak = Math.Max(rightPeak, Math.Abs(output[i + 1]));
        }
        Assert.That(leftPeak, Is.GreaterThan(0.1f));
        Assert.That(rightPeak, Is.LessThan(leftPeak * 0.1f));
    }

    [Test]
    public void NoteOutsideKeyRangeProducesNoVoice()
    {
        // region limited to keys 60-60; play 72 -> nothing
        var sf = BuildKeyRangeInstrument(lowKey: 60, highKey: 60);
        var sampler = NewSampler(sf);
        sampler.NoteOn(0, 72, 127);
        Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(0));
        sampler.NoteOn(0, 60, 127);
        Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(1));
    }

    [Test]
    public void OutputFormatIsStereoFloat()
    {
        var sampler = NewSampler(BuildConstantInstrument());
        Assert.That(sampler.WaveFormat.Channels, Is.EqualTo(2));
        Assert.That(sampler.WaveFormat.SampleRate, Is.EqualTo(SampleRate));
        Assert.That(sampler.WaveFormat.Encoding, Is.EqualTo(NAudio.Wave.WaveFormatEncoding.IeeeFloat));
    }

    #region instrument builders

    private static NAudio.SoundFont.SoundFont BuildRampInstrument(byte rootKey, int points, bool loop)
    {
        var data = new byte[points * 2];
        for (int i = 0; i < points; i++)
        {
            short v = (short)(i * 100);
            data[i * 2] = (byte)(v & 0xFF);
            data[i * 2 + 1] = (byte)((v >> 8) & 0xFF);
        }
        var igen = SoundFontTestBuilder.Chunk("igen", SoundFontTestBuilder.Concat(
            SoundFontTestBuilder.Gen(54, (ushort)(loop ? 1 : 0)),
            SoundFontTestBuilder.Gen(58, rootKey),
            SoundFontTestBuilder.Gen(53, 0)));
        return SoundFontTestBuilder.BuildSingleRegion(data, igen,
            0, (uint)points, 0, (uint)points, SampleRate, rootKey);
    }

    // a non-looped one-shot whose value falls linearly to zero at the end, so
    // the rendered body length is measurable and no de-click tail is added
    private static NAudio.SoundFont.SoundFont BuildDescendingRampInstrument(int points)
    {
        var data = new byte[points * 2];
        for (int i = 0; i < points; i++)
        {
            short v = (short)((points - 1 - i) * 60);
            data[i * 2] = (byte)(v & 0xFF);
            data[i * 2 + 1] = (byte)((v >> 8) & 0xFF);
        }
        var igen = SoundFontTestBuilder.Chunk("igen", SoundFontTestBuilder.Concat(
            SoundFontTestBuilder.Gen(58, 60),
            SoundFontTestBuilder.Gen(53, 0)));
        return SoundFontTestBuilder.BuildSingleRegion(data, igen,
            0, (uint)points, 0, (uint)points, SampleRate, 60);
    }

    private static NAudio.SoundFont.SoundFont BuildReleasingInstrument(short releaseTimecents)
    {
        var data = new byte[8];
        for (int i = 0; i < 4; i++) { data[i * 2] = 0x00; data[i * 2 + 1] = 0x40; } // 0.5 fs
        var igen = SoundFontTestBuilder.Chunk("igen", SoundFontTestBuilder.Concat(
            SoundFontTestBuilder.Gen(54, 1),                          // loop
            SoundFontTestBuilder.Gen(38, unchecked((ushort)releaseTimecents)), // release vol env
            SoundFontTestBuilder.Gen(58, 60),
            SoundFontTestBuilder.Gen(53, 0)));
        return SoundFontTestBuilder.BuildSingleRegion(data, igen, 0, 4, 0, 4, SampleRate, 60);
    }

    private static NAudio.SoundFont.SoundFont BuildPannedInstrument(short pan)
    {
        var data = new byte[8];
        for (int i = 0; i < 4; i++) { data[i * 2] = 0x00; data[i * 2 + 1] = 0x40; }
        var igen = SoundFontTestBuilder.Chunk("igen", SoundFontTestBuilder.Concat(
            SoundFontTestBuilder.Gen(54, 1),
            SoundFontTestBuilder.Gen(17, unchecked((ushort)pan)), // Pan
            SoundFontTestBuilder.Gen(58, 60),
            SoundFontTestBuilder.Gen(53, 0)));
        return SoundFontTestBuilder.BuildSingleRegion(data, igen, 0, 4, 0, 4, SampleRate, 60);
    }

    private static NAudio.SoundFont.SoundFont BuildKeyRangeInstrument(byte lowKey, byte highKey)
    {
        var data = new byte[8];
        for (int i = 0; i < 4; i++) { data[i * 2] = 0x00; data[i * 2 + 1] = 0x40; }
        ushort keyRange = (ushort)((highKey << 8) | lowKey);
        var igen = SoundFontTestBuilder.Chunk("igen", SoundFontTestBuilder.Concat(
            SoundFontTestBuilder.Gen(43, keyRange), // KeyRange first
            SoundFontTestBuilder.Gen(54, 1),
            SoundFontTestBuilder.Gen(58, 60),
            SoundFontTestBuilder.Gen(53, 0)));
        return SoundFontTestBuilder.BuildSingleRegion(data, igen, 0, 4, 0, 4, SampleRate, 60);
    }

    private static NAudio.SoundFont.SoundFont BuildExclusiveClassInstrument()
    {
        // two instrument zones, both exclusiveClass 1, different key ranges
        var data = new byte[8];
        for (int i = 0; i < 4; i++) { data[i * 2] = 0x00; data[i * 2 + 1] = 0x40; }
        return SoundFontTestBuilder.BuildTwoRegionExclusiveClass(data, SampleRate);
    }

    #endregion
}
