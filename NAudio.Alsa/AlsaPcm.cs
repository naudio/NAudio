using NAudio.Wave;
using System;

public abstract class AlsaPcm
{
    protected const uint PERIOD_QUANTITY = 8;
    protected const ulong PERIOD_SIZE = 1024;
    protected IntPtr Handle = default;
    protected bool isInitialized = false;
    public int Card { get; private set; }
    public uint Device { get; private set; }
    public string Id { get; private set; }
    public string Name { get; private set; }

}