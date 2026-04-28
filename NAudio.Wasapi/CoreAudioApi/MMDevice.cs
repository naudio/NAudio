using System;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wasapi.CoreAudioApi;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// A single Core Audio endpoint (<c>IMMDevice</c>) — render or capture, and one of
    /// the four <see cref="DeviceState"/> values. Provides access to the property store,
    /// session manager, endpoint volume, peak meter, device topology, and audio clients.
    /// Obtain instances from <see cref="MMDeviceEnumerator"/>.
    /// </summary>
    public class MMDevice : IDisposable
    {
        #region Variables
        private IMMDevice deviceInterface;
        private PropertyStore propertyStore;
        private AudioMeterInformation audioMeterInformation;
        private AudioEndpointVolume audioEndpointVolume;
        private AudioSessionManager audioSessionManager;
        private DeviceTopology deviceTopology;
        #endregion

        #region Guids
        // ReSharper disable InconsistentNaming
        private static readonly Guid IID_IAudioMeterInformation = new Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064");
        private static readonly Guid IID_IAudioEndpointVolume = new Guid("5CDF2C82-841E-4546-9722-0CF74078229A");
        private static readonly Guid IID_IAudioClient = new Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2");
        private static readonly Guid IDD_IAudioSessionManager = new Guid("BFA971F1-4D5E-40BB-935E-967039BFBEE4");
        private static readonly Guid IDD_IDeviceTopology = new Guid("2A07407E-6497-4A18-9787-32F79BD0D98F");
        // ReSharper restore InconsistentNaming
        #endregion

        #region Init
        /// <summary>
        /// Initializes the device's property store.
        /// </summary>
        /// <param name="stgmAccess">The storage-access mode to open store for.</param>
        /// <remarks>Administrative client is required for Write and ReadWrite modes.</remarks>
        public void GetPropertyInformation(StorageAccessMode stgmAccess = StorageAccessMode.Read)
        {
            CoreAudioException.ThrowIfFailed(deviceInterface.OpenPropertyStore(stgmAccess, out var ptr));
            try
            {
                var store = (IPropertyStore)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
                    ptr, CreateObjectFlags.UniqueInstance);
                propertyStore = new PropertyStore(store);
            }
            finally
            {
                Marshal.Release(ptr);
            }
        }

        private AudioClient GetAudioClient()
        {
            CoreAudioException.ThrowIfFailed(deviceInterface.Activate(IID_IAudioClient, ClsCtx.ALL, IntPtr.Zero, out var ptr));
            try
            {
                var client = (IAudioClient)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
                    ptr, CreateObjectFlags.UniqueInstance);
                return new AudioClient(client);
            }
            finally
            {
                Marshal.Release(ptr);
            }
        }

        private void GetAudioMeterInformation()
        {
            CoreAudioException.ThrowIfFailed(deviceInterface.Activate(IID_IAudioMeterInformation, ClsCtx.ALL, IntPtr.Zero, out var ptr));
            audioMeterInformation = new AudioMeterInformation(ptr);
        }

        private void GetAudioEndpointVolume()
        {
            CoreAudioException.ThrowIfFailed(deviceInterface.Activate(IID_IAudioEndpointVolume, ClsCtx.ALL, IntPtr.Zero, out var ptr));
            audioEndpointVolume = new AudioEndpointVolume(ptr);
        }

        private void GetAudioSessionManager()
        {
            CoreAudioException.ThrowIfFailed(deviceInterface.Activate(IDD_IAudioSessionManager, ClsCtx.ALL, IntPtr.Zero, out var ptr));
            audioSessionManager = new AudioSessionManager(ptr);
        }

        private void GetDeviceTopology()
        {
            CoreAudioException.ThrowIfFailed(deviceInterface.Activate(IDD_IDeviceTopology, ClsCtx.ALL, IntPtr.Zero, out var ptr));
            try
            {
                var topology = (IDeviceTopology)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
                    ptr, CreateObjectFlags.UniqueInstance);
                deviceTopology = new DeviceTopology(topology);
            }
            finally
            {
                Marshal.Release(ptr);
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Creates a new AudioClient for this device. Each call creates a new instance;
        /// the caller is responsible for disposing it.
        /// </summary>
        public AudioClient CreateAudioClient() => GetAudioClient();

        /// <summary>
        /// Audio Client — creates a new instance per call. Caller must dispose.
        /// </summary>
        [Obsolete("Use CreateAudioClient() instead — this property creates a new instance on every access, which is misleading.")]
        public AudioClient AudioClient => GetAudioClient();

        /// <summary>
        /// Audio Meter Information
        /// </summary>
        public AudioMeterInformation AudioMeterInformation
        {
            get
            {
                if (audioMeterInformation == null)
                    GetAudioMeterInformation();

                return audioMeterInformation;
            }
        }

        /// <summary>
        /// Audio Endpoint Volume
        /// </summary>
        public AudioEndpointVolume AudioEndpointVolume
        {
            get
            {
                if (audioEndpointVolume == null)
                    GetAudioEndpointVolume();

                return audioEndpointVolume;
            }
        }

        /// <summary>
        /// AudioSessionManager instance
        /// </summary>
        public AudioSessionManager AudioSessionManager
        {
            get
            {
                if (audioSessionManager == null)
                {
                    GetAudioSessionManager();
                }
                return audioSessionManager;
            }
        }

        /// <summary>
        /// DeviceTopology instance
        /// </summary>
        public DeviceTopology DeviceTopology
        {
            get
            {
                if (deviceTopology == null)
                {
                    GetDeviceTopology();
                }
                return deviceTopology;
            }
        }

        /// <summary>
        /// Properties
        /// </summary>
        public PropertyStore Properties
        {
            get
            {
                EnsurePropertyStoreExists();
                return propertyStore;
            }
        }

        private void EnsurePropertyStoreExists()
        {
            if (propertyStore == null)
                GetPropertyInformation();
        }

        /// <summary>
        /// Friendly name for the endpoint
        /// </summary>
        public string FriendlyName
        {
            get
            {
                EnsurePropertyStoreExists();

                return propertyStore.TryGetValue<string>(PropertyKeys.PKEY_Device_FriendlyName, out var value) 
                    ? value 
                    : "Unknown";
            }
        }

       /// <summary>
       /// Friendly name of device
       /// </summary>
        public string DeviceFriendlyName
        {
            get
            {
                EnsurePropertyStoreExists();
                
                return propertyStore.TryGetValue<string>(PropertyKeys.PKEY_DeviceInterface_FriendlyName, out var value)
                    ? value
                    : "Unknown";
            }
        }

        /// <summary>
        /// Icon path of device
        /// </summary>
        public string IconPath
        {
            get
            {
                EnsurePropertyStoreExists();
                
                return propertyStore.TryGetValue<string>(PropertyKeys.PKEY_Device_IconPath, out var value)
                    ? value
                    : "Unknown";
            }
        }

        /// <summary>
        /// Device Instance Id of Device
        /// </summary>
        public string InstanceId
        {
            get
            {
                EnsurePropertyStoreExists();
                
                return propertyStore.TryGetValue<string>(PropertyKeys.PKEY_Device_InstanceId, out var value)
                    ? value
                    : "Unknown";
            }
        }

        /// <summary>
        /// Device ID
        /// </summary>
        public string ID
        {
            get
            {
                CoreAudioException.ThrowIfFailed(deviceInterface.GetId(out var result));
                return result;
            }
        }

        /// <summary>
        /// Data Flow
        /// </summary>
        public DataFlow DataFlow
        {
            get
            {
                var ep = (IMMEndpoint)deviceInterface;
                CoreAudioException.ThrowIfFailed(ep.GetDataFlow(out var result));
                return result;
            }
        }

        /// <summary>
        /// Device State
        /// </summary>
        public DeviceState State
        {
            get
            {
                CoreAudioException.ThrowIfFailed(deviceInterface.GetState(out var result));
                return result;
            }
        }

        #endregion

        #region Constructor
        internal MMDevice(IMMDevice realDevice)
        {
            deviceInterface = realDevice;
        }
        #endregion

        /// <summary>
        /// To string
        /// </summary>
        public override string ToString()
        {
            return FriendlyName;
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            audioMeterInformation?.Dispose();
            audioMeterInformation = null;
            audioEndpointVolume?.Dispose();
            audioEndpointVolume = null;
            audioSessionManager?.Dispose();
            audioSessionManager = null;
            if (deviceInterface != null)
            {
                if ((object)deviceInterface is ComObject co)
                {
                    co.FinalRelease();
                }
                deviceInterface = null;
            }
            GC.SuppressFinalize(this);
        }
    }
}
