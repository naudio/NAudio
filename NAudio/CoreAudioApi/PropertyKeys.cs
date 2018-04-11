/*
  LICENSE
  -------
  Copyright (C) 2007 Ray Molenkamp

  This source code is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this source code or the software it produces.

  Permission is granted to anyone to use this source code for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this source code must not be misrepresented; you must not
     claim that you wrote the original source code.  If you use this source code
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original source code.
  3. This notice may not be removed or altered from any source distribution.
*/
using System;
// ReSharper disable InconsistentNaming

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Property Keys
    /// </summary>
    public static class PropertyKeys
    {
        /// <summary>
        /// PKEY_DeviceInterface_FriendlyName
        /// </summary>
        public static readonly PropertyKey PKEY_DeviceInterface_FriendlyName = new PropertyKey(new Guid(0x026e516e, unchecked((short)0xb814), 0x414b, 0x83, 0xcd, 0x85, 0x6d, 0x6f, 0xef, 0x48, 0x22), 2);
        /// <summary>
        /// PKEY_AudioEndpoint_FormFactor
        /// </summary>
        public static readonly PropertyKey PKEY_AudioEndpoint_FormFactor = new PropertyKey(new Guid(0x1da5d803, unchecked((short)0xd492), 0x4edd, 0x8c, 0x23, 0xe0, 0xc0, 0xff, 0xee, 0x7f, 0x0e), 0);
        /// <summary>
        /// PKEY_AudioEndpoint_ControlPanelPageProvider
        /// </summary>
        public static readonly PropertyKey PKEY_AudioEndpoint_ControlPanelPageProvider = new PropertyKey(new Guid(0x1da5d803, unchecked((short)0xd492), 0x4edd, 0x8c, 0x23, 0xe0, 0xc0, 0xff, 0xee, 0x7f, 0x0e), 1);
        /// <summary>
        /// PKEY_AudioEndpoint_Association
        /// </summary>
        public static readonly PropertyKey PKEY_AudioEndpoint_Association = new PropertyKey(new Guid(0x1da5d803, unchecked((short)0xd492), 0x4edd, 0x8c, 0x23, 0xe0, 0xc0, 0xff, 0xee, 0x7f, 0x0e), 2);
        /// <summary>
        /// PKEY_AudioEndpoint_PhysicalSpeakers
        /// </summary>
        public static readonly PropertyKey PKEY_AudioEndpoint_PhysicalSpeakers = new PropertyKey(new Guid(0x1da5d803, unchecked((short)0xd492), 0x4edd, 0x8c, 0x23, 0xe0, 0xc0, 0xff, 0xee, 0x7f, 0x0e), 3);
        /// <summary>
        /// PKEY_AudioEndpoint_GUID
        /// </summary>
        public static readonly PropertyKey PKEY_AudioEndpoint_GUID = new PropertyKey(new Guid(0x1da5d803, unchecked((short)0xd492), 0x4edd, 0x8c, 0x23, 0xe0, 0xc0, 0xff, 0xee, 0x7f, 0x0e), 4);
        /// <summary>
        /// PKEY_AudioEndpoint_Disable_SysFx 
        /// </summary>
        public static readonly PropertyKey PKEY_AudioEndpoint_Disable_SysFx = new PropertyKey(new Guid(0x1da5d803, unchecked((short)0xd492), 0x4edd, 0x8c, 0x23, 0xe0, 0xc0, 0xff, 0xee, 0x7f, 0x0e), 5);
        /// <summary>
        /// PKEY_AudioEndpoint_FullRangeSpeakers 
        /// </summary>
        public static readonly PropertyKey PKEY_AudioEndpoint_FullRangeSpeakers = new PropertyKey(new Guid(0x1da5d803, unchecked((short)0xd492), 0x4edd, 0x8c, 0x23, 0xe0, 0xc0, 0xff, 0xee, 0x7f, 0x0e), 6);
        /// <summary>
        /// PKEY_AudioEndpoint_Supports_EventDriven_Mode 
        /// </summary>
        public static readonly PropertyKey PKEY_AudioEndpoint_Supports_EventDriven_Mode = new PropertyKey(new Guid(0x1da5d803, unchecked((short)0xd492), 0x4edd, 0x8c, 0x23, 0xe0, 0xc0, 0xff, 0xee, 0x7f, 0x0e), 7);
        /// <summary>
        /// PKEY_AudioEndpoint_JackSubType
        /// </summary>
        public static readonly PropertyKey PKEY_AudioEndpoint_JackSubType = new PropertyKey(new Guid(0x1da5d803, unchecked((short)0xd492), 0x4edd, 0x8c, 0x23, 0xe0, 0xc0, 0xff, 0xee, 0x7f, 0x0e), 8);
        /// <summary>
        /// PKEY_AudioEngine_DeviceFormat 
        /// </summary>
        public static readonly PropertyKey PKEY_AudioEngine_DeviceFormat = new PropertyKey(new Guid(unchecked((int)0xf19f064d), 0x82c, 0x4e27, 0xbc, 0x73, 0x68, 0x82, 0xa1, 0xbb, 0x8e, 0x4c), 0);
        /// <summary>
        /// PKEY_AudioEngine_OEMFormat
        /// </summary>
        public static readonly PropertyKey PKEY_AudioEngine_OEMFormat = new PropertyKey(new Guid(unchecked((int)0xe4870e26), 0x3cc5, 0x4cd2, 0xba, 0x46, 0xca, 0xa, 0x9a, 0x70, 0xed, 0x4), 3);
        /// <summary>
        /// PKEY _Devie_FriendlyName
        /// </summary>
        public static readonly PropertyKey PKEY_Device_FriendlyName = new PropertyKey(new Guid(unchecked((int)0xa45c254e), unchecked((short)0xdf1c), 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0), 14);
        /// <summary>
        /// PKEY _Device_IconPath
        /// </summary>
        public static readonly PropertyKey PKEY_Device_IconPath = new PropertyKey(new Guid(unchecked((int)0x259abffc), unchecked((short)0x50a7), 0x47ce, 0xaf, 0x8, 0x68, 0xc9, 0xa7, 0xd7, 0x33, 0x66), 12);
        /// <summary>
        /// Device description property.
        /// </summary>
        public static readonly PropertyKey PKEY_Device_DeviceDesc = new PropertyKey(new Guid(unchecked((int)0xa45c254e), unchecked((short)0xdf1c), 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0), 2);
        /// <summary>
        /// Id of controller device for endpoint device property.
        /// </summary>
        public static readonly PropertyKey PKEY_Device_ControllerDeviceId = new PropertyKey(new Guid(unchecked((int)0xb3f8fa53), unchecked((short)0x0004), 0x438e, 0x90, 0x03, 0x51, 0xa4, 0x6e, 0x13, 0x9b, 0xfc), 2);
        /// <summary>
        /// Device interface key property.
        /// </summary>
        public static readonly PropertyKey PKEY_Device_InterfaceKey = new PropertyKey(new Guid(unchecked((int)0x233164c8), unchecked((short)0x1b2c), 0x4c7d, 0xbc, 0x68, 0xb6, 0x71, 0x68, 0x7a, 0x25, 0x67), 1);
    }
}
