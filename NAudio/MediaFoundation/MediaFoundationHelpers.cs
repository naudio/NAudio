using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Main interface for using Media Foundation with NAudio
    /// </summary>
    public static class MediaFoundationApi
    {
        private static bool initialized;
        /// <summary>
        /// initializes MediaFoundation - only needs to be called once per process
        /// </summary>
        public static void Startup()
        {
            if (!initialized)
            {
                MediaFoundationInterop.MFStartup(MediaFoundationInterop.MF_VERSION, 0);
                initialized = true;
            }
        }

        /// <summary>
        /// Enumerate the installed MediaFoundation transforms in the specified category
        /// </summary>
        /// <param name="category">A category from MediaFoundationTransformCategories</param>
        /// <returns></returns>
        public static IEnumerable<IMFActivate> EnumerateTransforms(Guid category)
        {
            IntPtr interfacesPointer;
            IMFActivate[] interfaces;
            int interfaceCount;
            MediaFoundationInterop.MFTEnumEx(category, _MFT_ENUM_FLAG.MFT_ENUM_FLAG_ALL,
                null, null, out interfacesPointer, out interfaceCount);
            interfaces = new IMFActivate[interfaceCount];
            for (int n = 0; n < interfaceCount; n++)
            {
                var ptr =
                    Marshal.ReadIntPtr(new IntPtr(interfacesPointer.ToInt64() + n*Marshal.SizeOf(interfacesPointer)));
                interfaces[n] = (IMFActivate) Marshal.GetObjectForIUnknown(ptr);
            }

            foreach (var i in interfaces)
            {
                yield return i;
            }
            foreach (var i in interfaces)
            {
                Marshal.ReleaseComObject(i);
            }
            Marshal.FreeCoTaskMem(interfacesPointer);
        }

        /// <summary>
        /// uninitializes MediaFoundation
        /// </summary>
        public static void Shutdown()
        {
            if (initialized)
            {
                MediaFoundationInterop.MFShutdown();
                initialized = false;
            }
        }
    }

    public static class MediaFoundationTransformCategories
    {
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
    }
}
