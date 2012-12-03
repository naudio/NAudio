using System;
using System.Runtime.InteropServices;
using System.Text;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.MediaFoundation;
using NAudio.Utils;

namespace NAudioWpfDemo.MediaFoundationEncode
{
    internal class MediaTypeViewModel
    {
        public MediaTypeViewModel()
        {
            
        }

        public MediaTypeViewModel(MediaType mediaType)
        {
            this.MediaType = mediaType;
            this.Name = ShortDescription(mediaType.MediaFoundationObject);
            this.Description = DescribeMediaType(mediaType.MediaFoundationObject);
        }

        public MediaType MediaType { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        private int TryGetUINT32(IMFAttributes att, Guid key)
        {
            int intValue = -1;
            try
            {
                att.GetUINT32(key, out intValue);
            }
            catch (COMException exception)
            {
                if (exception.ErrorCode == MediaFoundationErrors.MF_E_ATTRIBUTENOTFOUND)
                {
                    // not a problem, return the default
                }
                else if (exception.ErrorCode == MediaFoundationErrors.MF_E_INVALIDTYPE)
                {
                    throw new ArgumentException("Not a UINT32 parameter");
                }
                else
                {
                    throw;
                }
            }
            return intValue;
        }

        private string ShortDescription(IMFMediaType mediaType)
        {
            Guid subType;
            mediaType.GetGUID(MediaFoundationAttributes.MF_MT_SUBTYPE, out subType);
            int sampleRate;
            mediaType.GetUINT32(MediaFoundationAttributes.MF_MT_AUDIO_SAMPLES_PER_SECOND, out sampleRate);
            int bytesPerSecond;
            mediaType.GetUINT32(MediaFoundationAttributes.MF_MT_AUDIO_AVG_BYTES_PER_SECOND, out bytesPerSecond);
            int channels;
            mediaType.GetUINT32(MediaFoundationAttributes.MF_MT_AUDIO_NUM_CHANNELS, out channels);

            int bitsPerSample = TryGetUINT32(mediaType, MediaFoundationAttributes.MF_MT_AUDIO_BITS_PER_SAMPLE);

            //int bitsPerSample;
            //mediaType.GetUINT32(MediaFoundationAttributes.MF_MT_AUDIO_BITS_PER_SAMPLE, out bitsPerSample);
            var shortDescription = new StringBuilder();
            shortDescription.AppendFormat("{0:0.#}kbps, ", (8 * bytesPerSecond) / 1000M);
            shortDescription.AppendFormat("{0:0.#}kHz, ", sampleRate / 1000M);
            if (bitsPerSample != -1)
                shortDescription.AppendFormat("{0} bit, ", bitsPerSample);
            shortDescription.AppendFormat("{0}, ", channels == 1 ? "mono" : channels == 2 ? "stereo" : channels.ToString() + " channels");
            if (subType == AudioSubtypes.MFAudioFormat_AAC)
            {
                int payloadType = TryGetUINT32(mediaType, MediaFoundationAttributes.MF_MT_AAC_PAYLOAD_TYPE);
                if (payloadType != -1)
                    shortDescription.AppendFormat("Payload Type: {0}, ", (AacPayloadType)payloadType);
            }
            shortDescription.Length -= 2;
            return shortDescription.ToString();
        }

        private string DescribeMediaType(IMFMediaType mediaType)
        {
            int attributeCount;
            mediaType.GetCount(out attributeCount);
            var sb = new StringBuilder();
            for (int n = 0; n < attributeCount; n++)
            {
                Guid key;
                var val = new PropVariant();
                mediaType.GetItemByIndex(n, out key, ref val);
                string propertyName = FieldDescriptionHelper.Describe(typeof(MediaFoundationAttributes), key);
                sb.AppendFormat("{0}={1}\r\n", propertyName, val.Value);
                val.Clear();
            }
            return sb.ToString();
        }
    }
}