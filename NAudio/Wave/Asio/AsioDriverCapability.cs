using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.Wave.Asio
{
    /// <summary>
    /// ASIODriverCapability holds all the information from the ASIODriver.
    /// Use ASIODriverExt to get the Capabilities
    /// </summary>
    internal class AsioDriverCapability
    {
        public String DriverName;

        public int NbInputChannels;
        public int NbOutputChannels;

        public int InputLatency;
        public int OutputLatency;

        public int BufferMinSize;
        public int BufferMaxSize;
        public int BufferPreferredSize;
        public int BufferGranularity;

        public double SampleRate;

        public ASIOChannelInfo[] InputChannelInfos;
        public ASIOChannelInfo[] OutputChannelInfos;
    }
}
