using System;

namespace NAudio.Core.Wave.SampleProviders
{
    /// <summary>
    /// Defines common channel mixing matrixes for use with <see cref="ChannelMixerSampleProvider"/>.
    /// </summary>
    public static class ChannelMixMatrix
    {
        /// <summary>
        /// Converts a mono source to 2-channel source by copying the input to both outputs.
        /// </summary>
        public static readonly float[,] MonoToStereo = new float[,]
        {
            { 1.0f, 1.0f }
        };

        /// <summary>
        /// Converts a 2-channel source to a mono source by mixing the channels together using equal weight.
        /// </summary>
        public static readonly float[,] StereoToMono = new float[,]
        {
            { 0.5f },
            { 0.5f },
        };

        /// <summary>
        /// Modifies a 2-channel stream by selecting only the first channel. The output is 2-channel.
        /// </summary>
        public static readonly float[,] StereoLeft = new float[,]
        {
            { 1.0f, 0.0f },
            { 0.0f, 0.0f },
        };

        /// <summary>
        /// Modifies a 2-channel stream by selecting only the second channel. The output is 2-channel.
        /// </summary>
        public static readonly float[,] StereoRight = new float[,]
        {
            { 0.0f, 0.0f },
            { 0.0f, 1.0f },
        };

        /// <summary>
        /// Converts a 2-channel source to a canonical 5.1 output. The output has 6 channels:
        /// FrontLeft, FrontRight, Center, Sub, RearLeft, RearRight, in that order.
        /// </summary>
        /// <remarks>
        /// The matrix is designed so that the aggregate output volume from the 6-channel output
        /// is the same output volume that would've occurred if the original input was applied to
        /// two channels; if the original audio would've produced 200W of output spread across 2
        /// speakers, the transformed output would also produce 200W of output, instead spread
        /// across 6 speakers.
        ///
        /// This can be noted by the fact that no column in the matrix sums to 1.0. The loudest
        /// channel is the center channel, receiving a sum of 0.444 of its inputs (0.222 from left,
        /// 0.222 from right).
        ///
        /// If you would like a matrix that maximizes output volume, scale the matrix by a factor of
        /// 2.25. One might do this to preserve entropy during processing, with a final gain
        /// reduction step at the output to maintain intended power output.
        /// </remarks>
        public static readonly float[,] StereoTo5_1 = new float[,]
        {
            // Output Channels:
            //FL      FR      Centr   Subwf   RL      RR
            {0.314f, 0.000f, 0.222f, 0.031f, 0.268f, 0.164f}, // Left  Input
            {0.000f, 0.314f, 0.222f, 0.031f, 0.164f, 0.268f}  // Right Input
        };
    }
}
