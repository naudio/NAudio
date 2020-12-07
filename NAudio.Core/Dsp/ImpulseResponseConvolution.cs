using System;

namespace NAudio.Dsp
{
    /// <summary>
    /// Summary description for ImpulseResponseConvolution.
    /// </summary>
    public class ImpulseResponseConvolution
    {
        /// <summary>
        /// A very simple mono convolution algorithm
        /// </summary>
        /// <remarks>
        /// This will be very slow
        /// </remarks>
        public float[] Convolve(float[] input, float[] impulseResponse)
        {
            var output = new float[input.Length + impulseResponse.Length];
            for(int t = 0; t < output.Length; t++)
            {
                for(int n = 0; n < impulseResponse.Length; n++)
                {
                    if((t >= n) && (t-n < input.Length))
                    {
                        output[t] += impulseResponse[n] * input[t-n];
                    }
                }
            }
            Normalize(output);
            return output;
        }

        /// <summary>
        /// This is actually a downwards normalize for data that will clip
        /// </summary>
        public void Normalize(float[] data)
        {
            float max = 0;
            for(int n = 0; n < data.Length; n++)
                max = Math.Max(max,Math.Abs(data[n]));
            if(max > 1.0)
                for(int n = 0; n < data.Length; n++)
                    data[n] /= max;
        }
    }
}
