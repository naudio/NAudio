using System;
using System.IO;

namespace NAudio.Midi
{
    /// <summary>
    /// SMPTE Offset Event
    /// </summary>
    public class SmpteOffsetEvent : MetaEvent
    {
        private readonly byte hours;
        private readonly byte minutes;
        private readonly byte seconds;
        private readonly byte frames;
        private readonly byte subFrames; // 100ths of a frame

        /// <summary>
        /// Creates a new time signature event
        /// </summary>
        public SmpteOffsetEvent(byte hours, byte minutes, byte seconds, byte frames, byte subFrames)
        {
            this.hours = hours;
            this.minutes = minutes;
            this.seconds = seconds;
            this.frames = frames;
            this.subFrames = subFrames;
        }

        /// <summary>
        /// Reads a new time signature event from a MIDI stream
        /// </summary>
        /// <param name="br">The MIDI stream</param>
        /// <param name="length">The data length</param>
        public SmpteOffsetEvent(BinaryReader br,int length) 
        {
            if(length != 5) 
            {
                throw new FormatException(String.Format("Invalid SMPTE Offset length: Got {0}, expected 5",length));
            }
            hours = br.ReadByte();
            minutes = br.ReadByte();
            seconds = br.ReadByte();
            frames = br.ReadByte();
            subFrames = br.ReadByte();
        }

        /// <summary>
        /// Creates a deep clone of this MIDI event.
        /// </summary>
        public override MidiEvent Clone() => (SmpteOffsetEvent)MemberwiseClone();

        /// <summary>
        /// Hours
        /// </summary>
        public int Hours => hours;

        /// <summary>
        /// Minutes
        /// </summary>
        public int Minutes => minutes;

        /// <summary>
        /// Seconds
        /// </summary>
        public int Seconds => seconds;

        /// <summary>
        /// Frames
        /// </summary>
        public int Frames => frames;

        /// <summary>
        /// SubFrames
        /// </summary>
        public int SubFrames => subFrames;


        /// <summary>
        /// Describes this time signature event
        /// </summary>
        /// <returns>A string describing this event</returns>
        public override string ToString() 
        {
            return String.Format("{0} {1}:{2}:{3}:{4}:{5}",
                base.ToString(),hours,minutes,seconds,frames,subFrames);
        }

        /// <summary>
        /// Calls base class export first, then exports the data 
        /// specific to this event
        /// <seealso cref="MidiEvent.Export">MidiEvent.Export</seealso>
        /// </summary>
        public override void Export(ref long absoluteTime, BinaryWriter writer)
        {
            base.Export(ref absoluteTime, writer);
            writer.Write(hours);
            writer.Write(minutes);
            writer.Write(seconds);
            writer.Write(frames);
            writer.Write(subFrames);
        }
    }
}

