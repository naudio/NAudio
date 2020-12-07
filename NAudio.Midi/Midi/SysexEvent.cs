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
        //private int length;
        
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

            List<byte> sysexData = new List<byte>();
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
        public override MidiEvent Clone() => new SysexEvent { data = (byte[])data?.Clone() };

        /// <summary>
        /// Describes this sysex message
        /// </summary>
        /// <returns>A string describing the sysex message</returns>
        public override string ToString() 
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in data)
            {
                sb.AppendFormat("{0:X2} ", b);
            }
            return String.Format("{0} Sysex: {1} bytes\r\n{2}",this.AbsoluteTime,data.Length,sb.ToString());
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
            writer.Write(data, 0, data.Length);
            writer.Write((byte)0xF7);
        }
    }
}