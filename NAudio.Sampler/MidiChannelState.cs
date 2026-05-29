using NAudio.Dsp;

namespace NAudio.Sampler
{
    /// <summary>
    /// The live controller state for one of the 16 MIDI channels: selected
    /// bank/program, pitch-bend, and the controllers the voice engine reads
    /// (sustain pedal, channel volume, expression). Modulation routing of these
    /// controllers arrives with the modulator engine; for now the sampler reads
    /// the few that affect note triggering and overall gain.
    /// </summary>
    internal sealed class MidiChannelState
    {
        /// <summary>Selected bank number (CC0/CC32 combined; 128 = percussion).</summary>
        public int Bank { get; set; }

        /// <summary>Selected program (patch) number.</summary>
        public int Program { get; set; }

        /// <summary>Pitch-bend range in semitones (RPN 0). Default 2.</summary>
        public double PitchBendRangeSemitones { get; set; } = 2.0;

        /// <summary>Raw 14-bit pitch-bend value, centred at 8192.</summary>
        public int PitchBend { get; set; } = 8192;

        /// <summary>Whether the sustain pedal (CC64) is down.</summary>
        public bool SustainPedal { get; set; }

        /// <summary>Channel volume (CC7), 0..1.</summary>
        public float Volume { get; set; } = 1f;

        /// <summary>Expression (CC11), 0..1.</summary>
        public float Expression { get; set; } = 1f;

        /// <summary>The frequency ratio for the current pitch-bend and range.</summary>
        public double PitchBendRatio =>
            SynthMath.CentsToRatio((PitchBend - 8192) / 8192.0 * PitchBendRangeSemitones * 100.0);

        /// <summary>The combined channel gain from volume and expression.</summary>
        public float Gain => Volume * Expression;

        /// <summary>Resets controllers to their default values (e.g. on Reset All Controllers).</summary>
        public void ResetControllers()
        {
            PitchBend = 8192;
            SustainPedal = false;
            Expression = 1f;
        }
    }
}
