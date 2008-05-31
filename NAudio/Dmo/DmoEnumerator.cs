using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace NAudio.Dmo
{
    public class DmoEnumerator
    {
        public static IEnumerable<string> GetAudioEffectNames()
        {
            return GetNames(DmoGuids.DMOCATEGORY_AUDIO_EFFECT);
        }

        public static IEnumerable<string> GetAudioEncoderNames()
        {
            return GetNames(DmoGuids.DMOCATEGORY_AUDIO_ENCODER);
        }

        public static IEnumerable<string> GetAudioDecoderNames()
        {
            return GetNames(DmoGuids.DMOCATEGORY_AUDIO_DECODER);
        }

        private static IEnumerable<string> GetNames(Guid category)
        {
            IEnumDmo enumDmo;
            int hresult = DmoInterop.DMOEnum(ref category, DmoEnumFlags.None, 0, null, 0, null, out enumDmo);
            Marshal.ThrowExceptionForHR(hresult);
            Guid guid;
            int itemsFetched;
            IntPtr namePointer;
            do
            {
                enumDmo.Next(1, out guid, out namePointer, out itemsFetched);

                if (itemsFetched == 1)
                {
                    string name = Marshal.PtrToStringUni(namePointer);
                    Marshal.FreeCoTaskMem(namePointer);
                    yield return name;
                }
            } while (itemsFetched > 0);
        }
    }
}
