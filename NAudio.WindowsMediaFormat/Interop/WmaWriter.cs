//  Widows Media Format Utils classes
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
// Modified for NAudio by Mark Heath

using System;
using System.IO;
using System.Runtime.InteropServices;
using NAudio.Wave;

namespace NAudio.WindowsMediaFormat
{
    /// <summary>
    /// Audio Writer to write Windows Media Audio data to a stream.
    /// </summary>
    public class WmaWriter : IWMWriterSink, IDisposable
    {
        protected IWMWriter m_Writer;
        private IWMProfile m_Profile;
        private bool m_HeaderWrote = false;
        private long m_HeaderPosition;
        private uint m_HeaderLength;
        private ulong m_MsAudioTime = 0;
        private Stream m_outputStream;
        private WaveFormat m_inputFormat;


        public WmaWriter(Stream output, WaveFormat inputFormat, CodecFormat codecFormat)
            : this(output,inputFormat,codecFormat.GetProfile())
        {

        }

        /// <summary>
        /// Create the writer indicating Metadata information
        /// </summary>
        /// <param name="output"><see cref="System.IO.Stream"/> Where resulting WMA string will be written</param>
        /// <param name="inputFormat">PCM format of input data received in <see cref="WmaWriter.Write"/> method</param>
        /// <param name="profile">IWMProfile that describe the resulting compressed stream</param>
        /// <param name="MetadataAttributes">Array of <see cref="Yeti.WMFSdk.WM_Attr"/> structures describing the metadata information that will be in the result stream</param>
        public WmaWriter(Stream output, WaveFormat inputFormat, IWMProfile profile, WM_Attr[] MetadataAttributes)
        {
            this.m_outputStream = output;
            this.m_inputFormat = inputFormat;
            m_Writer = WM.CreateWriter();
            IWMWriterAdvanced wa = (IWMWriterAdvanced)m_Writer;
            wa.AddSink((IWMWriterSink)this);
            m_Writer.SetProfile(profile);
            uint inputs;
            m_Writer.GetInputCount(out inputs);
            if (inputs == 1)
            {
                IWMInputMediaProps InpProps;
                Guid type;
                m_Writer.GetInputProps(0, out InpProps);
                InpProps.GetType(out type);
                if (type == MediaTypes.WMMEDIATYPE_Audio)
                {
                    WM_MEDIA_TYPE mt;
                    mt.majortype = MediaTypes.WMMEDIATYPE_Audio;
                    mt.subtype = MediaTypes.WMMEDIASUBTYPE_PCM;
                    mt.bFixedSizeSamples = true;
                    mt.bTemporalCompression = false;
                    mt.lSampleSize = (uint)inputFormat.BlockAlign;
                    mt.formattype = MediaTypes.WMFORMAT_WaveFormatEx;
                    mt.pUnk = IntPtr.Zero;
                    mt.cbFormat = (uint)Marshal.SizeOf(inputFormat);

                    GCHandle h = GCHandle.Alloc(inputFormat, GCHandleType.Pinned);
                    try
                    {
                        mt.pbFormat = h.AddrOfPinnedObject();
                        InpProps.SetMediaType(ref mt);
                    }
                    finally
                    {
                        h.Free();
                    }
                    m_Writer.SetInputProps(0, InpProps);
                    if (MetadataAttributes != null)
                    {
                        WMHeaderInfo info = new WMHeaderInfo((IWMHeaderInfo)m_Writer);
                        foreach (WM_Attr attr in MetadataAttributes)
                        {
                            info.SetAttribute(attr);
                        }
                        info = null;
                    }
                    m_Writer.BeginWriting();
                    m_Profile = profile;
                }
                else
                {
                    throw new ArgumentException("Invalid profile", "profile");
                }
            }
            else
            {
                throw new ArgumentException("Invalid profile", "profile");
            }
        }
        /// <summary>
        /// Create the writer without metadata Information
        /// </summary>
        /// <param name="output"><see cref="System.IO.Stream"/> Where resulting WMA string will be written</param>
        /// <param name="format">PCM format of input data received in <see cref="WmaWriter.Write"/> method</param>
        /// <param name="profile">IWMProfile that describe the resulting compressed stream</param>
        public WmaWriter(Stream output, WaveFormat format, IWMProfile profile)
            : this(output, format, profile, null)
        {
        }

        #region IWMWriterSink implementation
        public void OnHeader([In, MarshalAs(UnmanagedType.Interface)] INSSBuffer pHeader)
        {
            byte[] buffer;
            uint Length;
            if (pHeader is ManBuffer)
            {
                buffer = ((ManBuffer)pHeader).Buffer;
                Length = ((ManBuffer)pHeader).UsedLength;
            }
            else
            {
                NSSBuffer b = new NSSBuffer(pHeader);
                Length = b.Length;
                buffer = new byte[Length];
                b.Read(buffer, 0, (int)Length);
            }
            if (!m_HeaderWrote)
            {
                if (m_outputStream.CanSeek)
                {
                    m_HeaderPosition = m_outputStream.Position;
                    m_HeaderLength = Length;
                }
                m_HeaderWrote = true;
                m_outputStream.Write(buffer, 0, (int)Length);
            }
            else if (m_outputStream.CanSeek && (Length == m_HeaderLength))
            {
                long pos = m_outputStream.Position;
                m_outputStream.Position = m_HeaderPosition;
                m_outputStream.Write(buffer, 0, (int)Length);
                m_outputStream.Position = pos;
            }
        }

        public void IsRealTime([Out, MarshalAs(UnmanagedType.Bool)] out bool pfRealTime)
        {
            pfRealTime = false;
        }

        public void AllocateDataUnit([In] uint cbDataUnit,
         [Out, MarshalAs(UnmanagedType.Interface)] out INSSBuffer ppDataUnit)
        {
            ppDataUnit = (INSSBuffer)(new ManBuffer(cbDataUnit));
        }

        public void OnDataUnit([In, MarshalAs(UnmanagedType.Interface)] INSSBuffer pDataUnit)
        {
            byte[] buffer;
            int Length;
            if (pDataUnit is ManBuffer)
            {
                buffer = ((ManBuffer)pDataUnit).Buffer;
                Length = (int)((ManBuffer)pDataUnit).UsedLength;
            }
            else
            {
                NSSBuffer b = new NSSBuffer(pDataUnit);
                Length = (int)b.Length;
                buffer = new byte[Length];
                b.Read(buffer, 0, Length);
            }
            m_outputStream.Write(buffer, 0, Length);
        }

        public void OnEndWriting()
        {
        }

        #endregion

        public void Dispose()
        {
            try
            {
                if (m_Writer != null)
                {
                    m_Writer.EndWriting();
                    IWMWriterAdvanced wa = (IWMWriterAdvanced)m_Writer;
                    wa.RemoveSink((IWMWriterSink)this);
                    m_Writer = null;
                    m_Profile = null;
                }
            }
            finally
            {
                m_outputStream.Close();
            }
        }


        /// <summary>
        /// Return the optimal size of buffer in each write operations. Other value could be
        /// more optimal. The only requirement for buffer size is that it must be multiple
        /// of PCM sample size <see cref="Yeti.WMFSdk.WmaWriterConfig.Format.nBlockAlign"/>
        /// </summary>
        /// <returns>Size equivalent to 100 milliseconds.</returns>
        public int GetOptimalBufferSize()
        {
            int res = m_inputFormat.AverageBytesPerSecond / 10;
            res += (res % m_inputFormat.BlockAlign);
            return res;
        }

        /// <summary>
        /// Write a buffer of uncompressed PCM data to the buffer.
        /// </summary>
        /// <param name="buffer">Byte array defining the buffer to write.</param>
        /// <param name="index">Index of first value to write</param>
        /// <param name="count">NUmber of byte to write. Must be multiple of PCM sample size <see cref="Yeti.WMFSdk.WmaWriterConfig.Format.nBlockAlign"/></param>
        public void Write(byte[] buffer, int index, int count)
        {
            if ((count % m_inputFormat.BlockAlign) == 0)
            {
                INSSBuffer IBuff;
                NSSBuffer NssBuff;
                m_Writer.AllocateSample((uint)count, out IBuff);
                NssBuff = new NSSBuffer(IBuff);
                NssBuff.Write(buffer, index, count);
                NssBuff.Length = (uint)count;
                m_Writer.WriteSample(0, m_MsAudioTime * 10000, 0, IBuff);
                m_MsAudioTime += ((ulong)count * 1000) / (ulong)m_inputFormat.AverageBytesPerSecond;
            }
            else
            {
                throw new ArgumentException(string.Format("Invalid buffer size. Buffer size must be aligned to {0} bytes.", m_inputFormat.BlockAlign), "count");
            }
        }
    }
}

