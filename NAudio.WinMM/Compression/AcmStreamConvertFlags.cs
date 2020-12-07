using System;

namespace NAudio.Wave
{
    [Flags]
    enum AcmStreamConvertFlags
    {
        /// <summary>
        /// ACM_STREAMCONVERTF_BLOCKALIGN
        /// </summary>
        BlockAlign = 0x00000004,
        /// <summary>
        /// ACM_STREAMCONVERTF_START
        /// </summary>
        Start = 0x00000010,
        /// <summary>
        /// ACM_STREAMCONVERTF_END
        /// </summary>
        End = 0x00000020,
    }
}
