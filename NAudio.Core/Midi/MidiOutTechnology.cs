using System;

namespace NAudio.Midi
{
    /// <summary>
    /// Represents the different types of technology used by a MIDI out device
    /// </summary>
    /// <remarks>from mmsystem.h</remarks>
    public enum MidiOutTechnology 
    {
        /// <summary>The device is a MIDI port</summary>
        MidiPort = 1,
        /// <summary>The device is a MIDI synth</summary>
        Synth = 2,
        /// <summary>The device is a square wave synth</summary>
        SquareWaveSynth = 3,
        /// <summary>The device is an FM synth</summary>
        FMSynth = 4,
        /// <summary>The device is a MIDI mapper</summary>
        MidiMapper = 5,
        /// <summary>The device is a WaveTable synth</summary>
        WaveTableSynth = 6,
        /// <summary>The device is a software synth</summary>
        SoftwareSynth = 7
    }
}
