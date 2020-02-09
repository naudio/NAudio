using System;
using System.IO;
using System.Text;

namespace NAudio.Midi 
{
    /// <summary>
    /// Represents a MIDI pitch wheel change event
    /// </summary>
    public class PitchWheelChangeEvent : MidiEvent 
    {
        private int pitch;
        
        /// <summary>
        /// Reads a pitch wheel change event from a MIDI stream
        /// </summary>
        /// <param name="br">The MIDI stream to read from</param>
        public PitchWheelChangeEvent(BinaryReader br) 
        {
            byte b1 = br.ReadByte();
            byte b2 = br.ReadByte();
            if((b1 & 0x80) != 0) 
            {
                // TODO: might be a follow-on				
                throw new FormatException("Invalid pitchwheelchange byte 1");
            }
            if((b2 & 0x80) != 0) 
            {
                throw new FormatException("Invalid pitchwheelchange byte 2");
            }
            
            pitch = b1 + (b2 << 7); // 0x2000 is normal
        }

        /// <summary>
        /// Creates a new pitch wheel change event
        /// </summary>
        /// <param name="absoluteTime">Absolute event time</param>
        /// <param name="channel">Channel</param>
        /// <param name="pitchWheel">Pitch wheel value</param>
        public PitchWheelChangeEvent(long absoluteTime, int channel, int pitchWheel)
            : base(absoluteTime, channel, MidiCommandCode.PitchWheelChange)
        {
            Pitch = pitchWheel;
        }
        
        /// <summary>
        /// Describes this pitch wheel change event
        /// </summary>
        /// <returns>String describing this pitch wheel change event</returns>
        public override string ToString() 
        {
            return String.Format("{0} Pitch {1} ({2})",
                base.ToString(),
                this.pitch,
                this.pitch - 0x2000);
        }

        /// <summary>
        /// Pitch Wheel Value 0 is minimum, 0x2000 (8192) is default, 0x3FFF (16383) is maximum
        /// </summary>
        public int Pitch
        {
            get
            {
                return pitch;
            }
            set
            {
                if (value < 0 || value >= 0x4000)
                {
                    throw new ArgumentOutOfRangeException("value", "Pitch value must be in the range 0 - 0x3FFF");
                }
                pitch = value;
            }
        }

        /// <summary>
        /// Gets a short message
        /// </summary>
        /// <returns>Integer to sent as short message</returns>
        public override int GetAsShortMessage()
        {
            return base.GetAsShortMessage() + ((pitch & 0x7f) << 8) + (((pitch >> 7) & 0x7f) << 16);
        }

        /// <summary>
        /// Calls base class export first, then exports the data 
        /// specific to this event
        /// <seealso cref="MidiEvent.Export">MidiEvent.Export</seealso>
        /// </summary>
        public override void Export(ref long absoluteTime, BinaryWriter writer)
        {
            base.Export(ref absoluteTime, writer);
            writer.Write((byte)(pitch & 0x7f));
            writer.Write((byte)((pitch >> 7) & 0x7f));
        }
    }
}