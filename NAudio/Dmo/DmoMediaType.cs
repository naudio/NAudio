using System;
using System.Collections.Generic;
using System.Text;
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
        
        public Guid MajorType
        {
            get { return majortype; }
        }

        public string MajorTypeName
        {
            get { return MediaTypes.GetMediaTypeName(majortype); }
        }

        public Guid SubType
        {
            get { return subtype; }
        }

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

        public bool FixedSizeSamples
        {
            get { return bFixedSizeSamples; }
        }

        public int SampleSize
        {
            get { return lSampleSize; }
        }

        public Guid FormatType
        {
            get { return formattype; }
        }

        public string FormatTypeName
        {
            get
            {
                if(formattype == DmoMediaTypeGuids.FORMAT_None)
                {
                    return "None";
                }
                else if (formattype == Guid.Empty)
                {
                    return "Null";
                }
                else if(formattype == DmoMediaTypeGuids.FORMAT_WaveFormatEx)
                {
                    return "WaveFormatEx";
                }
                else
                {
                    return FormatType.ToString();
                }
            }
        }

        public WaveFormat GetWaveFormat()
        {
            if (formattype == DmoMediaTypeGuids.FORMAT_WaveFormatEx)
            {
                WaveFormat waveFormat = new WaveFormat();
                Marshal.PtrToStructure(pbFormat, waveFormat);
                return waveFormat;
            }
            else
            {
                throw new InvalidOperationException("Not a WaveFormat type");
            }
        }

        public void SetWaveFormat(WaveFormat waveFormat)
        {
            majortype = MediaTypes.MEDIATYPE_Audio;
            // TODO: support WAVEFORMATEXTENSIBLE and reject invalid
            subtype = waveFormat.Encoding == WaveFormatEncoding.Pcm ? AudioMediaSubtypes.MEDIASUBTYPE_PCM :
                AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT;


            formattype = DmoMediaTypeGuids.FORMAT_WaveFormatEx;
            if (cbFormat < 18)
                throw new InvalidOperationException("Not enough memory assigned for a WaveFormat structure");
            Marshal.StructureToPtr(waveFormat, pbFormat, false);
        }
    }
}
