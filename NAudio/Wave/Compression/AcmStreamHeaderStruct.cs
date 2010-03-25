using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace NAudio.Wave.Compression
{
    /// <summary>
    /// Interop structure for ACM stream headers.
    /// ACMSTREAMHEADER 
    /// http://msdn.microsoft.com/en-us/library/dd742926%28VS.85%29.aspx
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    class AcmStreamHeaderStruct
    {
        public int cbStruct;
        public AcmStreamHeaderStatusFlags fdwStatus = 0;
        public IntPtr userData;
        public IntPtr sourceBufferPointer;
        public int sourceBufferLength;
        public int sourceBufferLengthUsed;
        public IntPtr sourceUserData;
        public IntPtr destBufferPointer;
        public int destBufferLength;
        public int destBufferLengthUsed = 0;
        public IntPtr destUserData;

        [MarshalAs(UnmanagedType.ByValArray,SizeConst=10)]
        public int[] reserved;

        /* not entirely sure this will work for x64 - take our chances for now and hope the C# marshalling code pins this array
        // have 10 members rather than an array just in case
        // the ACM driver uses this and the garbage collector moves it
        public int reserved0;
        public int reserved1;
        public int reserved2;
        public int reserved3;
        public int reserved4;
        public int reserved5;
        public int reserved6;
        public int reserved7;
        public int reserved8;
        public int reserved9;*/
    }
}
