using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace NAudio.Midi
{
    /// <summary>
    /// Utility class for comparing MidiEvent objects
    /// </summary>
    public class MidiEventComparer : IComparer<MidiEvent>
    {
        #region IComparer<MidiEvent> Members

        /// <summary>
        /// Compares two MidiEvents
        /// Sorts by time, with EndTrack always sorted to the end
        /// </summary>
        public int Compare(MidiEvent x, MidiEvent y)
        {
            long xTime = x.AbsoluteTime;
            long yTime = y.AbsoluteTime;

            if (xTime == yTime)
            {
                // sort meta events before note events, except end track
                MetaEvent xMeta = x as MetaEvent;
                MetaEvent yMeta = y as MetaEvent;

                if (xMeta != null)
                {
                    if (xMeta.MetaEventType == MetaEventType.EndTrack)
                        xTime = Int64.MaxValue;
                    else
                        xTime = Int64.MinValue;
                }
                if (yMeta != null)
                {
                    if (yMeta.MetaEventType == MetaEventType.EndTrack)
                        yTime = Int64.MaxValue;
                    else
                        yTime = Int64.MinValue;
                }
            }
            return xTime.CompareTo(yTime);
        }

        #endregion
    }
}