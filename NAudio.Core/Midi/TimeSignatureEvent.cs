using System;
using System.IO;
using System.Text;

namespace NAudio.Midi 
{
    /// <summary>
    /// Represents a MIDI time signature event
    /// </summary>
    public class TimeSignatureEvent : MetaEvent 
    {
        private byte numerator;
        private byte denominator;
        private byte ticksInMetronomeClick;
        private byte no32ndNotesInQuarterNote;
        
        /// <summary>
        /// Reads a new time signature event from a MIDI stream
        /// </summary>
        /// <param name="br">The MIDI stream</param>
        /// <param name="length">The data length</param>
        public TimeSignatureEvent(BinaryReader br,int length) 
        {
            if(length != 4) 
            {
                throw new FormatException(String.Format("Invalid time signature length: Got {0}, expected 4", length));
            }
            numerator = br.ReadByte();
            denominator = br.ReadByte(); //2=quarter, 3=eigth etc
            ticksInMetronomeClick = br.ReadByte();
            no32ndNotesInQuarterNote = br.ReadByte();
        }

        /// <summary>
        /// Creates a new TimeSignatureEvent
        /// </summary>
        /// <param name="absoluteTime">Time at which to create this event</param>
        /// <param name="numerator">Numerator</param>
        /// <param name="denominator">Denominator</param>
        /// <param name="ticksInMetronomeClick">Ticks in Metronome Click</param>
        /// <param name="no32ndNotesInQuarterNote">No of 32nd Notes in Quarter Click</param>
        public TimeSignatureEvent(long absoluteTime, int numerator, int denominator, int ticksInMetronomeClick, int no32ndNotesInQuarterNote)
            :
            base(MetaEventType.TimeSignature, 4, absoluteTime)
        {
            this.numerator = (byte)numerator;
            this.denominator = (byte)denominator;
            this.ticksInMetronomeClick = (byte)ticksInMetronomeClick;
            this.no32ndNotesInQuarterNote = (byte)no32ndNotesInQuarterNote;
        }

        /// <summary>
        /// Creates a deep clone of this MIDI event.
        /// </summary>
        public override MidiEvent Clone() => (TimeSignatureEvent)MemberwiseClone();

        /// <summary>
        /// Numerator (number of beats in a bar)
        /// </summary>
        public int Numerator
        {
            get { return numerator; }
        }

        /// <summary>
        /// Denominator (Beat unit),
        /// 1 means 2, 2 means 4 (crochet), 3 means 8 (quaver), 4 means 16 and 5 means 32
        /// </summary>
        public int Denominator
        {
            get { return denominator; }
        }

        /// <summary>
        /// Ticks in a metronome click
        /// </summary>
        public int TicksInMetronomeClick
        {
            get { return ticksInMetronomeClick; }
        }

        /// <summary>
        /// Number of 32nd notes in a quarter note
        /// </summary>
        public int No32ndNotesInQuarterNote
        {
            get { return no32ndNotesInQuarterNote; }
        }

        /// <summary>
        /// The time signature
        /// </summary>
        public string TimeSignature 
        {
            get 
            {
                string den = String.Format("Unknown ({0})",denominator);
                switch(denominator) 
                {
                case 1:
                    den = "2";
                    break;
                case 2:
                    den = "4";
                    break;
                case 3:
                    den = "8";
                    break;
                case 4:
                    den = "16";
                    break;
                case 5:
                    den = "32";
                    break;
                }
                return String.Format("{0}/{1}",numerator,den);
            }
        }
        
        /// <summary>
        /// Describes this time signature event
        /// </summary>
        /// <returns>A string describing this event</returns>
        public override string ToString() 
        {
            return String.Format("{0} {1} TicksInClick:{2} 32ndsInQuarterNote:{3}",
                base.ToString(),TimeSignature,ticksInMetronomeClick,no32ndNotesInQuarterNote);
        }

        /// <summary>
        /// Calls base class export first, then exports the data 
        /// specific to this event
        /// <seealso cref="MidiEvent.Export">MidiEvent.Export</seealso>
        /// </summary>
        public override void Export(ref long absoluteTime, BinaryWriter writer)
        {
            base.Export(ref absoluteTime, writer);
            writer.Write(numerator);
            writer.Write(denominator);
            writer.Write(ticksInMetronomeClick);
            writer.Write(no32ndNotesInQuarterNote);
        }
    }
}