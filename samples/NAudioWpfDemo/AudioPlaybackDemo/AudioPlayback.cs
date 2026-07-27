using System;
using System.Windows;
using NAudio.Extras;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace NAudioWpfDemo.AudioPlaybackDemo;

internal class AudioPlayback : IDisposable
{
    private IWavePlayer playbackDevice;
    private AudioFileReader fileStream;
    private VolumeSampleProvider volumeProvider;
    private float volume = 1.0f;

    public event EventHandler<FftEventArgs> FftCalculated;

    public event EventHandler<MaxSampleEventArgs> MaximumCalculated;

    /// <summary>Raised when the output device stops, including when the file plays to its end.</summary>
    public event EventHandler<StoppedEventArgs> PlaybackStopped;

    /// <summary>Sample rate of the most recently opened file, in Hz. Zero until a file is loaded.</summary>
    public int SampleRate { get; private set; }

    /// <summary>
    /// Linear output gain. Applied after the <see cref="SampleAggregator"/> so that turning the
    /// monitoring volume down doesn't flatten the visualisation — what you see stays the file's
    /// own level.
    /// </summary>
    public float Volume
    {
        get => volume;
        set
        {
            volume = value;
            if (volumeProvider != null) volumeProvider.Volume = value;
        }
    }

    public TimeSpan CurrentTime
    {
        get => fileStream?.CurrentTime ?? TimeSpan.Zero;
        set
        {
            if (fileStream != null) fileStream.CurrentTime = value;
        }
    }

    public TimeSpan TotalTime => fileStream?.TotalTime ?? TimeSpan.Zero;

    public bool HasFile => fileStream != null;

    public void Load(string fileName)
    {
        Stop();
        CloseFile();
        EnsureDeviceCreated();
        OpenFile(fileName);
    }

    private void CloseFile()
    {
        fileStream?.Dispose();
        fileStream = null;
        volumeProvider = null;
    }

    private void OpenFile(string fileName)
    {
        try
        {
            var inputStream = new AudioFileReader(fileName);
            fileStream = inputStream;
            SampleRate = inputStream.WaveFormat.SampleRate;
            // 4096 gives ~10.8 Hz bin spacing at 44.1 kHz. This halves the "stairstep" region
            // at the bass end of a log-frequency spectrum display (the crossover where each
            // display band spans one FFT bin moves from ~283 Hz down to ~142 Hz). Trade-off
            // is a ~93 ms analysis window vs 46 ms at 2048 — fine for a visualiser, music
            // transients still look lively.
            var aggregator = new SampleAggregator(inputStream, 4096);
            aggregator.NotificationCount = inputStream.WaveFormat.SampleRate / 100;
            aggregator.PerformFFT = true;
            aggregator.FftCalculated += (s, a) => FftCalculated?.Invoke(this, a);
            aggregator.MaximumCalculated += (s, a) => MaximumCalculated?.Invoke(this, a);
            volumeProvider = new VolumeSampleProvider(aggregator) { Volume = volume };
            playbackDevice.Init(volumeProvider);
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message, "Problem opening file");
            CloseFile();
        }
    }

    private void EnsureDeviceCreated()
    {
        if (playbackDevice == null)
        {
            CreateDevice();
        }
    }

    private void CreateDevice()
    {
        playbackDevice = new WaveOut { BufferMilliseconds = 200 };
        playbackDevice.PlaybackStopped += (s, a) => PlaybackStopped?.Invoke(this, a);
    }

    public void Play()
    {
        if (playbackDevice != null && fileStream != null && playbackDevice.PlaybackState != PlaybackState.Playing)
        {
            playbackDevice.Play();
        }
    }

    public void Pause()
    {
        playbackDevice?.Pause();
    }

    public void Stop()
    {
        playbackDevice?.Stop();
        if (fileStream != null)
        {
            fileStream.Position = 0;
        }
    }

    public void Dispose()
    {
        Stop();
        CloseFile();
        playbackDevice?.Dispose();
        playbackDevice = null;
    }
}
