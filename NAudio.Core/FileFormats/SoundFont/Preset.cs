using System;

namespace NAudio.SoundFont
{
    /// <summary>
    /// A SoundFont Preset
    /// </summary>
    public class Preset
    {
        internal ushort startPresetZoneIndex;
        internal ushort endPresetZoneIndex;
        internal uint library;
        internal uint genre;
        internal uint morphology;

        /// <summary>
        /// Preset name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Patch Number
        /// </summary>
        public ushort PatchNumber { get; set; }

        /// <summary>
        /// Bank number
        /// 0 - 127, GM percussion bank is 128
        /// </summary>
        public ushort Bank { get; set; }

        /// <summary>
        /// Zones
        /// </summary>
        public Zone[] Zones { get; set; }

        /// <summary>
        /// <see cref="Object.ToString"/>
        /// </summary>
        public override string ToString()
        {
            return $"{Bank}-{PatchNumber} {Name}";
        }
    }
}