// ReSharper disable InconsistentNaming
namespace NAudio.Wave.Asio
{
    /// <summary>
    /// ASIO Error Codes
    /// </summary>
    public enum AsioError
    {
        /// <summary>
        /// This value will be returned whenever the call succeeded
        /// </summary>
        ASE_OK = 0,
        /// <summary>
        /// unique success return value for ASIOFuture calls
        /// </summary>
        ASE_SUCCESS = 0x3f4847a0,
        /// <summary>
        /// hardware input or output is not present or available
        /// </summary>
        ASE_NotPresent = -1000,
        /// <summary>
        /// hardware is malfunctioning (can be returned by any ASIO function)
        /// </summary>
        ASE_HWMalfunction,
        /// <summary>
        /// input parameter invalid
        /// </summary>
        ASE_InvalidParameter,
        /// <summary>
        /// hardware is in a bad mode or used in a bad mode
        /// </summary>
        ASE_InvalidMode,
        /// <summary>
        /// hardware is not running when sample position is inquired
        /// </summary>
        ASE_SPNotAdvancing,
        /// <summary>
        /// sample clock or rate cannot be determined or is not present
        /// </summary>
        ASE_NoClock,
        /// <summary>
        /// not enough memory for completing the request
        /// </summary>
        ASE_NoMemory
    }
}