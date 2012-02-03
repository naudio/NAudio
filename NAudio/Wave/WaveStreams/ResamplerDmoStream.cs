using System;
using System.Collections.Generic;
using System.Text;
using NAudio.Dmo;
using System.Diagnostics;

namespace NAudio.Wave
{
    /// <summary>
    /// Wave Stream for converting between sample rates
    /// </summary>
    public class ResamplerDmoStream : WaveStream
    {
        private readonly IWaveProvider inputProvider;
        private readonly WaveStream inputStream;
        private readonly WaveFormat outputFormat;
        private DmoOutputDataBuffer outputBuffer;
        private Resampler resampler;
        private MediaBuffer inputMediaBuffer;
        private long position;

        /// <summary>
        /// WaveStream to resample using the DMO Resampler
        /// </summary>
        /// <param name="inputProvider">Input Stream</param>
        /// <param name="outputFormat">Desired Output Format</param>
        public ResamplerDmoStream(IWaveProvider inputProvider, WaveFormat outputFormat)
        {
            this.inputProvider = inputProvider;
            this.inputStream = inputProvider as WaveStream;
            this.outputFormat = outputFormat;
            this.resampler = new Resampler();
            if (!resampler.MediaObject.SupportsInputWaveFormat(0, inputProvider.WaveFormat))
            {
                throw new ArgumentException("Unsupported Input Stream format", "inputStream");
            }

            resampler.MediaObject.SetInputWaveFormat(0, inputProvider.WaveFormat);
            if (!resampler.MediaObject.SupportsOutputWaveFormat(0, outputFormat))
            {
                throw new ArgumentException("Unsupported Output Stream format", "outputStream");
            }
         
            resampler.MediaObject.SetOutputWaveFormat(0, outputFormat);
            if (inputStream != null)
            {
                position = InputToOutputPosition(inputStream.Position);
            }
            this.inputMediaBuffer = new MediaBuffer(inputProvider.WaveFormat.AverageBytesPerSecond);
            this.outputBuffer = new DmoOutputDataBuffer(outputFormat.AverageBytesPerSecond);
        }

        /// <summary>
        /// Stream Wave Format
        /// </summary>
        public override WaveFormat WaveFormat
        {
            get { return outputFormat; }
        }

        private long InputToOutputPosition(long inputPosition)
        {
            double ratio = (double)outputFormat.AverageBytesPerSecond
                / inputProvider.WaveFormat.AverageBytesPerSecond;
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
                / inputProvider.WaveFormat.AverageBytesPerSecond;
            long inputPosition = (long)(outputPosition / ratio);
            if (inputPosition % inputProvider.WaveFormat.BlockAlign != 0)
            {
                inputPosition -= inputPosition % inputProvider.WaveFormat.BlockAlign;
            }
            return inputPosition;
        }

        /// <summary>
        /// Stream length in bytes
        /// </summary>
        public override long Length
        {
            get 
            {
                if (this.inputStream == null)
                {
                    throw new InvalidOperationException("Cannot report length if the input was an IWaveProvider");
                }
                return InputToOutputPosition(inputStream.Length); 
            }
        }

        /// <summary>
        /// Stream position in bytes
        /// </summary>
        public override long Position
        {
            get
            {
                return position;
            }
            set
            {
                if (this.inputStream == null)
                {
                    throw new InvalidOperationException("Cannot set position if the input was not a WaveStream");
                }                
                inputStream.Position = OutputToInputPosition(value);
                position = InputToOutputPosition(inputStream.Position);
                resampler.MediaObject.Discontinuity(0);
            }
        }

        /// <summary>
        /// Reads data from input stream
        /// </summary>
        /// <param name="buffer">buffer</param>
        /// <param name="offset">offset into buffer</param>
        /// <param name="count">Bytes required</param>
        /// <returns>Number of bytes read</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            int outputBytesProvided = 0;

            while (outputBytesProvided < count)
            {
                if (resampler.MediaObject.IsAcceptingData(0))
                {
                    // 1. Read from the input stream 
                    int inputBytesRequired = (int)OutputToInputPosition(count - outputBytesProvided);
                    byte[] inputByteArray = new byte[inputBytesRequired];
                    int inputBytesRead = inputProvider.Read(inputByteArray, 0, inputBytesRequired);
                    if (inputBytesRead == 0)
                    {
                        //Debug.WriteLine("ResamplerDmoStream.Read: No input data available");
                        break;
                    }
                    // 2. copy into our DMO's input buffer
                    inputMediaBuffer.LoadData(inputByteArray, inputBytesRead);

                    // 3. Give the input buffer to the DMO to process
                    resampler.MediaObject.ProcessInput(0, inputMediaBuffer, DmoInputDataBufferFlags.None, 0, 0);

                    outputBuffer.MediaBuffer.SetLength(0);
                    outputBuffer.StatusFlags = DmoOutputDataBufferFlags.None;

                    // 4. Now ask the DMO for some output data
                    resampler.MediaObject.ProcessOutput(DmoProcessOutputFlags.None, 1, new DmoOutputDataBuffer[] { outputBuffer });

                    if (outputBuffer.Length == 0)
                    {
                        Debug.WriteLine("ResamplerDmoStream.Read: No output data available");
                        break;
                    }

                    // 5. Now get the data out of the output buffer
                    outputBuffer.RetrieveData(buffer, offset + outputBytesProvided);
                    outputBytesProvided += outputBuffer.Length;

                    Debug.Assert(!outputBuffer.MoreDataAvailable, "have not implemented more data available yet");
                }
                else
                {
                    Debug.Assert(false, "have not implemented not accepting logic yet");
                }
            }
            
            position += outputBytesProvided;
            return outputBytesProvided;
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing">True if disposing (not from finalizer)</param>
        protected override void Dispose(bool disposing)
        {
            if (inputMediaBuffer != null)
            {
                inputMediaBuffer.Dispose();
                inputMediaBuffer = null;
            }
            outputBuffer.Dispose();
            if (resampler != null)
            {
                //resampler.Dispose(); s
                resampler = null;
            }
            base.Dispose(disposing);
        }
    }
}
