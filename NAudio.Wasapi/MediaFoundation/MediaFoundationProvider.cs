using NAudio.Wave;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace NAudio.MediaFoundation
{
    public class MediaFoundationProvider : IWaveProvider
    {
        public IMFByteStream byteStream;
        public WaveFormat WaveFormat { get; private set; }

        public int Read(byte[] buffer, int offset, int count) {		
            if (count + offset > buffer.Length) throw new ArgumentException("The offset and the count are too large");
            MediaFoundationInterop.MFCreateMemoryBuffer(count, out IMFMediaBuffer mediabuffer);
            mediabuffer.Lock(out IntPtr pbuffer, out int length, out _);
            byteStream.Read(pbuffer, length, out int readcount);
            Marshal.Copy(pbuffer, buffer, offset, length);
            if(readcount < count)byteStream.SetCurrentPosition(0);
            byteStream.GetLength(out long _length);
            byteStream.GetCurrentPosition(out long p);
            return readcount;
        }
        /// <summary>
        /// Initialize the MediaFoundationProvider with specific file.
        /// </summary>
        public MediaFoundationProvider(string url)
        {
            MediaFoundationApi.Startup();
            if (!File.Exists(url)) throw new FileNotFoundException("This file doesn't exist");
            MediaFoundationInterop.MFCreateSourceResolver(out IMFSourceResolver resolver);
            //Creates both IMFMediaSource and IMFByteStream.Uses the stream for 'Read' method and uses the source to collect format information.
            resolver.CreateObjectFromURL(url, (uint)(SourceResolverFlags.MF_RESOLUTION_BYTESTREAM|SourceResolverFlags.MF_RESOLUTION_CONTENT_DOES_NOT_HAVE_TO_MATCH_EXTENSION_OR_MIME_TYPE), null, out _, out object _stream);
            byteStream = _stream as IMFByteStream;
            resolver.CreateObjectFromByteStream(byteStream, null, (uint)(SourceResolverFlags.MF_RESOLUTION_MEDIASOURCE | SourceResolverFlags.MF_RESOLUTION_CONTENT_DOES_NOT_HAVE_TO_MATCH_EXTENSION_OR_MIME_TYPE), null, out _, out object _source);
            IMFMediaSource source = _source as IMFMediaSource;
            source.CreatePresentationDescriptor(out IMFPresentationDescriptor descriptor);
            descriptor.GetStreamDescriptorCount(out uint sdcount);
            for (uint i = 0; i < sdcount; i++)
            {
                descriptor.GetStreamDescriptorByIndex(i, out _, out IMFStreamDescriptor sd);
                sd.GetMediaTypeHandler(out IMFMediaTypeHandler typeHandler);
                typeHandler.GetMediaTypeCount(out uint typecount);
                typeHandler.GetMediaTypeByIndex(0, out IMFMediaType mediaType);
                mediaType.GetMajorType(out Guid streamtype);
                if (streamtype == MediaTypes.MFMediaType_Audio)
                {
                    mediaType.GetUINT32(MediaFoundationAttributes.MF_MT_AUDIO_SAMPLES_PER_SECOND, out int rate);//SampleRate
                    mediaType.GetUINT32(MediaFoundationAttributes.MF_MT_AUDIO_NUM_CHANNELS, out int channelcount);
                    int samplesize;
                    try
                    {
                        mediaType.GetUINT32(MediaFoundationAttributes.MF_MT_AUDIO_BITS_PER_SAMPLE, out samplesize);
                    }
                    catch(COMException e)
                    {
                        if ((uint)e.HResult != 0xC00D36E6)
                            throw e;
                        else
                            samplesize = 8;
                    }
                    WaveFormat = new WaveFormat(rate, samplesize, channelcount);
                }
                else
                {
                    continue;
                }
            }
        }
    }
}