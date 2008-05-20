using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace NAudio.WASAPI.Interfaces
{
    [Guid("D666063F-1587-4E43-81F1-B948E807363F"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDevice
    {
        // activationParams is a propvariant
        int Activate(ref Guid id, ClsCtx clsCtx, IntPtr activationParams,
            out object interfacePointer);
        
        int OpenPropertyStore(int stgmAccess, out IPropertyStore properties);
        
        int GetId(out string id);
        
        int GetState(out int state);
    }

}
