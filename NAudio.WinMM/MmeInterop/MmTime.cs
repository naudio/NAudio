using System;
using System.Runtime.InteropServices;

namespace NAudio.Wave
{
    /// <summary>
    /// MmTime
    /// http://msdn.microsoft.com/en-us/library/dd757347(v=VS.85).aspx
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct MmTime
    {
        /// <summary>
        /// Time in milliseconds.
        /// </summary>
        public const int TIME_MS = 0x0001;
        /// <summary>
        /// Number of waveform-audio samples.
        /// </summary>
        public const int TIME_SAMPLES = 0x0002;
        /// <summary>
        /// Current byte offset from beginning of the file.
        /// </summary>
        public const int TIME_BYTES = 0x0004;

        /// <summary>
        /// Time format.
        /// </summary>
        [FieldOffset(0)]
        public UInt32 wType;
        /// <summary>
        /// Number of milliseconds. Used when wType is TIME_MS.
        /// </summary>
        [FieldOffset(4)]
        public UInt32 ms;
        /// <summary>
        /// Number of samples. Used when wType is TIME_SAMPLES.
        /// </summary>
        [FieldOffset(4)]
        public UInt32 sample;
        /// <summary>
        /// Byte count. Used when wType is TIME_BYTES.
        /// </summary>
        [FieldOffset(4)]
        public UInt32 cb;
        /// <summary>
        /// Ticks in MIDI stream. Used when wType is TIME_TICKS.
        /// </summary>
        [FieldOffset(4)]
        public UInt32 ticks;
        /// <summary>
        /// SMPTE time structure - hours. Used when wType is TIME_SMPTE.
        /// </summary>
        [FieldOffset(4)]
        public Byte smpteHour;
        /// <summary>
        /// SMPTE time structure - minutes. Used when wType is TIME_SMPTE.
        /// </summary>
        [FieldOffset(5)]
        public Byte smpteMin;
        /// <summary>
        /// SMPTE time structure - seconds. Used when wType is TIME_SMPTE.
        /// </summary>
        [FieldOffset(6)]
        public Byte smpteSec;
        /// <summary>
        /// SMPTE time structure - frames. Used when wType is TIME_SMPTE.
        /// </summary>
        [FieldOffset(7)]
        public Byte smpteFrame;
        /// <summary>
        /// SMPTE time structure - frames per second. Used when wType is TIME_SMPTE.
        /// </summary>
        [FieldOffset(8)]
        public Byte smpteFps;
        /// <summary>
        /// SMPTE time structure - dummy byte for alignment. Used when wType is TIME_SMPTE.
        /// </summary>
        [FieldOffset(9)]
        public Byte smpteDummy;
        /// <summary>
        /// SMPTE time structure - padding. Used when wType is TIME_SMPTE.
        /// </summary>
        [FieldOffset(10)]
        public Byte smptePad0;
        /// <summary>
        /// SMPTE time structure - padding. Used when wType is TIME_SMPTE.
        /// </summary>
        [FieldOffset(11)]
        public Byte smptePad1;
        /// <summary>
        /// MIDI time structure. Used when wType is TIME_MIDI.
        /// </summary>
        [FieldOffset(4)]
        public UInt32 midiSongPtrPos;
    }
}
