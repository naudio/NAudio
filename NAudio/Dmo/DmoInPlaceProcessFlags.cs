using System;

namespace NAudio.Dmo
{
    /// <summary>
    /// DMO Inplace Process Flags
    /// </summary>
    [Flags]
    public enum DmoInPlaceProcessFlags
    {
        /// <summary>
        /// DMO_INPLACE_NORMAL 
        /// </summary>
        Normal = 0,
        /// <summary>
        /// DMO_INPLACE_ZERO
        /// </summary>
        Zero = 0x1
    }
}