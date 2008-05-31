using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace NAudio.Dmo
{
    [Guid("59eff8b9-938c-4a26-82f2-95cb84cdc837"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMediaBuffer
    {
        int SetLength(int length);
        
        int GetMaxLength(out int maxLength);
        
        int GetBufferAndLength(out IntPtr bufferPointer, out int validDataLength);
    }
}
