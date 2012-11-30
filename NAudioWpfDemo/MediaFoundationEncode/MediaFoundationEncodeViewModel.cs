using System;
using System.Runtime.InteropServices;
using System.Windows.Input;
using Microsoft.Win32;
using NAudio.MediaFoundation;
using NAudio.Wave;
using NAudioWpfDemo.ViewModel;

namespace NAudioWpfDemo.MediaFoundationEncode
{
    internal class MediaFoundationEncodeViewModel : ViewModelBase, IDisposable
    {
        /// <summary>
        /// WMAudio Encoder DMO wmcodecdsp.h
        /// </summary>
        private Guid CWMAEncMediaObject = new Guid("70f598e9-f4ab-495a-99e2-a7c4d3d89abf");

        public MediaFoundationEncodeViewModel()
        {
            MediaFoundationApi.Startup();
            EncodeCommand = new DelegateCommand(Encode);
        }

        public ICommand EncodeCommand { get; private set; }

        private void Encode()
        {
            string inputFileName = SelectFileToConvert();
            if (inputFileName == null) return;
            using (var reader = new Mp3FileReader(inputFileName))
            {
                string outputUrl = SelectSaveFile();
                if (outputUrl == null) return;
                IMFMediaType mediaType;
                if (outputUrl.EndsWith(".mp4"))
                    mediaType = CreateAacTargetMediaType(reader.WaveFormat.SampleRate, reader.WaveFormat.Channels);
                else if (outputUrl.EndsWith(".wma"))
                    mediaType = CreateWmaTargetMediaType(16000); // get something roughly 128kbps
                else
                    throw new InvalidOperationException("Unrecognised output format");
                IMFSinkWriter writer;
                // not using this for now as we need to provide properly configured attributes or it will complain
                // that the output file is not found 
                //http://msdn.microsoft.com/en-gb/library/windows/desktop/dd389284%28v=vs.85%29.aspx
                //MediaFoundationInterop.MFCreateSinkWriterFromURL(outputUrl, null, null, out writer);

                writer = CreateSinkWriter(outputUrl);


                int streamIndex;
                writer.AddStream(mediaType, out streamIndex);

                // tell the writer what input format we are giving it
                var inputFormat = CreateMediaTypeFromWaveFormat(reader.WaveFormat);
                // n.b. can get 0xC00D36B4 - MF_E_INVALIDMEDIATYPE here
                writer.SetInputMediaType(streamIndex, inputFormat, null);

                
                writer.BeginWriting();
                int maxLength = reader.WaveFormat.AverageBytesPerSecond * 4;
                var managedBuffer = new byte[maxLength];

                long position = 0;
                do
                {
                    IMFMediaBuffer buffer;
                    MediaFoundationInterop.MFCreateMemoryBuffer(reader.WaveFormat.AverageBytesPerSecond * 4, out buffer);
                    buffer.GetMaxLength(out maxLength);
                    
                    IMFSample sample;
                    MediaFoundationInterop.MFCreateSample(out sample);
                    sample.AddBuffer(buffer);

                    IntPtr ptr;
                    int currentLength;
                    buffer.Lock(out ptr, out maxLength, out currentLength);
                    int read = reader.Read(managedBuffer, 0, maxLength);
                    if (read > 0)
                    {
                        long duration = BytesToNsPosition(read, reader.WaveFormat);
                        Marshal.Copy(managedBuffer, 0, ptr, read);
                        buffer.SetCurrentLength(read);
                        buffer.Unlock();
                        sample.SetSampleTime(position);
                        sample.SetSampleDuration(duration);
                        writer.WriteSample(streamIndex, sample);
                        //writer.Flush(streamIndex);
                        position += duration;
                    }
                    else
                    {
                        buffer.Unlock();
                        break;
                    }

                    Marshal.ReleaseComObject(sample);
                    Marshal.ReleaseComObject(buffer);
                } while (true);

                writer.DoFinalize();

                Marshal.ReleaseComObject(inputFormat);
                Marshal.ReleaseComObject(mediaType);
                Marshal.ReleaseComObject(writer);
            }

            // alternatively could try to interact directly with encoder objects
            //var encoder = new WindowsMediaEncoder();
            //var mft = (IMFTransform)encoder;

        }

        private long BytesToNsPosition(int bytes, WaveFormat waveFormat)
        {
            long nsPosition = (10000000L * bytes) / waveFormat.AverageBytesPerSecond;
            return nsPosition;
        }

        private IMFSinkWriter CreateSinkWriter(string url)
        {
            var factory = (IMFReadWriteClassFactory)(new MFReadWriteClassFactory());

            // Create the attributes
            IMFAttributes attributes;
            MediaFoundationInterop.MFCreateAttributes(out attributes, 1);
            attributes.SetUINT32(MediaFoundationAttributes.MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS, 1);

            object sinkWriterObject;
            // TODO: need to move the class IDs into their own class for use in cases like this
            var guidMFSinkWriter = new Guid("a3bbfb17-8273-4e52-9e0e-9739dc887990");
            var guidIMFSinkWriter = new Guid("3137f1cd-fe5e-4805-a5d8-fb477448cb3d");
            factory.CreateInstanceFromURL(guidMFSinkWriter, url, attributes, guidIMFSinkWriter, out sinkWriterObject);
            Marshal.ReleaseComObject(factory);
            return (IMFSinkWriter)sinkWriterObject;
        }

        private IMFMediaType CreateMediaTypeFromWaveFormat(WaveFormat waveFormat)
        {
            IMFMediaType mediaType;
            MediaFoundationInterop.MFCreateMediaType(out mediaType);
            mediaType.SetGUID(MediaFoundationAttributes.MF_MT_MAJOR_TYPE, MediaTypes.MFMediaType_Audio);
            if (waveFormat.Encoding == WaveFormatEncoding.Pcm)
                mediaType.SetGUID(MediaFoundationAttributes.MF_MT_SUBTYPE, AudioSubtypes.MFAudioFormat_PCM);
            else if (waveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
                mediaType.SetGUID(MediaFoundationAttributes.MF_MT_SUBTYPE, AudioSubtypes.MFAudioFormat_Float);
            else
                throw new ArgumentException("Only supporting PCM or IEEE float at the moment");
            mediaType.SetUINT32(MediaFoundationAttributes.MF_MT_AUDIO_NUM_CHANNELS, waveFormat.Channels); 
            mediaType.SetUINT32(MediaFoundationAttributes.MF_MT_AUDIO_SAMPLES_PER_SECOND, waveFormat.SampleRate);
            mediaType.SetUINT32(MediaFoundationAttributes.MF_MT_AUDIO_BITS_PER_SAMPLE, waveFormat.BitsPerSample);
            return mediaType;
        }

        /// <summary>
        /// Information on configuring an AAC media type can be found here:
        /// http://msdn.microsoft.com/en-gb/library/windows/desktop/dd742785%28v=vs.85%29.aspx
        /// </summary>
        private IMFMediaType CreateAacTargetMediaType(int sampleRate, int channels)
        {
            IMFMediaType mediaType = null;
            MediaFoundationInterop.MFCreateMediaType(out mediaType);
            mediaType.SetGUID(MediaFoundationAttributes.MF_MT_MAJOR_TYPE, MediaTypes.MFMediaType_Audio);
            mediaType.SetGUID(MediaFoundationAttributes.MF_MT_SUBTYPE, AudioSubtypes.MFAudioFormat_AAC);
            mediaType.SetUINT32(MediaFoundationAttributes.MF_MT_AUDIO_NUM_CHANNELS, channels); // 1 or 2 allowed
            mediaType.SetUINT32(MediaFoundationAttributes.MF_MT_AUDIO_SAMPLES_PER_SECOND, sampleRate); // 44100 or 48000 allowed
            mediaType.SetUINT32(MediaFoundationAttributes.MF_MT_AUDIO_BITS_PER_SAMPLE, 16); // must be 16
            // these fields are set by the encoder:
            // MF_MT_AUDIO_AVG_BYTES_PER_SECOND, MF_MT_AUDIO_BLOCK_ALIGNMENT MF_MT_AVG_BITRATE
            return mediaType;
        }

        private IMFMediaType CreateWmaTargetMediaType(int desiredBytesPerSecond)
        {
            var subType = AudioSubtypes.MFAudioFormat_WMAudioV8; // could also try v9
            int avgBitrateDiff = int.MaxValue;

            // look through the available output types
            IMFCollection availableTypes = null;
            MediaFoundationInterop.MFTranscodeGetAudioOutputAvailableTypes(subType, _MFT_ENUM_FLAG.MFT_ENUM_FLAG_ALL, null, out availableTypes);

            int elementCount;
            availableTypes.GetElementCount(out elementCount);

            IMFMediaType selectedType = null;

            // loop through for the closest bitrate to what we wanted
            for (var elementIndex = 0; elementIndex < elementCount; elementIndex++)
            {
                // Get the next element
                object supportedAttributes;
                availableTypes.GetElement(elementIndex, out supportedAttributes);
                var mediaType = (IMFMediaType)supportedAttributes;

                // Get the byte per second
                int avgBytePerSecond;
                mediaType.GetUINT32(MediaFoundationAttributes.MF_MT_AUDIO_AVG_BYTES_PER_SECOND, out avgBytePerSecond);

                // If this is better than the last one found remember the index
                var diff = Math.Abs(avgBytePerSecond - desiredBytesPerSecond);
                if (diff < avgBitrateDiff)
                {
                    if (selectedType != null)
                    {
                        Marshal.ReleaseComObject(selectedType);
                    }
                    selectedType = mediaType;
                    avgBitrateDiff = diff;
                }
                else
                {
                    Marshal.ReleaseComObject(mediaType);
                }
            }
            Marshal.ReleaseComObject(availableTypes);
            return selectedType;
        }

        private string SelectSaveFile()
        {
            var sfd = new SaveFileDialog();
            //return (sfd.ShowDialog() == true) ? new Uri(sfd.FileName).AbsoluteUri : null;
            return (sfd.ShowDialog() == true) ? sfd.FileName : null;
        }

        private string SelectFileToConvert()
        {
            var ofd = new OpenFileDialog();
            return (ofd.ShowDialog() == true) ? ofd.FileName : null;
        }

        [ComImport, Guid("70f598e9-f4ab-495a-99e2-a7c4d3d89abf")]
        class WindowsMediaEncoder
        {
        }

        public void Dispose()
        {
            MediaFoundationApi.Shutdown();
        }
    }
}