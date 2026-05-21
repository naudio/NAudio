using System;

namespace NAudio.Dmo
{
    /// <summary>
    /// Defines flags that describe the status of a DMO input stream.
    /// </summary>
    /// <remarks>
    /// Windows SDK name: _DMO_INPUT_STATUS_FLAGS (mediaobj.h).
    /// See https://learn.microsoft.com/windows/win32/api/mediaobj/ne-mediaobj-_dmo_input_status_flags
    /// </remarks>
    [Flags]
    enum DmoInputStatusFlags
    {
        /// <summary>
        /// No flags set.
        /// </summary>
        None,
        /// <summary>
        /// The input stream can accept more data.
        /// </summary>
        /// <remarks>DMO_INPUT_STATUSF_ACCEPT_DATA</remarks>
        AcceptData = 0x1
    }
}
