using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using NAudio.Dmo.Interfaces;
using NAudio.Wasapi.CoreAudioApi;

namespace NAudio.Dmo.Effect
{
    /// <summary>
    /// Shared activation logic for the nine effect DMOs (Echo, Chorus, Flanger,
    /// Compressor, Distortion, Gargle, ParamEq, WavesReverb, I3DL2Reverb).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="MediaObject"/> and <see cref="MediaObjectInPlace"/> are activated via
    /// <see cref="ComActivation"/> and produce thread-agile wrappers.
    /// </para>
    /// <para>
    /// The per-effect <c>IDirectSoundFX*</c> interface (used for parameter
    /// get/set) remains <c>[ComImport]</c> per <c>DmoModernization.md</c> — effects
    /// are typically created and consumed on the same thread, so the legacy
    /// thread-affine RCW is acceptable here. The legacy view is obtained via
    /// <c>Marshal.QueryInterface</c> on the same underlying COM object so all
    /// three views share one native instance.
    /// </para>
    /// </remarks>
    internal static class DmoEffectActivation
    {
        // IID_IMediaObject — mediaobj.h
        private static readonly Guid IID_IMediaObject = new Guid("d8ad0f58-5494-4102-97c5-ec798e59bcf4");

        public static (MediaObject MediaObject, MediaObjectInPlace MediaObjectInPlace, TFx EffectParams) Activate<TFx>(Guid clsid)
            where TFx : class
        {
            IntPtr unknown = ComActivation.CoCreateInstance(clsid, IID_IMediaObject);
            try
            {
                var imo = ComActivation.WrapUnique<IMediaObject>(unknown);
                var imoip = ComActivation.WrapUnique<IMediaObjectInPlace>(unknown);

                Guid fxIid = typeof(TFx).GUID;
                int hr = Marshal.QueryInterface(unknown, in fxIid, out IntPtr fxPtr);
                Marshal.ThrowExceptionForHR(hr);
                try
                {
                    var fx = (TFx)Marshal.GetObjectForIUnknown(fxPtr);
                    return (new MediaObject(imo), new MediaObjectInPlace(imoip), fx);
                }
                finally
                {
                    Marshal.Release(fxPtr);
                }
            }
            finally
            {
                Marshal.Release(unknown);
            }
        }
    }
}
