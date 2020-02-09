using System;
using NAudio.Wave;
using System.Runtime.InteropServices;

namespace NAudio.Dmo
{
    /// <summary>
    /// http://msdn.microsoft.com/en-us/library/aa929922.aspx
    /// DMO_MEDIA_TYPE 
    /// </summary>
    public struct DmoMediaType
    {
        Guid majortype;
        Guid subtype;
        bool bFixedSizeSamples;
        bool bTemporalCompression;
        int lSampleSize;
        Guid formattype;
        IntPtr pUnk; // not used
        int cbFormat;
        IntPtr pbFormat; 
        
        /// <summary>
        /// Major type
        /// </summary>
        public Guid MajorType => majortype;

        /// <summary>
        /// Major type name
        /// </summary>
        public string MajorTypeName => MediaTypes.GetMediaTypeName(majortype);

        /// <summary>
        /// Subtype
        /// </summary>
        public Guid SubType => subtype;

        /// <summary>
        /// Subtype name
        /// </summary>
        public string SubTypeName
        {
            get
            {
                if (majortype == MediaTypes.MEDIATYPE_Audio)
                {
                    return AudioMediaSubtypes.GetAudioSubtypeName(subtype);
                }
                return subtype.ToString();
            }
        }

        /// <summary>
        /// Fixed size samples
        /// </summary>
        public bool FixedSizeSamples => bFixedSizeSamples;

        /// <summary>
        /// Sample size
        /// </summary>
        public int SampleSize => lSampleSize;

        /// <summary>
        /// Format type
        /// </summary>
        public Guid FormatType => formattype;

        /// <summary>
        /// Format type name
        /// </summary>
        public string FormatTypeName
        {
            get
            {
                if(formattype == DmoMediaTypeGuids.FORMAT_None)
                {
                    return "None";
                }
                if (formattype == Guid.Empty)
                {
                    return "Null";
                }
                if(formattype == DmoMediaTypeGuids.FORMAT_WaveFormatEx)
                {
                    return "WaveFormatEx";
                }
                return FormatType.ToString();
            }
        }

        /// <summary>
        /// Gets the structure as a Wave format (if it is one)
        /// </summary>        
        public WaveFormat GetWaveFormat()
        {
            if (formattype == DmoMediaTypeGuids.FORMAT_WaveFormatEx)
            {                
                return WaveFormat.MarshalFromPtr(pbFormat);
            }
            throw new InvalidOperationException("Not a WaveFormat type");
        }

        /// <summary>
        /// Sets this object up to point to a wave format
        /// </summary>
        /// <param name="waveFormat">Wave format structure</param>
        public void SetWaveFormat(WaveFormat waveFormat)
        {
            majortype = MediaTypes.MEDIATYPE_Audio;
            
            var wfe = waveFormat as WaveFormatExtensible;
            if (wfe != null)
            {
                subtype = wfe.SubFormat;
            }
            else
            {
                switch (waveFormat.Encoding)
                {
                    case WaveFormatEncoding.Pcm:
                        subtype = AudioMediaSubtypes.MEDIASUBTYPE_PCM;
                        break;
                    case WaveFormatEncoding.IeeeFloat:
                        subtype = AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT;
                        break;
                    case WaveFormatEncoding.MpegLayer3:
                        subtype = AudioMediaSubtypes.WMMEDIASUBTYPE_MP3;
                        break;
                    default:
                        throw new ArgumentException($"Not a supported encoding {waveFormat.Encoding}");
                }
            }
            bFixedSizeSamples = SubType == AudioMediaSubtypes.MEDIASUBTYPE_PCM || SubType == AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT;
            formattype = DmoMediaTypeGuids.FORMAT_WaveFormatEx;
            if (cbFormat < Marshal.SizeOf(waveFormat))
                throw new InvalidOperationException("Not enough memory assigned for a WaveFormat structure");
            //Debug.Assert(cbFormat >= ,"Not enough space");
            Marshal.StructureToPtr(waveFormat, pbFormat, false);
        }
    }
}
