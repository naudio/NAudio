using System;

namespace NAudio.Sdl2.Structures
{
    [Flags]
    public enum AudioConversion
    {
        None = 0,
        Frequency = 2,
        Format = 4,
        Channels = 8,
        Samples = 16,
        Any = 32
    }
}
