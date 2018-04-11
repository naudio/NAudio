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

   (Modified for NAudio by Mark Heath)

 */

using System;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Utils;

namespace NAudio.CoreAudioApi
{
    // This class implements the IAudioEndpointVolumeCallback interface,
    // it is implemented in this class because implementing it on AudioEndpointVolume 
    // (where the functionality is really wanted, would cause the OnNotify function 
    // to show up in the public API. 
    internal class AudioEndpointVolumeCallback : IAudioEndpointVolumeCallback
    {
        private readonly AudioEndpointVolume parent;
        
        internal AudioEndpointVolumeCallback(AudioEndpointVolume parent)
        {
            this.parent = parent;
        }
        
        public void OnNotify(IntPtr notifyData)
        {
            //Since AUDIO_VOLUME_NOTIFICATION_DATA is dynamic in length based on the
            //number of audio channels available we cannot just call PtrToStructure 
            //to get all data, thats why it is split up into two steps, first the static
            //data is marshalled into the data structure, then with some IntPtr math the
            //remaining floats are read from memory.
            //
            var data = MarshalHelpers.PtrToStructure<AudioVolumeNotificationDataStruct>(notifyData);
            
            //Determine offset in structure of the first float
            var offset = MarshalHelpers.OffsetOf<AudioVolumeNotificationDataStruct>("ChannelVolume");
            //Determine offset in memory of the first float
            var firstFloatPtr = (IntPtr)((long)notifyData + (long)offset);

            var voldata = new float[data.nChannels];
            
            //Read all floats from memory.
            for (int i = 0; i < data.nChannels; i++)
            {
                voldata[i] = MarshalHelpers.PtrToStructure<float>(firstFloatPtr);
            }

            //Create combined structure and Fire Event in parent class.
            var notificationData = new AudioVolumeNotificationData(data.guidEventContext, data.bMuted, data.fMasterVolume, voldata, data.guidEventContext);
            parent.FireNotification(notificationData);
        }
    }
}
