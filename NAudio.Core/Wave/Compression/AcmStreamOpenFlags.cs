using System;

namespace NAudio.Wave.Compression
{
    [Flags]
    enum AcmStreamOpenFlags
    {
        /// <summary>
        /// ACM_STREAMOPENF_QUERY, ACM will be queried to determine whether it supports the given conversion. A conversion stream will not be opened, and no handle will be returned in the phas parameter. 
        /// </summary>
        Query = 0x00000001,
        /// <summary>
        /// ACM_STREAMOPENF_ASYNC, Stream conversion should be performed asynchronously. If this flag is specified, the application can use a callback function to be notified when the conversion stream is opened and closed and after each buffer is converted. In addition to using a callback function, an application can examine the fdwStatus member of the ACMSTREAMHEADER structure for the ACMSTREAMHEADER_STATUSF_DONE flag. 
        /// </summary>
        Async = 0x00000002,
        /// <summary>
        /// ACM_STREAMOPENF_NONREALTIME, ACM will not consider time constraints when converting the data. By default, the driver will attempt to convert the data in real time. For some formats, specifying this flag might improve the audio quality or other characteristics.
        /// </summary>
        NonRealTime = 0x00000004,
        /// <summary>
        /// CALLBACK_TYPEMASK, callback type mask
        /// </summary>
        CallbackTypeMask = 0x00070000,
        /// <summary>
        /// CALLBACK_NULL, no callback
        /// </summary>
        CallbackNull = 0x00000000,
        /// <summary>
        /// CALLBACK_WINDOW, dwCallback is a HWND
        /// </summary>
        CallbackWindow = 0x00010000,
        /// <summary>
        /// CALLBACK_TASK, dwCallback is a HTASK
        /// </summary>
        CallbackTask = 0x00020000,
        /// <summary>
        /// CALLBACK_FUNCTION, dwCallback is a FARPROC
        /// </summary>
        CallbackFunction = 0x00030000,
        /// <summary>
        /// CALLBACK_THREAD, thread ID replaces 16 bit task
        /// </summary>
        CallbackThread = CallbackTask,
        /// <summary>
        /// CALLBACK_EVENT, dwCallback is an EVENT Handle
        /// </summary>
        CallbackEvent = 0x00050000,
    }
}
