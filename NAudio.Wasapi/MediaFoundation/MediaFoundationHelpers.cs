using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using NAudio.Wasapi.CoreAudioApi;
using NAudio.Wave;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Main interface for using Media Foundation with NAudio.
    /// </summary>
    /// <remarks>
    /// **Factory return-shape convention** (Phase 2e′): factories return
    /// <c>(IntPtr Ptr, T Rcw)</c> when the caller needs the IntPtr to pass to a *different*
    /// native MF API (e.g. <see cref="CreateMediaType"/> → IntPtr to
    /// <c>IMFTransform::SetInputType</c>; <see cref="CreateSample"/> → IntPtr to
    /// <c>IMFSinkWriter::WriteSample</c>; <see cref="CreateAttributes"/> → IntPtr to
    /// <see cref="CreateSinkWriterFromUrl"/>). The wrapper holds both refs and must release
    /// both via <see cref="ComActivation.ReleaseBoth"/>. Factories return only <c>T</c>
    /// (or only <c>IntPtr</c>) when the caller never needs the other half — the helper
    /// releases the unneeded ref internally before returning.
    /// </remarks>
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
            try
            {
                for (int n = 0; n < interfaceCount; n++)
                {
                    var ptr = Marshal.ReadIntPtr(interfacesPointer + n * nint.Size);
                    var iface = ProjectFresh<Interfaces.IMFActivate>(ptr);
                    activates[n] = new MfActivate(iface, ptr);
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(interfacesPointer);
            }

            foreach (var a in activates)
            {
                yield return a;
            }
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
        /// Projects a freshly-activated native COM pointer onto a source-generated RCW. If
        /// the cast fails the input pointer is released — otherwise the caller would leak the
        /// single ref returned by the MF factory call.
        /// </summary>
        private static T ProjectFresh<T>(IntPtr ptr) where T : class
        {
            try
            {
                return (T)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
                    ptr, CreateObjectFlags.UniqueInstance);
            }
            catch
            {
                Marshal.Release(ptr);
                throw;
            }
        }

        /// <summary>
        /// Creates a Media type. Returns both the raw COM pointer (for passing to native APIs that
        /// take an IntPtr-typed IMFMediaType, e.g. <c>IMFTransform::SetInputType</c>) and the
        /// source-generated RCW (for typed attribute reads). Caller owns both refs and must release
        /// both via <see cref="ComActivation.ReleaseBoth"/>.
        /// </summary>
        internal static (IntPtr Ptr, Interfaces.IMFMediaType Rcw) CreateMediaType()
        {
            MediaFoundationInterop.MFCreateMediaType(out var ptr);
            var rcw = ProjectFresh<Interfaces.IMFMediaType>(ptr);
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
                ComActivation.ReleaseBoth(rcw, ptr);
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
            var rcw = ProjectFresh<Interfaces.IMFMediaBuffer>(ptr);
            return (ptr, rcw);
        }

        /// <summary>
        /// Creates a sample object
        /// </summary>
        internal static (IntPtr Ptr, Interfaces.IMFSample Rcw) CreateSample()
        {
            MediaFoundationInterop.MFCreateSample(out var ptr);
            var rcw = ProjectFresh<Interfaces.IMFSample>(ptr);
            return (ptr, rcw);
        }

        /// <summary>
        /// Creates a new attributes store
        /// </summary>
        /// <param name="initialSize">Initial size</param>
        internal static (IntPtr Ptr, Interfaces.IMFAttributes Rcw) CreateAttributes(int initialSize)
        {
            MediaFoundationInterop.MFCreateAttributes(out var ptr, initialSize);
            var rcw = ProjectFresh<Interfaces.IMFAttributes>(ptr);
            return (ptr, rcw);
        }

        // IID_IStream (objidl.h)
        private static readonly Guid IID_IStream = new Guid("0000000C-0000-0000-C000-000000000046");

        /// <summary>
        /// Creates a media foundation byte stream wrapping a managed <see cref="ComStream"/>.
        /// Applies the Phase 2f QI-for-IID rule: the IUnknown returned by
        /// <c>ComWrappers.GetOrCreateComInterfaceForObject</c> is NOT the IStream vtable.
        /// We <c>QueryInterface</c> for <c>IID_IStream</c> before handing the pointer to native,
        /// otherwise the resampler / source reader / sink writer would dispatch against the
        /// wrong vtable and AV on first byte-stream call.
        /// Returns the raw IntPtr only — the IMFByteStream RCW is never used by the caller
        /// (the pointer is immediately handed to <see cref="CreateSourceReaderFromByteStream"/>
        /// or <see cref="CreateSinkWriterFromUrl"/>, which AddRef internally).
        /// </summary>
        internal static IntPtr CreateByteStream(NAudio.Wave.ComStream comStream)
        {
            if (comStream is null) throw new ArgumentNullException(nameof(comStream));

            IntPtr unkPtr = ComActivation.ComWrappers.GetOrCreateComInterfaceForObject(
                comStream, CreateComInterfaceFlags.None);
            try
            {
                Marshal.ThrowExceptionForHR(
                    Marshal.QueryInterface(unkPtr, in IID_IStream, out IntPtr streamPtr));
                try
                {
                    MediaFoundationInterop.MFCreateMFByteStreamOnStream(streamPtr, out var bsPtr);
                    return bsPtr;
                }
                finally
                {
                    Marshal.Release(streamPtr);
                }
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
            var rcw = ProjectFresh<Interfaces.IMFSourceReader>(ptr);
            Marshal.Release(ptr);
            return rcw;
        }

        /// <summary>
        /// Creates a source reader from a URL
        /// </summary>
        internal static Interfaces.IMFSourceReader CreateSourceReaderFromUrl(string url, IntPtr attributesPtr = default)
        {
            MediaFoundationInterop.MFCreateSourceReaderFromURL(url, attributesPtr, out var ptr);
            var rcw = ProjectFresh<Interfaces.IMFSourceReader>(ptr);
            Marshal.Release(ptr);
            return rcw;
        }

        /// <summary>
        /// Creates a sink writer from a URL
        /// </summary>
        internal static Interfaces.IMFSinkWriter CreateSinkWriterFromUrl(string outputUrl, IntPtr byteStreamPtr = default, IntPtr attributesPtr = default)
        {
            MediaFoundationInterop.MFCreateSinkWriterFromURL(outputUrl, byteStreamPtr, attributesPtr, out var ptr);
            var rcw = ProjectFresh<Interfaces.IMFSinkWriter>(ptr);
            Marshal.Release(ptr);
            return rcw;
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
            var rcw = ProjectFresh<Interfaces.IMFCollection>(ptr);
            Marshal.Release(ptr);
            return rcw;
        }
    }
}
