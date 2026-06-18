using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using NAudio.Vst3.Interop;

namespace NAudio.Vst3.Hosting;

/// <summary>
/// Host-side managed implementation of <see cref="IAttributeList"/> — a typed string-keyed
/// dictionary that JUCE-wrapped and Steinberg-SDK-helper plug-ins use as the on-wire container
/// for their parameter state. Returned from <see cref="Vst3HostApplication.CreateInstance"/>
/// when the plug-in asks for <c>IAttributeList::iid</c>.
/// </summary>
/// <remarks>
/// <para>
/// Per the SDK contract: <c>setBinary</c> copies the supplied bytes; the pointer returned from
/// <c>getBinary</c> is owned by the attribute list and remains valid until the same key is
/// overwritten or the list is released. We honour both rules by allocating native memory for
/// each binary value and freeing it when overwritten or on finalisation.
/// </para>
/// <para>
/// <c>AttrID</c> keys are <c>const char*</c> in the SDK; we decode them as UTF-8 (which covers
/// the ASCII identifiers JUCE and the SDK helpers use in practice).
/// </para>
/// </remarks>
[GeneratedComClass]
internal sealed unsafe partial class Vst3HostAttributeList : IAttributeList
{
    private enum Kind { Int, Float, String, Binary }

    private readonly struct Value
    {
        public readonly Kind Kind;
        public readonly long IntValue;
        public readonly double FloatValue;
        public readonly string? StringValue;
        public readonly IntPtr BinaryPointer;
        public readonly uint BinarySize;

        public Value(long v) { Kind = Kind.Int; IntValue = v; FloatValue = 0; StringValue = null; BinaryPointer = IntPtr.Zero; BinarySize = 0; }
        public Value(double v) { Kind = Kind.Float; IntValue = 0; FloatValue = v; StringValue = null; BinaryPointer = IntPtr.Zero; BinarySize = 0; }
        public Value(string v) { Kind = Kind.String; IntValue = 0; FloatValue = 0; StringValue = v; BinaryPointer = IntPtr.Zero; BinarySize = 0; }
        public Value(IntPtr ptr, uint size) { Kind = Kind.Binary; IntValue = 0; FloatValue = 0; StringValue = null; BinaryPointer = ptr; BinarySize = size; }
    }

    private readonly Dictionary<string, Value> _values = new(StringComparer.Ordinal);

    public int SetInt(IntPtr id, long value)
    {
        if (!TryReadKey(id, out var key)) return TResultCodes.InvalidArgument;
        ReplaceValue(key, new Value(value));
        return TResultCodes.Ok;
    }

    public int GetInt(IntPtr id, out long value)
    {
        value = 0;
        if (!TryReadKey(id, out var key) || !_values.TryGetValue(key, out var v) || v.Kind != Kind.Int)
        {
            return TResultCodes.False;
        }
        value = v.IntValue;
        return TResultCodes.Ok;
    }

    public int SetFloat(IntPtr id, double value)
    {
        if (!TryReadKey(id, out var key)) return TResultCodes.InvalidArgument;
        ReplaceValue(key, new Value(value));
        return TResultCodes.Ok;
    }

    public int GetFloat(IntPtr id, out double value)
    {
        value = 0;
        if (!TryReadKey(id, out var key) || !_values.TryGetValue(key, out var v) || v.Kind != Kind.Float)
        {
            return TResultCodes.False;
        }
        value = v.FloatValue;
        return TResultCodes.Ok;
    }

    public int SetString(IntPtr id, IntPtr value)
    {
        if (!TryReadKey(id, out var key)) return TResultCodes.InvalidArgument;
        var managed = value == IntPtr.Zero ? string.Empty : Marshal.PtrToStringUni(value) ?? string.Empty;
        ReplaceValue(key, new Value(managed));
        return TResultCodes.Ok;
    }

    public int GetString(IntPtr id, IntPtr value, uint sizeInBytes)
    {
        if (!TryReadKey(id, out var key) || !_values.TryGetValue(key, out var v) || v.Kind != Kind.String)
        {
            return TResultCodes.False;
        }
        if (value == IntPtr.Zero || sizeInBytes < sizeof(char))
        {
            return TResultCodes.InvalidArgument;
        }
        var capacityChars = (int)(sizeInBytes / sizeof(char));
        var dst = new Span<char>((void*)value, capacityChars);
        dst.Clear();
        var s = v.StringValue!.AsSpan();
        var copy = Math.Min(s.Length, capacityChars - 1);
        s[..copy].CopyTo(dst);
        return TResultCodes.Ok;
    }

    public int SetBinary(IntPtr id, IntPtr data, uint sizeInBytes)
    {
        if (!TryReadKey(id, out var key)) return TResultCodes.InvalidArgument;
        // Copy into freshly allocated unmanaged memory — the SDK contract is that the attribute
        // list owns the bytes and the caller may free their copy on return.
        IntPtr buffer = IntPtr.Zero;
        if (sizeInBytes > 0)
        {
            buffer = (IntPtr)NativeMemory.Alloc(sizeInBytes);
            if (data != IntPtr.Zero)
            {
                Buffer.MemoryCopy((void*)data, (void*)buffer, sizeInBytes, sizeInBytes);
            }
        }
        ReplaceValue(key, new Value(buffer, sizeInBytes));
        return TResultCodes.Ok;
    }

    public int GetBinary(IntPtr id, out IntPtr data, out uint sizeInBytes)
    {
        data = IntPtr.Zero;
        sizeInBytes = 0;
        if (!TryReadKey(id, out var key) || !_values.TryGetValue(key, out var v) || v.Kind != Kind.Binary)
        {
            return TResultCodes.False;
        }
        data = v.BinaryPointer;
        sizeInBytes = v.BinarySize;
        return TResultCodes.Ok;
    }

    private void ReplaceValue(string key, Value newValue)
    {
        if (_values.TryGetValue(key, out var existing) && existing.Kind == Kind.Binary && existing.BinaryPointer != IntPtr.Zero)
        {
            NativeMemory.Free((void*)existing.BinaryPointer);
        }
        _values[key] = newValue;
    }

    private static bool TryReadKey(IntPtr ptr, out string key)
    {
        if (ptr == IntPtr.Zero)
        {
            key = string.Empty;
            return false;
        }
        // AttrID is a const char* — decode as UTF-8 (covers the ASCII identifiers in practice).
        var bytes = MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)ptr);
        key = Encoding.UTF8.GetString(bytes);
        return true;
    }

    ~Vst3HostAttributeList()
    {
        // Free any unmanaged binary buffers we still own. Finalisation runs after the CCW's
        // native refcount drops to zero and the GC has collected this managed instance.
        foreach (var v in _values.Values)
        {
            if (v.Kind == Kind.Binary && v.BinaryPointer != IntPtr.Zero)
            {
                NativeMemory.Free((void*)v.BinaryPointer);
            }
        }
        _values.Clear();
    }
}
