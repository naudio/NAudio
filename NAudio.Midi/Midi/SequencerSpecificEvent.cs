using System;
using System.IO;
using System.Text;

namespace NAudio.Midi
{
    /// <summary>
    /// Represents a Sequencer Specific event
    /// </summary>
    public class SequencerSpecificEvent : MetaEvent
    {
        private byte[] data;

        /// <summary>
        /// Reads a new sequencer specific event from a MIDI stream
        /// </summary>
        /// <param name="br">The MIDI stream</param>
        /// <param name="length">The data length</param>
        public SequencerSpecificEvent(BinaryReader br, int length)
        {
            this.data = br.ReadBytes(length);
        }

        /// <summary>
        /// Creates a new Sequencer Specific event
        /// </summary>
        /// <param name="data">The sequencer specific data</param>
        /// <param name="absoluteTime">Absolute time of this event</param>
        public SequencerSpecificEvent(byte[] data, long absoluteTime)
            : base(MetaEventType.SequencerSpecific, data.Length, absoluteTime)
        {
            this.data = data;
        }

        /// <summary>
        /// Creates a deep clone of this MIDI event.
        /// </summary>
        public override MidiEvent Clone() => new SequencerSpecificEvent((byte[])data.Clone(), AbsoluteTime);

        /// <summary>
        /// The contents of this sequencer specific
        /// </summary>
        public byte[] Data
        {
            get
            {
                return this.data;
            }
            set
            {
                this.data = value;
                this.metaDataLength = this.data.Length;
            }
        }

        /// <summary>
        /// Describes this MIDI text event
        /// </summary>
        /// <returns>A string describing this event</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(base.ToString());
            sb.Append(" ");
            foreach (var b in data)
            {
                sb.AppendFormat("{0:X2} ", b);
            }
            sb.Length--;
            return sb.ToString();
        }

        /// <summary>
        /// Calls base class export first, then exports the data 
        /// specific to this event
        /// <seealso cref="MidiEvent.Export">MidiEvent.Export</seealso>
        /// </summary>
        public override void Export(ref long absoluteTime, BinaryWriter writer)
        {
            base.Export(ref absoluteTime, writer);
            writer.Write(data);
        }
    }
}