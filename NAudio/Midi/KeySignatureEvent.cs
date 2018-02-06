using System;
using System.IO;

namespace NAudio.Midi
{
    /// <summary>
    /// Represents a MIDI key signature event event
    /// </summary>
    public class KeySignatureEvent : MetaEvent
    {
        private readonly byte sharpsFlats;
        private readonly byte majorMinor;

        /// <summary>
        /// Reads a new track sequence number event from a MIDI stream
        /// </summary>
        /// <param name="br">The MIDI stream</param>
        /// <param name="length">the data length</param>
        public KeySignatureEvent(BinaryReader br, int length)
        {
            if (length != 2)
            {
                throw new FormatException("Invalid key signature length");
            }
            sharpsFlats = br.ReadByte(); // sf=sharps/flats (-7=7 flats, 0=key of C,7=7 sharps)
            majorMinor = br.ReadByte(); // mi=major/minor (0=major, 1=minor)
        }

        /// <summary>
        /// Creates a new Key signature event with the specified data
        /// </summary>
        public KeySignatureEvent(int sharpsFlats, int majorMinor, long absoluteTime)
            : base(MetaEventType.KeySignature, 2, absoluteTime)
        {
            this.sharpsFlats = (byte) sharpsFlats;
            this.majorMinor = (byte) majorMinor;
        }

        /// <summary>
        /// Creates a deep clone of this MIDI event.
        /// </summary>
        public override MidiEvent Clone() => (KeySignatureEvent)MemberwiseClone();

        /// <summary>
        /// Number of sharps or flats
        /// </summary>
        public int SharpsFlats => (sbyte)sharpsFlats;

        /// <summary>
        /// Major or Minor key
        /// </summary>
        public int MajorMinor => majorMinor;

        /// <summary>
        /// Describes this event
        /// </summary>
        /// <returns>String describing the event</returns>
        public override string ToString()
        {
            return String.Format("{0} {1} {2}", base.ToString(), SharpsFlats, majorMinor);
        }

        /// <summary>
        /// Calls base class export first, then exports the data 
        /// specific to this event
        /// <seealso cref="MidiEvent.Export">MidiEvent.Export</seealso>
        /// </summary>
        public override void Export(ref long absoluteTime, BinaryWriter writer)
        {
            base.Export(ref absoluteTime, writer);
            writer.Write(sharpsFlats);
            writer.Write(majorMinor);
        }
    }
}