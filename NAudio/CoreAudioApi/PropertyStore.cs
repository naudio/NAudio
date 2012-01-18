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
// this version modified for NAudio from Ray Molenkamp's original
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi.Interfaces;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Property Store class, only supports reading properties at the moment.
    /// </summary>
    public class PropertyStore
    {
        private IPropertyStore storeInterface;

        /// <summary>
        /// Property Count
        /// </summary>
        public int Count
        {
            get
            {
                int result;
                Marshal.ThrowExceptionForHR(storeInterface.GetCount(out result));
                return result;
            }
        }

        /// <summary>
        /// Gets property by index
        /// </summary>
        /// <param name="index">Property index</param>
        /// <returns>The property</returns>
        public PropertyStoreProperty this[int index]
        {
            get
            {
                PropVariant result;
                PropertyKey key = Get(index);
                Marshal.ThrowExceptionForHR(storeInterface.GetValue(ref key, out result));
                return new PropertyStoreProperty(key, result);
            }
        }

        /// <summary>
        /// Contains property guid
        /// </summary>
        /// <param name="guid">Looks for a specific Guid</param>
        /// <returns>True if found</returns>
        public bool Contains(Guid guid)
        {
            for (int i = 0; i < Count; i++)
            {
                PropertyKey key = Get(i);
                if (key.formatId == guid)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Indexer by guid
        /// </summary>
        /// <param name="guid">Property guid</param>
        /// <returns>Property or null if not found</returns>
        public PropertyStoreProperty this[Guid guid]
        {
            get
            {
                PropVariant result;
                for (int i = 0; i < Count; i++)
                {
                    PropertyKey key = Get(i);
                    if (key.formatId == guid)
                    {
                        Marshal.ThrowExceptionForHR(storeInterface.GetValue(ref key, out result));
                        return new PropertyStoreProperty(key, result);
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Gets property key at sepecified index
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>Property key</returns>
        public PropertyKey Get(int index)
        {
            PropertyKey key;
            Marshal.ThrowExceptionForHR(storeInterface.GetAt(index, out key));
            return key;
        }

        /// <summary>
        /// Gets property value at specified index
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>Property value</returns>
        public PropVariant GetValue(int index)
        {
            PropVariant result;
            PropertyKey key = Get(index);
            Marshal.ThrowExceptionForHR(storeInterface.GetValue(ref key, out result));
            return result;
        }

        /// <summary>
        /// Creates a new property store
        /// </summary>
        /// <param name="store">IPropertyStore COM interface</param>
        internal PropertyStore(IPropertyStore store)
        {
            this.storeInterface = store;
        }
    }
}

