using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Contains statistics about the performance of the sink writer.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class MF_SINK_WRITER_STATISTICS
    {
        /// <summary>
        /// The size of the structure, in bytes.
        /// </summary>
        public int cb;
        /// <summary>
        /// The time stamp of the most recent sample given to the sink writer.
        /// </summary>
        public long llLastTimestampReceived;
        /// <summary>
        /// The time stamp of the most recent sample to be encoded.
        /// </summary>
        public long llLastTimestampEncoded;
        /// <summary>
        /// The time stamp of the most recent sample given to the media sink.
        /// </summary>
        public long llLastTimestampProcessed;
        /// <summary>
        /// The time stamp of the most recent stream tick. 
        /// </summary>
        public long llLastStreamTickReceived;
        /// <summary>
        /// The system time of the most recent sample request from the media sink. 
        /// </summary>
        public long llLastSinkSampleRequest;
        /// <summary>
        /// The number of samples received.
        /// </summary>
        public long qwNumSamplesReceived;
        /// <summary>
        /// The number of samples encoded.
        /// </summary>
        public long qwNumSamplesEncoded;
        /// <summary>
        /// The number of samples given to the media sink.
        /// </summary>
        public long qwNumSamplesProcessed;
        /// <summary>
        /// The number of stream ticks received.
        /// </summary>
        public long qwNumStreamTicksReceived;
        /// <summary>
        /// The amount of data, in bytes, currently waiting to be processed. 
        /// </summary>
        public int dwByteCountQueued;
        /// <summary>
        /// The total amount of data, in bytes, that has been sent to the media sink.
        /// </summary>
        public long qwByteCountProcessed;
        /// <summary>
        /// The number of pending sample requests.
        /// </summary>
        public int dwNumOutstandingSinkSampleRequests;
        /// <summary>
        /// The average rate, in media samples per 100-nanoseconds, at which the application sent samples to the sink writer.
        /// </summary>
        public int dwAverageSampleRateReceived;
        /// <summary>
        /// The average rate, in media samples per 100-nanoseconds, at which the sink writer sent samples to the encoder
        /// </summary>
        public int dwAverageSampleRateEncoded;
        /// <summary>
        /// The average rate, in media samples per 100-nanoseconds, at which the sink writer sent samples to the media sink.
        /// </summary>
        public int dwAverageSampleRateProcessed;
    }
}
