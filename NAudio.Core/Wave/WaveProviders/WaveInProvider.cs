using System;

namespace NAudio.Wave
{
    /// <summary>
    /// Buffered IWaveProvider taking source data from WaveIn
    /// </summary>
    public class WaveInProvider : IWaveProvider
    {
        private readonly IWaveIn waveIn;
        private readonly BufferedWaveProvider bufferedWaveProvider;

        /// <summary>
        /// Creates a new WaveInProvider
        /// n.b. Should make sure the WaveFormat is set correctly on IWaveIn before calling
        /// </summary>
        /// <param name="waveIn">The source of wave data</param>
        public WaveInProvider(IWaveIn waveIn)
        {
            this.waveIn = waveIn;
            waveIn.DataAvailable += OnDataAvailable;
            bufferedWaveProvider = new BufferedWaveProvider(WaveFormat);
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            // BufferSpan avoids materialising e.Buffer when the event is backed by a
            // ReadOnlyMemory<byte> wrapping a native WASAPI buffer (zero-copy capture path).
            bufferedWaveProvider.AddSamples(e.BufferSpan);
        }

        /// <summary>
        /// Reads data from the WaveInProvider
        /// </summary>
        public int Read(Span<byte> buffer)
        {
            return bufferedWaveProvider.Read(buffer);
        }

        /// <summary>
        /// The WaveFormat
        /// </summary>
        public WaveFormat WaveFormat => waveIn.WaveFormat;
    }
}
