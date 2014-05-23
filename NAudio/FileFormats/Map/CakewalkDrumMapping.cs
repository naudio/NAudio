using System;

namespace NAudio.FileFormats.Map
{
    /// <summary>
    /// Represents an entry in a Cakewalk drum map
    /// </summary>
    public class CakewalkDrumMapping
    {
        /// <summary>
        /// User customisable note name
        /// </summary>
        public string NoteName { get; set; }

        /// <summary>
        /// Input MIDI note number
        /// </summary>
        public int InNote { get; set; }

        /// <summary>
        /// Output MIDI note number
        /// </summary>
        public int OutNote { get; set; }

        /// <summary>
        /// Output port
        /// </summary>
        public int OutPort { get; set; }

        /// <summary>
        /// Output MIDI Channel
        /// </summary>
        public int Channel { get; set; }

        /// <summary>
        /// Velocity adjustment
        /// </summary>
        public int VelocityAdjust { get; set; }

        /// <summary>
        /// Velocity scaling - in percent
        /// </summary>
        public float VelocityScale { get; set; }

        /// <summary>
        /// Describes this drum map entry
        /// </summary>
        public override string ToString()
        {
            return String.Format("{0} In:{1} Out:{2} Ch:{3} Port:{4} Vel+:{5} Vel:{6}%",
                NoteName, InNote, OutNote, Channel, OutPort, VelocityAdjust, VelocityScale*100);
        }
    }
}
