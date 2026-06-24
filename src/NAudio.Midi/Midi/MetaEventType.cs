using System;

namespace NAudio.Midi
{
    /// <summary>
    /// MIDI MetaEvent Type
    /// </summary>
    public enum MetaEventType : byte 
    {
        /// <summary>Track sequence number</summary>
        TrackSequenceNumber = 0x00,
        /// <summary>Text event</summary>
        TextEvent = 0x01,
        /// <summary>Copyright</summary>
        Copyright = 0x02,
        /// <summary>Sequence track name</summary>
        SequenceTrackName = 0x03,
        /// <summary>Track instrument name</summary>
        TrackInstrumentName = 0x04,
        /// <summary>Lyric</summary>
        Lyric = 0x05,
        /// <summary>Marker</summary>
        Marker = 0x06,
        /// <summary>Cue point</summary>
        CuePoint = 0x07,
        /// <summary>Program (patch) name</summary>
        ProgramName = 0x08,
        /// <summary>Device (port) name</summary>
        DeviceName = 0x09,
        /// <summary>MIDI Channel (not official?)</summary>
        MidiChannel = 0x20,
        /// <summary>MIDI Port (not official?)</summary>
        MidiPort = 0x21,
        /// <summary>End track</summary>
        EndTrack = 0x2F,
        /// <summary>Set tempo</summary>
        SetTempo = 0x51,
        /// <summary>SMPTE offset</summary>
        SmpteOffset = 0x54,
        /// <summary>Time signature</summary>
        TimeSignature = 0x58,
        /// <summary>Key signature</summary>
        KeySignature = 0x59,
        /// <summary>Sequencer specific</summary>
        SequencerSpecific = 0x7F,
    }
}
