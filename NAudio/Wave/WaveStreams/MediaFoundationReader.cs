using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.MediaFoundation;

namespace NAudio.Wave
{
    /// <summary>
    /// Class for reading any file that Media Foundation can play
    /// Will only work in Windows Vista and above
    /// Automatically converts to PCM
    /// If it is a video file with multiple audio streams, it will pick out the first audio stream
    /// </summary>
    public class MediaFoundationReader : WaveStream
    {
        private IMFSourceReader pReader;
        private WaveFormat waveFormat;
        private long position;
        private long length;

        /// <summary>
        /// Creates a new MediaFoundationReader based on the supplied file
        /// </summary>
        /// <param name="file">Filename</param>
        public MediaFoundationReader(string file)
        {
            MediaFoundationApi.Startup();
            var uri = new Uri(file);
            MediaFoundationInterop.MFCreateSourceReaderFromURL(uri.AbsoluteUri, null, out pReader);
            pReader.SetStreamSelection(MediaFoundationInterop.MF_SOURCE_READER_ALL_STREAMS, false);
            pReader.SetStreamSelection(MediaFoundationInterop.MF_SOURCE_READER_FIRST_AUDIO_STREAM, true);
            
            /*IMFMediaType currentMediaType;
            pReader.GetCurrentMediaType(MediaFoundationInterop.MF_SOURCE_READER_FIRST_AUDIO_STREAM, out currentMediaType);
            Guid currentMajorType;
            currentMediaType.GetMajorType(out currentMajorType);
            IMFMediaType nativeMediaType;
            pReader.GetNativeMediaType(MediaFoundationInterop.MF_SOURCE_READER_FIRST_AUDIO_STREAM, 0, out nativeMediaType);*/

            // Create a partial media type indicating that we want uncompressed PCM audio
            IMFMediaType partialMediaType;
            MediaFoundationInterop.MFCreateMediaType(out partialMediaType);
            partialMediaType.SetGUID(MediaFoundationInterop.MF_MT_MAJOR_TYPE, MediaTypes.MFMediaType_Audio);
            partialMediaType.SetGUID(MediaFoundationInterop.MF_MT_SUBTYPE, AudioSubtypes.MFAudioFormat_PCM);

            // set the media type
            pReader.SetCurrentMediaType(MediaFoundationInterop.MF_SOURCE_READER_FIRST_AUDIO_STREAM, IntPtr.Zero, partialMediaType);
            Marshal.ReleaseComObject(partialMediaType);

            // now let's find out what we actually got
            IMFMediaType uncompressedMediaType;
            pReader.GetCurrentMediaType(MediaFoundationInterop.MF_SOURCE_READER_FIRST_AUDIO_STREAM, out uncompressedMediaType);

            // Two ways to query it, first is to ask for properties (section is to convet into WaveFormatEx using MFCreateWaveFormatExFromMFMediaType)
            Guid actualMajorType;
            uncompressedMediaType.GetGUID(MediaFoundationInterop.MF_MT_MAJOR_TYPE, out actualMajorType);
            Debug.Assert(actualMajorType == MediaTypes.MFMediaType_Audio);
            Guid audioSubType;
            uncompressedMediaType.GetGUID(MediaFoundationInterop.MF_MT_SUBTYPE, out audioSubType);
            Debug.Assert(audioSubType == AudioSubtypes.MFAudioFormat_PCM);
            int channels;
            uncompressedMediaType.GetUINT32(MediaFoundationInterop.MF_MT_AUDIO_NUM_CHANNELS, out channels);
            int bits;
            uncompressedMediaType.GetUINT32(MediaFoundationInterop.MF_MT_AUDIO_BITS_PER_SAMPLE, out bits);
            int sampleRate;
            uncompressedMediaType.GetUINT32(MediaFoundationInterop.MF_MT_AUDIO_SAMPLES_PER_SECOND, out sampleRate);

            waveFormat = new WaveFormat(sampleRate, bits, channels);

            pReader.SetStreamSelection(MediaFoundationInterop.MF_SOURCE_READER_FIRST_AUDIO_STREAM, true);
            length = GetLength();
        }

        private long GetLength()
        {
            CoreAudioApi.Interfaces.PropVariant variant;
            // http://msdn.microsoft.com/en-gb/library/windows/desktop/dd389281%28v=vs.85%29.aspx#getting_file_duration
            pReader.GetPresentationAttribute(MediaFoundationInterop.MF_SOURCE_READER_MEDIASOURCE,
                MediaFoundationInterop.MF_PD_DURATION, out variant);
            var lengthInBytes = (((long)variant.Value) * waveFormat.AverageBytesPerSecond) / 10000000L;
            variant.Clear();
            return lengthInBytes;
        }

        private byte[] decoderOutputBuffer;
        private int decoderOutputOffset;
        private int decoderOutputCount;

        private void EnsureBuffer(int bytesRequired)
        {
            if (decoderOutputBuffer == null || decoderOutputBuffer.Length < bytesRequired)
            {
                decoderOutputBuffer = new byte[bytesRequired];
            }
        }

        /// <summary>
        /// Reads from this wave stream
        /// </summary>
        /// <param name="buffer">Buffer to read into</param>
        /// <param name="offset">Offset in buffer</param>
        /// <param name="count">Bytes required</param>
        /// <returns>Number of bytes read; 0 indicates end of stream</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesWritten = 0;
            // read in any leftovers from last time
            if (decoderOutputCount > 0)
            {
                bytesWritten += ReadFromDecoderBuffer(buffer, offset, count - bytesWritten);
            }

            while (bytesWritten < count)
            {
                IMFSample pSample;
                int dwFlags;
                ulong timestamp;
                int actualStreamIndex;
                pReader.ReadSample(MediaFoundationInterop.MF_SOURCE_READER_FIRST_AUDIO_STREAM, 0, out actualStreamIndex, out dwFlags, out timestamp, out pSample);
                if (dwFlags != 0)
                {
                    // reached the end of the stream or media type changed
                    break;
                }/*
                if (dwFlags & MF_SOURCE_READERF_CURRENTMEDIATYPECHANGED)
                {
                    printf("Type change - not supported by WAVE file format.\n");
                    break;
                }
                if (dwFlags & MF_SOURCE_READERF_ENDOFSTREAM)
                {
                    printf("End of input file.\n");
                    break;
                }*/

                IMFMediaBuffer pBuffer;
                pSample.ConvertToContiguousBuffer(out pBuffer);
                IntPtr pAudioData = IntPtr.Zero;
                int cbBuffer;
                int pcbMaxLength;
                pBuffer.Lock(out pAudioData, out pcbMaxLength, out cbBuffer);
                EnsureBuffer(cbBuffer);
                Marshal.Copy(pAudioData, decoderOutputBuffer, 0, cbBuffer);
                decoderOutputOffset = 0;
                decoderOutputCount = cbBuffer;

                bytesWritten += ReadFromDecoderBuffer(buffer, offset + bytesWritten, count - bytesWritten);


                pBuffer.Unlock();
                Marshal.ReleaseComObject(pBuffer);
                Marshal.ReleaseComObject(pSample);
            }
            position += bytesWritten;
            return bytesWritten;
        }

        private int ReadFromDecoderBuffer(byte[] buffer, int offset, int needed)
        {
            int bytesFromDecoderOutput = Math.Min(needed, decoderOutputCount);
            Array.Copy(decoderOutputBuffer, decoderOutputOffset, buffer, offset, bytesFromDecoderOutput);
            decoderOutputOffset += bytesFromDecoderOutput;
            decoderOutputCount -= bytesFromDecoderOutput;
            if (decoderOutputCount == 0)
            {
                decoderOutputOffset = 0;
            }
            return bytesFromDecoderOutput;
        }

        /// <summary>
        /// WaveFormat of this stream (n.b. this is after converting to PCM)
        /// </summary>
        public override WaveFormat WaveFormat
        {
            get { return waveFormat; }
        }

        /// <summary>
        /// The bytesRequired of this stream in bytes (n.b may not be accurate)
        /// </summary>
        public override long Length
        {
            get
            {
                return length;
            }
        }

        /// <summary>
        /// Current position within this stream
        /// </summary>
        public override long Position
        {
            get { return position; }
            set
            {
                // should pass in a variant of type VT_I8 which is a long containing time in 100nanosecond units
                long nsPosition = (10000000L * value) / waveFormat.AverageBytesPerSecond;
                var pv = PropVariant.FromLong(nsPosition);
                pReader.SetCurrentPosition(Guid.Empty, ref pv);
                position = value;
                decoderOutputCount = 0;
                decoderOutputOffset = 0;
            }
        }

        /// <summary>
        /// Cleans up after finishing with this reader
        /// </summary>
        /// <param name="disposing">true if called from Dispose</param>
        protected override void Dispose(bool disposing)
        {
            if (pReader != null)
            {
                Marshal.ReleaseComObject(pReader);
                pReader = null;
            }
            base.Dispose(disposing);
        }
    }
}
