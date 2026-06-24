namespace NAudio.Wave
{
    /// <summary>
    /// Lifecycle states for an <see cref="AsioDevice"/>.
    /// </summary>
    public enum AsioDeviceState
    {
        /// <summary>
        /// Device is open but no <c>Init*</c> method has been called yet.
        /// </summary>
        Unconfigured,

        /// <summary>
        /// An <c>Init*</c> method has succeeded but <see cref="AsioDevice.Start"/> has not yet been called.
        /// </summary>
        Configured,

        /// <summary>
        /// The driver is currently running and delivering buffer callbacks.
        /// </summary>
        Running,

        /// <summary>
        /// The driver was started and has since been stopped. Can be restarted via <see cref="AsioDevice.Start"/>.
        /// If the stop was caused by a driver fault, the exception is delivered via the <see cref="AsioDevice.Stopped"/>
        /// event's <see cref="StoppedEventArgs.Exception"/>.
        /// </summary>
        Stopped,

        /// <summary>
        /// The device has been disposed. All further operations throw <see cref="System.ObjectDisposedException"/>.
        /// </summary>
        Disposed
    }
}
