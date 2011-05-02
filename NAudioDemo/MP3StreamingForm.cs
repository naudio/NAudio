using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using NAudio.Wave;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;

namespace NAudioDemo
{
    public partial class MP3StreamingForm : Form
    {
        public MP3StreamingForm()
        {
            InitializeComponent();
        }

        BufferedWaveProvider bufferedWaveProvider;
        IWavePlayer waveOut;
        volatile bool playing;

        private void StreamMP3(object state)
        {
            string url = (string)state;
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            byte[] buffer = new byte[16384 * 2];

            IMp3FrameDecompressor decompressor = null;
            try
            {
                using (var responseStream = resp.GetResponseStream())
                {
                    var readFullyStream = new ReadFullyStream(responseStream);
                    do
                    {
                        Mp3Frame frame = new Mp3Frame(readFullyStream, true);
                        if (decompressor == null)
                        {
                            // don't think these details matter too much - just help ACM select the right codec
                            // however, the buffered provider doesn't know what sample rate it is working at
                            // until we have a frame
                            WaveFormat waveFormat = new Mp3WaveFormat(frame.SampleRate, frame.ChannelMode == ChannelMode.Mono ? 1 : 2, frame.FrameLength, frame.BitRate);
                            decompressor = new AcmMp3FrameDecompressor(waveFormat);
                            this.bufferedWaveProvider = new BufferedWaveProvider(decompressor.OutputFormat);
                            this.bufferedWaveProvider.BufferDuration = TimeSpan.FromSeconds(20); // allow us to get well ahead of ourselves
                            //this.bufferedWaveProvider.BufferedDuration = 250;
                        }
                        int decompressed = decompressor.DecompressFrame(frame, buffer, 0);
                        //Debug.WriteLine(String.Format("Decompressed a frame {0}", decompressed));
                        bufferedWaveProvider.AddSamples(buffer, 0, decompressed);
                        if (bufferedWaveProvider.BufferLength - bufferedWaveProvider.BufferedBytes < bufferedWaveProvider.WaveFormat.AverageBytesPerSecond/4)
                        {
                            Debug.WriteLine("Buffer getting full, taking a break");
                            Thread.Sleep(500);
                        }
                    } while (playing);
                }
            }
            finally
            {
                if (decompressor != null)
                {
                    decompressor.Dispose();
                }
            }
        }

        private void buttonPlay_Click(object sender, EventArgs e)
        {
            if (!playing)
            {
                playing = true;
                this.bufferedWaveProvider = null;
                ThreadPool.QueueUserWorkItem(new WaitCallback(StreamMP3), textBoxStreamingUrl.Text);
                buttonPlay.Text = "Stop";
                timer1.Enabled = true;
            }
            else
            {
                StopPlayback();
            }
        }

        private void StopPlayback()
        {
            if (playing)
            { 
                playing = false;
                waveOut.Stop();
                waveOut.Dispose();
                waveOut = null;
                buttonPlay.Text = "Play";
                timer1.Enabled = false;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (playing)
            {
                if (this.waveOut == null && this.bufferedWaveProvider != null)
                {
                    this.waveOut = new WaveOut();
                    waveOut.Init(bufferedWaveProvider);
                    progressBarBuffer.Maximum = bufferedWaveProvider.BufferLength;
                }
                else if (bufferedWaveProvider != null)
                {
                    progressBarBuffer.Value = bufferedWaveProvider.BufferedBytes;
                    // make it stutter less if we 
                    var bufferedSeconds = bufferedWaveProvider.BufferedDuration.TotalSeconds;
                    if (bufferedSeconds < 0.5 && waveOut.PlaybackState == PlaybackState.Playing)
                    {
                        Debug.WriteLine("Not enough queued data, pausing");
                        waveOut.Pause();
                    }
                    else if (bufferedSeconds > 4 && waveOut.PlaybackState != PlaybackState.Playing)
                    {
                        Debug.WriteLine("Buffered enough, playing");
                        waveOut.Play();
                    }
                }
            }
        }

        private void MP3StreamingForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopPlayback();
        }
    }
}
