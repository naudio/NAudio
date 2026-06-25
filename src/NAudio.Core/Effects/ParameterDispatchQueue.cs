using System;
using System.Threading;

namespace NAudio.Effects;

/// <summary>
/// Lock-free single-producer/single-consumer queue that carries parameter
/// writes from a control thread (UI, automation, presets) to the audio thread.
/// The producer calls <see cref="Post"/> — invoked for you by
/// <see cref="EffectParameter.Value"/> once the parameter has been
/// <see cref="Attach"/>ed — and the audio thread calls <see cref="Drain"/> once
/// at the top of each processing block. Every pending write is then applied on
/// the audio thread, so coefficient recomputation, delay-buffer resizes and
/// filter swaps happen where there is no concurrent reader.
/// </summary>
/// <remarks>
/// Both <see cref="Post"/> and <see cref="Drain"/> are allocation-free and
/// wait-free. The queue is intended for exactly one producer thread and one
/// consumer thread, which matches the realtime harness (a single UI dispatcher
/// and one audio callback). It is bounded: when full, <see cref="Post"/> drops
/// the write — the next edit of the same parameter supersedes it anyway, and at
/// audio-block rate the queue never approaches full under human-rate edits.
/// </remarks>
public sealed class ParameterDispatchQueue : IParameterDispatch
{
    private readonly EffectParameter[] paramSlots;
    private readonly float[] valueSlots;
    private readonly int mask;
    private int head; // consumer index (audio thread)
    private int tail; // producer index (control thread)

    /// <summary>
    /// Creates the queue. <paramref name="capacity"/> is rounded up to a power
    /// of two and bounds how many un-drained writes can be buffered between
    /// blocks.
    /// </summary>
    public ParameterDispatchQueue(int capacity = 256)
    {
        if (capacity < 2)
            capacity = 2;
        var size = 1;
        while (size < capacity)
            size <<= 1;
        paramSlots = new EffectParameter[size];
        valueSlots = new float[size];
        mask = size - 1;
    }

    /// <inheritdoc />
    public void Post(EffectParameter parameter, float value)
    {
        if (parameter == null)
            return;
        var t = tail;
        var next = (t + 1) & mask;
        if (next == Volatile.Read(ref head))
            return; // full — drop; a later edit of this parameter supersedes it
        paramSlots[t] = parameter;
        valueSlots[t] = value;
        Volatile.Write(ref tail, next);
    }

    /// <summary>
    /// Applies every pending parameter write. Call once at the top of the audio
    /// processing block, on the audio thread, before any DSP.
    /// </summary>
    public void Drain()
    {
        var h = head;
        var t = Volatile.Read(ref tail);
        while (h != t)
        {
            paramSlots[h].ApplyDeferred(valueSlots[h]);
            paramSlots[h] = null;
            h = (h + 1) & mask;
        }
        Volatile.Write(ref head, h);
    }

    /// <summary>
    /// Routes every parameter of <paramref name="effect"/> through this queue,
    /// so off-thread writes are deferred to the next <see cref="Drain"/>.
    /// </summary>
    public void Attach(IParameterized effect)
    {
        ArgumentNullException.ThrowIfNull(effect);
        foreach (var p in effect.Parameters)
            p.SetDispatch(this);
    }

    /// <summary>
    /// Stops routing <paramref name="effect"/>'s parameters through this queue;
    /// subsequent writes apply inline again.
    /// </summary>
    public void Detach(IParameterized effect)
    {
        ArgumentNullException.ThrowIfNull(effect);
        foreach (var p in effect.Parameters)
            p.SetDispatch(null);
    }
}
