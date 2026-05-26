using System;
using System.Runtime.InteropServices;

namespace NAudio.Wave.DirectSoundInterop
{
    /// <summary>
    /// DSBUFFERDESC — describes the characteristics of a new DirectSound buffer being
    /// created via <see cref="IDirectSound.CreateSoundBuffer"/>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal struct BufferDescription
    {
        public int dwSize;
        [MarshalAs(UnmanagedType.U4)]
        public DirectSoundBufferCaps dwFlags;
        public uint dwBufferBytes;
        public int dwReserved;
        public IntPtr lpwfxFormat;
        public Guid guidAlgo;
    }

    /// <summary>
    /// DSBCAPS — capabilities of an existing DirectSound buffer, returned by
    /// <see cref="IDirectSoundBuffer.GetCaps"/>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal struct BufferCaps
    {
        public int dwSize;
        public int dwFlags;
        public int dwBufferBytes;
        public int dwUnlockTransferRate;
        public int dwPlayCpuOverhead;
    }

    /// <summary>
    /// DSBPOSITIONNOTIFY — pairs a buffer offset with an event handle that will be
    /// signalled when the play cursor reaches that offset.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct DirectSoundBufferPositionNotify
    {
        public uint dwOffset;
        public IntPtr hEventNotify;
    }

    internal enum DirectSoundCooperativeLevel : uint
    {
        DSSCL_NORMAL = 0x00000001,
        DSSCL_PRIORITY = 0x00000002,
        DSSCL_EXCLUSIVE = 0x00000003,
        DSSCL_WRITEPRIMARY = 0x00000004
    }

    [Flags]
    internal enum DirectSoundPlayFlags : uint
    {
        DSBPLAY_LOOPING = 0x00000001,
        DSBPLAY_LOCHARDWARE = 0x00000002,
        DSBPLAY_LOCSOFTWARE = 0x00000004,
        DSBPLAY_TERMINATEBY_TIME = 0x00000008,
        DSBPLAY_TERMINATEBY_DISTANCE = 0x000000010,
        DSBPLAY_TERMINATEBY_PRIORITY = 0x000000020
    }

    internal enum DirectSoundBufferLockFlag : uint
    {
        None = 0,
        FromWriteCursor = 0x00000001,
        EntireBuffer = 0x00000002
    }

    [Flags]
    internal enum DirectSoundBufferStatus : uint
    {
        DSBSTATUS_PLAYING = 0x00000001,
        DSBSTATUS_BUFFERLOST = 0x00000002,
        DSBSTATUS_LOOPING = 0x00000004,
        DSBSTATUS_LOCHARDWARE = 0x00000008,
        DSBSTATUS_LOCSOFTWARE = 0x00000010,
        DSBSTATUS_TERMINATED = 0x00000020
    }

    [Flags]
    internal enum DirectSoundBufferCaps : uint
    {
        DSBCAPS_PRIMARYBUFFER = 0x00000001,
        DSBCAPS_STATIC = 0x00000002,
        DSBCAPS_LOCHARDWARE = 0x00000004,
        DSBCAPS_LOCSOFTWARE = 0x00000008,
        DSBCAPS_CTRL3D = 0x00000010,
        DSBCAPS_CTRLFREQUENCY = 0x00000020,
        DSBCAPS_CTRLPAN = 0x00000040,
        DSBCAPS_CTRLVOLUME = 0x00000080,
        DSBCAPS_CTRLPOSITIONNOTIFY = 0x00000100,
        DSBCAPS_CTRLFX = 0x00000200,
        DSBCAPS_STICKYFOCUS = 0x00004000,
        DSBCAPS_GLOBALFOCUS = 0x00008000,
        DSBCAPS_GETCURRENTPOSITION2 = 0x00010000,
        DSBCAPS_MUTE3DATMAXDISTANCE = 0x00020000,
        DSBCAPS_LOCDEFER = 0x00040000
    }
}
