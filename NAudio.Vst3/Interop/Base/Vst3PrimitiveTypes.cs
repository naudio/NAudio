// Primitive type aliases used by the VST 3 interop layer.
// Source: pluginterfaces/base/ftypes.h and pluginterfaces/vst/vsttypes.h
// (https://github.com/steinbergmedia/vst3_pluginterfaces).

namespace NAudio.Vst3.Interop;

/// <summary>
/// VST 3 host-result code (the SDK's <c>tresult</c>; equivalent to a Windows COM <c>HRESULT</c>).
/// All plug-in entry points return this. Use <see cref="TResultCodes"/> for the well-known values.
/// </summary>
internal static class TResultCodes
{
    /// <summary>S_OK / kResultOk — operation succeeded.</summary>
    public const int Ok = 0;

    /// <summary>kResultTrue — alias for <see cref="Ok"/>.</summary>
    public const int True = 0;

    /// <summary>S_FALSE / kResultFalse — operation completed but the result is "false".</summary>
    public const int False = 1;

    /// <summary>E_NOINTERFACE / kNoInterface.</summary>
    public const int NoInterface = unchecked((int)0x80004002);

    /// <summary>E_INVALIDARG / kInvalidArgument.</summary>
    public const int InvalidArgument = unchecked((int)0x80070057);

    /// <summary>E_NOTIMPL / kNotImplemented.</summary>
    public const int NotImplemented = unchecked((int)0x80004001);

    /// <summary>E_FAIL / kInternalError.</summary>
    public const int InternalError = unchecked((int)0x80004005);

    /// <summary>E_UNEXPECTED / kNotInitialized.</summary>
    public const int NotInitialized = unchecked((int)0x8000FFFF);

    /// <summary>E_OUTOFMEMORY / kOutOfMemory.</summary>
    public const int OutOfMemory = unchecked((int)0x8007000E);
}

/// <summary>
/// VST 3 media-type discriminator (<c>Vst::MediaTypes</c>). Values used by
/// <c>IComponent::getBusCount</c>, <c>IComponent::getBusInfo</c>, and <c>RoutingInfo</c>.
/// </summary>
internal enum MediaType
{
    /// <summary>Audio bus.</summary>
    Audio = 0,
    /// <summary>Event (note/MIDI) bus.</summary>
    Event = 1,
}

/// <summary>
/// Bus direction (<c>Vst::BusDirections</c>).
/// </summary>
internal enum BusDirection
{
    Input = 0,
    Output = 1,
}

/// <summary>
/// Bus type (<c>Vst::BusTypes</c>) — main or auxiliary (side-chain).
/// </summary>
internal enum BusType
{
    Main = 0,
    Aux = 1,
}

/// <summary>
/// I/O mode (<c>Vst::IoModes</c>) — used only for instrument plug-ins.
/// </summary>
internal enum IoMode
{
    Simple = 0,
    Advanced = 1,
    OfflineProcessing = 2,
}

/// <summary>
/// Symbolic sample size (<c>Vst::SymbolicSampleSizes</c>) — single- or double-precision processing.
/// </summary>
internal enum SymbolicSampleSize
{
    /// <summary>32-bit floating-point samples (<c>kSample32</c>).</summary>
    Sample32 = 0,
    /// <summary>64-bit floating-point samples (<c>kSample64</c>).</summary>
    Sample64 = 1,
}

/// <summary>
/// Processing mode (<c>Vst::ProcessModes</c>) — realtime, prefetch, or offline.
/// </summary>
internal enum ProcessMode
{
    Realtime = 0,
    Prefetch = 1,
    Offline = 2,
}

/// <summary>
/// Bus flags (<c>BusInfo::BusFlags</c>).
/// </summary>
[System.Flags]
internal enum BusFlags : uint
{
    None = 0,
    /// <summary>Bus should be activated by default after instantiation.</summary>
    DefaultActive = 1u << 0,
    /// <summary>Bus carries control-voltage data instead of audio (VST 3.7.0+).</summary>
    IsControlVoltage = 1u << 1,
}
