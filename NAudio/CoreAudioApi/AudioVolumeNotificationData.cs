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
using System.Collections.Generic;
using System.Text;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Audio Volume Notification Data
    /// </summary>
    public class AudioVolumeNotificationData
    {
        private Guid _EventContext;
        private bool _Muted;
        private float _MasterVolume;
        private int _Channels;
        private float[] _ChannelVolume;

        /// <summary>
        /// Event Context
        /// </summary>
        public Guid EventContext
        {
            get
            {
                return _EventContext;
            }
        }

        /// <summary>
        /// Muted
        /// </summary>
        public bool Muted
        {
            get
            {
                return _Muted;
            }
        }

        /// <summary>
        /// Master Volume
        /// </summary>
        public float MasterVolume
        {
            get
            {
                return _MasterVolume;
            }
        }

        /// <summary>
        /// Channels
        /// </summary>
        public int Channels
        {
            get
            {
                return _Channels;
            }
        }

        /// <summary>
        /// Channel Volume
        /// </summary>
        public float[] ChannelVolume
        {
            get
            {
                return _ChannelVolume;
            }
        }

        /// <summary>
        /// Audio Volume Notification Data
        /// </summary>
        /// <param name="eventContext"></param>
        /// <param name="muted"></param>
        /// <param name="masterVolume"></param>
        /// <param name="channelVolume"></param>
        public AudioVolumeNotificationData(Guid eventContext, bool muted, float masterVolume, float[] channelVolume)
        {
            _EventContext = eventContext;
            _Muted = muted;
            _MasterVolume = masterVolume;
            _Channels = channelVolume.Length;
            _ChannelVolume = channelVolume;
        }
    }
}
