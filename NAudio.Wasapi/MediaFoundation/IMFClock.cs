namespace NAudio.MediaFoundation
{
    /// <summary>
    /// IMFClock interface
    /// https://docs.microsoft.com/en-us/windows/win32/api/mfidl/nn-mfidl-imfclock
    /// </summary>
    public interface IMFClock
    {
        /// <summary>
        /// Retrieves the characteristics of the clock.
        /// </summary>
        void GetClockCharacteristics(out uint pdwCharacteristics) ;
        /// <summary>
        /// Retrieves the last clock time that was correlated with system time.
        /// </summary>
        void GetCorrelatedTime(uint dwReserved,out long pllClockTime,out long phnsSystemTime) ;
        /// <summary>
        /// Retrieves the clock's continuity key. (Not supported.).
        /// </summary>
        void GetContinuityKey(out uint pdwContinuityKey) ;
        /// <summary>
        /// Retrieves the current state of the clock.
        /// </summary>
        void GetState(uint dwReserved, out MFCLOCK_STATE peClockState) ;
        /// <summary>
        /// Retrieves the properties of the clock.
        /// </summary>
        void GetProperties(out MFCLOCK_PROPERTIES pClockProperties) ;
    }
}