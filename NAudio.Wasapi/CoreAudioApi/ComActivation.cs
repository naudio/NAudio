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
                return WrapUnique<TInterface>(unknown);
            }
            finally
            {
                // GetOrCreateObjectForComInstance AddRef'd via QueryInterface internally,
                // so we release the reference acquired by CoCreateInstance here.
                Marshal.Release(unknown);
            }
        }

        /// <summary>
        /// Projects an existing <c>IUnknown*</c> onto a <see cref="GeneratedComInterfaceAttribute"/>
        /// wrapper using <see cref="CreateObjectFlags.UniqueInstance"/>, then suppresses the
        /// wrapper's finalizer.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Allowing <see cref="StrategyBasedComWrappers"/>' generated finalizer to run on the
        /// GC finalizer thread for our CoreAudio / DMO wrappers reliably trips an access
        /// violation in <c>Marshal.Release</c> (or a <c>FAIL_FAST</c> on an
        /// <c>UnmanagedCallersOnly</c> trampoline whose backing memory was freed). Captured
        /// in NAudioDemo via <c>MiniDumpInstaller</c> during the WasapiPlayer → Volume Mixer
        /// manual repro: with this <c>SuppressFinalize</c> commented out the next finalizer
        /// pass aborts the process. With it in place the demo runs cleanly.
        /// </para>
        /// <para>
        /// Mechanism is not fully understood. Investigation eliminated the obvious hypothesis:
        /// <c>IAgileObjectProbeTests</c> shows the four apartment-looking CoreAudio interfaces
        /// (<c>IMMDevice</c>, <c>IMMDeviceCollection</c>, <c>IPropertyStore</c>,
        /// <c>IDeviceTopology</c>) have no <c>IAgileObject</c>, no <c>IMarshal</c>, and no
        /// registered proxy/stub, which means there is no defined cross-apartment marshaling
        /// path at all — yet classic RCW happily released them on the finalizer thread,
        /// implying their <c>Release</c> is a thread-safe <c>InterlockedDecrement</c> and
        /// apartment isn't the issue. The remaining candidate is a use-after-free on a cached
        /// QI'd vtable inside the wrapper — somewhere in the <c>StrategyBasedComWrappers</c>
        /// cache strategy, the entry's underlying memory is gone by the time the finalizer
        /// dereferences it. We were not able to nail down the exact path within this session.
        /// </para>
        /// <para>
        /// What we ruled in: a separate, related self-inflicted bug — the explicit triple
        /// <c>FinalRelease</c> in <c>AudioClient.Dispose</c> (one each for
        /// <c>audioClientInterface3 / 2 / interface</c>) — was AVing on its own and was the
        /// stack the original handover documented. That has been removed (single
        /// <c>FinalRelease</c> on the aliased ComObject is sufficient since DICASTABLE
        /// returns the same wrapper). The dispose-path AV is gone; only the finalizer-path
        /// AV remains, and is what this suppression masks.
        /// </para>
        /// <para>
        /// Suppressing the finalizer converts "caller forgot to Dispose" from a process-fatal
        /// crash into a COM-reference leak that persists until process exit. The leak is
        /// recoverable; the crash is not. The deterministic release path
        /// (<c>((ComObject)(object)wrapper).FinalRelease()</c> from a wrapper's
        /// <c>Dispose</c>) is unchanged and remains the only correct way to release.
        /// </para>
        /// <para>
        /// Suppression is broader than strictly necessary — eight of the twelve CoreAudio
        /// interfaces the probe categorises are truly agile (<c>IAgileObject</c> + FTM) or
        /// FTM-aggregated, so suppressing their finalizers costs an avoidable leak on missed
        /// Dispose. Narrowing the suppression set is feasible in principle — bisect against
        /// <c>NAudioDemo</c> with <c>MiniDumpInstaller</c> in place — but until that's done
        /// the blanket suppression is the safe default. See <c>IAgileObjectProbeTests</c>
        /// for the per-interface agility matrix.
        /// </para>
        /// <para>
        /// Does NOT call <c>Marshal.Release</c> on <paramref name="ptr"/> — the caller still
        /// owns that reference and must release it (the wrapper holds its own QI'd ref).
        /// </para>
        /// </remarks>
        public static T WrapUnique<T>(IntPtr ptr) where T : class
        {
            var wrapper = (T)comWrappers.GetOrCreateObjectForComInstance(
                ptr, CreateObjectFlags.UniqueInstance);
            // Deliberately suppress the wrapper's finalizer (not 'this'): see method
            // remarks. The wrapper is owned by the caller and only its explicit
            // FinalRelease via Dispose is the safe release path.
#pragma warning disable CA1816
            GC.SuppressFinalize(wrapper);
#pragma warning restore CA1816
            return wrapper;
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
