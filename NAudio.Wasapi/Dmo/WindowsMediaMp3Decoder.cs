using System;
using System.Runtime.InteropServices;
using NAudio.Dmo.Interfaces;
using NAudio.Wasapi.CoreAudioApi;

namespace NAudio.Dmo
{
    // http://msdn.microsoft.com/en-us/library/ff819509%28VS.85%29.aspx
    // CLSID_CMP3DecMediaObject

    /// <summary>
    /// Legacy <c>[ComImport]</c> coclass marker for the Windows Media MP3 Decoder DMO.
    /// Retained as documentation; activation now goes via <see cref="ComActivation"/>.
    /// </summary>
    [ComImport, Guid("bbeea841-0a63-4f52-a7ab-a9b3a84ed38a")]
    class WindowsMediaMp3DecoderComObject
    {
    }

    /// <summary>
    /// Windows Media MP3 Decoder (as a DMO).
    /// Used internally by DmoMp3FrameDecompressor.
    /// </summary>
    public class WindowsMediaMp3Decoder : IDisposable
    {
        // CLSID_CMP3DecMediaObject
        private static readonly Guid Mp3DecoderClsid = new Guid("bbeea841-0a63-4f52-a7ab-a9b3a84ed38a");
        // IID_IMediaObject — mediaobj.h
        private static readonly Guid IID_IMediaObject = new Guid("d8ad0f58-5494-4102-97c5-ec798e59bcf4");

        MediaObject mediaObject;

        /// <summary>
        /// Creates a new Windows Media MP3 Decoder.
        /// </summary>
        /// <remarks>
        /// Activation goes via <see cref="ComActivation.CreateInstance{T}"/> rather than
        /// the legacy <c>[ComImport]</c> coclass path. The resulting wrapper is
        /// thread-agile, so the decoder can be constructed on one thread and consumed
        /// on another.
        /// </remarks>
        public WindowsMediaMp3Decoder()
        {
            var comInterface = ComActivation.CreateInstance<IMediaObject>(Mp3DecoderClsid, IID_IMediaObject);
            mediaObject = new MediaObject(comInterface);
        }

        /// <summary>
        /// Media Object
        /// </summary>
        public MediaObject MediaObject => mediaObject;

        #region IDisposable Members

        /// <summary>
        /// Releases the underlying COM objects.
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
