using System;
using System.Text;
using System.Runtime.InteropServices;

namespace NAudio.Dmo
{
    static class DmoInterop
    {
        [DllImport("msdmo.dll")]
        public static extern int DMOEnum(
            [In] ref Guid guidCategory,
            DmoEnumFlags flags,
            int inTypes,
            [In] DmoPartialMediaType[] inTypesArray,
            int outTypes,
            [In] DmoPartialMediaType[] outTypesArray,
            out IEnumDmo enumDmo);

        [DllImport("msdmo.dll")]
        public static extern int MoFreeMediaType(
            [In] ref DmoMediaType mediaType);

        [DllImport("msdmo.dll")]
        public static extern int MoInitMediaType(
            [In,Out] ref DmoMediaType mediaType, int formatBlockBytes);

        [DllImport("msdmo.dll")]
        public static extern int DMOGetName([In] ref Guid clsidDMO,
            // preallocate 80 characters
            [Out] StringBuilder name);
    }
}
