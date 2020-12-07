using System;
using System.Collections.Generic;
using System.IO;
namespace NAudio.SoundFont
{

    /// <summary>
    /// base class for structures that can read themselves
    /// </summary>
    internal abstract class StructureBuilder<T>
    {
        protected List<T> data;

        public StructureBuilder()
        {
            Reset();
        }

        public abstract T Read(BinaryReader br);
        public abstract void Write(BinaryWriter bw, T o);
        public abstract int Length { get; }

        public void Reset()
        {
            data = new List<T>();
        }

        public T[] Data => data.ToArray();
    }

}