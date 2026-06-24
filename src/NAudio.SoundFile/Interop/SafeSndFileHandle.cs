using System;
using Microsoft.Win32.SafeHandles;

namespace NAudio.SoundFile
{
    /// <summary>
    /// Owns a libsndfile <c>SNDFILE*</c> handle. The runtime's critical
    /// finalizer guarantees <see cref="ReleaseHandle"/> runs (closing the
    /// file, which also flushes the encoder) even if a caller forgets to
    /// dispose, so there is no hand-written finalizer in the assembly and no
    /// native leak on the missed-dispose path.
    /// </summary>
    /// <remarks>
    /// <c>[LibraryImport]</c> cannot marshal a <see cref="System.Runtime.InteropServices.SafeHandle"/>
    /// (SYSLIB1051), so the raw <c>IntPtr</c> from
    /// <see cref="System.Runtime.InteropServices.SafeHandle.DangerousGetHandle"/>
    /// is passed to the P/Invokes. The owning reader/writer keeps a strong
    /// reference for the file's lifetime, so no call is ever in flight across
    /// the close.
    /// </remarks>
    internal sealed class SafeSndFileHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeSndFileHandle(IntPtr existingHandle)
            : base(ownsHandle: true)
        {
            SetHandle(existingHandle);
        }

        protected override bool ReleaseHandle()
        {
            return SndFileInterop.Close(handle) == 0;
        }
    }
}
