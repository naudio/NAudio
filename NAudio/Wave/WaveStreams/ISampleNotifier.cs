using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.Wave
{
    public class ISampleNotifier
    {
        public event EventHandler Block;
        public event EventHandler<SampleEventArgs> Sample;
    }

    public class SampleEventArgs : EventArgs
    {
        public float left;
        public float right;
    }
}
