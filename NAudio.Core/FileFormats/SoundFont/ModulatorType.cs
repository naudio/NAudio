namespace NAudio.SoundFont
{
    /// <summary>
    /// Controller Sources
    /// </summary>
    public enum ControllerSourceEnum
    {
        /// <summary>
        /// No Controller
        /// </summary>
        NoController = 0,
        /// <summary>
        /// Note On Velocity
        /// </summary>
        NoteOnVelocity = 2,
        /// <summary>
        /// Note On Key Number
        /// </summary>
        NoteOnKeyNumber = 3,
        /// <summary>
        /// Poly Pressure
        /// </summary>
        PolyPressure = 10,
        /// <summary>
        /// Channel Pressure
        /// </summary>
        ChannelPressure = 13,
        /// <summary>
        /// Pitch Wheel
        /// </summary>
        PitchWheel = 14,
        /// <summary>
        /// Pitch Wheel Sensitivity
        /// </summary>
        PitchWheelSensitivity = 16
    }

    /// <summary>
    /// Source Types
    /// </summary>
    public enum SourceTypeEnum
    {
        /// <summary>
        /// Linear
        /// </summary>
        Linear,
        /// <summary>
        /// Concave
        /// </summary>
        Concave,
        /// <summary>
        /// Convex
        /// </summary>
        Convex,
        /// <summary>
        /// Switch
        /// </summary>
        Switch
    }

    /// <summary>
    /// Modulator source enumerator — the controller that drives a
    /// <see cref="Modulator"/>. Decodes the 16-bit "Sf2 modulator source"
    /// bit-field (SoundFont 2.04 §8.2): bits 0-6 are the controller index,
    /// bit 7 selects a MIDI continuous controller, bit 8 is the direction,
    /// bit 9 is the polarity, and bits 10-15 are the transform/source type.
    /// </summary>
    public class ModulatorType
    {
        /// <summary>
        /// Decodes a 16-bit SoundFont modulator source enumeration. Public so the
        /// synthesiser can build the implicit default modulators (SoundFont 2.04
        /// §8.4) and so callers can construct modulator routings programmatically.
        /// </summary>
        public ModulatorType(ushort raw)
        {
            RawValue = raw;
            Polarity = (raw & 0x0200) == 0x0200;
            Direction = (raw & 0x0100) == 0x0100;
            IsMidiContinuousController = (raw & 0x0080) == 0x0080;
            SourceType = (SourceTypeEnum)((raw & 0xFC00) >> 10);
            ControllerSource = (ControllerSourceEnum)(raw & 0x007F);
            MidiContinuousControllerNumber = (ushort)(raw & 0x007F);
        }

        /// <summary>
        /// The raw 16-bit source enumeration this type was decoded from. Used to
        /// test two modulators for identity (SoundFont 2.04 §9.5).
        /// </summary>
        public ushort RawValue { get; }

        /// <summary>
        /// Polarity: false = unipolar (0..1), true = bipolar (-1..1).
        /// </summary>
        public bool Polarity { get; }

        /// <summary>
        /// Direction: false = min-to-max (increasing), true = max-to-min (decreasing).
        /// </summary>
        public bool Direction { get; }

        /// <summary>
        /// Whether the source is a MIDI continuous controller (true) or one of the
        /// general controllers in <see cref="ControllerSource"/> (false).
        /// </summary>
        public bool IsMidiContinuousController { get; }

        /// <summary>
        /// The general controller source, valid when
        /// <see cref="IsMidiContinuousController"/> is false.
        /// </summary>
        public ControllerSourceEnum ControllerSource { get; }

        /// <summary>
        /// The transform/source curve applied to the controller value.
        /// </summary>
        public SourceTypeEnum SourceType { get; }

        /// <summary>
        /// The MIDI continuous controller number, valid when
        /// <see cref="IsMidiContinuousController"/> is true.
        /// </summary>
        public ushort MidiContinuousControllerNumber { get; }

        /// <summary>
        /// <see cref="object.ToString"/>
        /// </summary>
        public override string ToString()
        {
            return IsMidiContinuousController
                ? $"{SourceType} CC{MidiContinuousControllerNumber}"
                : $"{SourceType} {ControllerSource}";
        }
    }
}
