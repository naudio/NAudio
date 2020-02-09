// ReSharper disable InconsistentNaming
namespace NAudio.Wave.Asio
{
    /// <summary>
    /// ASIO Message Selector
    /// </summary>
    public enum AsioMessageSelector
    {
        /// <summary>
        /// selector in &lt;value&gt;, returns 1L if supported,
        /// </summary>
        kAsioSelectorSupported = 1,
        /// <summary>
        /// returns engine (host) asio implementation version,
        /// </summary>
        kAsioEngineVersion,
        /// <summary>
        /// request driver reset. if accepted, this
        /// </summary>
        kAsioResetRequest,
        /// <summary>
        /// not yet supported, will currently always return 0L.
        /// </summary>
        kAsioBufferSizeChange,
        /// <summary>
        /// the driver went out of sync, such that
        /// </summary>
        kAsioResyncRequest,
        /// <summary>
        /// the drivers latencies have changed. The engine
        /// </summary>
        kAsioLatenciesChanged,
        /// <summary>
        /// if host returns true here, it will expect the
        /// </summary>
        kAsioSupportsTimeInfo,
        /// <summary>
        /// supports timecode
        /// </summary>
        kAsioSupportsTimeCode,
        /// <summary>
        /// unused - value: number of commands, message points to mmc commands
        /// </summary>
        kAsioMMCCommand,
        /// <summary>
        /// kAsioSupportsXXX return 1 if host supports this
        /// </summary>
        kAsioSupportsInputMonitor,
        /// <summary>
        /// unused and undefined
        /// </summary>
        kAsioSupportsInputGain,
        /// <summary>
        /// unused and undefined
        /// </summary>
        kAsioSupportsInputMeter,
        /// <summary>
        /// unused and undefined
        /// </summary>
        kAsioSupportsOutputGain,
        /// <summary>
        /// unused and undefined
        /// </summary>
        kAsioSupportsOutputMeter,
        /// <summary>
        /// driver detected an overload
        /// </summary>
        kAsioOverload,
    }
}