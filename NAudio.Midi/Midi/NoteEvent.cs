using System;
using System.Collections.Generic;
using System.IO;

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
            NoteNumber = noteNumber;
            Velocity = velocity;
        }

        private static readonly string[] NoteNames = new string[] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

        private static readonly Dictionary<int, string> DrumNoteNames = new Dictionary<int, string>
        {
            { 35, "Acoustic Bass Drum" },
            { 36, "Bass Drum 1" },
            { 37, "Side Stick" },
            { 38, "Acoustic Snare" },
            { 39, "Hand Clap" },
            { 40, "Electric Snare" },
            { 41, "Low Floor Tom" },
            { 42, "Closed Hi-Hat" },
            { 43, "High Floor Tom" },
            { 44, "Pedal Hi-Hat" },
            { 45, "Low Tom" },
            { 46, "Open Hi-Hat" },
            { 47, "Low-Mid Tom" },
            { 48, "Hi-Mid Tom" },
            { 49, "Crash Cymbal 1" },
            { 50, "High Tom" },
            { 51, "Ride Cymbal 1" },
            { 52, "Chinese Cymbal" },
            { 53, "Ride Bell" },
            { 54, "Tambourine" },
            { 55, "Splash Cymbal" },
            { 56, "Cowbell" },
            { 57, "Crash Cymbal 2" },
            { 58, "Vibraslap" },
            { 59, "Ride Cymbal 2" },
            { 60, "Hi Bongo" },
            { 61, "Low Bongo" },
            { 62, "Mute Hi Conga" },
            { 63, "Open Hi Conga" },
            { 64, "Low Conga" },
            { 65, "High Timbale" },
            { 66, "Low Timbale" },
            { 67, "High Agogo" },
            { 68, "Low Agogo" },
            { 69, "Cabasa" },
            { 70, "Maracas" },
            { 71, "Short Whistle" },
            { 72, "Long Whistle" },
            { 73, "Short Guiro" },
            { 74, "Long Guiro" },
            { 75, "Claves" },
            { 76, "Hi Wood Block" },
            { 77, "Low Wood Block" },
            { 78, "Mute Cuica" },
            { 79, "Open Cuica" },
            { 80, "Mute Triangle" },
            { 81, "Open Triangle" }
        };

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
                    if (DrumNoteNames.TryGetValue(noteNumber, out var drumNoteName))
                    {
                        return drumNoteName;
                    }

                    return $"Drum {noteNumber}";
                }
                else
                {
                    int octave = noteNumber / 12;
                    return $"{NoteNames[noteNumber % 12]}{octave}";
                }
            }
        }

        /// <summary>
        /// Describes the Note Event
        /// </summary>
        /// <returns>Note event as a string</returns>
        public override string ToString()
        {
            return $"{base.ToString()} {NoteName} Vel:{Velocity}";
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