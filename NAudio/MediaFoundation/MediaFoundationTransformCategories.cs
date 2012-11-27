using System;
using System.Collections.Generic;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Media Foundation Transform Categories
    /// </summary>
    public static class MediaFoundationTransformCategories
    {
        /// <summary>
        /// Describes the category
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public static string Describe(Guid category)
        {
            string description;
            if (!descriptions.TryGetValue(category, out description))
                throw new ArgumentException("Unknown category");
            return description;
        }
        /// <summary>
        /// MFT_CATEGORY_VIDEO_DECODER
        /// </summary>
        public static readonly Guid VideoDecoder = new Guid("{d6c02d4b-6833-45b4-971a-05a4b04bab91}");
        /// <summary>
        /// MFT_CATEGORY_VIDEO_ENCODER
        /// </summary>
        public static readonly Guid VideoEncoder = new Guid("{f79eac7d-e545-4387-bdee-d647d7bde42a}");
        /// <summary>
        /// MFT_CATEGORY_VIDEO_EFFECT
        /// </summary>
        public static readonly Guid VideoEffect = new Guid("{12e17c21-532c-4a6e-8a1c-40825a736397}");
        /// <summary>
        /// MFT_CATEGORY_MULTIPLEXER
        /// </summary>
        public static readonly Guid Multiplexer = new Guid("{059c561e-05ae-4b61-b69d-55b61ee54a7b}");
        /// <summary>
        /// MFT_CATEGORY_DEMULTIPLEXER
        /// </summary>
        public static readonly Guid Demultiplexer = new Guid("{a8700a7a-939b-44c5-99d7-76226b23b3f1}");
        /// <summary>
        /// MFT_CATEGORY_AUDIO_DECODER
        /// </summary>
        public static readonly Guid AudioDecoder = new Guid("{9ea73fb4-ef7a-4559-8d5d-719d8f0426c7}");
        /// <summary>
        /// MFT_CATEGORY_AUDIO_ENCODER
        /// </summary>
        public static readonly Guid AudioEncoder = new Guid("{91c64bd0-f91e-4d8c-9276-db248279d975}");
        /// <summary>
        /// MFT_CATEGORY_AUDIO_EFFECT
        /// </summary>
        public static readonly Guid AudioEffect = new Guid("{11064c48-3648-4ed0-932e-05ce8ac811b7}");
        /// <summary>
        /// MFT_CATEGORY_VIDEO_PROCESSOR
        /// </summary>
        public static readonly Guid VideoProcessor = new Guid("{302EA3FC-AA5F-47f9-9F7A-C2188BB16302}");
        /// <summary>
        /// MFT_CATEGORY_OTHER
        /// </summary>
        public static readonly Guid Other = new Guid("{90175d57-b7ea-4901-aeb3-933a8747756f}");

        private static Dictionary<Guid, string> descriptions = new Dictionary<Guid, string>()
                                                                   {
                                                                       { AudioDecoder, "Audio Decoder"},
                                                                       { AudioEffect, "Audio Effect"},
                                                                       { AudioEncoder, "Audio Encoder"},
                                                                       { VideoDecoder, "Video Decoder"},
                                                                       { VideoEffect, "Video Effect"},
                                                                       { VideoProcessor, "Video Processor"},
                                                                       { VideoEncoder, "Video Encoder"},
                                                                       { Multiplexer, "Multiplexer"},
                                                                       { Demultiplexer, "Demultiplexer"},
                                                                       { Other, "Other"},
                                                                   };
    }
}