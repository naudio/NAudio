using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using NAudio.Vst3.Interop;

namespace NAudio.Vst3.Hosting;

/// <summary>
/// Host-side managed implementation of <see cref="IMessage"/> — a named bag with an owned
/// <see cref="IAttributeList"/>, returned from <see cref="Vst3HostApplication.CreateInstance"/>
/// when the plug-in asks for <c>IMessage::iid</c>.
/// </summary>
/// <remarks>
/// The owned <see cref="Vst3HostAttributeList"/> is materialised lazily on the first call to
/// <see cref="GetAttributes"/>. Per the SDK convention <see cref="GetAttributes"/> returns a
/// borrowed pointer (the caller does not <c>Release</c> it); we hold one native ref on the
/// attribute list to keep it alive for the lifetime of this message and release it from the
/// finalizer once the message's own native refcount drops to zero.
/// </remarks>
[GeneratedComClass]
internal sealed unsafe partial class Vst3HostMessage : IMessage
{
    private IntPtr _messageId; // null-terminated UTF-8, owned by this instance
    private Vst3HostAttributeList? _attributes;
    private IntPtr _attributesPtr; // AddRef'd once; released in finalizer

    public IntPtr GetMessageId() => _messageId;

    public void SetMessageId(IntPtr id)
    {
        if (_messageId != IntPtr.Zero)
        {
            NativeMemory.Free((void*)_messageId);
            _messageId = IntPtr.Zero;
        }
        if (id == IntPtr.Zero) return;

        // Copy the caller's bytes into our own storage. We assume the input is a null-terminated
        // C string and round-trip it through UTF-8 to managed and back — that way we own a
        // freshly-allocated buffer and don't rely on the caller's pointer staying alive.
        var src = MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)id);
        if (src.IsEmpty)
        {
            _messageId = (IntPtr)NativeMemory.AllocZeroed(1);
            return;
        }
        var managed = Encoding.UTF8.GetString(src);
        var encoded = Encoding.UTF8.GetBytes(managed);
        var buffer = (byte*)NativeMemory.Alloc((nuint)encoded.Length + 1);
        encoded.AsSpan().CopyTo(new Span<byte>(buffer, encoded.Length));
        buffer[encoded.Length] = 0;
        _messageId = (IntPtr)buffer;
    }

    public IntPtr GetAttributes()
    {
        if (_attributesPtr == IntPtr.Zero)
        {
            _attributes = new Vst3HostAttributeList();
            var unk = Vst3ComWrappers.Instance.GetOrCreateComInterfaceForObject(
                _attributes, CreateComInterfaceFlags.None);
            try
            {
                var iid = Vst3StandardInterfaceIds.IAttributeList;
                var hr = Marshal.QueryInterface(unk, in iid, out _attributesPtr);
                if (hr != 0 || _attributesPtr == IntPtr.Zero)
                {
                    _attributesPtr = IntPtr.Zero;
                    return IntPtr.Zero;
                }
            }
            finally
            {
                Marshal.Release(unk);
            }
        }
        return _attributesPtr;
    }

    ~Vst3HostMessage()
    {
        if (_messageId != IntPtr.Zero)
        {
            NativeMemory.Free((void*)_messageId);
            _messageId = IntPtr.Zero;
        }
        if (_attributesPtr != IntPtr.Zero)
        {
            Marshal.Release(_attributesPtr);
            _attributesPtr = IntPtr.Zero;
        }
        _attributes = null;
    }
}
