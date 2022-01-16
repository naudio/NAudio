using NAudio.Wave;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace NAudio.MediaFoundation
{
    public class MediaFoundationProvider : IWaveProvider
    {
        private long streamlength;
        private MemoryStream datastream;
        public WaveFormat WaveFormat { get; private set; }
        
        public int Read(byte[] buffer, int offset, int count) {		
            if (count + offset > buffer.Length) throw new ArgumentException("The offset and the count are too large");
            return datastream.Read(buffer, offset, count);
            
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
            resolver.CreateObjectFromURL(url, SourceResolverFlags.MF_RESOLUTION_BYTESTREAM|SourceResolverFlags.MF_RESOLUTION_CONTENT_DOES_NOT_HAVE_TO_MATCH_EXTENSION_OR_MIME_TYPE, null, out _, out object _stream);
            IMFByteStream byteStream = _stream as IMFByteStream;
            resolver.CreateObjectFromByteStream(byteStream, null, SourceResolverFlags.MF_RESOLUTION_MEDIASOURCE | SourceResolverFlags.MF_RESOLUTION_CONTENT_DOES_NOT_HAVE_TO_MATCH_EXTENSION_OR_MIME_TYPE, null, out _, out object _source);
            Marshal.FinalReleaseComObject(resolver);
            IMFMediaSource source = _source as IMFMediaSource;
            source.CreatePresentationDescriptor(out IMFPresentationDescriptor descriptor);
            descriptor.GetStreamDescriptorCount(out uint sdcount);
            for (uint i = 0; i < sdcount; i++)
            {
                descriptor.GetStreamDescriptorByIndex(i, out _, out IMFStreamDescriptor sd);
                sd.GetMediaTypeHandler(out IMFMediaTypeHandler typeHandler);
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
            byteStream.GetLength(out streamlength);
            byteStream.SetCurrentPosition(0);
            //Moves all the bytes in IMFByteStream to MemoryStream.
            MediaFoundationInterop.MFCreateMemoryBuffer(unchecked((int)streamlength), out IMFMediaBuffer mediabuffer);
            mediabuffer.Lock(out IntPtr pbuffer, out int length, out _);
            byteStream.Read(pbuffer, length, out _);
            byte[] buffer = new byte[length];
            Marshal.Copy(pbuffer, buffer,0, length);
            datastream = new MemoryStream(buffer);
        }
    }
}