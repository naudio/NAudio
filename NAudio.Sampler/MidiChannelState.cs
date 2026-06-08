using NAudio.Dsp;

namespace NAudio.Sampler
{
    /// <summary>
    /// The live controller state for one of the 16 MIDI channels: selected
    /// bank/program, pitch-bend and bend range, channel pressure (aftertouch),
    /// and the full set of MIDI continuous controllers. The modulator engine
    /// reads these as modulator sources (CC1 mod wheel, CC7 volume, CC10 pan,
    /// CC11 expression, CC91/CC93 sends, channel pressure, pitch wheel), while
    /// the voice engine reads the few that affect note triggering directly
    /// (sustain pedal, bank/program for region selection).
    /// </summary>
    internal sealed class MidiChannelState
    {
        // MIDI controller numbers read directly by the sampler
        private const int CcModWheel = 1;
        private const int CcChannelVolume = 7;
        private const int CcPan = 10;
        private const int CcExpression = 11;

        private readonly byte[] controllers = new byte[128];

        public MidiChannelState()
        {
            ResetControllers();
        }

        /// <summary>Selected bank number (CC0/CC32 combined; 128 = percussion).</summary>
        public int Bank { get; set; }

        /// <summary>Selected program (patch) number.</summary>
        public int Program { get; set; }

        /// <summary>Pitch-bend range in semitones (RPN 0). Default 2.</summary>
        public double PitchBendRangeSemitones { get; set; } = 2.0;

        /// <summary>Raw 14-bit pitch-bend value, centred at 8192.</summary>
        public int PitchBend { get; set; } = 8192;

        /// <summary>Channel pressure (aftertouch), 0..127.</summary>
        public int ChannelPressure { get; set; }

        /// <summary>Whether the sustain pedal (CC64) is down.</summary>
        public bool SustainPedal { get; set; }

        /// <summary>The current value (0..127) of a MIDI continuous controller.</summary>
        public int Controller(int cc) => (uint)cc < (uint)controllers.Length ? controllers[cc] : 0;

        /// <summary>Stores the value (0..127) of a MIDI continuous controller.</summary>
        public void SetController(int cc, int value)
        {
            if ((uint)cc < (uint)controllers.Length) controllers[cc] = (byte)value;
        }

        /// <summary>The frequency ratio for the current pitch-bend and range.</summary>
        public double PitchBendRatio =>
            SynthMath.CentsToRatio((PitchBend - 8192) / 8192.0 * PitchBendRangeSemitones * 100.0);

        /// <summary>
        /// Resets controllers to their default values (e.g. on Reset All
        /// Controllers): volume and expression full (so a channel with no volume
        /// sent plays at unity), pan centred, everything else 0, pitch-bend
        /// centred and the sustain pedal up.
        /// </summary>
        public void ResetControllers()
        {
            for (int i = 0; i < controllers.Length; i++) controllers[i] = 0;
            controllers[CcChannelVolume] = 127;
            controllers[CcExpression] = 127;
            controllers[CcPan] = 64;
            controllers[CcModWheel] = 0;
            PitchBend = 8192;
            ChannelPressure = 0;
            SustainPedal = false;
        }
    }
}
