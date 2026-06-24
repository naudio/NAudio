using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using NAudio.Vst3.Interop;

namespace NAudio.Vst3.Hosting;

/// <summary>
/// Host-side <c>IHostApplication</c> (+ <c>IPlugInterfaceSupport</c>) implementation passed to a
/// plug-in as the <c>context</c> argument of <c>IPluginBase::initialize</c>.
/// </summary>
/// <remarks>
/// <para>
/// Source-generated CCW (<c>[GeneratedComClass]</c>) — implements two VST 3 interfaces so a
/// plug-in's <c>queryInterface</c> on the host pointer reaches either of them.
/// </para>
/// <para>
/// <see cref="CreateInstance"/> mints host-side <see cref="Vst3HostMessage"/> and
/// <see cref="Vst3HostAttributeList"/> CCWs on demand. JUCE-wrapped plug-ins lean on
/// <c>IAttributeList</c> during <c>IComponent::getState</c>, so this entry point gates state
/// save/load for the most popular wrapper.
/// </para>
/// </remarks>
[GeneratedComClass]
internal sealed unsafe partial class Vst3HostApplication : IHostApplication, IPlugInterfaceSupport
{
    /// <summary>Name reported to the plug-in. Kept short — the buffer is 128 UTF-16 chars.</summary>
    public const string HostName = "NAudio.Vst3";

    // Interfaces this host claims to implement via IPlugInterfaceSupport. Order is not
    // significant; identity is by IID.
    private static readonly Guid[] SupportedInterfaces =
    {
        Vst3StandardInterfaceIds.IHostApplication,
        Vst3StandardInterfaceIds.IPlugInterfaceSupport,
        Vst3StandardInterfaceIds.IComponentHandler,
        Vst3StandardInterfaceIds.IConnectionPoint,
        Vst3StandardInterfaceIds.IBStream,
        Vst3StandardInterfaceIds.IMessage,
        Vst3StandardInterfaceIds.IAttributeList,
    };

    public int GetName(IntPtr name)
    {
        if (name == IntPtr.Zero)
        {
            return TResultCodes.InvalidArgument;
        }

        var dst = new Span<char>((void*)name, 128);
        dst.Clear();
        var src = HostName.AsSpan();
        var copy = Math.Min(src.Length, dst.Length - 1);
        src[..copy].CopyTo(dst);
        return TResultCodes.Ok;
    }

    public int CreateInstance(IntPtr cid, IntPtr iid, out IntPtr obj)
    {
        obj = IntPtr.Zero;
        if (cid == IntPtr.Zero || iid == IntPtr.Zero)
        {
            return TResultCodes.False;
        }

        var cidGuid = ReadTuid(cid);
        var iidGuid = ReadTuid(iid);

        // The SDK's reference HostApplication only mints message/attribute-list helpers, and only
        // when the caller asks for the canonical (cid == iid) pairing.
        if (cidGuid == Vst3StandardInterfaceIds.IMessage && iidGuid == Vst3StandardInterfaceIds.IMessage)
        {
            obj = CreateCcw(new Vst3HostMessage(), iidGuid);
            return obj == IntPtr.Zero ? TResultCodes.False : TResultCodes.Ok;
        }
        if (cidGuid == Vst3StandardInterfaceIds.IAttributeList && iidGuid == Vst3StandardInterfaceIds.IAttributeList)
        {
            obj = CreateCcw(new Vst3HostAttributeList(), iidGuid);
            return obj == IntPtr.Zero ? TResultCodes.False : TResultCodes.Ok;
        }
        return TResultCodes.False;
    }

    public int IsPlugInterfaceSupported(IntPtr iid)
    {
        if (iid == IntPtr.Zero) return TResultCodes.False;
        var iidGuid = ReadTuid(iid);
        foreach (var supported in SupportedInterfaces)
        {
            if (supported == iidGuid)
            {
                return TResultCodes.True;
            }
        }
        return TResultCodes.False;
    }

    private static IntPtr CreateCcw(object ccwTarget, Guid iidGuid)
    {
        var unk = Vst3ComWrappers.Instance.GetOrCreateComInterfaceForObject(
            ccwTarget, CreateComInterfaceFlags.None);
        try
        {
            var hr = Marshal.QueryInterface(unk, in iidGuid, out var ptr);
            return hr == 0 ? ptr : IntPtr.Zero;
        }
        finally
        {
            Marshal.Release(unk);
        }
    }

    private static Guid ReadTuid(IntPtr ptr) =>
        new(new ReadOnlySpan<byte>((void*)ptr, 16));
}
