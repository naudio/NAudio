using System;

namespace NAudio.Dmo
{
    /// <summary>
    /// DMO Process Output Flags
    /// </summary>
    [Flags]
    public enum DmoProcessOutputFlags
    {
        /// <summary>
        /// None
        /// </summary>
        None,
        /// <summary>
        /// DMO_PROCESS_OUTPUT_DISCARD_WHEN_NO_BUFFER
        /// </summary>
        DiscardWhenNoBuffer = 0x00000001
    }
}
