using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.Vst3.Interop;

/// <summary>
/// Event type discriminator (<c>Vst::Event::EventTypes</c>).
/// </summary>
internal enum EventType : ushort
{
    NoteOn = 0,
    NoteOff = 1,
    Data = 2,
    PolyPressure = 3,
    NoteExpressionValue = 4,
    NoteExpressionText = 5,
    Chord = 6,
    Scale = 7,
    NoteExpressionIntValue = 8,
    LegacyMIDICCOut = 65535,
}

/// <summary>Event flags (<c>Vst::Event::EventFlags</c>).</summary>
[Flags]
internal enum EventFlags : ushort
{
    None = 0,
    IsLive = 1 << 0,
    UserReserved1 = 1 << 14,
    UserReserved2 = 1 << 15,
}

/// <summary>Note-on data (<c>Vst::NoteOnEvent</c>).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct NoteOnEvent
{
    public short Channel;
    public short Pitch;
    /// <summary>Tuning offset in cents (1.0 = +1 cent).</summary>
    public float Tuning;
    /// <summary>Velocity, range [0, 1].</summary>
    public float Velocity;
    /// <summary>Length in samples (optional; -1 = unknown).</summary>
    public int Length;
    /// <summary>Note identifier; -1 if unused.</summary>
    public int NoteId;
}

/// <summary>Note-off data (<c>Vst::NoteOffEvent</c>).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct NoteOffEvent
{
    public short Channel;
    public short Pitch;
    public float Velocity;
    public int NoteId;
    public float Tuning;
}

/// <summary>Poly-pressure data (<c>Vst::PolyPressureEvent</c>).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct PolyPressureEvent
{
    public short Channel;
    public short Pitch;
    public float Pressure;
    public int NoteId;
}

/// <summary>
/// SysEx / arbitrary data event (<c>Vst::DataEvent</c>).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct DataEvent
{
    public uint Size;
    /// <summary>Data-type discriminator; 0 = MIDI SysEx.</summary>
    public uint Type;
    public IntPtr Bytes;
}

/// <summary>Legacy MIDI CC out event (<c>Vst::LegacyMIDICCOutEvent</c>).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct LegacyMIDICCOutEvent
{
    public byte ControlNumber;
    public sbyte Channel;
    public sbyte Value;
    public sbyte Value2;
}

/// <summary>
/// Single VST 3 event (<c>Vst::Event</c>) — header followed by a 16-byte-aligned discriminated
/// union of variant payloads. The union slot is exposed as a 48-byte opaque buffer; typed
/// accessors land in Phase 5 alongside the MIDI plumbing.
/// </summary>
/// <remarks>
/// <para>
/// Layout matches the C++ <c>Event</c> struct under <c>pragma pack(push, 16)</c>: the header
/// fields (<c>busIndex</c>, <c>sampleOffset</c>, <c>ppqPosition</c>, <c>flags</c>, <c>type</c>)
/// occupy bytes 0–19, then 4 bytes of compiler padding land the union at offset 24 so that its
/// 8-byte-aligned members (e.g. <see cref="DataEvent.Bytes"/>) sit correctly.
/// </para>
/// <para>
/// 48 bytes is a generous over-allocation for the union — the largest known variant
/// (<see cref="NoteOnEvent"/>) needs 20 bytes, and the note-expression variants (defined in
/// <c>ivstnoteexpression.h</c>) are smaller still.
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct Event
{
    public int BusIndex;
    public int SampleOffset;
    /// <summary>Position in the project, in quarter notes (<c>TQuarterNotes</c> = <c>double</c>).</summary>
    public double PpqPosition;
    public ushort Flags;
    public ushort Type;

    /// <summary>
    /// Padding so the union below lands at offset 24 — matching natural 8-byte alignment of the
    /// largest union member.
    /// </summary>
    private readonly int _padding;

    /// <summary>
    /// Opaque storage for the variant payload. Reinterpret as <see cref="NoteOnEvent"/>,
    /// <see cref="NoteOffEvent"/>, <see cref="PolyPressureEvent"/>, <see cref="DataEvent"/>, etc.
    /// according to <see cref="Type"/>.
    /// </summary>
    public fixed byte UnionData[48];
}

/// <summary>
/// List of <see cref="Event"/> values for a single process block (<c>Vst::IEventList</c>).
/// </summary>
[GeneratedComInterface]
[Guid("3A2C4214-3463-49FE-B2C4-F397B9695A44")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface IEventList
{
    [PreserveSig]
    int GetEventCount();

    [PreserveSig]
    int GetEvent(int index, out Event e);

    [PreserveSig]
    int AddEvent(ref Event e);
}
