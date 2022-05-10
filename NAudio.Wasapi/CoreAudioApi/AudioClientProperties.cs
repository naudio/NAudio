using System;
using System.Runtime.InteropServices;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// The AudioClientProperties structure is used to set the parameters that describe the properties of the client's audio stream.
    /// </summary>
    /// <remarks>https://docs.microsoft.com/en-us/windows/win32/api/audioclient/ns-audioclient-audioclientproperties-r1</remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct AudioClientProperties
    {
        /// <summary>
        /// The size of the buffer for the audio stream.
        /// </summary>
        public UInt32 cbSize;
        /// <summary>
        /// Boolean value to indicate whether or not the audio stream is hardware-offloaded
        /// </summary>
        public int bIsOffload;
        /// <summary>
        /// An enumeration that is used to specify the category of the audio stream.
        /// </summary>
        public AudioStreamCategory eCategory;
        /// <summary>
        /// A bit-field describing the characteristics of the stream. Supported in Windows 8.1 and later.
        /// </summary>
        public AudioClientStreamOptions Options;
    }
}