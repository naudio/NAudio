using System;
using System.IO;
using System.Runtime.InteropServices;

namespace NAudio.MediaFoundation
{
    [ComDefaultInterface(typeof(IMFSourceReaderCallback))]
    public class SourceReaderCallback : IMFSourceReaderCallback
    {
        /// <summary>
        /// Called when the IMFSourceReader::ReadSample method completes.
        /// </summary>
        [AllowReversePInvokeCalls()]
        public void OnReadSample(uint hrStatus, uint dwStreamIndex, uint dwStreamFlags, long llTimestamp, IMFSample pSample)
        {
            if (pSample != null)
            {
                pSample.ConvertToContiguousBuffer(out IMFMediaBuffer samplebuffer);
                samplebuffer.Lock(out IntPtr _data, out int length, out _);
                byte[] data = new byte[length];
                Marshal.Copy(_data, data, 0, length);
                datastream.WriteAsync(data, 0, length);
                NewSample = true;
            }
        }
        /// <summary>
        /// Called when the IMFSourceReader::Flush method completes.
        /// </summary>
        public void OnFlush(uint dwStreamIndex)
        {

        }
        /// <summary>
        /// Called when the source reader receives certain events from the media source.
        /// </summary>
        public void OnEvent(uint dwStreamIndex, IMFMediaEvent pEvent)
        {
            pEvent.GetType(out MediaEventType type);
        }
        public int Read(out byte[] dest)
        {
            int length = unchecked((int)datastream.Length);
            byte[] _data = new byte[length];
            int readcount= datastream.Read(_data, 0, unchecked(length));
            dest = _data;
            NewSample = false;
            return readcount;
        }
        private MemoryStream datastream = new MemoryStream();
        public bool NewSample = false;
    }  
}