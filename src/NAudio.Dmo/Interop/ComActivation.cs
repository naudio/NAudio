using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.Dmo.Interop;

/// <summary>
/// Activates COM objects via raw <c>CoCreateInstance</c> and projects them onto
/// <see cref="GeneratedComInterfaceAttribute"/> wrappers using
/// <see cref="StrategyBasedComWrappers"/>.
/// </summary>
/// <remarks>
/// <para>
/// Mirrors the helper of the same name in <c>NAudio.Wasapi</c>. Duplicated as an
/// internal helper rather than shared via a third "common" assembly — the surface
/// is tiny and a shared package would add a dependency-graph node for no real
/// benefit. Each NAudio Windows-COM package can keep its own copy.
/// </para>
/// <para>
/// Returned objects own the underlying COM reference. Cast to <see cref="IDisposable"/>
/// or use a <c>using</c> statement to release deterministically — the wrappers are
/// produced with <see cref="CreateObjectFlags.UniqueInstance"/>, which means the caller
/// is responsible for their lifetime.
/// </para>
/// </remarks>
internal static partial class ComActivation
{
    private const int CLSCTX_INPROC_SERVER = 0x1;

    private static readonly StrategyBasedComWrappers comWrappers = new();

    /// <summary>
    /// IID_IUnknown.
    /// </summary>
    public static readonly Guid IID_IUnknown = new("00000000-0000-0000-C000-000000000046");

    /// <summary>
    /// The shared <see cref="StrategyBasedComWrappers"/> used for both managed-to-native
    /// and native-to-managed projection of NAudio COM objects.
    /// </summary>
    public static StrategyBasedComWrappers ComWrappers => comWrappers;

    /// <summary>
    /// Activates the COM object identified by <paramref name="clsid"/> and returns a
    /// managed wrapper for the requested <typeparamref name="TInterface"/>.
    /// </summary>
    public static TInterface CreateInstance<TInterface>(Guid clsid, Guid iid)
        where TInterface : class
    {
        IntPtr unknown = CoCreateInstance(clsid, iid);
        try
        {
            return (TInterface)comWrappers.GetOrCreateObjectForComInstance(
                unknown, CreateObjectFlags.UniqueInstance);
        }
        finally
        {
            Marshal.Release(unknown);
        }
    }

    /// <summary>
    /// Releases both halves of a "raw IntPtr + source-generated RCW" pair.
    /// </summary>
    public static void ReleaseBoth<TInterface>(TInterface rcw, IntPtr ptr)
        where TInterface : class
    {
        if (rcw != null)
        {
            ((ComObject)(object)rcw).FinalRelease();
        }
        if (ptr != IntPtr.Zero)
        {
            Marshal.Release(ptr);
        }
    }

    /// <summary>
    /// Performs the raw <c>CoCreateInstance</c> P/Invoke and throws on failure.
    /// </summary>
    public static IntPtr CoCreateInstance(Guid clsid, Guid iid)
    {
        int hr = CoCreateInstanceNative(
            in clsid,
            IntPtr.Zero,
            CLSCTX_INPROC_SERVER,
            in iid,
            out IntPtr ptr);
        Marshal.ThrowExceptionForHR(hr);
        return ptr;
    }

    [LibraryImport("ole32.dll", EntryPoint = "CoCreateInstance")]
    private static partial int CoCreateInstanceNative(
        in Guid rclsid,
        IntPtr pUnkOuter,
        int dwClsContext,
        in Guid riid,
        out IntPtr ppv);
}
