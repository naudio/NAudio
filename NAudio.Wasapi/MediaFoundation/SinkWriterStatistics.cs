using System.Runtime.InteropServices;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Contains statistics about the performance of the sink writer.
    /// </summary>
    /// <remarks>
    /// Windows SDK name: MF_SINK_WRITER_STATISTICS (mfreadwrite.h).
    /// See https://learn.microsoft.com/windows/win32/api/mfreadwrite/ns-mfreadwrite-mf_sink_writer_statistics
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct SinkWriterStatistics
    {
        /// <summary>
        /// The size of the structure, in bytes.
        /// </summary>
        /// <remarks>cb</remarks>
        public int Size;
        /// <summary>
        /// The time stamp of the most recent sample given to the sink writer.
        /// </summary>
        /// <remarks>llLastTimestampReceived</remarks>
        public long LastTimestampReceived;
        /// <summary>
        /// The time stamp of the most recent sample to be encoded.
        /// </summary>
        /// <remarks>llLastTimestampEncoded</remarks>
        public long LastTimestampEncoded;
        /// <summary>
        /// The time stamp of the most recent sample given to the media sink.
        /// </summary>
        /// <remarks>llLastTimestampProcessed</remarks>
        public long LastTimestampProcessed;
        /// <summary>
        /// The time stamp of the most recent stream tick.
        /// </summary>
        /// <remarks>llLastStreamTickReceived</remarks>
        public long LastStreamTickReceived;
        /// <summary>
        /// The system time of the most recent sample request from the media sink.
        /// </summary>
        /// <remarks>llLastSinkSampleRequest</remarks>
        public long LastSinkSampleRequest;
        /// <summary>
        /// The number of samples received.
        /// </summary>
        /// <remarks>qwNumSamplesReceived</remarks>
        public long SamplesReceived;
        /// <summary>
        /// The number of samples encoded.
        /// </summary>
        /// <remarks>qwNumSamplesEncoded</remarks>
        public long SamplesEncoded;
        /// <summary>
        /// The number of samples given to the media sink.
        /// </summary>
        /// <remarks>qwNumSamplesProcessed</remarks>
        public long SamplesProcessed;
        /// <summary>
        /// The number of stream ticks received.
        /// </summary>
        /// <remarks>qwNumStreamTicksReceived</remarks>
        public long StreamTicksReceived;
        /// <summary>
        /// The amount of data, in bytes, currently waiting to be processed.
        /// </summary>
        /// <remarks>dwByteCountQueued</remarks>
        public int ByteCountQueued;
        /// <summary>
        /// The total amount of data, in bytes, that has been sent to the media sink.
        /// </summary>
        /// <remarks>qwByteCountProcessed</remarks>
        public long ByteCountProcessed;
        /// <summary>
        /// The number of pending sample requests.
        /// </summary>
        /// <remarks>dwNumOutstandingSinkSampleRequests</remarks>
        public int OutstandingSinkSampleRequests;
        /// <summary>
        /// The average rate, in media samples per 100-nanoseconds, at which the application sent samples to the sink writer.
        /// </summary>
        /// <remarks>dwAverageSampleRateReceived</remarks>
        public int AverageSampleRateReceived;
        /// <summary>
        /// The average rate, in media samples per 100-nanoseconds, at which the sink writer sent samples to the encoder.
        /// </summary>
        /// <remarks>dwAverageSampleRateEncoded</remarks>
        public int AverageSampleRateEncoded;
        /// <summary>
        /// The average rate, in media samples per 100-nanoseconds, at which the sink writer sent samples to the media sink.
        /// </summary>
        /// <remarks>dwAverageSampleRateProcessed</remarks>
        public int AverageSampleRateProcessed;
    }
}
