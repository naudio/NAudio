using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
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
        /// Initializes Media Foundation - only needs to be called once per process
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
        /// Enumerate the installed Media Foundation transforms in the specified category
        /// </summary>
        /// <param name="category">A category from MediaFoundationTransformCategories</param>
        public static IEnumerable<MfActivate> EnumerateTransforms(Guid category)
        {
            MediaFoundationInterop.MFTEnumEx(category, MftEnumFlags.All,
                null, null, out var interfacesPointer, out var interfaceCount);
            var activates = new MfActivate[interfaceCount];
            for (int n = 0; n < interfaceCount; n++)
            {
                var ptr = Marshal.ReadIntPtr(interfacesPointer + n * nint.Size);
                var iface = (Interfaces.IMFActivate)Marshal.GetObjectForIUnknown(ptr);
                activates[n] = new MfActivate(iface, ptr);
            }

            foreach (var a in activates)
            {
                yield return a;
            }
            Marshal.FreeCoTaskMem(interfacesPointer);
        }

        /// <summary>
        /// Shuts down Media Foundation
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
        internal static IMFMediaType CreateMediaType()
        {
            MediaFoundationInterop.MFCreateMediaType(out IMFMediaType mediaType);
            return mediaType;
        }

        /// <summary>
        /// Creates a media type from a WaveFormat
        /// </summary>
        internal static IMFMediaType CreateMediaTypeFromWaveFormat(WaveFormat waveFormat)
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
        internal static IMFMediaBuffer CreateMemoryBuffer(int bufferSize)
        {
            MediaFoundationInterop.MFCreateMemoryBuffer(bufferSize, out IMFMediaBuffer buffer);
            return buffer;
        }

        /// <summary>
        /// Creates a sample object
        /// </summary>
        /// <returns>The sample object</returns>
        internal static IMFSample CreateSample()
        {
            MediaFoundationInterop.MFCreateSample(out IMFSample sample);
            return sample;
        }

        /// <summary>
        /// Creates a new attributes store
        /// </summary>
        /// <param name="initialSize">Initial size</param>
        /// <returns>The attributes store</returns>
        internal static IMFAttributes CreateAttributes(int initialSize)
        {
            MediaFoundationInterop.MFCreateAttributes(out IMFAttributes attributes, initialSize);
            return attributes;
        }

        /// <summary>
        /// Creates a media foundation byte stream based on a stream object
        /// </summary>
        /// <param name="stream">The input stream (must implement IStream)</param>
        /// <returns>A media foundation byte stream</returns>
        internal static IMFByteStream CreateByteStream(object stream)
        {
            if (stream is IStream comStream)
            {
                MediaFoundationInterop.MFCreateMFByteStreamOnStream(comStream, out var byteStream);
                return byteStream;
            }
            throw new ArgumentException("Stream must implement IStream", nameof(stream));
        }

        /// <summary>
        /// Creates a source reader based on a byte stream
        /// </summary>
        /// <param name="byteStream">The byte stream</param>
        /// <returns>A media foundation source reader</returns>
        internal static IMFSourceReader CreateSourceReaderFromByteStream(IMFByteStream byteStream)
        {
            MediaFoundationInterop.MFCreateSourceReaderFromByteStream(byteStream, null, out IMFSourceReader reader);
            return reader;
        }

        /// <summary>
        /// Creates a source reader from a URL
        /// </summary>
        /// <param name="url">The URL or file path</param>
        /// <param name="attributes">Optional attributes</param>
        /// <returns>A media foundation source reader</returns>
        internal static IMFSourceReader CreateSourceReaderFromUrl(string url, IMFAttributes attributes = null)
        {
            MediaFoundationInterop.MFCreateSourceReaderFromURL(url, attributes, out IMFSourceReader reader);
            return reader;
        }

        /// <summary>
        /// Creates a sink writer from a URL
        /// </summary>
        /// <param name="outputUrl">The output URL or file path</param>
        /// <param name="byteStream">Optional byte stream</param>
        /// <param name="attributes">Optional attributes</param>
        /// <returns>A media foundation sink writer</returns>
        internal static IMFSinkWriter CreateSinkWriterFromUrl(string outputUrl, IMFByteStream byteStream = null, IMFAttributes attributes = null)
        {
            MediaFoundationInterop.MFCreateSinkWriterFromURL(outputUrl, byteStream, attributes, out IMFSinkWriter writer);
            return writer;
        }

        /// <summary>
        /// Gets a list of output formats from an audio encoder
        /// </summary>
        /// <param name="audioSubType">Audio subtype GUID</param>
        /// <param name="flags">Enumeration flags</param>
        /// <param name="codecConfig">Optional codec configuration attributes</param>
        /// <returns>A collection of available output types</returns>
        internal static IMFCollection GetAudioOutputAvailableTypes(Guid audioSubType, MftEnumFlags flags, IMFAttributes codecConfig = null)
        {
            MediaFoundationInterop.MFTranscodeGetAudioOutputAvailableTypes(audioSubType, flags, codecConfig, out IMFCollection availableTypes);
            return availableTypes;
        }
    }
}
