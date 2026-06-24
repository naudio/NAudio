using System;
using System.IO;

namespace NAudio.Midi 
{
    /// <summary>
    /// Represents a MIDI time signature event
    /// </summary>
    public class TimeSignatureEvent : MetaEvent 
    {
        private readonly byte numerator;
        private readonly byte denominator;
        private readonly byte ticksInMetronomeClick;
        private readonly byte no32ndNotesInQuarterNote;
        
        /// <summary>
        /// Reads a new time signature event from a MIDI stream
        /// </summary>
        /// <param name="br">The MIDI stream</param>
        /// <param name="length">The data length</param>
        public TimeSignatureEvent(BinaryReader br, int length) 
        {
            if(length != 4) 
            {
                throw new FormatException($"Invalid time signature length: Got {length}, expected 4");
            }
            numerator = br.ReadByte();
            denominator = br.ReadByte(); // exponent: denominator is 2^value
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
            this.numerator = ValidateByteRange(nameof(numerator), numerator);
            this.denominator = ValidateByteRange(nameof(denominator), denominator);
            this.ticksInMetronomeClick = ValidateByteRange(nameof(ticksInMetronomeClick), ticksInMetronomeClick);
            this.no32ndNotesInQuarterNote = ValidateByteRange(nameof(no32ndNotesInQuarterNote), no32ndNotesInQuarterNote);
        }

        private static byte ValidateByteRange(string parameterName, int value)
        {
            if (value < 0 || value > byte.MaxValue)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value must be in the range 0-255");
            }
            return (byte)value;
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
        /// Denominator (Beat unit), stored as power-of-two exponent (dd), where denominator is 2^dd
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
                string den;
                if (denominator <= 30)
                {
                    den = (1 << denominator).ToString();
                }
                else
                {
                    den = $"Unknown ({denominator})";
                }

                return $"{numerator}/{den}";
            }
        }
        
        /// <summary>
        /// Describes this time signature event
        /// </summary>
        /// <returns>A string describing this event</returns>
        public override string ToString() 
        {
            return $"{base.ToString()} {TimeSignature} TicksInClick:{ticksInMetronomeClick} 32ndsInQuarterNote:{no32ndNotesInQuarterNote}";
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