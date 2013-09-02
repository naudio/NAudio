using System;

namespace NAudio.Midi
{
    /// <summary>
    /// Represents a MIDI message
    /// </summary>
    public class MidiMessage
    {
        private int rawData;

        /// <summary>
        /// Creates a new MIDI message
        /// </summary>
        /// <param name="status">Status</param>
        /// <param name="data1">Data parameter 1</param>
        /// <param name="data2">Data parameter 2</param>
        public MidiMessage(int status, int data1, int data2)
        {
            rawData = status + (data1 << 8) + (data2 << 16);
        }

        /// <summary>
        /// Creates a new MIDI message from a raw message
        /// </summary>
        /// <param name="rawData">A packed MIDI message from an MMIO function</param>
        public MidiMessage(int rawData)
        {
            this.rawData = rawData;
        }

        /// <summary>
        /// Creates a Note On message
        /// </summary>
        /// <param name="note">Note number</param>
        /// <param name="volume">Volume</param>
        /// <param name="channel">MIDI channel</param>
        /// <returns>A new MidiMessage object</returns>
        public static MidiMessage StartNote(int note, int volume, int channel)
        {
            return new MidiMessage((int)MidiCommandCode.NoteOn + channel - 1, note, volume);
        }

        /// <summary>
        /// Creates a Note Off message
        /// </summary>
        /// <param name="note">Note number</param>
        /// <param name="volume">Volume</param>
        /// <param name="channel">MIDI channel (1-16)</param>
        /// <returns>A new MidiMessage object</returns>
        public static MidiMessage StopNote(int note, int volume, int channel)
        {
            return new MidiMessage((int)MidiCommandCode.NoteOff + channel - 1, note, volume);
        }

        /// <summary>
        /// Creates a patch change message
        /// </summary>
        /// <param name="patch">The patch number</param>
        /// <param name="channel">The MIDI channel number (1-16)</param>
        /// <returns>A new MidiMessageObject</returns>
        public static MidiMessage ChangePatch(int patch, int channel)
        {
            return new MidiMessage((int)MidiCommandCode.PatchChange + channel - 1, patch, 0);
        }

        /// <summary>
        /// Creates a Control Change message
        /// </summary>
        /// <param name="controller">The controller number to change</param>
        /// <param name="value">The value to set the controller to</param>
        /// <param name="channel">The MIDI channel number (1-16)</param>
        /// <returns>A new MidiMessageObject</returns>
        public static MidiMessage ChangeControl(int controller, int value, int channel)
        {
            return new MidiMessage((int)MidiCommandCode.ControlChange + channel - 1, controller, value);
        }

        /// <summary>
        /// Returns the raw MIDI message data
        /// </summary>
        public int RawData
        {
            get
            {
                return rawData;
            }
        }
    }
}
