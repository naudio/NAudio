using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wasapi.CoreAudioApi;
using NAudio.Wasapi.CoreAudioApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace NAudio.CoreAudioApi
{
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

        public string Name
        {
            get
            {
                partInterface.GetName(out var result);
                return result;
            }
        }

        public uint LocalId
        {
            get
            {
                partInterface.GetLocalId(out var result);
                return result;
            }
        }

        public string GlobalId
        {
            get
            {
                partInterface.GetGlobalId(out var result);
                return result;
            }
        }

        public PartTypeEnum PartType
        {
            get
            {
                partInterface.GetPartType(out var result);
                return result;
            }
        }

        public Guid GetSubType
        {
            get
            {
                partInterface.GetSubType(out var result);
                return result;
            }
        }

        public uint ControlInterfaceCount
        {
            get
            {
                partInterface.GetControlInterfaceCount(out var result);
                return result;
            }
        }

        public IControlInterface GetControlInterface(uint index)
        {
            partInterface.GetControlInterface(index, out var result);
            return result;
        }

        public PartsList PartsIncoming
        {
            get
            {
                var hr = partInterface.EnumPartsIncoming(out var result);
                return hr == 0 ? new PartsList(result) : hr == E_NOTFOUND ? new PartsList(null) : throw new COMException(nameof(IPart.EnumPartsIncoming), hr);
            }
        }

        public PartsList PartsOutgoing
        {
            get
            {
                var hr = partInterface.EnumPartsOutgoing(out var result);
                return hr == 0 ? new PartsList(result) : hr == E_NOTFOUND ? new PartsList(null) : throw new COMException(nameof(IPart.EnumPartsOutgoing), hr);
            }
        }

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

        public AudioVolumeLevel AudioVolumeLevel
        {
            get
            {
                var hr = partInterface.Activate(ClsCtx.ALL, ref IID_IAudioVolumeLevel, out var result);
                return hr == 0 ? new AudioVolumeLevel(result as IAudioVolumeLevel) : null;
            }
        }

        public AudioMute AudioMute
        {
            get
            {
                var hr = partInterface.Activate(ClsCtx.ALL, ref IID_IAudioMute, out var result);
                return hr == 0 ? new AudioMute(result as IAudioMute) : null;
            }
        }

        public KsJackDescription JackDescription
        {
            get
            {
                var hr = partInterface.Activate(ClsCtx.ALL, ref IID_IKsJackDescription, out var result);
                return hr == 0 ? new KsJackDescription(result as IKsJackDescription) : null;
            }
        }

        private void GetDeviceTopology()
        {
            Marshal.ThrowExceptionForHR(partInterface.GetTopologyObject(out var result));
            deviceTopology = new DeviceTopology(result as IDeviceTopology);
        }
    }
}
