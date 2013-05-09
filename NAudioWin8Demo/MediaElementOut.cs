using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace NAudio.Wave
{
    /// <summary>
    /// An experimental idea - can we fool MediaElement into thinking it is
    /// playing an infinitely long WAV file?
    /// </summary>
    public class MediaElementOut : IWavePlayer
    {
        private readonly MediaElement mediaElement;

        public MediaElementOut(MediaElement mediaElement)
        {
            this.mediaElement = mediaElement;
            mediaElement.MediaFailed += mediaElement_MediaFailed;
            mediaElement.MediaOpened += MediaElementOnMediaOpened;
            mediaElement.CurrentStateChanged += MediaElementOnCurrentStateChanged;
        }

        private void MediaElementOnCurrentStateChanged(object sender, RoutedEventArgs routedEventArgs)
        {
            if (mediaElement.CurrentState == MediaElementState.Stopped)
            {
                OnPlaybackStopped(new StoppedEventArgs());
            }
        }

        private void MediaElementOnMediaOpened(object sender, RoutedEventArgs routedEventArgs)
        {
            
        }

        void mediaElement_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            
        }

        public void Play()
        {
            mediaElement.Play();
        }

        public void Stop()
        {
            mediaElement.Stop();
        }

        public void Pause()
        {
            mediaElement.Pause();
        }

        public Task Init(IWaveProvider waveProvider)
        {
            // do this still on the gui thread
            mediaElement.SetSource(new WaveProviderRandomAccessStream(waveProvider), "audio/wav");
            // must be a better way than this
            return new Task(() =>{});
        }

        public PlaybackState PlaybackState { get; private set; }
        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        protected virtual void OnPlaybackStopped(StoppedEventArgs e)
        {
            EventHandler<StoppedEventArgs> handler = PlaybackStopped;
            if (handler != null) handler(this, e);
        }
    }

    class WaveProviderStream : Stream
    {
        private IWaveProvider waveProvider;
        private MemoryStream header;

        public WaveProviderStream(IWaveProvider waveProvider)
        {
            this.waveProvider = waveProvider;
            this.header = new MemoryStream();

            var writer = new BinaryWriter(header, System.Text.Encoding.UTF8);
            writer.Write(Encoding.UTF8.GetBytes("RIFF"));
            writer.Write(Int32.MaxValue);
            writer.Write(Encoding.UTF8.GetBytes("WAVE"));
            writer.Write(Encoding.UTF8.GetBytes("fmt "));
            //waveProvider.WaveFormat.Serialize(writer);
            var format = waveProvider.WaveFormat;
            writer.Write((short)format.Encoding);
            writer.Write((short)format.Channels);
            writer.Write((int)format.SampleRate);
            writer.Write((int)format.AverageBytesPerSecond);
            writer.Write((short)format.BlockAlign);
            writer.Write((short)format.BitsPerSample);
            writer.Write(Encoding.UTF8.GetBytes("data"));
            writer.Write(Int32.MaxValue - 32);
            header.Position = 0;
        }

        public override void Flush()
        {
            
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var fromHeader = Math.Min(count, header.Length - header.Position);
            int bytesRead = 0;
            if (fromHeader > 0)
                bytesRead = header.Read(buffer, offset, (int)fromHeader);

            return waveProvider.Read(buffer, offset + bytesRead, count - bytesRead);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            // media element comes in a few times asking for the header?
            if (offset == 0)
                header.Position = 0;
            return 0;
        }

        public override void SetLength(long value)
        {
            throw new InvalidOperationException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException();
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override long Length
        {
            get { return long.MaxValue; }
        }

        public override long Position { get; set; }
    }

    class WaveProviderRandomAccessStream : IRandomAccessStream
    {
        private readonly IWaveProvider waveProvider;
        private WaveProviderStream wps;
        private IInputStream inputStream;
        public WaveProviderRandomAccessStream(IWaveProvider waveProvider)
        {
            this.waveProvider = waveProvider;
            wps = new WaveProviderStream(waveProvider);
            inputStream = wps.AsInputStream();
        }

        public void Dispose()
        {
            
        }

        public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
        {
            return inputStream.ReadAsync(buffer, count, options);
        }

        public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
        {
            throw new InvalidOperationException("This stream is not writable");
        }

        public IAsyncOperation<bool> FlushAsync()
        {
            throw new InvalidOperationException("This stream is cannot be flushed");
        }

        public IInputStream GetInputStreamAt(ulong position)
        {
            if (position != 0)
                throw new InvalidOperationException("This stream is not seekable");
            return inputStream;
        }

        public IOutputStream GetOutputStreamAt(ulong position)
        {
            throw new InvalidOperationException("This stream is not writable");
        }

        public void Seek(ulong position)
        {
            if (position != 0)
                throw new InvalidOperationException("This stream is not seekable");
            wps.Seek(0, SeekOrigin.Begin);
        }

        public IRandomAccessStream CloneStream()
        {
            throw new InvalidOperationException("This stream does not support cloning");
        }

        public bool CanRead { get { return true; } }
        public bool CanWrite { get { return false; } }
        public ulong Position { get; private set; }
        public ulong Size { get; set; }
    }
}
