using System;
using System.Runtime.InteropServices;

namespace NAudio.Wasapi.CoreAudioApi.Interfaces
{
    /// <summary>
    /// Provides a callback to indicate that activation of a WASAPI interface is complete.
    /// </summary>
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("41D949AB-9862-444A-80F6-C261334DA5EB")]
    public interface IActivateAudioInterfaceCompletionHandler
    {
        //virtual HRESULT STDMETHODCALLTYPE ActivateCompleted(/*[in]*/ _In_  
        //   IActivateAudioInterfaceAsyncOperation *activateOperation) = 0;
        /// <summary>
        /// Indicates that activation of a WASAPI interface is complete and results are available.
        /// </summary>
        void ActivateCompleted(IActivateAudioInterfaceAsyncOperation activateOperation);
    }
}
