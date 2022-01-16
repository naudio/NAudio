using System;

namespace NAudio.MediaFoundation
{
    public struct MFCLOCK_PROPERTIES
    {
        public ulong qwCorrelationRate;
        public Guid guidClockId;
        public uint dwClockFlags;
        public ulong qwClockFrequency;
        public uint dwClockTolerance;
        public uint dwClockJitter;       
    }
}