using System;
using System.IO;

namespace NAudio.SoundFont
{
    /// <summary>
    /// SoundFont Version Structure
    /// </summary>
    public class SFVersion
    {

        /// <summary>
        /// Major Version
        /// </summary>
        public ushort Major { get; set; }

        /// <summary>
        /// Minor Version
        /// </summary>
        public ushort Minor { get; set; }
    }
}