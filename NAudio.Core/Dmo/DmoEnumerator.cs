using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace NAudio.Dmo
{
    /// <summary>
    /// DirectX Media Object Enumerator
    /// </summary>
    public class DmoEnumerator
    {
        /// <summary>
        /// Get audio effect names
        /// </summary>
        /// <returns>Audio effect names</returns>
        public static IEnumerable<DmoDescriptor> GetAudioEffectNames()
        {
            return GetDmos(DmoGuids.DMOCATEGORY_AUDIO_EFFECT);
        }

        /// <summary>
        /// Get audio encoder names
        /// </summary>
        /// <returns>Audio encoder names</returns>
        public static IEnumerable<DmoDescriptor> GetAudioEncoderNames()
        {
            return GetDmos(DmoGuids.DMOCATEGORY_AUDIO_ENCODER);
        }

        /// <summary>
        /// Get audio decoder names
        /// </summary>
        /// <returns>Audio decoder names</returns>
        public static IEnumerable<DmoDescriptor> GetAudioDecoderNames()
        {
            return GetDmos(DmoGuids.DMOCATEGORY_AUDIO_DECODER);
        }
        
        private static IEnumerable<DmoDescriptor> GetDmos(Guid category)
        {
            IEnumDmo enumDmo;
            var hresult = DmoInterop.DMOEnum(ref category, DmoEnumFlags.None, 0, null, 0, null, out enumDmo);
            Marshal.ThrowExceptionForHR(hresult);
            int itemsFetched;
            do
            {
                Guid guid;
                IntPtr namePointer;
                enumDmo.Next(1, out guid, out namePointer, out itemsFetched);

                if (itemsFetched == 1)
                {
                    string name = Marshal.PtrToStringUni(namePointer);
                    Marshal.FreeCoTaskMem(namePointer);
                    yield return new DmoDescriptor(name, guid);
                }
            } while (itemsFetched > 0);
        }
    }
}
