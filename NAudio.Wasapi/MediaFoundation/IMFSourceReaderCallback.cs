using System.Runtime.InteropServices;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// IMFSourceReaderCallback interface.
    /// https://docs.microsoft.com/en-us/windows/win32/api/mfreadwrite/nn-mfreadwrite-imfsourcereadercallback
    /// </summary>
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(true), ComImport, Guid("deec8d99-fa1d-4d82-84c2-2c8969944867")]
    public interface IMFSourceReaderCallback
    {
        /// <summary>
        /// Called when the IMFSourceReader::ReadSample method completes.
        /// </summary>
        void OnReadSample(uint hrStatus, uint dwStreamIndex, uint dwStreamFlags, long llTimestamp, IMFSample pSample);
        /// <summary>
        /// Called when the IMFSourceReader::Flush method completes.
        /// </summary>
        void OnFlush(uint dwStreamIndex);
        /// <summary>
        /// Called when the source reader receives certain events from the media source.
        /// </summary>
        void OnEvent(uint dwStreamIndex, IMFMediaEvent pEvent);
    }
}