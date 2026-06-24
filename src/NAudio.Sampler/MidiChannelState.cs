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

        // monotonic change stamp + cached pitch-bend ratio (see Version below)
        private int version;
        private int pitchBend = 8192;
        private int channelPressure;
        private double pitchBendRangeSemitones = 2.0;
        private double pitchBendRatio = 1.0;
        private bool pitchBendRatioValid = true; // 8192 at range 2 is ratio 1

        // ---- RPN/NRPN data-entry state ----
        // CC101/100 select a registered parameter (MSB/LSB), CC6/38 write to it.
        // Power-on state is RPN null (127,127) = nothing selected, so data entry
        // arriving before any selection is ignored.
        private const int RpnPitchBendRange = 0; // RPN (0,0) = pitch-bend sensitivity
        private const int RpnNull = (127 << 7) | 127;
        private int rpnMsb = 127;
        private int rpnLsb = 127;
        private bool nrpnSelected; // an NRPN selection routes data entry away from RPNs
        private int bendRangeSemitones = 2; // the data-entry MSB part of RPN 0
        private int bendRangeCents;         // the data-entry LSB part (cents)

        public MidiChannelState()
        {
            ResetControllers();
        }

        /// <summary>
        /// Selected bank number: the bank-select MSB (CC0, 0-127), which is what
        /// SF2 wBank stores. 128 (the percussion bank) is never selected via CC —
        /// the engine forces it for the percussion channel.
        /// </summary>
        public int Bank { get; set; }

        /// <summary>The bank-select LSB (CC32), kept for GS/XG-style variation use.</summary>
        public int BankLsb { get; set; }

        /// <summary>Selected program (patch) number.</summary>
        public int Program { get; set; }

        /// <summary>
        /// A monotonic change stamp covering every input the modulator engine can
        /// read as a source (continuous controllers, pitch bend and its range,
        /// channel pressure). Voices compare it at control rate and skip
        /// re-evaluating their modulator list while nothing has changed.
        /// </summary>
        public int Version => version;

        /// <summary>
        /// Pitch-bend range in semitones (RPN 0: data MSB semitones + data LSB
        /// cents/100). Default 2. Set via the RPN data-entry methods below; not
        /// reset by Reset All Controllers (RP-015).
        /// </summary>
        public double PitchBendRangeSemitones
        {
            get => pitchBendRangeSemitones;
            set { pitchBendRangeSemitones = value; version++; pitchBendRatioValid = false; }
        }

        /// <summary>Raw 14-bit pitch-bend value, centred at 8192.</summary>
        public int PitchBend
        {
            get => pitchBend;
            set { pitchBend = value; version++; pitchBendRatioValid = false; }
        }

        /// <summary>Channel pressure (aftertouch), 0..127.</summary>
        public int ChannelPressure
        {
            get => channelPressure;
            set { channelPressure = value; version++; }
        }

        /// <summary>Whether the sustain pedal (CC64) is down.</summary>
        public bool SustainPedal { get; set; }

        /// <summary>The current value (0..127) of a MIDI continuous controller.</summary>
        public int Controller(int cc) => (uint)cc < (uint)controllers.Length ? controllers[cc] : 0;

        /// <summary>Stores the value (0..127) of a MIDI continuous controller.</summary>
        public void SetController(int cc, int value)
        {
            if ((uint)cc < (uint)controllers.Length)
            {
                controllers[cc] = (byte)value;
                version++;
            }
        }

        /// <summary>
        /// The frequency ratio for the current pitch-bend and range. Cached: the
        /// underlying 2^x is recomputed only when the bend or its range changes,
        /// not per control block per voice.
        /// </summary>
        public double PitchBendRatio
        {
            get
            {
                if (!pitchBendRatioValid)
                {
                    pitchBendRatio = SynthMath.CentsToRatio(
                        (pitchBend - 8192) / 8192.0 * pitchBendRangeSemitones * 100.0);
                    pitchBendRatioValid = true;
                }
                return pitchBendRatio;
            }
        }

        /// <summary>Selects the RPN MSB (CC101), clearing any NRPN selection.</summary>
        public void SelectRpnMsb(int value)
        {
            rpnMsb = value & 0x7F;
            nrpnSelected = false;
        }

        /// <summary>Selects the RPN LSB (CC100), clearing any NRPN selection.</summary>
        public void SelectRpnLsb(int value)
        {
            rpnLsb = value & 0x7F;
            nrpnSelected = false;
        }

        /// <summary>
        /// Notes that an NRPN (CC98/CC99) was selected: until an RPN is selected
        /// again, data entry belongs to the NRPN and must not change RPN state.
        /// (NRPNs themselves are not decoded.)
        /// </summary>
        public void SelectNrpn() => nrpnSelected = true;

        // the currently addressed RPN, or -1 when none is (RPN null 127,127
        // deselects, and an NRPN selection routes data entry elsewhere)
        private int SelectedRpn
        {
            get
            {
                if (nrpnSelected) return -1;
                int rpn = (rpnMsb << 7) | rpnLsb;
                return rpn == RpnNull ? -1 : rpn;
            }
        }

        /// <summary>
        /// Data Entry MSB (CC6) for the selected RPN. RPN 0 (pitch-bend range):
        /// the MSB is the range in semitones; a new MSB resets the cents part,
        /// matching common device practice. Ignored when no RPN is selected.
        /// </summary>
        public void DataEntryMsb(int value)
        {
            if (SelectedRpn != RpnPitchBendRange) return;
            bendRangeSemitones = value & 0x7F;
            bendRangeCents = 0;
            PitchBendRangeSemitones = bendRangeSemitones + bendRangeCents / 100.0;
        }

        /// <summary>
        /// Data Entry LSB (CC38) for the selected RPN. RPN 0 (pitch-bend range):
        /// the LSB adds cents (range = MSB + LSB/100). Ignored when no RPN is
        /// selected.
        /// </summary>
        public void DataEntryLsb(int value)
        {
            if (SelectedRpn != RpnPitchBendRange) return;
            bendRangeCents = value & 0x7F;
            PitchBendRangeSemitones = bendRangeSemitones + bendRangeCents / 100.0;
        }

        /// <summary>
        /// Resets controllers to their default values (e.g. on Reset All
        /// Controllers): volume and expression full (so a channel with no volume
        /// sent plays at unity), pan centred, everything else 0, pitch-bend
        /// centred and the sustain pedal up. Per RP-015 the pitch-bend range and
        /// the RPN/NRPN selection are deliberately left untouched.
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
            version++; // the direct controller-array writes above must stamp too
        }
    }
}
