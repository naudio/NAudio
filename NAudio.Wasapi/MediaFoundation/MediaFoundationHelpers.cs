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
        // Let's be sure that the static initializer assigns the below variables to correct initial values.
        private static bool initialized = false;
        private static uint queue_token = 0U;

        /// <summary>
        /// Initializes Media Foundation - only needs to be called once per process
        /// </summary>
        public static void Startup()
        {
            if (!initialized)
            {
                MediaFoundationException.ThrowIfFailed(
                    MediaFoundationInterop.MFStartup(MediaFoundationInterop.MF_VERSION, 0)
                );
                queue_token = CreateWorkQueue(WorkQueueType.Standard);
                initialized = true;
            }
        }

        /// <summary>
        /// Shuts down Media Foundation
        /// </summary>
        public static void Shutdown()
        {
            if (initialized)
            {
                MediaFoundationException.ThrowIfFailed(MediaFoundationInterop.MFShutdown());
                ReleaseWorkQueue(queue_token);
                queue_token = 0U;
                initialized = false;
            }
        }

        /// <summary>
        /// Provides a standard Media Foundation work queue token used by NAudio <br />
        /// Currently used by the async methods on our byte stream wrapper, leaving it here for additional features if so required.
        /// </summary>
        public static uint AllocatedWorkQueueToken => queue_token;

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
            MediaFoundationException.ThrowIfFailed(MediaFoundationInterop.MFCreateMediaType(out var ptr));
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
            MediaFoundationException.ThrowIfFailed(MediaFoundationInterop.MFCreateMemoryBuffer(bufferSize, out var ptr));
            var rcw = ProjectFresh<Interfaces.IMFMediaBuffer>(ptr);
            return (ptr, rcw);
        }

        /// <summary>
        /// Creates a sample object
        /// </summary>
        internal static (IntPtr Ptr, Interfaces.IMFSample Rcw) CreateSample()
        {
            MediaFoundationException.ThrowIfFailed(MediaFoundationInterop.MFCreateSample(out var ptr));
            var rcw = ProjectFresh<Interfaces.IMFSample>(ptr);
            return (ptr, rcw);
        }

        /// <summary>
        /// Creates a new attributes store
        /// </summary>
        /// <param name="initialSize">Initial size</param>
        internal static (IntPtr Ptr, Interfaces.IMFAttributes Rcw) CreateAttributes(int initialSize)
        {
            MediaFoundationException.ThrowIfFailed(MediaFoundationInterop.MFCreateAttributes(out var ptr, initialSize));
            var rcw = ProjectFresh<Interfaces.IMFAttributes>(ptr);
            return (ptr, rcw);
        }

        // IID_IStream (objidl.h)
        private static readonly Guid IID_IStream = new Guid("0000000C-0000-0000-C000-000000000046");
        private static readonly Guid IID_IMFByteStream = new("ad4c1b00-4bf7-422f-9175-756693d9130d");
        private static readonly Guid IID_IMFAsyncResult = new("ac6b7889-0740-4d51-8619-905994a55cc6");
        private static readonly Guid IID_IMFAsyncCallback = new("a27003cf-2354-4f2a-8d6a-ab7cff15437e");

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
                    MediaFoundationException.ThrowIfFailed(
                        MediaFoundationInterop.MFCreateMFByteStreamOnStream(streamPtr, out var bsPtr));
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

        internal static IntPtr CreateByteStream(MFByteStreamFromStream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            IntPtr unknown = ComActivation.ComWrappers.GetOrCreateComInterfaceForObject(stream, CreateComInterfaceFlags.None);

            try
            {
                Marshal.ThrowExceptionForHR(Marshal.QueryInterface(unknown, in IID_IMFByteStream, out IntPtr streamPtr));
                return streamPtr;
            }
            finally
            {
                Marshal.Release(unknown);
            }
        }

        /// <summary>
        /// Creates a source reader based on a byte stream
        /// </summary>
        internal static Interfaces.IMFSourceReader CreateSourceReaderFromByteStream(IntPtr byteStreamPtr)
        {
            MediaFoundationException.ThrowIfFailed(
                MediaFoundationInterop.MFCreateSourceReaderFromByteStream(byteStreamPtr, IntPtr.Zero, out var ptr));
            var rcw = ProjectFresh<Interfaces.IMFSourceReader>(ptr);
            Marshal.Release(ptr);
            return rcw;
        }

        /// <summary>
        /// Creates a source reader from a URL
        /// </summary>
        internal static Interfaces.IMFSourceReader CreateSourceReaderFromUrl(string url, IntPtr attributesPtr = default)
        {
            MediaFoundationException.ThrowIfFailed(
                MediaFoundationInterop.MFCreateSourceReaderFromURL(url, attributesPtr, out var ptr));
            var rcw = ProjectFresh<Interfaces.IMFSourceReader>(ptr);
            Marshal.Release(ptr);
            return rcw;
        }

        /// <summary>
        /// Creates a sink writer from a URL
        /// </summary>
        internal static Interfaces.IMFSinkWriter CreateSinkWriterFromUrl(string outputUrl, IntPtr byteStreamPtr = default, IntPtr attributesPtr = default)
        {
            MediaFoundationException.ThrowIfFailed(
                MediaFoundationInterop.MFCreateSinkWriterFromURL(outputUrl, byteStreamPtr, attributesPtr, out var ptr));
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
            MediaFoundationException.ThrowIfFailed(
                MediaFoundationInterop.MFTranscodeGetAudioOutputAvailableTypes(in audioSubType, flags, codecConfigPtr, out var ptr));
            var rcw = ProjectFresh<Interfaces.IMFCollection>(ptr);
            Marshal.Release(ptr);
            return rcw;
        }

        /// <summary>
        /// Creates an <see cref="Interfaces.IMFAsyncResult"/> object from an caller-implemented <see cref="Interfaces.IMFAsyncCallback"/>.
        /// </summary>
        /// <param name="callback">The callback.</param>
        /// <param name="p_object">Optional. Object to be passed to the async result.</param>
        /// <param name="p_state">Optional. State object to be passed to the async result.</param>
        /// <returns>A new <see cref="Interfaces.IMFAsyncResult"/> instance.</returns>
        internal static Interfaces.IMFAsyncResult CreateAsyncResult(Interfaces.IMFAsyncCallback callback, IntPtr p_object = default, IntPtr p_state = default)
        {
            ArgumentNullException.ThrowIfNull(callback);

            IntPtr translated_callback = ComActivation.ComWrappers.GetOrCreateComInterfaceForObject(callback, CreateComInterfaceFlags.None);

            // mdcdi1315: TODO: Test whether the below is valid.
            try
            {
                Marshal.ThrowExceptionForHR(Marshal.QueryInterface(translated_callback, in IID_IMFAsyncCallback, out IntPtr callback_pointer));
                int hr = MediaFoundationInterop.MFCreateAsyncResult(p_object, callback_pointer, p_state, out IntPtr result);
                if (hr < 0)
                {
                    Marshal.Release(callback_pointer);
                    throw new MediaFoundationException(hr);
                }
                else
                {
                    var rcw = ProjectFresh<Interfaces.IMFAsyncResult>(result);
                    Marshal.Release(result);
                    return rcw;
                }
            }
            finally
            {
                Marshal.Release(translated_callback);
            }
        }

        /// <summary>
        /// Puts a work item to the specified Media Foundation work queue.
        /// </summary>
        /// <param name="queue">The work queue token identifying the work queue to put the work item to.</param>
        /// <param name="callback">The callback defining the work to call later</param>
        /// <param name="p_state">Optional. A state object representing the state of the current call.</param>
        /// <returns>HRESULT value indicating whether the work item was inserted to the specified work queue.</returns>
        internal static int PutWorkItem(uint queue, Interfaces.IMFAsyncCallback callback, IntPtr p_state)
        {
            ArgumentNullException.ThrowIfNull(callback);

            IntPtr translated_callback = ComActivation.ComWrappers.GetOrCreateComInterfaceForObject(callback, CreateComInterfaceFlags.None);

            try
            {
                Marshal.ThrowExceptionForHR(Marshal.QueryInterface(translated_callback, in IID_IMFAsyncCallback, out IntPtr callback_pointer));
                return MediaFoundationInterop.MFPutWorkItem(queue, callback_pointer, p_state);
            }
            finally
            {
                Marshal.Release(translated_callback);
            }
        }

        /// <summary>
        /// Puts a work item to the NAudio's Media Foundation work queue (Can be queried by <see cref="AllocatedWorkQueueToken"/> property)
        /// </summary>
        /// <param name="callback">The callback defining the work to call later</param>
        /// <param name="p_state">Optional. A state object representing the state of the current call.</param>
        /// <returns>HRESULT value indicating whether the work item was inserted to the NAudio work queue.</returns>
        internal static int PutWorkItem(Interfaces.IMFAsyncCallback callback, IntPtr p_state) => PutWorkItem(queue_token, callback, p_state);

        internal static int InvokeCallback(Interfaces.IMFAsyncResult result)
        {
            ArgumentNullException.ThrowIfNull(result);

            IntPtr translated_callback = ComActivation.ComWrappers.GetOrCreateComInterfaceForObject(result, CreateComInterfaceFlags.None);

            try
            {
                Marshal.ThrowExceptionForHR(Marshal.QueryInterface(translated_callback, in IID_IMFAsyncResult, out IntPtr result_pointer));
                return MediaFoundationInterop.MFInvokeCallback(result_pointer);
            }
            finally
            {
                Marshal.Release(translated_callback);
            }
        }

        /// <summary>
        /// Creates a work queue of the specified type.
        /// </summary>
        /// <param name="type">The type of the work queue to create (Standard, Window, Multi-threaded)</param>
        /// <returns>Queue token, can be subsequently used to put work items or dispose it by calling <see cref="ReleaseWorkQueue(uint)"/>.</returns>
        internal static uint CreateWorkQueue(WorkQueueType type)
        {
            MediaFoundationException.ThrowIfFailed(MediaFoundationInterop.MFAllocateWorkQueueEx((uint)type, out uint work_queue_id));
            return work_queue_id;
        }

        /// <summary>Disposes a previously created work queue.</summary>
        /// <param name="queue_token">Token returned by the <see cref="CreateWorkQueue(WorkQueueType)"/> method.</param>
        internal static void ReleaseWorkQueue(uint queue_token) => MediaFoundationException.ThrowIfFailed(MediaFoundationInterop.MFUnlockWorkQueue(queue_token));


    }
}
