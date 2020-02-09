using System;
using System.IO;
using System.Text;

namespace NAudio.Midi
{
    /// <summary>
    /// Represents a MIDI note on event
    /// </summary>
    public class NoteOnEvent : NoteEvent
    {
        private NoteEvent offEvent;

        /// <summary>
        /// Reads a new Note On event from a stream of MIDI data
        /// </summary>
        /// <param name="br">Binary reader on the MIDI data stream</param>
        public NoteOnEvent(BinaryReader br)
            : base(br)
        {
        }

        /// <summary>
        /// Creates a NoteOn event with specified parameters
        /// </summary>
        /// <param name="absoluteTime">Absolute time of this event</param>
        /// <param name="channel">MIDI channel number</param>
        /// <param name="noteNumber">MIDI note number</param>
        /// <param name="velocity">MIDI note velocity</param>
        /// <param name="duration">MIDI note duration</param>
        public NoteOnEvent(long absoluteTime, int channel, int noteNumber,
            int velocity, int duration)
            : base(absoluteTime, channel, MidiCommandCode.NoteOn, noteNumber, velocity)
        {
            this.OffEvent = new NoteEvent(absoluteTime, channel, MidiCommandCode.NoteOff,
                noteNumber, 0);
            NoteLength = duration;
        }

        /// <summary>
        /// Creates a deep clone of this MIDI event.
        /// </summary>
        public override MidiEvent Clone() => new NoteOnEvent(AbsoluteTime, Channel, NoteNumber, Velocity, NoteLength);

        /// <summary>
        /// The associated Note off event
        /// </summary>
        public NoteEvent OffEvent
        {
            get
            {
                return offEvent;
            }
            set
            {
                if (!MidiEvent.IsNoteOff(value))
                {
                    throw new ArgumentException("OffEvent must be a valid MIDI note off event");
                }
                if (value.NoteNumber != this.NoteNumber)
                {
                    throw new ArgumentException("Note Off Event must be for the same note number");
                }
                if (value.Channel != this.Channel)
                {
                    throw new ArgumentException("Note Off Event must be for the same channel");
                }
                offEvent = value;

            }
        }

        /// <summary>
        /// Get or set the Note Number, updating the off event at the same time
        /// </summary>
        public override int NoteNumber
        {
            get
            {
                return base.NoteNumber;
            }
            set
            {
                base.NoteNumber = value;
                if (OffEvent != null)
                {
                    OffEvent.NoteNumber = NoteNumber;
                }
            }
        }

        /// <summary>
        /// Get or set the channel, updating the off event at the same time
        /// </summary>
        public override int Channel
        {
            get
            {
                return base.Channel;
            }
            set
            {
                base.Channel = value;
                if (OffEvent != null)
                {
                    OffEvent.Channel = Channel;
                }
            }
        }

        /// <summary>
        /// The duration of this note
        /// </summary>
        /// <remarks>
        /// There must be a note off event
        /// </remarks>
        public int NoteLength
        {
            get
            {
                return (int)(offEvent.AbsoluteTime - this.AbsoluteTime);
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("NoteLength must be 0 or greater");
                }
                offEvent.AbsoluteTime = this.AbsoluteTime + value;
            }
        }

        /// <summary>
        /// Calls base class export first, then exports the data 
        /// specific to this event
        /// <seealso cref="MidiEvent.Export">MidiEvent.Export</seealso>
        /// </summary>
        public override string ToString()
        {
            if ((this.Velocity == 0) && (OffEvent == null))
            {
                return String.Format("{0} (Note Off)",
                    base.ToString());
            }
            return String.Format("{0} Len: {1}",
                base.ToString(),
                (this.OffEvent == null) ? "?" : this.NoteLength.ToString());
        }
    }
}