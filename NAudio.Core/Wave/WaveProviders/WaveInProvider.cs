namespace NAudio.Wave
{
    /// <summary>
    /// Buffered WaveProvider taking source data from WaveIn
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
            bufferedWaveProvider.AddSamples(e.Buffer, 0, e.BytesRecorded);
        }

        /// <summary>
        /// Reads data from the WaveInProvider
        /// </summary>
        public int Read(byte[] buffer, int offset, int count)
        {
            return bufferedWaveProvider.Read(buffer, offset, count);
        }

        /// <summary>
        /// The WaveFormat
        /// </summary>
        public WaveFormat WaveFormat => waveIn.WaveFormat;
    }
}
