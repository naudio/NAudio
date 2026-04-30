using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.Wasapi.CoreAudioApi.Interfaces
{
    /// <summary>
    /// Marks a COM object as agile (free-threaded), indicating it can be called from any apartment.
    /// </summary>
    [GeneratedComInterface, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("94ea2b94-e9cc-49e0-c0ff-ee64ca8f5b90")]
    internal partial interface IAgileObject
    {
    }
}
