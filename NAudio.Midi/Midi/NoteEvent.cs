using System;
using System.IO;
using System.Text;

namespace NAudio.Midi
{
    /// <summary>
    /// Represents a note MIDI event
    /// </summary>
    public class NoteEvent : MidiEvent
    {
        private int noteNumber;
        private int velocity;

        /// <summary>
        /// Reads a NoteEvent from a stream of MIDI data
        /// </summary>
        /// <param name="br">Binary Reader for the stream</param>
        public NoteEvent(BinaryReader br)
        {
            NoteNumber = br.ReadByte();
            velocity = br.ReadByte();
            // it seems it is possible for cubase
            // to output some notes with a NoteOff velocity > 127
            if (velocity > 127)
            {
                velocity = 127;
            }
        }

        /// <summary>
        /// Creates a MIDI Note Event with specified parameters
        /// </summary>
        /// <param name="absoluteTime">Absolute time of this event</param>
        /// <param name="channel">MIDI channel number</param>
        /// <param name="commandCode">MIDI command code</param>
        /// <param name="noteNumber">MIDI Note Number</param>
        /// <param name="velocity">MIDI Note Velocity</param>
        public NoteEvent(long absoluteTime, int channel, MidiCommandCode commandCode, int noteNumber, int velocity)
            : base(absoluteTime, channel, commandCode)
        {
            this.NoteNumber = noteNumber;
            this.Velocity = velocity;
        }

        private static readonly string[] NoteNames = new string[] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

        /// <summary>
        /// <see cref="MidiEvent.GetAsShortMessage" />
        /// </summary>
        public override int GetAsShortMessage()
        {
            return base.GetAsShortMessage() + (noteNumber << 8) + (velocity << 16);
        }

        /// <summary>
        /// The MIDI note number
        /// </summary>
        public virtual int NoteNumber
        {
            get
            {
                return noteNumber;
            }
            set
            {
                if (value < 0 || value > 127)
                {
                    throw new ArgumentOutOfRangeException("value", "Note number must be in the range 0-127");
                }
                noteNumber = value;
            }
        }

        /// <summary>
        /// The note velocity
        /// </summary>
        public int Velocity
        {
            get
            {
                return velocity;
            }
            set
            {
                if (value < 0 || value > 127)
                {
                    throw new ArgumentOutOfRangeException("value", "Velocity must be in the range 0-127");
                }
                velocity = value;
            }
        }

        /// <summary>
        /// The note name
        /// </summary>
        public string NoteName
        {
            get
            {
                if ((Channel == 16) || (Channel == 10))
                {
                    switch (noteNumber)
                    {
                        case 35: return "Acoustic Bass Drum";
                        case 36: return "Bass Drum 1";
                        case 37: return "Side Stick";
                        case 38: return "Acoustic Snare";
                        case 39: return "Hand Clap";
                        case 40: return "Electric Snare";
                        case 41: return "Low Floor Tom";
                        case 42: return "Closed Hi-Hat";
                        case 43: return "High Floor Tom";
                        case 44: return "Pedal Hi-Hat";
                        case 45: return "Low Tom";
                        case 46: return "Open Hi-Hat";
                        case 47: return "Low-Mid Tom";
                        case 48: return "Hi-Mid Tom";
                        case 49: return "Crash Cymbal 1";
                        case 50: return "High Tom";
                        case 51: return "Ride Cymbal 1";
                        case 52: return "Chinese Cymbal";
                        case 53: return "Ride Bell";
                        case 54: return "Tambourine";
                        case 55: return "Splash Cymbal";
                        case 56: return "Cowbell";
                        case 57: return "Crash Cymbal 2";
                        case 58: return "Vibraslap";
                        case 59: return "Ride Cymbal 2";
                        case 60: return "Hi Bongo";
                        case 61: return "Low Bongo";
                        case 62: return "Mute Hi Conga";
                        case 63: return "Open Hi Conga";
                        case 64: return "Low Conga";
                        case 65: return "High Timbale";
                        case 66: return "Low Timbale";
                        case 67: return "High Agogo";
                        case 68: return "Low Agogo";
                        case 69: return "Cabasa";
                        case 70: return "Maracas";
                        case 71: return "Short Whistle";
                        case 72: return "Long Whistle";
                        case 73: return "Short Guiro";
                        case 74: return "Long Guiro";
                        case 75: return "Claves";
                        case 76: return "Hi Wood Block";
                        case 77: return "Low Wood Block";
                        case 78: return "Mute Cuica";
                        case 79: return "Open Cuica";
                        case 80: return "Mute Triangle";
                        case 81: return "Open Triangle";
                        default: return String.Format("Drum {0}", noteNumber);
                    }
                }
                else
                {
                    int octave = noteNumber / 12;
                    return String.Format("{0}{1}", NoteNames[noteNumber % 12], octave);
                }
            }
        }

        /// <summary>
        /// Describes the Note Event
        /// </summary>
        /// <returns>Note event as a string</returns>
        public override string ToString()
        {
            return String.Format("{0} {1} Vel:{2}",
                base.ToString(),
                this.NoteName,
                this.Velocity);
        }

        /// <summary>
        /// <see cref="MidiEvent.Export"/>
        /// </summary>
        public override void Export(ref long absoluteTime, BinaryWriter writer)
        {
            base.Export(ref absoluteTime, writer);
            writer.Write((byte)noteNumber);
            writer.Write((byte)velocity);
        }
    }
}