using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.Wasapi.CoreAudioApi
{
    /// <summary>
    /// Activates COM objects via raw <c>CoCreateInstance</c> and projects them onto
    /// <see cref="GeneratedComInterfaceAttribute"/> wrappers using
    /// <see cref="StrategyBasedComWrappers"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the modern replacement for the <c>new SomeComImportCoclass()</c> activation
    /// pattern. The legacy pattern produces a thread-affine RCW that fails with
    /// <c>InvalidComObjectException</c> when accessed across an apartment boundary
    /// (the canonical NAudio scenario being a DMO constructed on a WinForms / WPF STA
    /// thread and consumed from an MTA audio thread).
    /// </para>
    /// <para>
    /// Returned objects own the underlying COM reference. Cast to <see cref="IDisposable"/>
    /// or use a <c>using</c> statement to release deterministically — the wrappers are
    /// produced with <see cref="CreateObjectFlags.UniqueInstance"/>, which means the caller
    /// is responsible for their lifetime.
    /// </para>
    /// </remarks>
    internal static class ComActivation
    {
        private static readonly StrategyBasedComWrappers comWrappers = new();

        /// <summary>
        /// IID_IUnknown.
        /// </summary>
        public static readonly Guid IID_IUnknown = new Guid("00000000-0000-0000-C000-000000000046");

        /// <summary>
        /// The shared <see cref="StrategyBasedComWrappers"/> used for both managed-to-native
        /// and native-to-managed projection of NAudio COM objects.
        /// </summary>
        public static StrategyBasedComWrappers ComWrappers => comWrappers;

        /// <summary>
        /// Activates the COM object identified by <paramref name="clsid"/> and returns a
        /// managed wrapper for the requested <typeparamref name="TInterface"/>.
        /// </summary>
        /// <typeparam name="TInterface">
        /// A <see cref="GeneratedComInterfaceAttribute"/>-decorated interface.
        /// </typeparam>
        /// <param name="clsid">The CLSID of the COM coclass to activate.</param>
        /// <param name="iid">
        /// The IID requested from <c>CoCreateInstance</c>. Pass the IID of
        /// <typeparamref name="TInterface"/> (or any compatible interface — typically IUnknown
        /// or the same interface).
        /// </param>
        /// <returns>
        /// A managed wrapper that owns the underlying COM reference. Dispose to release.
        /// </returns>
        public static TInterface CreateInstance<TInterface>(Guid clsid, Guid iid)
            where TInterface : class
        {
            IntPtr unknown = CoCreateInstance(clsid, iid);
            try
            {
                return (TInterface)comWrappers.GetOrCreateObjectForComInstance(
                    unknown,
                    CreateObjectFlags.UniqueInstance);
            }
            finally
            {
                // GetOrCreateObjectForComInstance AddRef'd via QueryInterface internally,
                // so we release the reference acquired by CoCreateInstance here.
                Marshal.Release(unknown);
            }
        }

        /// <summary>
        /// Performs the raw <c>CoCreateInstance</c> P/Invoke and throws on failure.
        /// </summary>
        public static IntPtr CoCreateInstance(Guid clsid, Guid iid)
        {
            int hr = NativeMethods.CoCreateInstance(
                in clsid,
                IntPtr.Zero,
                NativeMethods.CLSCTX_INPROC_SERVER,
                in iid,
                out IntPtr ptr);
            Marshal.ThrowExceptionForHR(hr);
            return ptr;
        }
    }
}
