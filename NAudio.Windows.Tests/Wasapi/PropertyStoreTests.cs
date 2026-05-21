using System;
using System.Diagnostics;
using NAudio.CoreAudioApi;
using NAudioTests.Utils;
using NUnit.Framework;

namespace NAudioTests.Wasapi
{
    [TestFixture]
    [Category("IntegrationTest")]
    public class PropertyStoreTests
    {
        [SetUp]
        public void SetUp()
        {
            OSUtils.RequireVista();
        }

        [Test]
        public void CanReadFriendlyNameFormFactorAndStateForAllOutputDevices()
        {
            var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.All);

            Assert.That(devices.Count, Is.GreaterThan(0), "expected at least one render endpoint");

            foreach (MMDevice device in devices)
            {
                // FriendlyName goes through PropertyStore.TryGetValue<string> → VT_LPWSTR
                string friendlyName = device.FriendlyName;
                Assert.That(friendlyName, Is.Not.Null.And.Not.Empty, "FriendlyName should be populated");

                // DataFlow / State are not PropertyStore-backed but exercise the wider device path
                Assert.That(device.DataFlow, Is.EqualTo(DataFlow.Render));
                Assert.That(Enum.IsDefined(typeof(DeviceState), device.State), Is.True);

                // FormFactor is VT_UI4 — exercise the indexer + PropertyStoreProperty.Value path
                if (device.Properties.Contains(PropertyKeys.PKEY_AudioEndpoint_FormFactor))
                {
                    var prop = device.Properties[PropertyKeys.PKEY_AudioEndpoint_FormFactor];
                    Assert.That(prop.Value, Is.Not.Null);
                    Assert.That(prop.Value, Is.TypeOf<uint>());
                }

                // GUID is VT_LPWSTR (a stringified GUID) — another VT_LPWSTR exercise
                if (device.Properties.Contains(PropertyKeys.PKEY_AudioEndpoint_GUID))
                {
                    var prop = device.Properties[PropertyKeys.PKEY_AudioEndpoint_GUID];
                    Assert.That(prop.Value, Is.InstanceOf<string>());
                }

                Debug.WriteLine($"{friendlyName}: state={device.State}, props={device.Properties.Count}");
            }
        }

        [Test]
        public void CanEnumerateEveryPropertyOnDefaultRenderEndpoint()
        {
            var enumerator = new MMDeviceEnumerator();
            var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);

            int count = device.Properties.Count;
            Assert.That(count, Is.GreaterThan(0));

            for (int i = 0; i < count; i++)
            {
                // Some VTs (e.g. VT_VECTOR | VT_LPWSTR) are not handled by PropVariant.Value yet —
                // tolerate that here; the goal of this test is to verify the GeneratedComInterface
                // migration round-trips for every property index without dangling pointers or AVs.
                try
                {
                    var prop = device.Properties[i];
                    Debug.WriteLine($"  [{i}] {prop.Key.formatId}:{prop.Key.propertyId} -> {prop.Value ?? "(null)"}");
                }
                catch (NotImplementedException ex)
                {
                    Debug.WriteLine($"  [{i}] unsupported VT: {ex.Message}");
                }
            }
        }

        [Test]
        public void DeviceFormatBlobReadsAsByteArray()
        {
            var enumerator = new MMDeviceEnumerator();
            var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);

            // PKEY_AudioEngine_DeviceFormat is a VT_BLOB containing a WAVEFORMATEX(TENSIBLE) — exercise the BLOB path.
            if (!device.Properties.Contains(PropertyKeys.PKEY_AudioEngine_DeviceFormat))
            {
                Assert.Ignore("Default render endpoint does not expose PKEY_AudioEngine_DeviceFormat");
            }

            var prop = device.Properties[PropertyKeys.PKEY_AudioEngine_DeviceFormat];
            Assert.That(prop.Value, Is.InstanceOf<byte[]>());
            var bytes = (byte[])prop.Value;
            Assert.That(bytes.Length, Is.GreaterThanOrEqualTo(18), "WAVEFORMATEX is at least 18 bytes");
        }
    }
}
