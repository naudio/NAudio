using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.WindowsMediaFormat
{
    public class CodecFormat
    {
        public Codec Codec {get;private set;}
        public WMStreamConfig StreamConfig { get; private set; }
        public int Index { get; private set; }
        public string Description
        {
            get
            {
                StringBuilder name;
                int namelen = 0;
                Guid mediaType = Codec.MediaType;
                IWMStreamConfig config;

                Codec.CodecInformation.GetCodecFormatDesc(mediaType, (int)Codec.Index, Index, out config, null, ref namelen);
                name = new StringBuilder(namelen);
                Codec.CodecInformation.GetCodecFormatDesc(mediaType, (int)Codec.Index, Index, out config,  name, ref namelen);
                return name.ToString();
            }
        }

        public CodecFormat(Codec codec, WMStreamConfig wmStreamConfig, int index)
        {
            Codec = codec;
            StreamConfig = wmStreamConfig;
            Index = index;
        }

        public IWMProfile GetProfile()
        {
            IWMProfile profile;
            ((IWMProfileManager)Codec.CodecInformation).CreateEmptyProfile(WMT_VERSION.WMT_VER_9_0, out profile);

            StreamConfig.StreamNumber = 1;

            profile.AddStream(StreamConfig.StreamConfig);

            return profile;
        }


        /// <summary>
        /// Gets all media formats for a codec.
        /// </summary>
        /// <param name="codec">Codec</param>
        /// <returns>All media formats for the specified codec</returns>
        public static CodecFormat[] GetMediaFormats(Codec codec)
        {
            var codecInfo = codec.CodecInformation;

            Guid mediaType = codec.MediaType;
            int formatCount;
            codecInfo.GetCodecFormatCount(mediaType, codec.Index, out formatCount);

            var formats = new CodecFormat[formatCount];
            for (int i = 0; i < formatCount; i++)
            {
                IWMStreamConfig config;
                codecInfo.GetCodecFormat(mediaType , codec.Index,  i, out config);
                WMStreamConfig stream = new WMStreamConfig(config);
                formats[i] = new CodecFormat(codec, stream, (int)i);
            }

            return formats;
        }
    }
}
