using System;
using Microsoft.Win32.SafeHandles;

namespace NAudio.Wave.Alsa;

/// <summary>
/// Owns a <c>snd_pcm_t*</c> handle. The runtime's critical finalizer
/// guarantees <see cref="ReleaseHandle"/> runs (closing the device) even
/// if a caller forgets to dispose, so there is no hand-written finalizer
/// anywhere in the assembly and no native leak on the missed-Dispose path.
/// </summary>
/// <remarks>
/// <c>[LibraryImport]</c> cannot marshal a <see cref="System.Runtime.InteropServices.SafeHandle"/>
/// (SYSLIB1051), so the raw <c>IntPtr</c> from the inherited
/// <see cref="System.Runtime.InteropServices.SafeHandle.DangerousGetHandle"/>
/// is passed to the P/Invokes. The owning <c>AlsaPcm</c> keeps a strong
/// reference for the device's lifetime and the streaming worker is joined
/// before <see cref="System.Runtime.InteropServices.SafeHandle.Dispose()"/>
/// closes it, so no call is ever in flight across the close.
/// </remarks>
internal sealed class SafePcmHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public SafePcmHandle(IntPtr existingHandle)
        : base(ownsHandle: true)
    {
        SetHandle(existingHandle);
    }

    protected override bool ReleaseHandle()
    {
        return AlsaInterop.PcmClose(handle) == 0;
    }
}
