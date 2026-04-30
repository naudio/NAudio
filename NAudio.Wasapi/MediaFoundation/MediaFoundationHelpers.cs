using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices.Marshalling;
using NAudio.Wasapi.CoreAudioApi;
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
                var iface = (Interfaces.IMFActivate)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
                    ptr, CreateObjectFlags.UniqueInstance);
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
        /// Creates a Media type. Returns both the raw COM pointer (for passing to native APIs that
        /// take an IntPtr-typed IMFMediaType, e.g. <c>IMFTransform::SetInputType</c>) and the
        /// source-generated RCW (for typed attribute reads). Caller owns both refs and must release
        /// both: <c>((ComObject)(object)Rcw).FinalRelease(); Marshal.Release(Ptr);</c>.
        /// </summary>
        internal static (IntPtr Ptr, Interfaces.IMFMediaType Rcw) CreateMediaType()
        {
            MediaFoundationInterop.MFCreateMediaType(out var ptr);
            var rcw = (Interfaces.IMFMediaType)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
                ptr, CreateObjectFlags.UniqueInstance);
            return (ptr, rcw);
        }

        /// <summary>
        /// Creates a media type from a WaveFormat
        /// </summary>
        internal static (IntPtr Ptr, Interfaces.IMFMediaType Rcw) CreateMediaTypeFromWaveFormat(WaveFormat waveFormat)
        {
            var (ptr, rcw) = CreateMediaType();
            try
            {
                MediaFoundationInterop.MFInitMediaTypeFromWaveFormatEx(ptr, waveFormat, Marshal.SizeOf(waveFormat));
                return (ptr, rcw);
            }
            catch
            {
                if (rcw != null) ((ComObject)(object)rcw).FinalRelease();
                Marshal.Release(ptr);
                throw;
            }
        }

        /// <summary>
        /// Creates a memory buffer of the specified size
        /// </summary>
        /// <param name="bufferSize">Memory buffer size in bytes</param>
        internal static (IntPtr Ptr, Interfaces.IMFMediaBuffer Rcw) CreateMemoryBuffer(int bufferSize)
        {
            MediaFoundationInterop.MFCreateMemoryBuffer(bufferSize, out var ptr);
            var rcw = (Interfaces.IMFMediaBuffer)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
                ptr, CreateObjectFlags.UniqueInstance);
            return (ptr, rcw);
        }

        /// <summary>
        /// Creates a sample object
        /// </summary>
        internal static (IntPtr Ptr, Interfaces.IMFSample Rcw) CreateSample()
        {
            MediaFoundationInterop.MFCreateSample(out var ptr);
            var rcw = (Interfaces.IMFSample)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
                ptr, CreateObjectFlags.UniqueInstance);
            return (ptr, rcw);
        }

        /// <summary>
        /// Creates a new attributes store
        /// </summary>
        /// <param name="initialSize">Initial size</param>
        internal static (IntPtr Ptr, Interfaces.IMFAttributes Rcw) CreateAttributes(int initialSize)
        {
            MediaFoundationInterop.MFCreateAttributes(out var ptr, initialSize);
            var rcw = (Interfaces.IMFAttributes)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
                ptr, CreateObjectFlags.UniqueInstance);
            return (ptr, rcw);
        }

        /// <summary>
        /// Creates a media foundation byte stream wrapping a managed Stream that implements IStream.
        /// </summary>
        /// <param name="stream">Object implementing <see cref="IStream"/> (typically <c>ComStream</c>).</param>
        internal static (IntPtr Ptr, Interfaces.IMFByteStream Rcw) CreateByteStream(object stream)
        {
            if (stream is not IStream comStream)
            {
                throw new ArgumentException("Stream must implement IStream", nameof(stream));
            }

            // Phase 2e' note: this path goes through built-in COM interop because
            // ComStream implements ComTypes.IStream ([ComImport]). Phase 2e' Step 5
            // migrates ComStream to [GeneratedComClass] + Phase 2f QI-for-IID.
            IntPtr unkPtr = Marshal.GetIUnknownForObject(comStream);
            try
            {
                MediaFoundationInterop.MFCreateMFByteStreamOnStream(unkPtr, out var bsPtr);
                var rcw = (Interfaces.IMFByteStream)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
                    bsPtr, CreateObjectFlags.UniqueInstance);
                return (bsPtr, rcw);
            }
            finally
            {
                Marshal.Release(unkPtr);
            }
        }

        /// <summary>
        /// Creates a source reader based on a byte stream
        /// </summary>
        internal static Interfaces.IMFSourceReader CreateSourceReaderFromByteStream(IntPtr byteStreamPtr)
        {
            MediaFoundationInterop.MFCreateSourceReaderFromByteStream(byteStreamPtr, IntPtr.Zero, out var ptr);
            try
            {
                return (Interfaces.IMFSourceReader)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
                    ptr, CreateObjectFlags.UniqueInstance);
            }
            finally
            {
                Marshal.Release(ptr);
            }
        }

        /// <summary>
        /// Creates a source reader from a URL
        /// </summary>
        internal static Interfaces.IMFSourceReader CreateSourceReaderFromUrl(string url, IntPtr attributesPtr = default)
        {
            MediaFoundationInterop.MFCreateSourceReaderFromURL(url, attributesPtr, out var ptr);
            try
            {
                return (Interfaces.IMFSourceReader)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
                    ptr, CreateObjectFlags.UniqueInstance);
            }
            finally
            {
                Marshal.Release(ptr);
            }
        }

        /// <summary>
        /// Creates a sink writer from a URL
        /// </summary>
        internal static Interfaces.IMFSinkWriter CreateSinkWriterFromUrl(string outputUrl, IntPtr byteStreamPtr = default, IntPtr attributesPtr = default)
        {
            MediaFoundationInterop.MFCreateSinkWriterFromURL(outputUrl, byteStreamPtr, attributesPtr, out var ptr);
            try
            {
                return (Interfaces.IMFSinkWriter)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
                    ptr, CreateObjectFlags.UniqueInstance);
            }
            finally
            {
                Marshal.Release(ptr);
            }
        }

        /// <summary>
        /// Gets a list of output formats from an audio encoder
        /// </summary>
        /// <param name="audioSubType">Audio subtype GUID</param>
        /// <param name="flags">Enumeration flags</param>
        /// <param name="codecConfigPtr">Optional codec configuration attributes (IntPtr to IMFAttributes), or IntPtr.Zero.</param>
        internal static Interfaces.IMFCollection GetAudioOutputAvailableTypes(Guid audioSubType, MftEnumFlags flags, IntPtr codecConfigPtr = default)
        {
            MediaFoundationInterop.MFTranscodeGetAudioOutputAvailableTypes(audioSubType, flags, codecConfigPtr, out var ptr);
            try
            {
                return (Interfaces.IMFCollection)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
                    ptr, CreateObjectFlags.UniqueInstance);
            }
            finally
            {
                Marshal.Release(ptr);
            }
        }
    }
}
