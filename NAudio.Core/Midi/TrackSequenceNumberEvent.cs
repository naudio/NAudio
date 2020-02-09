using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace NAudio.Midi
{
    /// <summary>
    /// Represents a MIDI track sequence number event event
    /// </summary>
    public class TrackSequenceNumberEvent : MetaEvent
    {
        private ushort sequenceNumber;

        /// <summary>
        /// Creates a new track sequence number event
        /// </summary>
        public TrackSequenceNumberEvent(ushort sequenceNumber)
        {
            this.sequenceNumber = sequenceNumber;
        }

        /// <summary>
        /// Reads a new track sequence number event from a MIDI stream
        /// </summary>
        /// <param name="br">The MIDI stream</param>
        /// <param name="length">the data length</param>
        public TrackSequenceNumberEvent(BinaryReader br, int length)
        {
            // TODO: there is a form of the TrackSequenceNumberEvent that
            // has a length of zero
            if(length != 2) 
            {
                throw new FormatException("Invalid sequence number length");
            }
            sequenceNumber = (ushort) ((br.ReadByte() << 8) + br.ReadByte());
        }

        /// <summary>
        /// Creates a deep clone of this MIDI event.
        /// </summary>
        public override MidiEvent Clone() => (TrackSequenceNumberEvent)MemberwiseClone();

        /// <summary>
        /// Describes this event
        /// </summary>
        /// <returns>String describing the event</returns>
        public override string ToString()
        {
            return String.Format("{0} {1}", base.ToString(), sequenceNumber);
        }

        /// <summary>
        /// Calls base class export first, then exports the data 
        /// specific to this event
        /// <seealso cref="MidiEvent.Export">MidiEvent.Export</seealso>
        /// </summary>
        public override void Export(ref long absoluteTime, BinaryWriter writer)
        {
            base.Export(ref absoluteTime, writer);
            writer.Write((byte)((sequenceNumber >> 8) & 0xFF));
            writer.Write((byte)(sequenceNumber & 0xFF));
        }
    }
}
