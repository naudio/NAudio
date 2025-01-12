using NAudio.CoreAudioApi.Interfaces;
using System;

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
                    throw new IndexOutOfRangeException();
                }

                partsListInterface.GetPart(index, out IPart part);
                return new Part(part);
            }
        }
    }
}
