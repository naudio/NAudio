using System;

namespace NAudio.Dmo
{
    [Flags]
    enum InputStreamInfoFlags
    {
        None,
        DMO_INPUT_STREAMF_WHOLE_SAMPLES = 0x00000001,
        DMO_INPUT_STREAMF_SINGLE_SAMPLE_PER_BUFFER = 0x00000002,
        DMO_INPUT_STREAMF_FIXED_SAMPLE_SIZE = 0x00000004,
        DMO_INPUT_STREAMF_HOLDS_BUFFERS = 0x00000008
    }
}
