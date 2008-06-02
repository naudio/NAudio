using System;
using System.Collections.Generic;
using System.Text;
using NAudio.Dmo;

namespace NAudio.Wave
{
    public class ResamplerDmoStream : WaveStream
    {
        WaveStream inputStream;
        WaveFormat outputFormat;
        Resampler resampler;
        long position;
        MediaBuffer inputBuffer;
        DmoOutputDataBuffer outputBuffer;

        public ResamplerDmoStream(WaveStream inputStream, WaveFormat outputFormat)
        {
            this.inputStream = inputStream;
            this.outputFormat = outputFormat;
            this.resampler = new Resampler();
            resampler.MediaObject.SetInputWaveFormat(0, inputStream.WaveFormat);
            resampler.MediaObject.SetOutputWaveFormat(0, outputFormat);
            position = InputToOutputPosition(inputStream.Position);
            inputBuffer = new MediaBuffer(inputStream.WaveFormat.AverageBytesPerSecond);
            outputBuffer = new DmoOutputDataBuffer(outputFormat.AverageBytesPerSecond);
        }

        public override WaveFormat WaveFormat
        {
            get { return outputFormat; }
        }

        private long InputToOutputPosition(long inputPosition)
        {
            double ratio = (double)outputFormat.AverageBytesPerSecond
                / inputStream.WaveFormat.AverageBytesPerSecond;
            long outputPosition = (long)(inputPosition * ratio);
            if (outputPosition % outputFormat.BlockAlign != 0)
            {
                outputPosition -= outputPosition % outputFormat.BlockAlign;
            }
            return outputPosition;
        }

        private long OutputToInputPosition(long outputPosition)
        {
            double ratio = (double)outputFormat.AverageBytesPerSecond
                / inputStream.WaveFormat.AverageBytesPerSecond;
            long inputPosition = (long)(outputPosition / ratio);
            if (inputPosition % inputStream.WaveFormat.BlockAlign != 0)
            {
                inputPosition -= inputPosition % inputStream.WaveFormat.BlockAlign;
            }
            return outputPosition;
        }

        public override long Length
        {
            get { return InputToOutputPosition(inputStream.Length); }
        }

        public override long Position
        {
            get
            {
                return position;
            }
            set
            {
                inputStream.Position = OutputToInputPosition(value);
                position = InputToOutputPosition(inputStream.Position);
                resampler.MediaObject.Discontinuity(0);
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int outputBytesProvided = 0;
            // TODO: possibly loop here

            // 1. Read from the input stream 
            int inputBytesRequired = (int)OutputToInputPosition(count - outputBytesProvided);
            byte[] inputByteArray = new byte[inputBytesRequired];
            int inputBytesRead = inputStream.Read(inputByteArray,0,inputBytesRequired);

            // 2. copy into our DMO's input buffer
            inputBuffer.LoadData(inputByteArray, inputBytesRead);

            // 3. Give the input buffer to the DMO to process
            resampler.MediaObject.ProcessInput(0, inputBuffer, DmoInputDataBufferFlags.None, 0, 0);

            // 4. Now ask the DMO for some output data
            resampler.MediaObject.ProcessOutput(DmoProcessOutputFlags.None, 1, new DmoOutputDataBuffer[] { outputBuffer });

            outputBytesProvided += outputBuffer.Length;
            outputBuffer.RetrieveData(buffer, offset);

            position += outputBytesProvided;
            return outputBytesProvided;
        }

        protected override void Dispose(bool disposing)
        {
            if (inputBuffer != null)
            {
                inputBuffer.Dispose();
                inputBuffer = null;
            }
            outputBuffer.Dispose();
            base.Dispose(disposing);
        }
    }
}
