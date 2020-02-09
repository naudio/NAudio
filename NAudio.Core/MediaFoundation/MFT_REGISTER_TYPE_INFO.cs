using System;
using System.Runtime.InteropServices;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Contains media type information for registering a Media Foundation transform (MFT). 
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class MFT_REGISTER_TYPE_INFO 
    {
        /// <summary>
        /// The major media type.
        /// </summary>
        public Guid guidMajorType;
        /// <summary>
        /// The Media Subtype
        /// </summary>
        public Guid guidSubtype;
    }
}