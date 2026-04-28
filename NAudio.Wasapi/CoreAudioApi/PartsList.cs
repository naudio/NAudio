using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wasapi.CoreAudioApi;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Parts List
    /// </summary>
    public class PartsList
    {
        private IPartsList partsListInterface;

        internal PartsList(IPartsList partsList)
        {
            partsListInterface = partsList;
        }

        /// <summary>
        /// Part count
        /// </summary>
        public uint Count
        {
            get
            {
                uint result = 0;
                if (partsListInterface != null)
                {
                    partsListInterface.GetCount(out result);
                }

                return result;
            }
        }

        /// <summary>
        /// Get part by index
        /// </summary>
        public Part this[uint index]
        {
            get
            {
                if (partsListInterface == null)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                partsListInterface.GetPart(index, out var ptr);
                try
                {
                    return new Part(ComActivation.WrapUnique<IPart>(ptr));
                }
                finally
                {
                    Marshal.Release(ptr);
                }
            }
        }
    }
}
