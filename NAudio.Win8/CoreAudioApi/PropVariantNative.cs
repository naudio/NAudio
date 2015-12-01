using NAudio.CoreAudioApi.Interfaces;
using System.Runtime.InteropServices;

namespace NAudio.CoreAudioApi.Interfaces
{
    class PropVariantNative
    {
        // For Windows 8 we need to import api-ms-win-core-com-l1-1-0.dll, so we use this since we target 8.0
        // TODO Windows 8.1 requires api-ms-win-core-com-l1-1-1.dll for PropVariantClear
        [DllImport("api-ms-win-core-com-l1-1-0.dll")]
        internal static extern int PropVariantClear(ref PropVariant pvar);
    }
}
