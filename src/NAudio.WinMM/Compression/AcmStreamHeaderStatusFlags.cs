using System;

namespace NAudio.Wave.Compression
{
    [Flags]
    enum AcmStreamHeaderStatusFlags
    {
        /// <summary>
        /// ACMSTREAMHEADER_STATUSF_DONE
        /// </summary>
        Done = 0x00010000,
        /// <summary>
        /// ACMSTREAMHEADER_STATUSF_PREPARED
        /// </summary>
        Prepared = 0x00020000,
        /// <summary>
        /// ACMSTREAMHEADER_STATUSF_INQUEUE
        /// </summary>
        InQueue = 0x00100000,
    }
}
