using System.Linq;

namespace NAudio.Wave.Asio
{
    /// <summary>
    /// AsioDriverCapability holds all the information from the AsioDriver.
    /// Use AsioDriverExt to get the Capabilities
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

        /// <summary>
        /// All physical input channel indices as an int array: <c>[0, 1, ..., NbInputChannels - 1]</c>.
        /// Convenience for selecting every input in <c>AsioRecordingOptions.InputChannels</c> or
        /// <c>AsioDuplexOptions.InputChannels</c>.
        /// </summary>
        public int[] AllInputChannels => Enumerable.Range(0, NbInputChannels).ToArray();

        /// <summary>
        /// All physical output channel indices as an int array: <c>[0, 1, ..., NbOutputChannels - 1]</c>.
        /// Convenience for selecting every output in <c>AsioPlaybackOptions.OutputChannels</c> or
        /// <c>AsioDuplexOptions.OutputChannels</c>.
        /// </summary>
        public int[] AllOutputChannels => Enumerable.Range(0, NbOutputChannels).ToArray();
    }
}
