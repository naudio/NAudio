using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.Wave
{
    /// <summary>
    /// An interface for WaveStreams which can report notification of individual samples
    /// </summary>
    public interface ISampleNotifier
    {
        /// <summary>
        /// A sample has been detected
        /// </summary>
        event EventHandler<SampleEventArgs> Sample;
    }

    /// <summary>
    /// Sample event arguments
    /// </summary>
    public class SampleEventArgs : EventArgs
    {
        /// <summary>
        /// Left sample
        /// </summary>
        public float Left { get; set; }
        /// <summary>
        /// Right sample
        /// </summary>
        public float Right { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public SampleEventArgs(float left, float right)
        {
            this.Left = left;
            this.Right = right;
        }
    }
}
