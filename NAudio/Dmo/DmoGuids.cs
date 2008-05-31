using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.Dmo
{
    /// <summary>
    /// DMO Guids for use with DMOEnum
    /// dmoreg.h
    /// </summary>
    static class DmoGuids
    {
        public static readonly Guid DMOCATEGORY_AUDIO_DECODER = new Guid("57f2db8b-e6bb-4513-9d43-dcd2a6593125");
        public static readonly Guid DMOCATEGORY_AUDIO_ENCODER = new Guid("33D9A761-90C8-11d0-BD43-00A0C911CE86");
        public static readonly Guid DMOCATEGORY_VIDEO_DECODER = new Guid("4a69b442-28be-4991-969c-b500adf5d8a8");
        public static readonly Guid DMOCATEGORY_VIDEO_ENCODER = new Guid("33D9A760-90C8-11d0-BD43-00A0C911CE86");        
        public static readonly Guid DMOCATEGORY_AUDIO_EFFECT = new Guid("f3602b3f-0592-48df-a4cd-674721e7ebeb");
        public static readonly Guid DMOCATEGORY_VIDEO_EFFECT = new Guid("d990ee14-776c-4723-be46-3da2f56f10b9");
        public static readonly Guid DMOCATEGORY_AUDIO_CAPTURE_EFFECT = new Guid("f665aaba-3e09-4920-aa5f-219811148f09");
     }
}
