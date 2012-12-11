using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.FileFormats.Map
{
    /// <summary>
    /// Represents an entry in a Cakewalk drum map
    /// </summary>
    public class CakewalkDrumMapping
    {
        string noteName;
        int inNote;
        int outNote;
        int outPort;
        int channel;
        int velocityAdjust;
        float velocityScale;

        /// <summary>
        /// User customisable note name
        /// </summary>
        public string NoteName
        {
            get { return noteName; }
            set { noteName = value; }
        }

        /// <summary>
        /// Input MIDI note number
        /// </summary>
        public int InNote
        {
            get { return inNote; }
            set { inNote = value; }
        }

        /// <summary>
        /// Output MIDI note number
        /// </summary>
        public int OutNote
        {
            get { return outNote; }
            set { outNote = value; }
        }

        /// <summary>
        /// Output port
        /// </summary>
        public int OutPort
        {
            get { return outPort; }
            set { outPort = value; }
        }

        /// <summary>
        /// Output MIDI Channel
        /// </summary>
        public int Channel
        {
            get { return channel; }
            set { channel = value; }
        }

        /// <summary>
        /// Velocity adjustment
        /// </summary>
        public int VelocityAdjust
        {
          get { return velocityAdjust; }
          set { velocityAdjust = value; }
        }

        /// <summary>
        /// Velocity scaling - in percent
        /// </summary>
        public float VelocityScale
        {
            get { return velocityScale; }
            set { velocityScale = value; }
        }

        /// <summary>
        /// Describes this drum map entry
        /// </summary>
        public override string ToString()
        {
            return String.Format("{0} In:{1} Out:{2} Ch:{3} Port:{4} Vel+:{5} Vel:{6}%",
                noteName, inNote, outNote, channel, outPort, velocityAdjust, VelocityScale*100);
        }

    }
}
