using System;
using NAudio.Utils;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Converts a mono sample provider to stereo, with a customisable pan strategy
    /// </summary>
    public class PanningSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider source;
        private float pan;
        private float leftMultiplier;
        private float rightMultiplier;
        private readonly WaveFormat waveFormat;
        private float[] sourceBuffer;
        private IPanStrategy panStrategy;

        /// <summary>
        /// Initialises a new instance of the PanningSampleProvider
        /// </summary>
        /// <param name="source">Source sample provider, must be mono</param>
        public PanningSampleProvider(ISampleProvider source)
        {
            if (source.WaveFormat.Channels != 1)
            {
                throw new ArgumentException("Source sample provider must be mono");
            }
            this.source = source;
            waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(source.WaveFormat.SampleRate, 2);
            panStrategy = new SinPanStrategy();
        }

        /// <summary>
        /// Pan value, must be between -1 (left) and 1 (right)
        /// </summary>
        public float Pan
        {
            get
            {
                return pan;
            }
            set
            {
                if (value < -1.0f || value > 1.0f)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Pan must be in the range -1 to 1");
                }
                pan = value;
                UpdateMultipliers();
            }
        }

        /// <summary>
        /// The pan strategy currently in use
        /// </summary>
        public IPanStrategy PanStrategy
        {
            get
            {
                return panStrategy;
            }
            set
            {
                panStrategy = value;
                UpdateMultipliers();
            }
        }

        private void UpdateMultipliers()
        {
            var multipliers = panStrategy.GetMultipliers(Pan);
            leftMultiplier = multipliers.Left;
            rightMultiplier = multipliers.Right;
        }

        /// <summary>
        /// The WaveFormat of this sample provider
        /// </summary>
        public WaveFormat WaveFormat => waveFormat;

        /// <summary>
        /// Reads samples from this sample provider
        /// </summary>
        /// <param name="buffer">Sample buffer</param>
        /// <param name="offset">Offset into sample buffer</param>
        /// <param name="count">Number of samples desired</param>
        /// <returns>Number of samples read</returns>
        public int Read(float[] buffer, int offset, int count)
        {
            int sourceSamplesRequired = count / 2;
            sourceBuffer = BufferHelpers.Ensure(sourceBuffer, sourceSamplesRequired);
            int sourceSamplesRead = source.Read(sourceBuffer, 0, sourceSamplesRequired);
            int outIndex = offset;
            for (int n = 0; n < sourceSamplesRead; n++)
            {
                buffer[outIndex++] = leftMultiplier * sourceBuffer[n];
                buffer[outIndex++] = rightMultiplier * sourceBuffer[n];
            }
            return sourceSamplesRead * 2;
        }
    }

    /// <summary>
    /// Pair of floating point values, representing samples or multipliers
    /// </summary>
    public struct StereoSamplePair
    {
        /// <summary>
        /// Left value
        /// </summary>
        public float Left { get; set; }
        /// <summary>
        /// Right value
        /// </summary>
        public float Right { get; set; }
    }

    /// <summary>
    /// Required Interface for a Panning Strategy
    /// </summary>
    public interface IPanStrategy
    {
        /// <summary>
        /// Gets the left and right multipliers for a given pan value
        /// </summary>
        /// <param name="pan">Pan value from -1 to 1</param>
        /// <returns>Left and right multipliers in a stereo sample pair</returns>
        StereoSamplePair GetMultipliers(float pan);
    }

    /// <summary>
    /// Simplistic "balance" control - treating the mono input as if it was stereo
    /// In the centre, both channels full volume. Opposite channel decays linearly 
    /// as balance is turned to to one side
    /// </summary>
    public class StereoBalanceStrategy : IPanStrategy
    {
        /// <summary>
        /// Gets the left and right channel multipliers for this pan value
        /// </summary>
        /// <param name="pan">Pan value, between -1 and 1</param>
        /// <returns>Left and right multipliers</returns>
        public StereoSamplePair GetMultipliers(float pan)
        {
            float leftChannel = (pan <= 0) ? 1.0f : ((1 - pan) / 2.0f);
            float rightChannel = (pan >= 0) ? 1.0f : ((pan + 1) / 2.0f);
            // Console.WriteLine(pan + ": " + leftChannel + "," + rightChannel);
            return new StereoSamplePair() { Left = leftChannel, Right = rightChannel };
        }
    }


    /// <summary>
    /// Square Root Pan, thanks to Yuval Naveh
    /// </summary>
    public class SquareRootPanStrategy : IPanStrategy
    {
        /// <summary>
        /// Gets the left and right channel multipliers for this pan value
        /// </summary>
        /// <param name="pan">Pan value, between -1 and 1</param>
        /// <returns>Left and right multipliers</returns>
        public StereoSamplePair GetMultipliers(float pan)
        {
            // -1..+1  -> 1..0
            float normPan = (-pan + 1) / 2;
            float leftChannel = (float)Math.Sqrt(normPan);
            float rightChannel = (float)Math.Sqrt(1 - normPan);
            // Console.WriteLine(pan + ": " + leftChannel + "," + rightChannel);
            return new StereoSamplePair() { Left = leftChannel, Right = rightChannel };
        }
    }

    /// <summary>
    /// Sinus Pan, thanks to Yuval Naveh
    /// </summary>
    public class SinPanStrategy : IPanStrategy
    {
        private const float HalfPi = (float)Math.PI / 2;

        /// <summary>
        /// Gets the left and right channel multipliers for this pan value
        /// </summary>
        /// <param name="pan">Pan value, between -1 and 1</param>
        /// <returns>Left and right multipliers</returns>
        public StereoSamplePair GetMultipliers(float pan)
        {
            // -1..+1  -> 1..0
            float normPan = (-pan + 1) / 2;
            float leftChannel = (float)Math.Sin(normPan * HalfPi);
            float rightChannel = (float)Math.Cos(normPan * HalfPi);
            // Console.WriteLine(pan + ": " + leftChannel + "," + rightChannel);
            return new StereoSamplePair() { Left = leftChannel, Right = rightChannel };
        }
    }

    /// <summary>
    /// Linear Pan
    /// </summary>
    public class LinearPanStrategy : IPanStrategy
    {
        /// <summary>
        /// Gets the left and right channel multipliers for this pan value
        /// </summary>
        /// <param name="pan">Pan value, between -1 and 1</param>
        /// <returns>Left and right multipliers</returns>
        public StereoSamplePair GetMultipliers(float pan)
        {
            // -1..+1  -> 1..0
            float normPan = (-pan + 1) / 2;
            float leftChannel = normPan;
            float rightChannel = 1 - normPan;
            return new StereoSamplePair() { Left = leftChannel, Right = rightChannel };
        }
    }
}
