using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudioTests.Utils;
using NUnit.Framework;

namespace NAudioTests.Wasapi
{
    /// <summary>
    /// Diagnostic probe that records, for each CoreAudio wrapper NAudio produces via
    /// ComWrappers, whether the underlying COM object exposes <c>IAgileObject</c> /
    /// <c>IMarshal</c> / a registered proxy-stub. Originally written to test the
    /// apartment-marshaling hypothesis for the .NET 8 finalizer crash; that crash was
    /// later root-caused to dotnet/runtime PR #110007 (DICASTABLE CastCache poisoning,
    /// fixed in .NET 9) and the floor TFM was bumped accordingly. Kept around as a
    /// reusable diagnostic for any future apartment-related question.
    /// </summary>
    [TestFixture]
    [Category("IntegrationTest")]
    public class IAgileObjectProbeTests
    {
        // IID_IAgileObject from objidlbase.h
        private static readonly Guid IID_IAgileObject = new Guid("94ea2b94-e9cc-49e0-c0ff-ee64ca8f5b90");

        // IID_IMarshal — IUnknown-derived; vtable slot 3 (after QI/AddRef/Release) is GetUnmarshalClass.
        private static readonly Guid IID_IMarshal = new Guid("00000003-0000-0000-C000-000000000046");

        // CLSID of the Free-Threaded Marshaler. If GetUnmarshalClass returns this, the
        // object aggregates the FTM and is functionally agile (safe to use from any thread)
        // even though it doesn't expose IAgileObject.
        private static readonly Guid CLSID_InProcFreeMarshaler = new Guid("0000033A-0000-0000-C000-000000000046");

        // MSHCTX_INPROC = 3 (in-process marshaling), MSHLFLAGS_NORMAL = 0.
        private const int MSHCTX_INPROC = 3;
        private const int MSHLFLAGS_NORMAL = 0;

        [Test]
        public void ProbeCoreAudioObjectsForIAgileObject()
        {
            OSUtils.RequireVista();

            var report = new StringBuilder();
            report.AppendLine("CoreAudio IAgileObject probe — current thread apartment: " +
                Thread.CurrentThread.GetApartmentState());
            report.AppendLine();

            try
            {
                ProbeAll(report);
            }
            finally
            {
                Console.WriteLine(report.ToString());
                TestContext.Out.WriteLine(report.ToString());
            }
        }

        private static void ProbeAll(StringBuilder report)
        {
            using var enumerator = new MMDeviceEnumerator();
            ProbeWrapperField(enumerator, "realEnumerator", "IMMDeviceEnumerator", report);

            using var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            ProbeWrapperField(devices, "mmDeviceCollection", "IMMDeviceCollection", report);

            if (devices.Count == 0)
            {
                report.AppendLine("(no active render endpoints — skipping per-device probes)");
                Assert.Inconclusive("No active render endpoints to probe.");
                return;
            }

            using var device = devices[0];
            ProbeWrapperField(device, "deviceInterface", "IMMDevice", report);

            // Force property store to be populated, then probe it via PropertyStore.storeInterface.
            _ = device.FriendlyName;
            var propertyStore = GetPrivateField<object>(device, "propertyStore");
            ProbeWrapperField(propertyStore, "storeInterface", "IPropertyStore", report);

            using var audioClient = device.CreateAudioClient();
            ProbeWrapperField(audioClient, "audioClientInterface", "IAudioClient", report);

            // Initialize so we can probe IAudioRenderClient / IAudioClockClient via GetService.
            audioClient.Initialize(AudioClientShareMode.Shared, AudioClientStreamFlags.None,
                100 * 10000L, 0, audioClient.MixFormat, Guid.Empty);

            var renderClient = audioClient.AudioRenderClient;
            ProbeWrapperField(renderClient, "audioRenderClientInterface", "IAudioRenderClient", report);

            var clockClient = audioClient.AudioClockClient;
            ProbeWrapperField(clockClient, "audioClockClientInterface", "IAudioClockClient", report);

            var sessionManager = device.AudioSessionManager;
            ProbeWrapperField(sessionManager, "audioSessionInterface", "IAudioSessionManager", report);

            var sessions = sessionManager.Sessions;
            if (sessions != null)
            {
                ProbeWrapperField(sessions, "audioSessionEnumerator", "IAudioSessionEnumerator", report);
                if (sessions.Count > 0)
                {
                    using var session = sessions[0];
                    ProbeWrapperField(session, "audioSessionControlInterface", "IAudioSessionControl", report);
                }
                else
                {
                    report.AppendLine("(no audio sessions on default endpoint — skipping IAudioSessionControl probe)");
                }
            }

            var deviceTopology = device.DeviceTopology;
            ProbeWrapperField(deviceTopology, "deviceTopologyInterface", "IDeviceTopology", report);

            var endpointVolume = device.AudioEndpointVolume;
            ProbeWrapperField(endpointVolume, "audioEndPointVolume", "IAudioEndpointVolume", report);
        }

        private static void ProbeWrapperField(object owner, string fieldName, string interfaceName, StringBuilder report)
        {
            if (owner == null)
            {
                report.AppendLine($"  {interfaceName,-32} : owner is null — skipped");
                return;
            }

            var wrapper = GetPrivateField<object>(owner, fieldName);
            if (wrapper == null)
            {
                report.AppendLine($"  {interfaceName,-32} : field '{fieldName}' is null — skipped");
                return;
            }

            if (!ComWrappers.TryGetComInstance(wrapper, out var unknown))
            {
                report.AppendLine($"  {interfaceName,-32} : not a ComWrappers wrapper (type {wrapper.GetType().FullName})");
                return;
            }

            try
            {
                var iidAgile = IID_IAgileObject;
                int hrAgile = Marshal.QueryInterface(unknown, in iidAgile, out var agile);
                string agileLabel;
                if (hrAgile == 0)
                {
                    Marshal.Release(agile);
                    agileLabel = "IAgileObject=YES";
                }
                else
                {
                    agileLabel = $"IAgileObject=no(0x{hrAgile:X8})";
                }

                string marshalLabel = ProbeFreeThreadedMarshaler(unknown);
                report.AppendLine($"  {interfaceName,-32} : {agileLabel,-30} {marshalLabel}");
            }
            finally
            {
                Marshal.Release(unknown);
            }
        }

        private static string ProbeFreeThreadedMarshaler(IntPtr unknown)
        {
            var iidMarshal = IID_IMarshal;
            int hrQi = Marshal.QueryInterface(unknown, in iidMarshal, out IntPtr pMarshal);
            if (hrQi != 0)
            {
                return $"IMarshal=no(0x{hrQi:X8}) — uses default proxy/stub";
            }
            try
            {
                // Vtable slot 3 = GetUnmarshalClass.
                // HRESULT GetUnmarshalClass(REFIID riid, void* pv, DWORD dwDestContext,
                //     void* pvDestContext, DWORD mshlflags, CLSID* pCid)
                IntPtr vtable = Marshal.ReadIntPtr(pMarshal);
                IntPtr pGetUnmarshalClass = Marshal.ReadIntPtr(vtable, 3 * IntPtr.Size);

                Guid clsid = Guid.Empty;
                Guid iidUnk = new Guid("00000000-0000-0000-C000-000000000046");
                int hr;
                unsafe
                {
                    var fn = (delegate* unmanaged<IntPtr, Guid*, IntPtr, int, IntPtr, int, Guid*, int>)
                        pGetUnmarshalClass;
                    hr = fn(pMarshal, &iidUnk, IntPtr.Zero, MSHCTX_INPROC, IntPtr.Zero, MSHLFLAGS_NORMAL, &clsid);
                }
                if (hr != 0)
                {
                    return $"IMarshal=YES, GetUnmarshalClass hr=0x{hr:X8}";
                }
                if (clsid == CLSID_InProcFreeMarshaler)
                {
                    return "IMarshal=FTM (free-threaded)";
                }
                return $"IMarshal=YES, custom marshaler {clsid}";
            }
            finally
            {
                Marshal.Release(pMarshal);
            }
        }

        private static T GetPrivateField<T>(object owner, string fieldName)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            for (var t = owner.GetType(); t != null; t = t.BaseType)
            {
                var field = t.GetField(fieldName, flags);
                if (field != null)
                    return (T)field.GetValue(owner);
            }
            throw new InvalidOperationException(
                $"Field '{fieldName}' not found on {owner.GetType().FullName} or its bases.");
        }
    }
}
