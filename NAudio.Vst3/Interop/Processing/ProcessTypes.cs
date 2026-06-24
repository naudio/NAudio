using System;
using System.Runtime.InteropServices;

namespace NAudio.Vst3.Interop;

/// <summary>
/// Audio processing setup (<c>Vst::ProcessSetup</c>) passed to
/// <c>IAudioProcessor::setupProcessing</c>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct ProcessSetup
{
    public int ProcessMode;
    public int SymbolicSampleSize;
    public int MaxSamplesPerBlock;
    public double SampleRate;
}

/// <summary>
/// Per-bus audio buffer set (<c>Vst::AudioBusBuffers</c>). The union of 32- and 64-bit channel
/// pointers is projected as a single <see cref="IntPtr"/> — interpret it as either
/// <c>float**</c> or <c>double**</c> depending on the active <see cref="SymbolicSampleSize"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct AudioBusBuffers
{
    public int NumChannels;
    public ulong SilenceFlags;
    /// <summary>
    /// Pointer to <c>float**</c> or <c>double**</c> per the symbolic sample size in
    /// <see cref="ProcessData.SymbolicSampleSize"/>.
    /// </summary>
    public IntPtr ChannelBuffers;
}

/// <summary>
/// Frame rate (<c>Vst::FrameRate</c>) — part of <see cref="ProcessContext"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct FrameRate
{
    public uint FramesPerSecond;
    public uint Flags;
}

/// <summary>
/// Chord info (<c>Vst::Chord</c>) — part of <see cref="ProcessContext"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct Chord
{
    public byte KeyNote;
    public byte RootNote;
    public short ChordMask;
}

/// <summary>
/// Audio processing context (<c>Vst::ProcessContext</c>) — host transport / tempo / time
/// information passed each block via <see cref="ProcessData.ProcessContext"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct ProcessContext
{
    /// <summary>Combination of <c>StatesAndFlags</c> (playing / recording / *Valid bits).</summary>
    public uint State;
    public double SampleRate;
    /// <summary>Project time in samples (always valid).</summary>
    public long ProjectTimeSamples;
    /// <summary>System time in nanoseconds (optional).</summary>
    public long SystemTime;
    /// <summary>Project time without loop (optional).</summary>
    public long ContinousTimeSamples;
    /// <summary>Musical position in quarter notes (optional).</summary>
    public double ProjectTimeMusic;
    /// <summary>Last bar start position in quarter notes (optional).</summary>
    public double BarPositionMusic;
    public double CycleStartMusic;
    public double CycleEndMusic;
    /// <summary>Tempo in BPM (optional).</summary>
    public double Tempo;
    public int TimeSigNumerator;
    public int TimeSigDenominator;
    public Chord Chord;
    public int SmpteOffsetSubframes;
    public FrameRate FrameRate;
    public int SamplesToNextClock;
}

/// <summary>
/// Audio processing block payload (<c>Vst::ProcessData</c>) — input/output buffers, parameter
/// changes, events, and context, all passed to <c>IAudioProcessor::process</c> each block.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct ProcessData
{
    public int ProcessMode;
    public int SymbolicSampleSize;
    public int NumSamples;
    public int NumInputs;
    public int NumOutputs;
    /// <summary>Pointer to an array of <see cref="AudioBusBuffers"/>, one per input bus.</summary>
    public IntPtr Inputs;
    /// <summary>Pointer to an array of <see cref="AudioBusBuffers"/>, one per output bus.</summary>
    public IntPtr Outputs;
    /// <summary>Native <c>IParameterChanges*</c> (in).</summary>
    public IntPtr InputParameterChanges;
    /// <summary>Native <c>IParameterChanges*</c> (out, optional).</summary>
    public IntPtr OutputParameterChanges;
    /// <summary>Native <c>IEventList*</c> (in, optional).</summary>
    public IntPtr InputEvents;
    /// <summary>Native <c>IEventList*</c> (out, optional).</summary>
    public IntPtr OutputEvents;
    /// <summary>Pointer to <see cref="ProcessContext"/> (optional, but most welcome).</summary>
    public IntPtr ProcessContext;
}

/// <summary>
/// Process-context requirement flags (<c>IProcessContextRequirements::Flags</c>) — bitmask
/// returned by VST 3.7+ plug-ins to declare which <see cref="ProcessContext"/> fields they read.
/// </summary>
[Flags]
internal enum ProcessContextRequirementFlags : uint
{
    NeedSystemTime = 1u << 0,
    NeedContinousTimeSamples = 1u << 1,
    NeedProjectTimeMusic = 1u << 2,
    NeedBarPositionMusic = 1u << 3,
    NeedCycleMusic = 1u << 4,
    NeedSamplesToNextClock = 1u << 5,
    NeedTempo = 1u << 6,
    NeedTimeSignature = 1u << 7,
    NeedChord = 1u << 8,
    NeedFrameRate = 1u << 9,
    NeedTransportState = 1u << 10,
}
