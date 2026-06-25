using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.Vst3.Interop;

/// <summary>
/// VST 3 audio processing interface (<c>Vst::IAudioProcessor</c>). Required on every audio module
/// class alongside <see cref="IComponent"/>. Defined in
/// <c>pluginterfaces/vst/ivstaudioprocessor.h</c>.
/// </summary>
/// <remarks>
/// <para>
/// The interface is a peer of <see cref="IComponent"/>, not a base — the same class implements
/// both, and the host queries for each via <c>queryInterface</c>.
/// </para>
/// <para>
/// <see cref="Process"/> runs on the realtime audio thread; everything else runs on the host's
/// setup thread.
/// </para>
/// </remarks>
[GeneratedComInterface]
[Guid("42043F99-B7DA-453C-A569-E79D9AAEC33D")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface IAudioProcessor
{
    /// <summary>
    /// Try to set a wanted speaker arrangement for inputs and outputs.
    /// </summary>
    /// <param name="inputs">Pointer to an array of <c>SpeakerArrangement</c> (<c>uint64*</c>).</param>
    /// <param name="numIns">Number of entries in <paramref name="inputs"/>.</param>
    /// <param name="outputs">Pointer to an array of <c>SpeakerArrangement</c>.</param>
    /// <param name="numOuts">Number of entries in <paramref name="outputs"/>.</param>
    [PreserveSig]
    int SetBusArrangements(IntPtr inputs, int numIns, IntPtr outputs, int numOuts);

    /// <summary>Gets the bus arrangement for a given direction and index.</summary>
    [PreserveSig]
    int GetBusArrangement(BusDirection dir, int index, ref ulong arr);

    /// <summary>Asks whether a given <see cref="SymbolicSampleSize"/> is supported.</summary>
    [PreserveSig]
    int CanProcessSampleSize(int symbolicSampleSize);

    /// <summary>Returns the plug-in's group delay in samples.</summary>
    [PreserveSig]
    uint GetLatencySamples();

    [PreserveSig]
    int SetupProcessing(ref ProcessSetup setup);

    /// <summary>
    /// Toggles processing on/off. Called with <c>true</c> before any <see cref="Process"/> calls
    /// and with <c>false</c> when processing stops. May be invoked on the UI or processing thread.
    /// </summary>
    [PreserveSig]
    int SetProcessing(byte state);

    /// <summary>The realtime process call. Audio thread only.</summary>
    [PreserveSig]
    int Process(ref ProcessData data);

    /// <summary>Tail length in samples (e.g. reverb decay). Used for offline rendering tails.</summary>
    [PreserveSig]
    uint GetTailSamples();
}

/// <summary>
/// VST 3.7+ extension (<c>Vst::IProcessContextRequirements</c>) — declares which
/// <see cref="ProcessContext"/> fields the plug-in actually reads, so the host can skip work for
/// the others.
/// </summary>
[GeneratedComInterface]
[Guid("2A654303-EF76-4E3D-95B5-FE83730EF6D0")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface IProcessContextRequirements
{
    /// <summary>Returns a bitmask of <see cref="ProcessContextRequirementFlags"/>.</summary>
    [PreserveSig]
    uint GetProcessContextRequirements();
}
