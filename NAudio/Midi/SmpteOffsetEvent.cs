using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace NAudio.Midi
{
    class SmpteOffsetEvent : MetaEvent
    {
        private byte hours;
        private byte minutes;
        private byte seconds;
        private byte frames;
        private byte subFrames; // 100ths of a frame
        
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
        /// Hours
        /// </summary>
        public int Hours
        {
            get { return hours; }
        }

        /// <summary>
        /// Minutes
        /// </summary>
        public int Minutes
        {
            get { return minutes; }
        }

        /// <summary>
        /// Seconds
        /// </summary>
        public int Seconds
        {
            get { return seconds; }
        }

        /// <summary>
        /// Frames
        /// </summary>
        public int Frames
        {
            get { return frames; }
        }

        /// <summary>
        /// SubFrames
        /// </summary>
        public int SubFrames
        {
            get { return subFrames; }
        }

        
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

