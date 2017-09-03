using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.CoreAudioApi.Interfaces
{
    /// <summary>
    /// MMDevice STGM enumeration
    /// </summary>
    public enum StorageAccessMode
    {
        /// <summary>
        /// Read-only access mode.
        /// </summary>
        Read,
        /// <summary>
        /// Write-only access mode.
        /// </summary>
        Write,
        /// <summary>
        /// Read-write access mode.
        /// </summary>
        ReadWrite
    }
}
