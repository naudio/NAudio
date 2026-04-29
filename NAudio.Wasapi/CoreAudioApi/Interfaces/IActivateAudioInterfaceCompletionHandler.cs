using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.Wasapi.CoreAudioApi.Interfaces
{
    /// <summary>
    /// Provides a callback to indicate that activation of a WASAPI interface is complete.
    /// </summary>
    [GeneratedComInterface, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("41D949AB-9862-444A-80F6-C261334DA5EB")]
    internal partial interface IActivateAudioInterfaceCompletionHandler
    {
        /// <summary>
        /// Indicates that activation of a WASAPI interface is complete and results are available.
        /// </summary>
        void ActivateCompleted(IntPtr activateOperation);
    }
}
