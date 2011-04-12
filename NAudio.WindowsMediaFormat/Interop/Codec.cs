using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.WindowsMediaFormat
{
    public class Codec
    {
        public IWMCodecInfo2 CodecInformation { get; set; }
        public Guid MediaType {get;set;}

        public string Name
        {
            get
            {
                StringBuilder name;
                int namelen = 0;

                CodecInformation.GetCodecName(MediaType, Index, null, ref namelen);
                name = new StringBuilder(namelen);
                CodecInformation.GetCodecName(MediaType, Index, name, ref namelen);
                return name.ToString();
            }
        }

        public uint Index {get; private set;}
        public CodecFormat[] CodecFormats {get; private set;}

        public Codec(IWMCodecInfo2 codecInfo, uint index, Guid mediaType)
        {
            CodecInformation = codecInfo;
            Index = index;
            MediaType = mediaType;

            CodecFormats = CodecFormat.GetMediaFormats(this);
        }

        /// <summary>
        /// Gets all Windows media Codecs.
        /// </summary>
        /// <param name="guid">MediaTypes WMMEDIATYPE_Audio or WMMEDIATYPE_Video expected</param>
        public static Codec[] GetCodecs(Guid mediaType)
        {
            
            IWMCodecInfo2 codecInfo = (IWMCodecInfo2)WM.CreateProfileManager();

            uint count;
            codecInfo.GetCodecInfoCount(ref mediaType, out count);
            var list = new Codec[count];
            for (uint i = 0; i < count; i++)
                list[i] = new Codec(codecInfo,i,mediaType);

            return list;
        }
    }
}
