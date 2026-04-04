namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Sample provider interface to make WaveChannel32 extensible
    /// Still a bit ugly, hence internal at the moment - and might even make these into
    /// bit depth converting WaveProviders
    /// </summary>
    interface ISampleChunkConverter
    {
        bool Supports(WaveFormat format);
        void LoadNextChunk(IWaveProvider sourceProvider, int samplePairsRequired);
        bool GetNextSample(out float sampleLeft, out float sampleRight);
    }
}
