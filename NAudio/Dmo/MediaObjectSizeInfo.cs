using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.Dmo
{
    /// <summary>
    /// Media Object Size Info
    /// </summary>
    public class MediaObjectSizeInfo
    {
        /// <summary>
        /// Minimum Buffer Size, in bytes
        /// </summary>
        public int Size { get; private set; }

        /// <summary>
        /// Max Lookahead
        /// </summary>
        public int MaxLookahead { get; private set; }

        /// <summary>
        /// Alignment
        /// </summary>
        public int Alignment { get; private set; }

        /// <summary>
        /// Media Object Size Info
        /// </summary>
        public MediaObjectSizeInfo(int size, int maxLookahead, int alignment)
        {
            Size = size;
            MaxLookahead = maxLookahead;
            Alignment = alignment;
        }

        /// <summary>
        /// ToString
        /// </summary>        
        public override string ToString()
        {
            return String.Format("Size: {0}, Alignment {1}, MaxLookahead {2}",
                Size, Alignment, MaxLookahead);
        }

    }
}
