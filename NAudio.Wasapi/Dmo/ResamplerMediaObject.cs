using System;
using NAudio.Dmo.Interfaces;
using NAudio.Wasapi.CoreAudioApi;

namespace NAudio.Dmo
{
    /// <summary>
    /// DMO Resampler
    /// </summary>
    public class DmoResampler : IDisposable
    {
        // CLSID_CResamplerMediaObject — wmcodecdsp.h
        private static readonly Guid ResamplerClsid = new Guid("f447b69e-1884-4a7e-8055-346f74d6edb3");
        // IID_IMediaObject — mediaobj.h
        private static readonly Guid IID_IMediaObject = new Guid("d8ad0f58-5494-4102-97c5-ec798e59bcf4");

        MediaObject mediaObject;

        /// <summary>
        /// Creates a new Resampler based on the DMO Resampler.
        /// </summary>
        /// <remarks>
        /// Activation goes via <see cref="ComActivation.CreateInstance{T}"/> rather than
        /// the legacy <c>[ComImport]</c> coclass / RCW path. The resulting wrapper is
        /// thread-agile, so a <see cref="DmoResampler"/> constructed on (e.g.) a WPF
        /// STA thread can be safely consumed from a background MTA audio thread.
        /// </remarks>
        public DmoResampler()
        {
            var comInterface = ComActivation.CreateInstance<IMediaObject>(ResamplerClsid, IID_IMediaObject);
            mediaObject = new MediaObject(comInterface);
        }

        /// <summary>
        /// Media Object
        /// </summary>
        public MediaObject MediaObject => mediaObject;

        #region IDisposable Members

        /// <summary>
        /// Releases the underlying COM object.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if (mediaObject != null)
            {
                mediaObject.Dispose();
                mediaObject = null;
            }
        }

        #endregion
    }
}
