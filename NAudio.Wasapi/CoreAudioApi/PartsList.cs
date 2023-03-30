using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.CoreAudioApi
{
    public class PartsList
    {
        private IPartsList partsListInterface;

        internal PartsList(IPartsList partsList)
        {
            partsListInterface = partsList;
        }

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
