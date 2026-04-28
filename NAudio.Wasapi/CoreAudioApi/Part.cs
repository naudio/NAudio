using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wasapi.CoreAudioApi;
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Audio Part
    /// </summary>
    public class Part
    {
        private const int E_NOTFOUND = unchecked((int)0x80070490);
        private readonly IPart partInterface;
        private DeviceTopology deviceTopology;
        private static Guid IID_IAudioVolumeLevel = new Guid("7FB7B48F-531D-44A2-BCB3-5AD5A134B3DC");
        private static Guid IID_IAudioMute = new Guid("DF45AEEA-B74A-4B6B-AFAD-2366B6AA012E");
        private static Guid IID_IAudioEndpointVolume = new Guid("5CDF2C82-841E-4546-9722-0CF74078229A");
        private static Guid IID_IKsJackDescription = new Guid("4509F757-2D46-4637-8E62-CE7DB944F57B");

        internal Part(IPart part)
        {
            partInterface = part;
        }

        // Projects a COM IntPtr returned by IPart::* onto a [GeneratedComInterface]
        // wrapper (UniqueInstance) and releases the input pointer. Caller takes ownership
        // of the returned wrapper.
        private static T WrapAndRelease<T>(IntPtr ptr) where T : class
        {
            try
            {
                return ComActivation.WrapUnique<T>(ptr);
            }
            finally
            {
                Marshal.Release(ptr);
            }
        }

        /// <summary>
        /// Name
        /// </summary>
        public string Name
        {
            get
            {
                partInterface.GetName(out var result);
                return result;
            }
        }

        /// <summary>
        /// Local ID
        /// </summary>
        public uint LocalId
        {
            get
            {
                partInterface.GetLocalId(out var result);
                return result;
            }
        }

        /// <summary>
        /// Global ID
        /// </summary>
        public string GlobalId
        {
            get
            {
                partInterface.GetGlobalId(out var result);
                return result;
            }
        }

        /// <summary>
        /// Part Type
        /// </summary>
        public PartTypeEnum PartType
        {
            get
            {
                partInterface.GetPartType(out var result);
                return result;
            }
        }

        /// <summary>
        /// Sub Type
        /// </summary>
        public Guid GetSubType
        {
            get
            {
                partInterface.GetSubType(out var result);
                return result;
            }
        }

        /// <summary>
        /// Control Interface Count
        /// </summary>
        public uint ControlInterfaceCount
        {
            get
            {
                partInterface.GetControlInterfaceCount(out var result);
                return result;
            }
        }

        /// <summary>
        /// Get Control Interface by index
        /// </summary>
        internal IControlInterface GetControlInterface(uint index)
        {
            partInterface.GetControlInterface(index, out var ptr);
            return WrapAndRelease<IControlInterface>(ptr);
        }

        /// <summary>
        /// Incoming parts list
        /// </summary>
        public PartsList PartsIncoming
        {
            get
            {
                var hr = partInterface.EnumPartsIncoming(out var ptr);
                if (hr == 0)
                {
                    return new PartsList(WrapAndRelease<IPartsList>(ptr));
                }
                return hr == E_NOTFOUND ? new PartsList(null) : throw new InvalidOperationException($"{nameof(IPart.EnumPartsIncoming)} failed (HRESULT: 0x{hr:X8})");
            }
        }

        /// <summary>
        /// Outgoing parts list
        /// </summary>
        public PartsList PartsOutgoing
        {
            get
            {
                var hr = partInterface.EnumPartsOutgoing(out var ptr);
                if (hr == 0)
                {
                    return new PartsList(WrapAndRelease<IPartsList>(ptr));
                }
                return hr == E_NOTFOUND ? new PartsList(null) : throw new InvalidOperationException($"{nameof(IPart.EnumPartsOutgoing)} failed (HRESULT: 0x{hr:X8})");
            }
        }

        /// <summary>
        /// Device topology
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
        /// Audio Volume Level
        /// </summary>
        public AudioVolumeLevel AudioVolumeLevel
        {
            get
            {
                var hr = partInterface.Activate(ClsCtx.ALL, ref IID_IAudioVolumeLevel, out var ptr);
                if (hr != 0 || ptr == IntPtr.Zero) return null;
                return new AudioVolumeLevel(WrapAndRelease<IAudioVolumeLevel>(ptr));
            }
        }

        /// <summary>
        /// Audio Mute
        /// </summary>
        public AudioMute AudioMute
        {
            get
            {
                var hr = partInterface.Activate(ClsCtx.ALL, ref IID_IAudioMute, out var ptr);
                if (hr != 0 || ptr == IntPtr.Zero) return null;
                return new AudioMute(WrapAndRelease<IAudioMute>(ptr));
            }
        }

        /// <summary>
        /// Jack Description
        /// </summary>
        public KsJackDescription JackDescription
        {
            get
            {
                var hr = partInterface.Activate(ClsCtx.ALL, ref IID_IKsJackDescription, out var ptr);
                if (hr != 0 || ptr == IntPtr.Zero) return null;
                return new KsJackDescription(WrapAndRelease<IKsJackDescription>(ptr));
            }
        }

        private void GetDeviceTopology()
        {
            CoreAudioException.ThrowIfFailed(partInterface.GetTopologyObject(out var ptr));
            deviceTopology = new DeviceTopology(WrapAndRelease<IDeviceTopology>(ptr));
        }
    }
}
