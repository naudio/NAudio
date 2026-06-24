using System;
using System.IO;
using System.Text;

namespace NAudio.Midi 
{
    /// <summary>
    /// Represents a MIDI meta event
    /// </summary>
    public class MetaEvent : MidiEvent 
    {
        private MetaEventType metaEvent;
        internal int metaDataLength;

        /// <summary>
        /// Gets the type of this meta event
        /// </summary>
        public MetaEventType MetaEventType
        {
            get
            {
                return metaEvent;
            }
        }

        /// <summary>
        /// Empty constructor
        /// </summary>
        protected MetaEvent()
        {
        }

        /// <summary>
        /// Custom constructor for use by derived types, who will manage the data themselves
        /// </summary>
        /// <param name="metaEventType">Meta event type</param>
        /// <param name="metaDataLength">Meta data length</param>
        /// <param name="absoluteTime">Absolute time</param>
        public MetaEvent(MetaEventType metaEventType, int metaDataLength, long absoluteTime)
            : base(absoluteTime,1,MidiCommandCode.MetaEvent)
        {
            this.metaEvent = metaEventType;
            this.metaDataLength = metaDataLength;
        }

        /// <summary>
        /// Creates a deep clone of this MIDI event.
        /// </summary>
        public override MidiEvent Clone() => new MetaEvent(metaEvent, metaDataLength, AbsoluteTime);

        /// <summary>
        /// Reads a meta-event from a stream
        /// </summary>
        /// <param name="br">A binary reader based on the stream of MIDI data</param>
        /// <returns>A new MetaEvent object</returns>
        public static MetaEvent ReadMetaEvent(BinaryReader br) 
        {
            MetaEventType metaEvent = (MetaEventType) br.ReadByte();
            int length = ReadVarInt(br);
            
            MetaEvent me = new MetaEvent();
            switch(metaEvent) 
            {
            case MetaEventType.TrackSequenceNumber: // Sets the track's sequence number.
                me = new TrackSequenceNumberEvent(br,length);
                break;
            case MetaEventType.TextEvent: // Text event
            case MetaEventType.Copyright: // Copyright
            case MetaEventType.SequenceTrackName: // Sequence / Track Name
            case MetaEventType.TrackInstrumentName: // Track instrument name
            case MetaEventType.Lyric: // lyric
            case MetaEventType.Marker: // marker
            case MetaEventType.CuePoint: // cue point
            case MetaEventType.ProgramName:
            case MetaEventType.DeviceName:
                me = new TextEvent(br,length);
                break;
            case MetaEventType.EndTrack: // This event must come at the end of each track
                if(length != 0) 
                {
                    throw new FormatException("End track length");
                }
                break;
            case MetaEventType.SetTempo: // Set tempo
                me = new TempoEvent(br,length);
                break;
            case MetaEventType.TimeSignature: // Time signature
                me = new TimeSignatureEvent(br,length);
                break;
            case MetaEventType.KeySignature: // Key signature
                me = new KeySignatureEvent(br, length);
                break;
            case MetaEventType.SequencerSpecific: // Sequencer specific information
                me = new SequencerSpecificEvent(br, length);
                break;
            case MetaEventType.SmpteOffset:
                me = new SmpteOffsetEvent(br, length);
                break;
            default:
//System.Windows.Forms.MessageBox.Show(String.Format("Unsupported MetaEvent {0} length {1} pos {2}",metaEvent,length,br.BaseStream.Position));
                var data = br.ReadBytes(length);
                if (data.Length != length)
                {
                    throw new FormatException("Failed to read metaevent's data fully");
                }
                return new RawMetaEvent(metaEvent, default(long), data);
            }
            me.metaEvent = metaEvent;
            me.metaDataLength = length;
            
            return me;
        }

        /// <summary>
        /// Describes this meta event
        /// </summary>
        public override string ToString() 
        {
            return $"{AbsoluteTime} {metaEvent}";
        }

        /// <summary>
        /// <see cref="MidiEvent.Export"/>
        /// </summary>
        public override void Export(ref long absoluteTime, BinaryWriter writer)
        {
            base.Export(ref absoluteTime, writer);
            writer.Write((byte)metaEvent);
            WriteVarInt(writer, metaDataLength);
        }
    }
}