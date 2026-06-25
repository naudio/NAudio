using System.Collections.Generic;
using System.Runtime.InteropServices.Marshalling;
using NAudio.Vst3.Interop;

namespace NAudio.Vst3.Hosting;

/// <summary>
/// Host-side input <c>IEventList</c> — the per-block note/event queue handed to a plug-in via
/// <c>ProcessData.inputEvents</c>. The host fills it before each <c>process()</c> call (via
/// <see cref="Clear"/> + <see cref="Add"/>); the plug-in reads it back through
/// <see cref="GetEventCount"/> / <see cref="GetEvent"/>.
/// </summary>
/// <remarks>
/// Mirrors the shape of <see cref="Vst3HostParameterChanges"/>: a managed CCW we own and refill
/// each block. <see cref="AddEvent"/> is the plug-in-facing path (used for output event lists);
/// for the input list the host populates it directly with <see cref="Add"/>.
/// </remarks>
[GeneratedComClass]
internal sealed partial class Vst3HostEventList : IEventList
{
    private readonly List<Event> _events = new();

    /// <summary>Empties the list — called by the host at the start of each block.</summary>
    public void Clear() => _events.Clear();

    /// <summary>Appends an event the host wants the plug-in to receive this block.</summary>
    public void Add(in Event e) => _events.Add(e);

    public int GetEventCount() => _events.Count;

    public int GetEvent(int index, out Event e)
    {
        if ((uint)index >= (uint)_events.Count)
        {
            e = default;
            return TResultCodes.InvalidArgument;
        }
        e = _events[index];
        return TResultCodes.Ok;
    }

    public int AddEvent(ref Event e)
    {
        _events.Add(e);
        return TResultCodes.Ok;
    }
}
