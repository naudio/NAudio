using System.Runtime.InteropServices;

namespace NAudio.CoreAudioApi.Interfaces
{
    class PropVariantNative
    {
        [DllImport("ole32.dll")]
        internal static extern int PropVariantClear(ref PropVariant pvar);
    }
}
