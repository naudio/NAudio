using System;

namespace NAudio.Midi
{
    /// <summary>
    /// MidiController enumeration
    /// http://www.midi.org/techspecs/midimessages.php#3
    /// </summary>
    public enum MidiController : byte 
    {
        /// <summary>Bank Select (MSB)</summary>
        BankSelect = 0,
        /// <summary>Modulation (MSB)</summary>
        Modulation = 1,
        /// <summary>Breath Controller</summary>
        BreathController = 2,
        /// <summary>Foot controller (MSB)</summary>
        FootController = 4,
        /// <summary>Main volume</summary>
        MainVolume = 7,
        /// <summary>Pan</summary>
        Pan = 10,
        /// <summary>Expression</summary>
        Expression = 11,
        /// <summary>Bank Select LSB</summary>
        BankSelectLsb = 32,
        /// <summary>Sustain</summary>
        Sustain = 64,
        /// <summary>Portamento On/Off</summary>
        Portamento = 65,
        /// <summary>Sostenuto On/Off</summary>
        Sostenuto = 66,
        /// <summary>Soft Pedal On/Off</summary>
        SoftPedal = 67,
        /// <summary>Legato Footswitch</summary>
        LegatoFootswitch = 68,
        /// <summary>Reset all controllers</summary>
        ResetAllControllers = 121,
        /// <summary>All notes off</summary>
        AllNotesOff = 123,
    }
}
