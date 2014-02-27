#region © Copyright 2010 Yuval Naveh. MIT.
/* Copyright (c) 2010, Yuval Naveh

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using NAudio.Wave;

namespace NAudio.WindowsMediaFormat
{
    /// <summary>
    /// NAudio reader for WMA Vorbis files
    /// </summary>
    /// <remarks>
    /// Written By Yuval Naveh
    /// </remarks>
    public class WMAFileReader : WaveStream
    {
        #region Constructors

        /// <summary>Constructor - Supports opening a WMA file</summary>
        public WMAFileReader(string wmaFileName)
        {
            m_wmaStream = new WmaStream(wmaFileName);
            m_waveFormat = m_wmaStream.Format;
        }

        #endregion

        #region WaveStream Overrides - Implement logic which is specific to WMA

        /// <summary>
        /// This is the length in bytes of data available to be read out from the Read method
        /// (i.e. the decompressed WMA length)
        /// n.b. this may return 0 for files whose length is unknown
        /// </summary>
        public override long Length
        {
            get
            {
                return m_wmaStream.Length;
            }
        }

        /// <summary>
        /// <see cref="WaveStream.WaveFormat"/>
        /// </summary>
        public override WaveFormat WaveFormat
        {
            get { return m_waveFormat; }
        }

        /// <summary>
        /// <see cref="Stream.Position"/>
        /// </summary>
        public override long Position
        {
            get
            {
                return m_wmaStream.Position;
            }
            set
            {
                lock (m_repositionLock)
                {
                    m_wmaStream.Position = value;
                }
            }
        }

        /// <summary>
        /// Reads decompressed PCM data from our WMA file.
        /// </summary>
        public override int Read(byte[] sampleBuffer, int offset, int numBytes)
        {
            int bytesRead = 0;
            lock (m_repositionLock)
            {
                // Read PCM bytes from the WMA File into the sample buffer
                bytesRead = m_wmaStream.Read(sampleBuffer, offset, numBytes);
            }

            return bytesRead;
        }

        #endregion

        #region Dispose

        /// <summary>
        /// Disposes this WaveStream
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (m_wmaStream != null)
                {
                    m_wmaStream.Close();
                    m_wmaStream.Dispose();
                    m_wmaStream = null;
                }
            }
            base.Dispose(disposing);
        }

        #endregion

        #region Private Members

        private WaveFormat m_waveFormat;
        private object m_repositionLock = new object();
        private WmaStream m_wmaStream;

        #endregion
    }

}
