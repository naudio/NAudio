using System.Runtime.InteropServices;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Contains statistics about the performance of the sink writer.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SinkWriterStatistics
    {
        /// <summary>
        /// The size of the structure, in bytes.
        /// </summary>
        public int Size;
        /// <summary>
        /// The time stamp of the most recent sample given to the sink writer.
        /// </summary>
        public long LastTimestampReceived;
        /// <summary>
        /// The time stamp of the most recent sample to be encoded.
        /// </summary>
        public long LastTimestampEncoded;
        /// <summary>
        /// The time stamp of the most recent sample given to the media sink.
        /// </summary>
        public long LastTimestampProcessed;
        /// <summary>
        /// The time stamp of the most recent stream tick.
        /// </summary>
        public long LastStreamTickReceived;
        /// <summary>
        /// The system time of the most recent sample request from the media sink.
        /// </summary>
        public long LastSinkSampleRequest;
        /// <summary>
        /// The number of samples received.
        /// </summary>
        public long SamplesReceived;
        /// <summary>
        /// The number of samples encoded.
        /// </summary>
        public long SamplesEncoded;
        /// <summary>
        /// The number of samples given to the media sink.
        /// </summary>
        public long SamplesProcessed;
        /// <summary>
        /// The number of stream ticks received.
        /// </summary>
        public long StreamTicksReceived;
        /// <summary>
        /// The amount of data, in bytes, currently waiting to be processed.
        /// </summary>
        public int ByteCountQueued;
        /// <summary>
        /// The total amount of data, in bytes, that has been sent to the media sink.
        /// </summary>
        public long ByteCountProcessed;
        /// <summary>
        /// The number of pending sample requests.
        /// </summary>
        public int OutstandingSinkSampleRequests;
        /// <summary>
        /// The average rate, in media samples per 100-nanoseconds, at which the application sent samples to the sink writer.
        /// </summary>
        public int AverageSampleRateReceived;
        /// <summary>
        /// The average rate, in media samples per 100-nanoseconds, at which the sink writer sent samples to the encoder.
        /// </summary>
        public int AverageSampleRateEncoded;
        /// <summary>
        /// The average rate, in media samples per 100-nanoseconds, at which the sink writer sent samples to the media sink.
        /// </summary>
        public int AverageSampleRateProcessed;
    }
}
