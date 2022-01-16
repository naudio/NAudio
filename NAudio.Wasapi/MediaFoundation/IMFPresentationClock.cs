using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace NAudio.MediaFoundation
{
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("868CE85C-8EA9-4f55-AB82-B009A910A805"),ComVisible(true)]
    public interface IMFPresentationClock
    {
        /// <summary>
        /// Retrieves the characteristics of the clock.
        /// </summary>
        void GetClockCharacteristics(out uint pdwCharacteristics);
        /// <summary>
        /// Retrieves the last clock time that was correlated with system time.
        /// </summary>
        void GetCorrelatedTime(uint dwReserved, out long pllClockTime, out long phnsSystemTime);
        /// <summary>
        /// Retrieves the clock's continuity key. (Not supported.).
        /// </summary>
        void GetContinuityKey(out uint pdwContinuityKey);
        /// <summary>
        /// Retrieves the current state of the clock.
        /// </summary>
        void GetState(uint dwReserved, out MFCLOCK_STATE peClockState);
        /// <summary>
        /// Retrieves the properties of the clock.
        /// </summary>
        void GetProperties(out MFCLOCK_PROPERTIES pClockProperties);
        /// <summary>
        /// Sets the time source for the presentation clock. The time source is the object that drives the clock by providing the current time.
        /// </summary>
        void SetTimeSource([MarshalAs(UnmanagedType.Interface)] object pTimeSource);
        /// <summary>
        /// Retrieves the clock's presentation time source.
        /// </summary>
        void GetTimeSource([MarshalAs(UnmanagedType.Interface)] out object ppTimeSource);
        /// <summary>
        /// Retrieves the latest clock time.
        /// </summary>
        void GetTime(out long phnsClockTime);
        /// <summary>
        /// Registers an object to be notified whenever the clock starts, stops, or pauses, or changes rate.
        /// </summary>
        void AddClockStateSink([MarshalAs(UnmanagedType.Interface)] object pStateSink);
        /// <summary>
        /// Unregisters an object that is receiving state-change notifications from the clock.
        /// </summary>
        void RemoveClockStateSink([MarshalAs(UnmanagedType.Interface)] object pStateSink);
        /// <summary>
        /// Starts the presentation clock.
        /// </summary>
        void Start(long llClockStartOffset);
        /// <summary>
        /// Stops the presentation clock. 
        /// While the clock is stopped, the clock time does not advance, and the clock's IMFPresentationClock::GetTime method returns zero.
        /// </summary>
        void Stop();
        /// <summary>
        /// Pauses the presentation clock. 
        /// While the clock is paused, the clock time does not advance, and the clock's IMFPresentationClock::GetTime returns the time at which the clock was paused.
        /// </summary>
        void Pause();
        
    }
}
