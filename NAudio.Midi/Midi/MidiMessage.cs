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
        /// <param name="note">Note number (0 to 127)</param>
        /// <param name="volume">Volume (0 to 127)</param>
        /// <param name="channel">MIDI channel (1 to 16)</param>
        /// <returns>A new MidiMessage object</returns>
        public static MidiMessage StartNote(int note, int volume, int channel)
        {
            ValidateNoteParameters(note, volume, channel);
            return new MidiMessage((int)MidiCommandCode.NoteOn + channel - 1, note, volume);
        }

        private static void ValidateNoteParameters(int note, int volume, int channel)
        {
            ValidateChannel(channel);
            if (note < 0 || note > 127)
            {
                throw new ArgumentOutOfRangeException("note", "Note number must be in the range 0-127");
            }
            if (volume < 0 || volume > 127)
            {
                throw new ArgumentOutOfRangeException("volume", "Velocity must be in the range 0-127");
            }
        }

        private static void ValidateChannel(int channel)
        {
            if ((channel < 1) || (channel > 16))
            {
                throw new ArgumentOutOfRangeException("channel", channel,
                    String.Format("Channel must be 1-16 (Got {0})", channel));
            }
        }

        /// <summary>
        /// Creates a Note Off message
        /// </summary>
        /// <param name="note">Note number</param>
        /// <param name="volume">Volume </param>
        /// <param name="channel">MIDI channel (1-16)</param>
        /// <returns>A new MidiMessage object</returns>
        public static MidiMessage StopNote(int note, int volume, int channel)
        {
            ValidateNoteParameters(note, volume, channel);
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
            ValidateChannel(channel);
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
            ValidateChannel(channel);
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
