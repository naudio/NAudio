 using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace NAudio.MediaFoundation
{
    public class MediaFoundationCapturer :IWaveIn
    {
        private WaveFormat format;
        private IMFSourceReader2 sourceReader;
        private bool Recording = false;
        private IMFMediaSource source;
        private SourceReaderCallback callback = new SourceReaderCallback();
        private Thread recordthread;
        private MemoryStream datastream = new MemoryStream();
        public WaveFormat WaveFormat {
            get 
            {
                return format;
            }
            set
            {
                if (Recording) throw new InvalidOperationException("Can't modify format while recording.");
                SetFormat(value);
            }
        }

        public event EventHandler<WaveInEventArgs> DataAvailable;
        public event EventHandler<StoppedEventArgs> RecordingStopped;

        private void DoRecord()
        {
            while (Recording)
            {
                sourceReader.ReadSample(MediaFoundationInterop.MF_SOURCE_READER_FIRST_AUDIO_STREAM, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                if (callback.NewSample) {
                    int bytecount = callback.Read(out byte[] data);
                    DataAvailable?.Invoke(this, new WaveInEventArgs(data, bytecount));
                }

            }
        }

        private void SetFormat(WaveFormat format)
        {
            source.CreatePresentationDescriptor(out IMFPresentationDescriptor descriptor);
            descriptor.GetStreamDescriptorCount(out uint sdcount);
            bool hasaudio=false;
            for (uint i = 0; i < sdcount; i++)
            {
                descriptor.GetStreamDescriptorByIndex(i, out _, out IMFStreamDescriptor sd);
                descriptor.SelectStream(i);
                sd.GetMediaTypeHandler(out IMFMediaTypeHandler typeHandler);
                typeHandler.GetMediaTypeByIndex(0, out IMFMediaType mediaType);
                mediaType.GetMajorType(out Guid streamtype);
                if (streamtype == MediaTypes.MFMediaType_Audio)
                {
                    try
                    {
                        mediaType.SetUINT32(MediaFoundationAttributes.MF_MT_AUDIO_SAMPLES_PER_SECOND, format.SampleRate);//SampleRate
                        mediaType.SetUINT32(MediaFoundationAttributes.MF_MT_AUDIO_NUM_CHANNELS, format.Channels);
                        mediaType.SetUINT32(MediaFoundationAttributes.MF_MT_AUDIO_BITS_PER_SAMPLE, format.BitsPerSample);
                        mediaType.SetGUID(MediaFoundationAttributes.MF_MT_SUBTYPE, AudioSubtypes.MFAudioFormat_PCM);
                    }
                    catch (COMException)
                    {
                        throw new InvalidOperationException("Can't configure the source with specific format.");
                    }
                    hasaudio = true;
                }
                else
                {
                    continue;
                }              
            }
            if (!hasaudio) throw new ArgumentException("The device doesn't have audio stream.");
            this.format = format;
            IMFAttributes readerattr = MediaFoundationApi.CreateAttributes(2);
            readerattr.SetUnknown(MediaFoundationAttributes.MF_SOURCE_READER_ASYNC_CALLBACK,callback);
            MediaFoundationInterop.MFCreateSourceReaderFromMediaSource(source, readerattr, out IMFSourceReader  _sourceReader);
            sourceReader = _sourceReader as IMFSourceReader2;
        }
        public MediaFoundationCapturer(IMFActivate devsource)
        {
            MediaFoundationApi.Startup();
            WaveFormat format = new WaveFormat();
            try
            {
                devsource.ActivateObject(typeof(IMFMediaSource).GUID, out object _source);
                source = _source as IMFMediaSource;                
            }
            catch (COMException)
            {
                throw new ArgumentException("Can't create media source with the devsource.");
            }
            SetFormat(format);
            recordthread = new Thread(DoRecord);
        }
        public void StartRecording()
        {
            Recording = true;
            recordthread.Start();
        }

        public void StopRecording()
        {
            Recording = false;
            RecordingStopped?.Invoke(this, new StoppedEventArgs());
        }
        public void Dispose()
        {
            StopRecording();
            Marshal.FinalReleaseComObject(source);
            Marshal.FinalReleaseComObject(sourceReader);
            recordthread.Join();
            GC.SuppressFinalize(this);
        }
    }
}
