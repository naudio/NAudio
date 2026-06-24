using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.CoreAudioApi.Interfaces
{
    /// <summary>
    /// The IControlInterface interface represents a control interface on a part (connector or subunit) in a device topology. The client obtains a reference to a part's IControlInterface interface by calling the IPart::GetControlInterface method.
    /// </summary>
    [Guid("45d37c3f-5140-444a-ae24-400789f3cbf3"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        GeneratedComInterface]
    internal partial interface IControlInterface
    {
    }
}
