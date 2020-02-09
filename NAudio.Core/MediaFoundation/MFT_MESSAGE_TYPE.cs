using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Defines messages for a Media Foundation transform (MFT).
    /// </summary>
    public enum MFT_MESSAGE_TYPE
    {
        /// <summary>
        /// Requests the MFT to flush all stored data. 
        /// </summary>
        MFT_MESSAGE_COMMAND_FLUSH           = 0x00000000,
        /// <summary>
        /// Requests the MFT to drain any stored data.
        /// </summary>
        MFT_MESSAGE_COMMAND_DRAIN           = 0x00000001,
        /// <summary>
        /// Sets or clears the Direct3D Device Manager for DirectX Video Accereration (DXVA). 
        /// </summary>
        MFT_MESSAGE_SET_D3D_MANAGER         = 0x00000002,
        /// <summary>
        /// Drop samples - requires Windows 7
        /// </summary>
        MFT_MESSAGE_DROP_SAMPLES            = 0x00000003,
        /// <summary>
        /// Command Tick - requires Windows 8
        /// </summary>
        MFT_MESSAGE_COMMAND_TICK            = 0x00000004,
        /// <summary>
        /// Notifies the MFT that streaming is about to begin. 
        /// </summary>
        MFT_MESSAGE_NOTIFY_BEGIN_STREAMING  = 0x10000000,
        /// <summary>
        /// Notifies the MFT that streaming is about to end. 
        /// </summary>
        MFT_MESSAGE_NOTIFY_END_STREAMING    = 0x10000001,
        /// <summary>
        /// Notifies the MFT that an input stream has ended. 
        /// </summary>
        MFT_MESSAGE_NOTIFY_END_OF_STREAM    = 0x10000002,
        /// <summary>
        /// Notifies the MFT that the first sample is about to be processed. 
        /// </summary>
        MFT_MESSAGE_NOTIFY_START_OF_STREAM  = 0x10000003,
        /// <summary>
        /// Marks a point in the stream. This message applies only to asynchronous MFTs. Requires Windows 7 
        /// </summary>
        MFT_MESSAGE_COMMAND_MARKER          = 0x20000000
    }
}
