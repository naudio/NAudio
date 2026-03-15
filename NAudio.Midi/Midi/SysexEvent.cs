using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace NAudio.Midi 
{
    /// <summary>
    /// Represents a MIDI sysex message
    /// </summary>
    public class SysexEvent : MidiEvent 
    {
        private byte[] data;

        /// <summary>
        /// Creates a new sysex event
        /// </summary>
        public SysexEvent()
            : base(0, 1, MidiCommandCode.Sysex)
        {
            data = Array.Empty<byte>();
        }

        /// <summary>
        /// Creates a new sysex event with the specified payload.
        /// Payload data should not include the 0xF0 status byte or the 0xF7 terminator byte.
        /// </summary>
        /// <param name="absoluteTime">Absolute time of this event</param>
        /// <param name="data">Sysex payload bytes (excluding 0xF0/0xF7)</param>
        public SysexEvent(long absoluteTime, byte[] data)
            : base(absoluteTime, 1, MidiCommandCode.Sysex)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            this.data = (byte[])data.Clone();
        }

        /// <summary>
        /// Reads a sysex message from a MIDI stream
        /// </summary>
        /// <param name="br">Stream of MIDI data</param>
        /// <returns>a new sysex message</returns>
        public static SysexEvent ReadSysexEvent(BinaryReader br) 
        {
            SysexEvent se = new SysexEvent();
            //se.length = ReadVarInt(br);
            //se.data = br.ReadBytes(se.length);

            var sysexData = new List<byte>();
            bool loop = true;
            while(loop) 
            {
                byte b = br.ReadByte();
                if(b == 0xF7) 
                {
                    loop = false;
                }
                else 
                {
                    sysexData.Add(b);
                }
            }
            
            se.data = sysexData.ToArray();

            return se;
        }

        /// <summary>
        /// Creates a deep clone of this MIDI event.
        /// </summary>
        public override MidiEvent Clone()
        {
            var clone = (SysexEvent)MemberwiseClone();
            clone.data = (byte[])data?.Clone();
            return clone;
        }

        /// <summary>
        /// Describes this sysex message
        /// </summary>
        /// <returns>A string describing the sysex message</returns>
        public override string ToString() 
        {
            var sysexData = data ?? Array.Empty<byte>();
            var sb = new StringBuilder();
            foreach (byte b in sysexData)
            {
                sb.AppendFormat("{0:X2} ", b);
            }
            return $"{this.AbsoluteTime} Sysex: {sysexData.Length} bytes\r\n{sb}";
        }
        
        /// <summary>
        /// Calls base class export first, then exports the data 
        /// specific to this event
        /// <seealso cref="MidiEvent.Export">MidiEvent.Export</seealso>
        /// </summary>
        public override void Export(ref long absoluteTime, BinaryWriter writer)
        {
            base.Export(ref absoluteTime, writer);
            //WriteVarInt(writer,length);
            //writer.Write(data, 0, data.Length);
            var sysexData = data ?? Array.Empty<byte>();
            writer.Write(sysexData, 0, sysexData.Length);
            writer.Write((byte)0xF7);
        }
    }
}