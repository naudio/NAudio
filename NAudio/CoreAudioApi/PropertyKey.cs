using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// PROPERTYKEY is defined in wtypes.h
    /// </summary>
    public struct PropertyKey
    {
        /// <summary>
        /// Format ID
        /// </summary>
        public Guid formatId;
        /// <summary>
        /// Property ID
        /// </summary>
        public int propertyId;
    }
}
