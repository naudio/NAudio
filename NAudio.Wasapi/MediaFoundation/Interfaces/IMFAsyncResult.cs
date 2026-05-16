
using System;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.MediaFoundation.Interfaces
{
    /// <summary>
    ///     This interface is used to represent the result from an asynchronous operation. <br />
    ///     For most Media Foundation components and applications that need to 
    ///     create an <see cref="IMFAsyncResult"/> implementation, 
    ///     CreateAsyncResult(object, IMFAsyncCallback, object)
    ///     , which instantiates the MF implementation of this interface, will suffice. <br />
    ///     Any implementation of <see cref="IMFAsyncResult"/> must inherit from the
    ///     MFASYNCRESULT structure defined in mfapi.h
    /// </summary>
    [GeneratedComInterface]
    [Guid("ac6b7889-0740-4d51-8619-905994a55cc6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IMFAsyncResult
    {
        /// <summary>
        ///     Retrieves an IUnknown pointer to the state object associated with
        ///     the asynchronous operation, if any.
        ///     If there is no associated state, then *ppunkState is set to NULL.
        /// </summary>
        [PreserveSig]
        int GetState(/* IUnknown */ [MaybeNull] out IntPtr ppunkState);

        /// <summary>
        ///     Returns an HRESULT indicating the success or failure of the 
        ///     asynchronous operation
        /// </summary>
        [PreserveSig]
        int GetStatus();

        /// <summary>
        ///     Sets the HRESULT status code to indicate the success or failure
        ///     of the asynchronous operation.  
        /// </summary>
        [PreserveSig]
        int SetStatus(int hrStatus); // Gets and returns HRESULT

        /// <summary>
        ///     Retrieves an IUnknown pointer to the object associated with the
        ///     asynchronous operation, if any.
        ///     If there is no associated object, then *ppunkObject is set to NULL.
        /// </summary>
        [PreserveSig]
        int GetObject(/* IUnknown */ [MaybeNull] out IntPtr ppObject);

        /// <summary>
        ///     Returns an IUnknown pointer to the state object associated with
        ///     the asynchronous operation, if any, without incrementing its
        ///     reference count
        /// </summary>
        [PreserveSig]
        [return: MaybeNull]
        /* IUnknown */
        IntPtr GetStateNoAddRef();
    }
}
