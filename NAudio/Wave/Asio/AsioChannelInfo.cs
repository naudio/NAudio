using System.Runtime.InteropServices;

namespace NAudio.Wave.Asio
{
    /// <summary>
    /// ASIO Channel Info
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
    public struct AsioChannelInfo
    {
        /// <summary>
        /// on input, channel index
        /// </summary>
        public int channel;
        /// <summary>
        /// Is Input
        /// </summary>
        public bool isInput; 
        /// <summary>
        /// Is Active
        /// </summary>
        public bool isActive; 
        /// <summary>
        /// Channel Info
        /// </summary>
        public int channelGroup;
        /// <summary>
        /// ASIO Sample Type
        /// </summary>
        [MarshalAs(UnmanagedType.U4)]
        public AsioSampleType type;
        /// <summary>
        /// Name
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string name;
    };
}