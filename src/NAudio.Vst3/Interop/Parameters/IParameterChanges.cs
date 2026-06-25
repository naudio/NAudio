using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.Vst3.Interop;

/// <summary>
/// Queue of automation/parameter-change points for a single parameter
/// (<c>Vst::IParamValueQueue</c>). Each point is a (sample-offset, normalised-value) pair; the
/// plug-in linearly interpolates between consecutive points across the process block.
/// </summary>
[GeneratedComInterface]
[Guid("01263A18-ED07-4F6F-98C9-D3564686F9BA")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface IParamValueQueue
{
    [PreserveSig]
    uint GetParameterId();

    [PreserveSig]
    int GetPointCount();

    [PreserveSig]
    int GetPoint(int index, out int sampleOffset, out double value);

    [PreserveSig]
    int AddPoint(int sampleOffset, double value, out int index);
}

/// <summary>
/// Collection of per-parameter change queues for a single process block
/// (<c>Vst::IParameterChanges</c>). Only parameters that actually change carry a queue; unchanged
/// parameters are simply absent.
/// </summary>
[GeneratedComInterface]
[Guid("A4779663-0BB6-4A56-B443-84A8466FEB9D")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface IParameterChanges
{
    [PreserveSig]
    int GetParameterCount();

    /// <summary>Returns the native <c>IParamValueQueue*</c> at <paramref name="index"/>.</summary>
    [PreserveSig]
    IntPtr GetParameterData(int index);

    /// <summary>Adds a new queue for the given parameter id; returns its native pointer.</summary>
    [PreserveSig]
    IntPtr AddParameterData(in uint id, out int index);
}
