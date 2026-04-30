using System.Runtime.InteropServices;

namespace NAudio.Wave
{
    /// <summary>
    /// Exception thrown by DirectSound API operations. Inherits from
    /// <see cref="COMException"/> for backwards compatibility with code that catches
    /// <see cref="COMException"/> from the legacy <c>[ComImport]</c> path (which auto-threw
    /// on failure HRESULTs via <c>PreserveSig=false</c>). The
    /// <c>[GeneratedComInterface]</c> migration moved DirectSound to explicit
    /// <c>[PreserveSig] int</c>-returning slots, so HRESULT inspection is now done via
    /// <see cref="ThrowIfFailed"/>.
    /// </summary>
    public class DirectSoundException : COMException
    {
        /// <summary>
        /// Creates a new <see cref="DirectSoundException"/>.
        /// </summary>
        public DirectSoundException(int hresult)
            : base($"DirectSound error 0x{hresult:X8}", hresult)
        {
        }

        /// <summary>
        /// Throws a <see cref="DirectSoundException"/> if the HRESULT indicates failure.
        /// </summary>
        public static void ThrowIfFailed(int hresult)
        {
            if (hresult < 0)
                throw new DirectSoundException(hresult);
        }
    }
}
