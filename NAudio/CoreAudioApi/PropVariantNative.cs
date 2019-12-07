using System;
using System.Runtime.InteropServices;

namespace NAudio.CoreAudioApi.Interfaces
{
    class PropVariantNative
    {
#if WINDOWS_UWP
        // Windows 10 requires api-ms-win-core-com-l1-1-1.dll
        [DllImport("api-ms-win-core-com-l1-1-1.dll")]
#else
        [DllImport("ole32.dll")]
#endif
        internal static extern int PropVariantClear(ref PropVariant pvar);

#if WINDOWS_UWP
        [DllImport("api-ms-win-core-com-l1-1-1.dll")]
#else
        [DllImport("ole32.dll")]
#endif
        internal static extern int PropVariantClear(IntPtr pvar);
    }
}
