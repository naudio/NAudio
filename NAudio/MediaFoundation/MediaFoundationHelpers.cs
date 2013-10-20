using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using NAudio.Wave;

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
            int interfaceCount;
            MediaFoundationInterop.MFTEnumEx(category, _MFT_ENUM_FLAG.MFT_ENUM_FLAG_ALL,
                null, null, out interfacesPointer, out interfaceCount);
            var interfaces = new IMFActivate[interfaceCount];
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

        /// <summary>
        /// Creates a Media type
        /// </summary>
        public static IMFMediaType CreateMediaType()
        {
            IMFMediaType mediaType;
            MediaFoundationInterop.MFCreateMediaType(out mediaType);
            return mediaType;
        }

        /// <summary>
        /// Creates a media type from a WaveFormat
        /// </summary>
        public static IMFMediaType CreateMediaTypeFromWaveFormat(WaveFormat waveFormat)
        {
            var mediaType = CreateMediaType();
            try
            {
                MediaFoundationInterop.MFInitMediaTypeFromWaveFormatEx(mediaType, waveFormat, Marshal.SizeOf(waveFormat));
            }
            catch (Exception)
            {
                Marshal.ReleaseComObject(mediaType);
                throw;
            }
            return mediaType;
        }

        /// <summary>
        /// Creates a memory buffer of the specified size
        /// </summary>
        /// <param name="bufferSize">Memory buffer size in bytes</param>
        /// <returns>The memory buffer</returns>
        public static IMFMediaBuffer CreateMemoryBuffer(int bufferSize)
        {
            IMFMediaBuffer buffer;
            MediaFoundationInterop.MFCreateMemoryBuffer(bufferSize, out buffer);
            return buffer;
        }

        /// <summary>
        /// Creates a sample object
        /// </summary>
        /// <returns>The sample object</returns>
        public static IMFSample CreateSample()
        {
            IMFSample sample;
            MediaFoundationInterop.MFCreateSample(out sample);
            return sample;
        }

        /// <summary>
        /// Creates a new attributes store
        /// </summary>
        /// <param name="initialSize">Initial size</param>
        /// <returns>The attributes store</returns>
        public static IMFAttributes CreateAttributes(int initialSize)
        {
            IMFAttributes attributes;
            MediaFoundationInterop.MFCreateAttributes(out attributes, initialSize);
            return attributes;
        }

        /// <summary>
        /// Creates a media foundation byte stream based on a stream object
        /// (usable with WinRT streams)
        /// </summary>
        /// <param name="stream">The input stream</param>
        /// <returns>A media foundation byte stream</returns>
        public static IMFByteStream CreateByteStream(object stream)
        {
            IMFByteStream byteStream;
            MediaFoundationInterop.MFCreateMFByteStreamOnStreamEx(stream, out byteStream);
            return byteStream;
        }

        /// <summary>
        /// Creates a source reader based on a byte stream
        /// </summary>
        /// <param name="byteStream">The byte stream</param>
        /// <returns>A media foundation source reader</returns>
        public static IMFSourceReader CreateSourceReaderFromByteStream(IMFByteStream byteStream)
        {
            IMFSourceReader reader;
            MediaFoundationInterop.MFCreateSourceReaderFromByteStream(byteStream, null, out reader);
            return reader;
        }
    }
}
