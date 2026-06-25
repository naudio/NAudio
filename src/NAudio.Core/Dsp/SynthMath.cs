using System;

namespace NAudio.Dsp;

/// <summary>
/// Conversions between the musical / MIDI domain and the DSP domain that a
/// sampler or synthesiser needs: note numbers, frequencies, cents, timecents
/// and centibels. These are the units SoundFont generators and SFZ opcodes
/// are expressed in, but the helpers are deliberately format-neutral so any
/// instrument can share them.
/// </summary>
public static class SynthMath
{
    /// <summary>
    /// Frequency in Hz of MIDI note 0 (C-1), i.e. the reference used by the
    /// SoundFont "absolute cents" pitch scale. Equal to 440 * 2^((0-69)/12).
    /// </summary>
    public const double MidiNote0Frequency = 8.1757989156437073336;

    /// <summary>
    /// Converts a MIDI note number to a frequency in Hz (A4 = note 69 = 440 Hz).
    /// The note may be fractional to express detuning.
    /// </summary>
    public static double MidiNoteToFrequency(double midiNote)
    {
        return 440.0 * Math.Pow(2.0, (midiNote - 69.0) / 12.0);
    }

    /// <summary>
    /// Converts a frequency in Hz to a (fractional) MIDI note number.
    /// </summary>
    public static double FrequencyToMidiNote(double frequency)
    {
        return 69.0 + 12.0 * Math.Log2(frequency / 440.0);
    }

    /// <summary>
    /// Converts a pitch offset in cents (1/100th of a semitone) to a linear
    /// frequency ratio. 1200 cents = one octave = a ratio of 2.
    /// </summary>
    public static double CentsToRatio(double cents)
    {
        return Math.Pow(2.0, cents / 1200.0);
    }

    /// <summary>
    /// Converts a frequency ratio to a pitch offset in cents.
    /// </summary>
    public static double RatioToCents(double ratio)
    {
        return 1200.0 * Math.Log2(ratio);
    }

    /// <summary>
    /// Converts a SoundFont "absolute cents" value to a frequency in Hz.
    /// Used for filter cutoff and LFO/envelope-to-pitch destinations.
    /// 6900 absolute cents = 440 Hz.
    /// </summary>
    public static double AbsoluteCentsToHertz(double absoluteCents)
    {
        return MidiNote0Frequency * Math.Pow(2.0, absoluteCents / 1200.0);
    }

    /// <summary>
    /// Converts a frequency in Hz to SoundFont "absolute cents".
    /// </summary>
    public static double HertzToAbsoluteCents(double hertz)
    {
        return 1200.0 * Math.Log2(hertz / MidiNote0Frequency);
    }

    /// <summary>
    /// Converts a duration expressed in timecents to seconds. Timecents are
    /// the units SoundFont envelope stages (delay/attack/hold/decay/release)
    /// use: seconds = 2^(timecents/1200), so 0 timecents = 1 second.
    /// </summary>
    public static double TimecentsToSeconds(double timecents)
    {
        return Math.Pow(2.0, timecents / 1200.0);
    }

    /// <summary>
    /// Converts a duration in seconds to timecents.
    /// </summary>
    public static double SecondsToTimecents(double seconds)
    {
        return 1200.0 * Math.Log2(seconds);
    }

    /// <summary>
    /// Converts a level expressed in centibels (1/10th of a decibel) to a
    /// linear gain multiplier, where positive centibels mean louder.
    /// </summary>
    public static double CentibelsToGain(double centibels)
    {
        return Math.Pow(10.0, centibels / 200.0);
    }

    /// <summary>
    /// Converts a SoundFont attenuation in centibels to a linear gain
    /// multiplier. Attenuation reduces level, so 0 cB = unity gain and
    /// larger values are quieter (gain = 10^(-cB/200)).
    /// </summary>
    public static double AttenuationCentibelsToGain(double centibels)
    {
        return Math.Pow(10.0, -centibels / 200.0);
    }

    /// <summary>
    /// Converts a level in decibels to a linear gain multiplier.
    /// </summary>
    public static double DecibelsToGain(double decibels)
    {
        return Math.Pow(10.0, decibels / 20.0);
    }

    /// <summary>
    /// Converts a linear gain multiplier to decibels.
    /// </summary>
    public static double GainToDecibels(double gain)
    {
        return 20.0 * Math.Log10(gain);
    }

    /// <summary>
    /// Converts a SoundFont filter resonance (initialFilterQ) in centibels to
    /// a linear biquad Q value. The input is clamped to the spec range of
    /// 0..960 cB (SF2.04 §8.1.2 gen 8) so malformed values cannot reach
    /// float infinity. 0 cB means a flat, non-resonant response, so the
    /// mapping subtracts 3.01 dB before converting —
    /// Q = 10^((cB/10 - 3.01)/20) — giving the Butterworth Q of ~0.707 at
    /// 0 cB (the convention FluidSynth also documents for this generator).
    /// </summary>
    public static double ResonanceCentibelsToQ(double centibels)
    {
        double cb = Math.Clamp(centibels, 0.0, 960.0);
        return Math.Pow(10.0, (cb / 10.0 - 3.01) / 20.0);
    }
}
