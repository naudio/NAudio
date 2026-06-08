
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using NAudio.Wave;
using NAudio.CoreAudioApi;
using NAudio.MediaFoundation.Interfaces;

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

        /// <summary>
        /// Initializes Media Foundation - only needs to be called once per process
        /// </summary>
        public static void Startup()
        {
            if (!initialized)
            {
                MediaFoundationException.ThrowIfFailed(MediaFoundationInterop.MFStartup(MediaFoundationInterop.MF_VERSION, 0));
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
                initialized = false;
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
                    var iface = ProjectFresh<IMFActivate>(ptr);
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
        internal static (IntPtr Ptr, IMFMediaType Rcw) CreateMediaType()
        {
            MediaFoundationException.ThrowIfFailed(MediaFoundationInterop.MFCreateMediaType(out var ptr));
            var rcw = ProjectFresh<IMFMediaType>(ptr);
            return (ptr, rcw);
        }

        /// <summary>
        /// Creates a media type from a WaveFormat
        /// </summary>
        internal static (IntPtr Ptr, IMFMediaType Rcw) CreateMediaTypeFromWaveFormat(WaveFormat waveFormat)
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
        internal static (IntPtr Ptr, IMFMediaBuffer Rcw) CreateMemoryBuffer(int bufferSize)
        {
            MediaFoundationException.ThrowIfFailed(MediaFoundationInterop.MFCreateMemoryBuffer(bufferSize, out var ptr));
            var rcw = ProjectFresh<IMFMediaBuffer>(ptr);
            return (ptr, rcw);
        }

        /// <summary>
        /// Creates a sample object
        /// </summary>
        internal static (IntPtr Ptr, IMFSample Rcw) CreateSample()
        {
            MediaFoundationException.ThrowIfFailed(MediaFoundationInterop.MFCreateSample(out var ptr));
            var rcw = ProjectFresh< IMFSample>(ptr);
            return (ptr, rcw);
        }

        /// <summary>
        /// Creates a new attributes store
        /// </summary>
        /// <param name="initialSize">Initial size</param>
        internal static (IntPtr Ptr, IMFAttributes Rcw) CreateAttributes(int initialSize)
        {
            MediaFoundationException.ThrowIfFailed(MediaFoundationInterop.MFCreateAttributes(out var ptr, initialSize));
            var rcw = ProjectFresh<IMFAttributes>(ptr);
            return (ptr, rcw);
        }

        // See mfobjects.h header file.
        private static readonly Guid IID_IMFByteStream = new("ad4c1b00-4bf7-422f-9175-756693d9130d");
        private static readonly Guid IID_IMFAsyncCallback = new("a27003cf-2354-4f2a-8d6a-ab7cff15437e");

        /// <summary>
        /// Creates a <see cref="IMFByteStream"/> object constructed
        /// through an object of the <see cref="MfByteStreamFromStream"/> class, 
        /// which acts as a full wrapper for a <see cref="System.IO.Stream"/> instance.
        /// </summary>
        /// <param name="stream">The <see cref="MfByteStreamFromStream"/> instance.</param>
        /// <returns>A COM object of type <see cref="IMFByteStream"/>.</returns>
        internal static unsafe IntPtr CreateByteStream(MfByteStreamFromStream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);
            IntPtr unkPtr = ComActivation
                .ComWrappers
                .GetOrCreateComInterfaceForObject(stream, CreateComInterfaceFlags.None);
            try
            {
                Marshal.ThrowExceptionForHR(
                    Marshal.QueryInterface(unkPtr, in IID_IMFByteStream, out IntPtr streamPtr)
                );
                return streamPtr;
            }
            finally
            {
                Marshal.Release(unkPtr);
            }
        }

        /// <summary>
        /// Creates a source reader based on a byte stream
        /// </summary>
        internal static IMFSourceReader CreateSourceReaderFromByteStream(IntPtr byteStreamPtr)
        {
            MediaFoundationException.ThrowIfFailed(
                MediaFoundationInterop.MFCreateSourceReaderFromByteStream(byteStreamPtr, IntPtr.Zero, out var ptr));
            var rcw = ProjectFresh<IMFSourceReader>(ptr);
            Marshal.Release(ptr);
            return rcw;
        }

        /// <summary>
        /// Creates a source reader from a URL
        /// </summary>
        internal static IMFSourceReader CreateSourceReaderFromUrl(string url, IntPtr attributesPtr = default)
        {
            MediaFoundationException.ThrowIfFailed(
                MediaFoundationInterop.MFCreateSourceReaderFromURL(url, attributesPtr, out var ptr));
            var rcw = ProjectFresh<IMFSourceReader>(ptr);
            Marshal.Release(ptr);
            return rcw;
        }

        /// <summary>
        /// Creates a sink writer from a URL
        /// </summary>
        internal static IMFSinkWriter CreateSinkWriterFromUrl(string outputUrl, IntPtr byteStreamPtr = default, IntPtr attributesPtr = default)
        {
            MediaFoundationException.ThrowIfFailed(
                MediaFoundationInterop.MFCreateSinkWriterFromURL(outputUrl, byteStreamPtr, attributesPtr, out var ptr));
            var rcw = ProjectFresh<IMFSinkWriter>(ptr);
            Marshal.Release(ptr);
            return rcw;
        }

        /// <summary>
        /// Gets a list of output formats from an audio encoder
        /// </summary>
        /// <param name="audioSubType">Audio subtype GUID</param>
        /// <param name="flags">Enumeration flags</param>
        /// <param name="codecConfigPtr">Optional codec configuration attributes (IntPtr to IMFAttributes), or IntPtr.Zero.</param>
        internal static IMFCollection GetAudioOutputAvailableTypes(Guid audioSubType, MftEnumFlags flags, IntPtr codecConfigPtr = default)
        {
            MediaFoundationException.ThrowIfFailed(
                MediaFoundationInterop.MFTranscodeGetAudioOutputAvailableTypes(in audioSubType, flags, codecConfigPtr, out var ptr));
            var rcw = ProjectFresh<IMFCollection>(ptr);
            Marshal.Release(ptr);
            return rcw;
        }

        /// <summary>
        /// Creates an <see cref="IMFAsyncResult"/> object from an caller-implemented <see cref="IMFAsyncCallback"/>.
        /// </summary>
        /// <param name="callback">The callback.</param>
        /// <param name="ptrObject">Optional. Object to be passed to the async result.</param>
        /// <param name="ptrState">Optional. State object to be passed to the async result.</param>
        /// <returns>A new <see cref="IMFAsyncResult"/> instance.</returns>
        internal static unsafe (IntPtr pointer, IMFAsyncResult Rcw) CreateAsyncResult(IMFAsyncCallback callback, IntPtr ptrObject = default, IntPtr ptrState = default)
        {
            ArgumentNullException.ThrowIfNull(callback);

            IntPtr pCreatedObjectTemp = ComActivation
                .ComWrappers
                .GetOrCreateComInterfaceForObject(callback, CreateComInterfaceFlags.None);

            try
            {
                Marshal.ThrowExceptionForHR(
                    Marshal.QueryInterface(pCreatedObjectTemp, in IID_IMFAsyncCallback, out IntPtr pCallbackObject)
                );
                try
                {
                    MediaFoundationException.ThrowIfFailed(
                        MediaFoundationInterop.MFCreateAsyncResult(ptrObject, pCallbackObject, ptrState, out IntPtr asyncResult)
                    );
                    return (asyncResult, ProjectFresh<IMFAsyncResult>(asyncResult));
                }
                finally
                {
                    Marshal.Release(pCallbackObject);
                }
            }
            finally
            {
                Marshal.Release(pCreatedObjectTemp);
            }
        }

        /// <summary>
        /// Creates an <see cref="IMFAsyncResult"/> object from the specified <see cref="IMFAsyncCallback"/> object.
        /// </summary>
        /// <param name="callback">A <see cref="IMFAsyncCallback"/> pointer that represents the callback.</param>
        /// <param name="ptrObject">Optional. Object to be passed to the async result.</param>
        /// <param name="ptrState">Optional. State object to be passed to the async result.</param>
        /// <returns>A new <see cref="IMFAsyncResult"/> instance.</returns>
        internal static unsafe (IntPtr pointer, IMFAsyncResult Rcw) CreateAsyncResult(IntPtr callback, IntPtr ptrObject = default, IntPtr ptrState = default)
        {
            ArgumentNullException.ThrowIfNull(callback.ToPointer());

            int hr = MediaFoundationInterop.MFCreateAsyncResult(ptrObject, callback, ptrState, out IntPtr result);
            if (NAudio.Utils.HResult.IsError(hr))
            {
                throw new MediaFoundationException(hr);
            }
            else
            {
                return (result, ProjectFresh<IMFAsyncResult>(result));
            }
        }

        /// <summary>
        /// Puts a work item to the specified Media Foundation work queue.
        /// </summary>
        /// <param name="queue">The work queue token identifying the work queue to put the work item to.</param>
        /// <param name="callback">The callback object defining the work to call later</param>
        /// <param name="ptrState">Optional. A state object representing the state of the current call.</param>
        /// <returns>HRESULT value indicating whether the work item was inserted to the specified work queue.</returns>
        internal static unsafe int PutWorkItem(uint queue, IntPtr callback, IntPtr ptrState)
        {
            ArgumentNullException.ThrowIfNull(callback.ToPointer(), nameof(callback));
            return MediaFoundationInterop.MFPutWorkItem(queue, callback, ptrState);
        }

        /// <summary>
        /// Puts a work item to the specified Media Foundation work queue.
        /// </summary>
        /// <param name="queue">The work queue token identifying the work queue to put the work item to.</param>
        /// <param name="ptrResult">The result object defining the work to call later</param>
        /// <returns>HRESULT value indicating whether the work item was inserted to the specified work queue.</returns>
        internal static unsafe int PutWorkItem(uint queue, IntPtr ptrResult)
        {
            ArgumentNullException.ThrowIfNull(ptrResult.ToPointer(), nameof(ptrResult));
            return MediaFoundationInterop.MFPutWorkItemEx(queue, ptrResult);
        }

        /// <summary>
        /// Puts a work item to the default multi-threaded Media Foundation work queue (Is the <see cref="MediaFoundationInterop.MFASYNC_CALLBACK_QUEUE_MULTITHREADED"/> value)
        /// </summary>
        /// <param name="callback">The callback object defining the work to call later</param>
        /// <param name="ptrState">Optional. A state object representing the state of the current call.</param>
        /// <returns>HRESULT value indicating whether the work item was inserted to the default multi-threaded work queue.</returns>
        internal static int PutWorkItem(IntPtr callback, IntPtr ptrState) => PutWorkItem(MediaFoundationInterop.MFASYNC_CALLBACK_QUEUE_MULTITHREADED, callback, ptrState);

        /// <summary>
        /// Puts a work item to the default multi-threaded Media Foundation work queue (Is the <see cref="MediaFoundationInterop.MFASYNC_CALLBACK_QUEUE_MULTITHREADED"/> value)
        /// </summary>
        /// <param name="ptrResult">The result object defining the work to call later</param>
        /// <returns>HRESULT value indicating whether the work item was inserted to the default multi-threaded work queue.</returns>
        internal static int PutWorkItem(IntPtr ptrResult) => PutWorkItem(MediaFoundationInterop.MFASYNC_CALLBACK_QUEUE_MULTITHREADED, ptrResult);

        /// <summary>
        /// Invokes a callback to any available Media Foundation work queue.
        /// </summary>
        /// <param name="ptrResult">The result object defining the work to call later</param>
        /// <returns>HRESULT value indicating whether the work item was inserted to any work queue.</returns>
        internal static unsafe int InvokeCallback(IntPtr ptrResult)
        {
            ArgumentNullException.ThrowIfNull(ptrResult.ToPointer(), nameof(ptrResult));
            return MediaFoundationInterop.MFInvokeCallback(ptrResult);
        }

        /// <summary>
        /// Creates a work queue of the specified type.
        /// </summary>
        /// <param name="type">The type of the work queue to create (Standard, Window, Multi-threaded)</param>
        /// <returns>Queue token, can be subsequently used to put work items or dispose it by calling <see cref="ReleaseWorkQueue(uint)"/>.</returns>
        internal static uint CreateWorkQueue(WorkQueueType type)
        {
            MediaFoundationException.ThrowIfFailed(MediaFoundationInterop.MFAllocateWorkQueueEx(type, out uint work_queue_id));
            return work_queue_id;
        }

        /// <summary>Disposes a previously created work queue.</summary>
        /// <param name="queue_token">Token returned by the <see cref="CreateWorkQueue(WorkQueueType)"/> method.</param>
        internal static void ReleaseWorkQueue(uint queue_token) => MediaFoundationException.ThrowIfFailed(MediaFoundationInterop.MFUnlockWorkQueue(queue_token));
    }
}
