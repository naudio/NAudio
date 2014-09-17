#region Original License
//Widows Media Format Interfaces
//
//  THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
//  PURPOSE. IT CAN BE DISTRIBUTED FREE OF CHARGE AS LONG AS THIS HEADER
//  REMAINS UNCHANGED.
//
//  Email:  yetiicb@hotmail.com
//
//  Copyright (C) 2002-2004 Idael Cardoso.
//
#endregion

#region Code Modifications Note
// Yuval Naveh, 2010
// Note - The code below has been changed and fixed from its original form.
// Changes include - Formatting, Layout, Coding standards and removal of compilation warnings

// Mark Heath, 2010 - modified for inclusion in NAudio
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using NAudio.Wave;

namespace NAudio.WindowsMediaFormat
{
    /// <summary>
    /// Stream that provides uncompressed audio data from any file that
    /// can be read using the WMF (WMA, WMV, MP3, MPE, ASF, etc)
    /// </summary>
    public class WmaStream : Stream
    {
        /// <summary>
        /// Create WmaStream with specific format for for uncompressed audio data.
        /// </summary>
        /// <param name="FileName">Name of asf file</param>
        /// <param name="OutputFormat">WaveFormat that define the desired audio data format</param>
        public WmaStream(string FileName, WaveFormat OutputFormat)
        {
            m_reader = WM.CreateSyncReader(WMT_RIGHTS.WMT_RIGHT_NO_DRM);
            try
            {
                m_reader.Open(FileName);
                Init(OutputFormat);
            }
            catch
            {
                try
                {
                    m_reader.Close();
                }
                catch
                {
                }
                m_reader = null;
                throw;
            }
        }

        /// <summary>
        /// Create WmaStream. The first PCM available for audio outputs will be used as output format.
        /// Output format can be checked in <see cref="Yeti.WMFSDK.WmaStream.Format"/> property.
        /// </summary>
        /// <param name="FileName">Name of asf file</param>
        public WmaStream(string FileName)
            : this(FileName, null)
        {
        }

        ~WmaStream()
        {
            Dispose(false);
        }

        /// <summary>
        /// Give the <see cref="WaveLib.WaveFormat"/> that describes the format of ouput data in each Read operation.
        /// </summary>
        public WaveFormat Format
        {
            get { return new WaveFormat(m_outputFormat.SampleRate, m_outputFormat.BitsPerSample, m_outputFormat.Channels); }
        }

        /// <summary>
        /// IWMProfile of the input ASF file.
        /// </summary>
        public IWMProfile Profile
        {
            get { return (IWMProfile)m_reader; }
        }

        /// <summary>
        /// IWMHeaderInfo related to the input ASF file.
        /// </summary>
        public IWMHeaderInfo HeaderInfo
        {
            get { return (IWMHeaderInfo)m_reader; }
        }

        /// <summary>
        /// Recomended size of buffer in each <see cref="Yeti.WMFSDK.WmaStream.Read"/> operation
        /// </summary>
        public int SampleSize
        {
            get { return (int)m_sampleSize; }
        }

        /// <summary>
        /// If Seek if allowed every seek operation must be to a value multiple of SeekAlign
        /// </summary>
        public long SeekAlign
        {
            get { return Math.Max(SampleTime2BytePosition(1), (long)m_outputFormat.BlockAlign); }
        }

        /// <summary>
        /// Convert a time value in 100 nanosecond unit to a byte position 
        /// of byte array containing the PCM data. <seealso cref="Yeti.WMFSDK.WmaStream.BytePosition2SampleTime"/>
        /// </summary>
        /// <param name="SampleTime">Sample time in 100 nanosecond units</param>
        /// <returns>Byte position</returns>
        protected long SampleTime2BytePosition(ulong SampleTime)
        {
            ulong res = SampleTime * (ulong)m_outputFormat.AverageBytesPerSecond / 10000000;
            //Align to sample position
            res -= (res % (ulong)m_outputFormat.BlockAlign);
            return (long)res;
        }

        /// <summary>
        /// Returns the sample time in 100 nanosecond units corresponding to
        /// byte position in a byte array of PCM data. <see cref="Yeti.WMFSDK.WmaStream.SampleTime2BytePosition"/>
        /// </summary>
        /// <param name="pos">Byte position</param>
        /// <returns>Sample time in 100 nanosecond units</returns>
        protected ulong BytePosition2SampleTime(long pos)
        {
            //Align to sample position
            pos -= (pos % (long)m_outputFormat.BlockAlign);
            return (ulong)pos * 10000000 / (ulong)m_outputFormat.AverageBytesPerSecond;
        }

        /// <summary>
        /// Index that give the string representation of the Metadata attribute whose
        /// name is the string index. If the Metadata is not present returns <code>null</code>. 
        /// This only return the file level Metadata info, to read stream level metadata use <see cref="Yeti.WMFSDK.WmaStream.HeaderInfo"/>
        /// </summary>
        /// <example>
        /// <code>
        /// using (WmaStream str = new WmaStream("somefile.asf") )
        /// {
        ///   string Title = str[WM.g_wszWMTitle];
        ///   if ( Title != null )
        ///   {
        ///     Console.WriteLine("Title: {0}", Title);
        ///   }
        /// }
        /// </code>
        /// </example>
        [System.Runtime.CompilerServices.IndexerName("Attributes")]
        public string this[string AttrName]
        {
            get
            {
                WMHeaderInfo head = new WMHeaderInfo(HeaderInfo);
                try
                {
                    return head[AttrName].Value.ToString();
                }
                catch (COMException e)
                {
                    if (e.ErrorCode == WM.ASF_E_NOTFOUND)
                    {
                        return null;
                    }
                    else
                    {
                        throw (e);
                    }
                }
            }
        }

        #region Private methods to interact with the WMF

        private void Init(WaveFormat OutputFormat)
        {
            m_outputNumber = GetAudioOutputNumber(m_reader);
            if (m_outputNumber == InvalidOuput)
            {
                throw new ArgumentException("An audio stream was not found");
            }
            int[] FormatIndexes = GetPCMOutputNumbers(m_reader, (uint)m_outputNumber);
            if (FormatIndexes.Length == 0)
            {
                throw new ArgumentException("An audio stream was not found");
            }
            if (OutputFormat != null)
            {
                m_outputFormatNumber = -1;
                for (int i = 0; i < FormatIndexes.Length; i++)
                {
                    WaveFormat fmt = GetOutputFormat(m_reader, (uint)m_outputNumber, (uint)FormatIndexes[i]);
                    if (// (fmt.wFormatTag == OutputFormat.wFormatTag) &&
                      (fmt.AverageBytesPerSecond == OutputFormat.AverageBytesPerSecond) &&
                      (fmt.BlockAlign == OutputFormat.BlockAlign) &&
                      (fmt.Channels == OutputFormat.Channels) &&
                      (fmt.SampleRate == OutputFormat.SampleRate) &&
                      (fmt.BitsPerSample == OutputFormat.BitsPerSample))
                    {
                        m_outputFormatNumber = FormatIndexes[i];
                        m_outputFormat = fmt;
                        break;
                    }
                }
                if (m_outputFormatNumber < 0)
                {
                    throw new ArgumentException("No PCM output found");
                }
            }
            else
            {
                m_outputFormatNumber = FormatIndexes[0];
                m_outputFormat = GetOutputFormat(m_reader, (uint)m_outputNumber, (uint)FormatIndexes[0]);
            }
            uint OutputCtns = 0;
            m_reader.GetOutputCount(out OutputCtns);
            ushort[] StreamNumbers = new ushort[OutputCtns];
            WMT_STREAM_SELECTION[] StreamSelections = new WMT_STREAM_SELECTION[OutputCtns];
            for (uint i = 0; i < OutputCtns; i++)
            {
                m_reader.GetStreamNumberForOutput(i, out StreamNumbers[i]);
                if (i == m_outputNumber)
                {
                    StreamSelections[i] = WMT_STREAM_SELECTION.WMT_ON;
                    m_outputStream = StreamNumbers[i];
                    m_reader.SetReadStreamSamples(m_outputStream, false);
                }
                else
                {
                    StreamSelections[i] = WMT_STREAM_SELECTION.WMT_OFF;
                }
            }
            m_reader.SetStreamsSelected((ushort)OutputCtns, StreamNumbers, StreamSelections);
            IWMOutputMediaProps Props = null;
            m_reader.GetOutputFormat((uint)m_outputNumber, (uint)m_outputFormatNumber, out Props);
            m_reader.SetOutputProps((uint)m_outputNumber, Props);

            int size = 0;
            Props.GetMediaType(IntPtr.Zero, ref size);
            IntPtr buffer = Marshal.AllocCoTaskMem(size);
            try
            {
                WM_MEDIA_TYPE mt;
                Props.GetMediaType(buffer, ref size);
                mt = (WM_MEDIA_TYPE)Marshal.PtrToStructure(buffer, typeof(WM_MEDIA_TYPE));
                m_sampleSize = mt.lSampleSize;
            }
            finally
            {
                Marshal.FreeCoTaskMem(buffer);
                Props = null;
            }

            m_seekable = false;
            m_length = -1;
            WMHeaderInfo head = new WMHeaderInfo(HeaderInfo);
            try
            {
                m_seekable = (bool)head[WM.g_wszWMSeekable];
                // Yuval Naveh
                ulong nanoDuration = (ulong)head[WM.g_wszWMDuration];
                m_duration = new TimeSpan((long)nanoDuration);
                m_length = SampleTime2BytePosition(nanoDuration);
            }
            catch (COMException e)
            {
                if (e.ErrorCode != WM.ASF_E_NOTFOUND)
                {
                    throw (e);
                }
            }

        }

        private static uint GetAudioOutputNumber(IWMSyncReader Reader)
        {
            uint res = InvalidOuput;
            uint OutCounts = 0;
            Reader.GetOutputCount(out OutCounts);
            for (uint i = 0; i < OutCounts; i++)
            {
                IWMOutputMediaProps Props = null;
                Reader.GetOutputProps(i, out Props);
                Guid mt;
                Props.GetType(out mt);
                if (mt == MediaTypes.WMMEDIATYPE_Audio)
                {
                    res = i;
                    break;
                }
            }
            return res;
        }

        protected const uint WAVE_FORMAT_EX_SIZE = 18;

        private static int[] GetPCMOutputNumbers(IWMSyncReader Reader, uint OutputNumber)
        {
            var result = new List<int>();
            uint FormatCount = 0;
            Reader.GetOutputFormatCount(OutputNumber, out FormatCount);
            int BufferSize = Marshal.SizeOf(typeof(WM_MEDIA_TYPE)) + Marshal.SizeOf(typeof(WaveFormat));
            IntPtr buffer = Marshal.AllocCoTaskMem(BufferSize);
            try
            {
                for (int i = 0; i < FormatCount; i++)
                {
                    IWMOutputMediaProps Props = null;
                    int size = 0;
                    WM_MEDIA_TYPE mt;
                    Reader.GetOutputFormat(OutputNumber, (uint)i, out Props);
                    Props.GetMediaType(IntPtr.Zero, ref size);
                    if (size > BufferSize)
                    {
                        BufferSize = size;
                        Marshal.FreeCoTaskMem(buffer);
                        buffer = Marshal.AllocCoTaskMem(BufferSize);
                    }
                    Props.GetMediaType(buffer, ref size);
                    mt = (WM_MEDIA_TYPE)Marshal.PtrToStructure(buffer, typeof(WM_MEDIA_TYPE));
                    if ((mt.majortype == MediaTypes.WMMEDIATYPE_Audio) &&
                         (mt.subtype == MediaTypes.WMMEDIASUBTYPE_PCM) &&
                         (mt.formattype == MediaTypes.WMFORMAT_WaveFormatEx) &&
                         (mt.cbFormat >= WAVE_FORMAT_EX_SIZE))
                    {
                        result.Add(i);
                    }
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(buffer);
            }
            return result.ToArray();
        }

        private static WaveFormat GetOutputFormat(IWMSyncReader reader, uint outputNumber, uint formatNumber)
        {
            IWMOutputMediaProps Props = null;
            int size = 0;
            WaveFormat fmt = null;
            reader.GetOutputFormat(outputNumber, formatNumber, out Props);
            Props.GetMediaType(IntPtr.Zero, ref size);
            IntPtr buffer = Marshal.AllocCoTaskMem(Math.Max(size, Marshal.SizeOf(typeof(WM_MEDIA_TYPE)) + Marshal.SizeOf(typeof(WaveFormat))));
            try
            {
                Props.GetMediaType(buffer, ref size);
                var mt = (WM_MEDIA_TYPE)Marshal.PtrToStructure(buffer, typeof(WM_MEDIA_TYPE));
                if ((mt.majortype == MediaTypes.WMMEDIATYPE_Audio) &&
                     (mt.subtype == MediaTypes.WMMEDIASUBTYPE_PCM) &&
                     (mt.formattype == MediaTypes.WMFORMAT_WaveFormatEx) &&
                     (mt.cbFormat >= WAVE_FORMAT_EX_SIZE))
                {
                    fmt = new WaveFormat(44100, 16, 2);
                    Marshal.PtrToStructure(mt.pbFormat, fmt);
                }
                else
                {
                    throw new ArgumentException(string.Format("The format {0} of the output {1} is not a valid PCM format", formatNumber, outputNumber));
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(buffer);
            }
            return fmt;
        }
        #endregion

        #region Overrided Stream methods
        public override void Close()
        {
            if (m_reader != null)
            {
                m_reader.Close();
                m_reader = null;
            }
            base.Close();
        }

        private NSSBuffer m_BufferReader = null;

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (m_reader != null)
            {
                int read = 0;
                if ((m_length > 0) && ((m_length - m_position) < count))
                {
                    count = (int)(m_length - m_position);
                }
                if (m_BufferReader != null)
                {
                    while ((m_BufferReader.Position < m_BufferReader.Length) && (read < count))
                    {
                        read += m_BufferReader.Read(buffer, offset, count);
                    }
                }
                while (read < count)
                {
                    INSSBuffer sample = null;
                    ulong SampleTime = 0;
                    ulong Duration = 0;
                    uint Flags = 0;
                    try
                    {
                        m_reader.GetNextSample(m_outputStream, out sample, out SampleTime, out Duration, out Flags, out m_outputNumber, out m_outputStream);
                    }
                    catch (COMException e)
                    {
                        if (e.ErrorCode == WM.NS_E_NO_MORE_SAMPLES)
                        { //No more samples
                            if (m_outputFormat.BitsPerSample == 8)
                            {
                                while (read < count)
                                {
                                    buffer[offset + read] = 0x80;
                                    read++;
                                }
                            }
                            else
                            {
                                Array.Clear(buffer, offset + read, count - read);
                                read = count;
                            }
                            break;
                        }
                        else
                        {
                            throw (e);
                        }
                    }
                    m_BufferReader = new NSSBuffer(sample);
                    read += m_BufferReader.Read(buffer, offset + read, count - read);
                }
                if ((m_BufferReader != null) && (m_BufferReader.Position >= m_BufferReader.Length))
                {
                    m_BufferReader = null;
                }
                m_position += read;
                return read;
            }
            else
            {
                throw new ObjectDisposedException(this.ToString());
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (CanSeek)
            {
                switch (origin)
                {
                    case SeekOrigin.Current:
                        offset += m_position;
                        break;
                    case SeekOrigin.End:
                        offset += m_length;
                        break;
                }
                if (offset == m_position)
                {
                    return m_position; // :-)
                }
                if ((offset < 0) || (offset > m_length))
                {
                    throw new ArgumentException("Offset out of range", "offset");
                }
                if ((offset % SeekAlign) > 0)
                {
                    throw new ArgumentException(string.Format("Offset must be aligned by a value of SeekAlign ({0})", SeekAlign), "offset");
                }
                ulong SampleTime = BytePosition2SampleTime(offset);
                m_reader.SetRange(SampleTime, 0);
                m_position = offset;
                m_BufferReader = null;
                return offset;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public override void Flush()
        {
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override bool CanRead
        {
            get
            {
                if (m_reader != null)
                {
                    return true;
                }
                else
                {
                    throw new ObjectDisposedException(this.ToString());
                }
            }
        }

        public override bool CanSeek
        {
            get
            {
                if (m_reader != null)
                {
                    return m_seekable && (m_length > 0);
                }
                else
                {
                    throw new ObjectDisposedException(this.ToString());
                }
            }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public TimeSpan Duration
        {
            get
            {
                return m_duration;
            }
        }

        public override long Length
        {
            get
            {
                if (m_reader != null)
                {
                    if (CanSeek)
                    {
                        return m_length;
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }
                else
                {
                    throw new ObjectDisposedException(this.ToString());
                }
            }
        }

        public override long Position
        {
            get
            {
                if (m_reader != null)
                {
                    return m_position;
                }
                else
                {
                    throw new ObjectDisposedException(this.ToString());
                }
            }
            set
            {
                Seek(value, SeekOrigin.Begin);
            }
        }
        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (m_reader != null)
                {
                    m_reader.Close();
                    m_reader = null;
                }
            }
        }

        private IWMSyncReader m_reader = null;
        private uint m_outputNumber;
        private ushort m_outputStream;
        private int m_outputFormatNumber;
        private long m_position = 0;
        private long m_length = -1;
        private bool m_seekable = false;
        private uint m_sampleSize = 0;
        private WaveFormat m_outputFormat = null;
        private const uint InvalidOuput = 0xFFFFFFFF;

        private TimeSpan m_duration;
    }

}
