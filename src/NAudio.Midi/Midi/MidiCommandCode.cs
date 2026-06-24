namespace NAudio.Midi 
{
    /// <summary>
    /// MIDI command codes
    /// </summary>
    public enum MidiCommandCode : byte 
    {
        /// <summary>Note Off</summary>
        NoteOff = 0x80,
        /// <summary>Note On</summary>
        NoteOn = 0x90,
        /// <summary>Key After-touch</summary>
        KeyAfterTouch = 0xA0,
        /// <summary>Control change</summary>
        ControlChange = 0xB0,
        /// <summary>Patch change</summary>
        PatchChange = 0xC0,
        /// <summary>Channel after-touch</summary>
        ChannelAfterTouch = 0xD0,
        /// <summary>Pitch wheel change</summary>
        PitchWheelChange = 0xE0,
        /// <summary>Sysex message</summary>
        Sysex = 0xF0,
        /// <summary>Eox (comes at end of a sysex message)</summary>
        Eox = 0xF7,
        /// <summary>Timing clock (used when synchronization is required)</summary>
        TimingClock = 0xF8,
        /// <summary>Start sequence</summary>
        StartSequence = 0xFA,
        /// <summary>Continue sequence</summary>
        ContinueSequence = 0xFB,
        /// <summary>Stop sequence</summary>
        StopSequence = 0xFC,
        /// <summary>Auto-Sensing</summary>
        AutoSensing = 0xFE,
        /// <summary>Meta-event</summary>
        MetaEvent = 0xFF,
    }
}