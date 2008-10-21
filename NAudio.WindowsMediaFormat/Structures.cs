using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace NAudio.WindowsMediaFormat
{
    // wmsdkidl.h
    [StructLayout(LayoutKind.Sequential)]
    public class WM_MEDIA_TYPE
    {
        public Guid majortype;
        public Guid subtype;
        public bool bFixedSizeSamples;
        public bool bTemporalCompression;
        public uint lSampleSize;
        public Guid formattype;
        public object pUnk; //IUnknown *pUnk;
        public uint cbFormat;
        public IntPtr pbFormat; // BYTE *
    }
}
