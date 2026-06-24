using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.Marshalling;
using NAudio.Vst3.Interop;

namespace NAudio.Vst3.Hosting;

/// <summary>
/// Host-side managed implementation of <see cref="IParamValueQueue"/> — a list of
/// <c>(sampleOffset, normalisedValue)</c> automation points for a single parameter id, supplied
/// to the plug-in through <see cref="Vst3HostParameterChanges"/> on the next <c>process</c> call.
/// </summary>
/// <remarks>
/// Instances are pooled by the owning <see cref="Vst3HostParameterChanges"/> and reused block by
/// block — <see cref="Configure"/> resets the id and clears any stale points.
/// </remarks>
[GeneratedComClass]
internal sealed partial class Vst3HostParamValueQueue : IParamValueQueue
{
    private uint _parameterId;
    private readonly List<(int SampleOffset, double Value)> _points = new();

    /// <summary>Reuses this queue for a new parameter id, clearing any previous points.</summary>
    public void Configure(uint parameterId)
    {
        _parameterId = parameterId;
        _points.Clear();
    }

    /// <summary>Host-side population — appends a point. Order matters; offsets should be monotonic.</summary>
    internal void Append(int sampleOffset, double value) => _points.Add((sampleOffset, value));

    public uint GetParameterId() => _parameterId;

    public int GetPointCount() => _points.Count;

    public int GetPoint(int index, out int sampleOffset, out double value)
    {
        if ((uint)index >= (uint)_points.Count)
        {
            sampleOffset = 0;
            value = 0;
            return TResultCodes.InvalidArgument;
        }
        (sampleOffset, value) = _points[index];
        return TResultCodes.Ok;
    }

    public int AddPoint(int sampleOffset, double value, out int index)
    {
        _points.Add((sampleOffset, value));
        index = _points.Count - 1;
        return TResultCodes.Ok;
    }
}
