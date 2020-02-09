using System;

namespace NAudio.SoundFont
{
    /// <summary>
    /// SoundFont instrument
    /// </summary>
    public class Instrument
    {
        internal ushort startInstrumentZoneIndex;
        internal ushort endInstrumentZoneIndex;

        /// <summary>
        /// instrument name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Zones
        /// </summary>
        public Zone[] Zones { get; set; }

        /// <summary>
        /// <see cref="Object.ToString"/>
        /// </summary>
        public override string ToString() => Name;
    }
}