using System;
using System.Runtime.InteropServices;

namespace NAudio.Wasapi.CoreAudioApi.Interfaces
{
    /// <summary>
    /// Represents an asynchronous operation activating a WASAPI interface and provides a method to retrieve the results of the activation.
    /// </summary>
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("72A22D78-CDE4-431D-B8CC-843A71199B6D")]
    public interface IActivateAudioInterfaceAsyncOperation
    {
        //virtual HRESULT STDMETHODCALLTYPE GetActivateResult(/*[out]*/ _Out_  
        //  HRESULT *activateResult, /*[out]*/ _Outptr_result_maybenull_  IUnknown **activatedInterface) = 0;
        /// <summary>
        /// Gets the results of an asynchronous activation of a WASAPI interface initiated by an application calling the ActivateAudioInterfaceAsync function
        /// </summary>
        void GetActivateResult([Out] out int activateResult,
                               [Out, MarshalAs(UnmanagedType.IUnknown)] out object activateInterface);
    }
}
