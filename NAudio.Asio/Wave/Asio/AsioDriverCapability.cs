using System;

namespace NAudio.Wave.Asio
{
    /// <summary>
    /// ASIODriverCapability holds all the information from the AsioDriver.
    /// Use ASIODriverExt to get the Capabilities
    /// </summary>
    public class AsioDriverCapability
    {
        /// <summary>
        /// Drive Name
        /// </summary>
        public string DriverName;

        /// <summary>
        /// Number of Input Channels
        /// </summary>
        public int NbInputChannels;

        /// <summary>
        /// Number of Output Channels
        /// </summary>
        public int NbOutputChannels;

        /// <summary>
        /// Input Latency
        /// </summary>
        public int InputLatency;
        /// <summary>
        /// Output Latency
        /// </summary>
        public int OutputLatency;

        /// <summary>
        /// Buffer Minimum Size
        /// </summary>
        public int BufferMinSize;
        /// <summary>
        /// Buffer Maximum Size
        /// </summary>
        public int BufferMaxSize;
        /// <summary>
        /// Buffer Preferred Size
        /// </summary>
        public int BufferPreferredSize;
        /// <summary>
        /// Buffer Granularity
        /// </summary>
        public int BufferGranularity;

        /// <summary>
        /// Sample Rate
        /// </summary>
        public double SampleRate;

        /// <summary>
        /// Input Channel Info
        /// </summary>
        public AsioChannelInfo[] InputChannelInfos;
        /// <summary>
        /// Output Channel Info
        /// </summary>
        public AsioChannelInfo[] OutputChannelInfos;
    }
}
