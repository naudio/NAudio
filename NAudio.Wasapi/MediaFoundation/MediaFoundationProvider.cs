using NAudio.Wave;
using System;
using System.IO;

namespace NAudio.MediaFoundation
{
	public class MediaFoundationProvider : IWaveProvider
	{
        private IMFByteStream byteStream;
        public WaveFormat WaveFormat { get; private set; }

        public int Read(byte[] buffer, int offset, int count) {
			if (count + offset > buffer.Length) throw new ArgumentException("The offset and the count are too large");
			byte[] _buffer = new byte[count];
			byteStream.Read(ref _buffer, count, out int readcount);
			for (int i = 0; i <readcount; i++)
            {
				buffer[i + offset] = _buffer[i];//Fills the buffer with the data in IMFByteStream.
            }
			return readcount;
		}
		/// <summary>
        /// Initialize the MediaFoundationProvider with specific file.
        /// </summary>
		public MediaFoundationProvider(string url)
        {
			if (!File.Exists(url)) throw new FileNotFoundException("This file doesn't exist");
			MediaFoundationInterop.MFCreateSourceResolver(out IMFSourceResolver resolver);
			//Creates both IMFMediaSource and IMFByteStream.Uses the stream for 'Read' method and uses the source to collect format information.
			resolver.CreateObjectFromURL(url, (uint)(SourceResolverFlags.MF_RESOLUTION_MEDIASOURCE|SourceResolverFlags.MF_RESOLUTION_CONTENT_DOES_NOT_HAVE_TO_MATCH_EXTENSION_OR_MIME_TYPE), null, out MF_OBJECT_TYPE _t1, out object _source);
			resolver.CreateObjectFromURL(url, (uint)(SourceResolverFlags.MF_RESOLUTION_BYTESTREAM|SourceResolverFlags.MF_RESOLUTION_CONTENT_DOES_NOT_HAVE_TO_MATCH_EXTENSION_OR_MIME_TYPE), null, out MF_OBJECT_TYPE _t2, out object _stream);
			byteStream = _stream as IMFByteStream;
			IMFMediaSource source = _source as IMFMediaSource;
			source.CreatePresentationDescriptor(out IMFPresentationDescriptor descriptor);
			if (MediaFoundationInterop.MFRequireProtectedEnvironment(descriptor) == 0)//Is the media file protected.
			{
				throw new ArgumentException("The file is protected.");
			}
			descriptor.GetStreamDescriptorCount(out uint sdcount);
			for (uint i = 0; i < sdcount; i++)
			{
                descriptor.GetStreamDescriptorByIndex(i, out _, out IMFStreamDescriptor sd);
				sd.GetMediaTypeHandler(out IMFMediaTypeHandler typeHandler);
				typeHandler.GetCurrentMediaType(out IMFMediaType mediaType);
				mediaType.GetMajorType(out Guid streamtype);
				if (streamtype == MediaTypes.MFMediaType_Audio)
				{
					mediaType.GetUINT32(MediaFoundationAttributes.MF_MT_AUDIO_BITS_PER_SAMPLE, out int samplesize);
					mediaType.GetUINT32(MediaFoundationAttributes.MF_MT_AUDIO_NUM_CHANNELS, out int channelcount);
					mediaType.GetUINT32(MediaFoundationAttributes.MF_MT_AUDIO_SAMPLES_PER_SECOND, out int rate);//SampleRate
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