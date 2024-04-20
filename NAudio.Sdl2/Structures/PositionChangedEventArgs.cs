using System;

namespace NAudio.Sdl2.Structures;

public class PositionChangedEventArgs : EventArgs
{
    public ulong Position { get; }

    public PositionChangedEventArgs(ulong positionMs)
    {
        Position = positionMs;
    }
}