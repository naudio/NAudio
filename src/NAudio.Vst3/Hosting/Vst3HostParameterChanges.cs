using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using NAudio.Vst3.Interop;

namespace NAudio.Vst3.Hosting;

/// <summary>
/// Host-side managed implementation of <see cref="IParameterChanges"/> — the per-block bundle of
/// per-parameter automation queues handed to the plug-in via <c>ProcessData::inputParameterChanges</c>.
/// </summary>
/// <remarks>
/// <para>
/// Each block, the host calls <see cref="BeginBlock"/> to recycle the previous block's active
/// queues back into the pool, then <see cref="AcquireQueue"/> for every parameter that changed.
/// The plug-in reads the queues during <c>process</c>; nothing on the host side touches them
/// while the audio call is in flight.
/// </para>
/// <para>
/// Native <c>IParamValueQueue*</c> pointers are produced once per managed queue, cached for the
/// lifetime of <see cref="Vst3HostParameterChanges"/>, and released in <see cref="Dispose"/>.
/// </para>
/// </remarks>
[GeneratedComClass]
internal sealed partial class Vst3HostParameterChanges : IParameterChanges, IDisposable
{
    private readonly List<Vst3HostParamValueQueue> _active = new();
    private readonly List<Vst3HostParamValueQueue> _pool = new();

    // Reference-keyed: each managed queue maps to the cached IParamValueQueue* ptr we return
    // from GetParameterData. Allocated lazily when a queue first enters the active set.
    private readonly Dictionary<Vst3HostParamValueQueue, IntPtr> _queuePtrs =
        new(ReferenceEqualityComparer.Instance);

    private bool _disposed;

    /// <summary>
    /// Pulls a queue out of the pool (or allocates a new one) configured for the given parameter
    /// id. Caller follows up with <see cref="Vst3HostParamValueQueue.Append"/> for each point.
    /// </summary>
    public Vst3HostParamValueQueue AcquireQueue(uint parameterId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        Vst3HostParamValueQueue q;
        if (_pool.Count > 0)
        {
            q = _pool[^1];
            _pool.RemoveAt(_pool.Count - 1);
        }
        else
        {
            q = new Vst3HostParamValueQueue();
            EnsureNativePointer(q);
        }
        q.Configure(parameterId);
        _active.Add(q);
        return q;
    }

    /// <summary>Recycles last block's active queues back into the pool for reuse.</summary>
    public void BeginBlock()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_active.Count == 0) return;
        _pool.AddRange(_active);
        _active.Clear();
    }

    public int GetParameterCount() => _active.Count;

    public IntPtr GetParameterData(int index)
    {
        if ((uint)index >= (uint)_active.Count)
        {
            return IntPtr.Zero;
        }
        return _queuePtrs[_active[index]];
    }

    public IntPtr AddParameterData(in uint id, out int index)
    {
        // Plug-ins shouldn't drive host-side IParameterChanges through this entry point — it
        // exists so plug-ins can supply OUTPUT parameter changes (which we'd plumb on
        // ProcessData::outputParameterChanges, a separate object). Refuse cleanly.
        index = -1;
        return IntPtr.Zero;
    }

    private void EnsureNativePointer(Vst3HostParamValueQueue queue)
    {
        var unk = Vst3ComWrappers.Instance.GetOrCreateComInterfaceForObject(
            queue, CreateComInterfaceFlags.None);
        try
        {
            var iid = Vst3StandardInterfaceIds.IParamValueQueue;
            var hr = Marshal.QueryInterface(unk, in iid, out var queuePtr);
            if (hr != 0 || queuePtr == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    $"Failed to QI Vst3HostParamValueQueue for IParamValueQueue (HRESULT 0x{hr:X8}).");
            }
            _queuePtrs[queue] = queuePtr;
        }
        finally
        {
            Marshal.Release(unk);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var ptr in _queuePtrs.Values)
        {
            if (ptr != IntPtr.Zero)
            {
                Marshal.Release(ptr);
            }
        }
        _queuePtrs.Clear();
        _active.Clear();
        _pool.Clear();
    }
}
