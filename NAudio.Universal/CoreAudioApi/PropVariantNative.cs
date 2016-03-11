using System;
using System.Runtime.InteropServices;

namespace NAudio.CoreAudioApi.Interfaces
{
    class PropVariantNative
    {
        // Windows 10 requires api-ms-win-core-com-l1-1-1.dll
        [DllImport("api-ms-win-core-com-l1-1-1.dll")]
        internal static extern int PropVariantClear(ref PropVariant pvar);

        [DllImport("api-ms-win-core-com-l1-1-1.dll")]
        internal static extern int PropVariantClear(IntPtr pvar);

    }
}
