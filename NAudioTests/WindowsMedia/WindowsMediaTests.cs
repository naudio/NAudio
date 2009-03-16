using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NAudio.WindowsMediaFormat;
using System.Runtime.InteropServices;

namespace NAudioTests.WindowsMedia
{
    [TestFixture]
    public class WindowsMediaTests
    {
        [Test]
        public void CanCreateSyncReader()
        {
            IWMSyncReader reader;
            uint hresult = Functions.WMCreateSyncReader(IntPtr.Zero, WMT_RIGHTS.None, out reader);
            Assert.AreEqual(0, hresult);
            Assert.IsNotNull(reader);
            Marshal.ReleaseComObject(reader);            
        }

        [Test]
        public void CanOpenWmaFile()
        {
            IWMSyncReader reader = OpenWmaFile();
            uint hresult = reader.Close();
            Assert.AreEqual(0, hresult);
            Marshal.ReleaseComObject(reader);
        }

        [Test]
        public void CanGetOutputCount()
        {
            IWMSyncReader reader = OpenWmaFile();
            uint outputs;
            uint hresult = reader.GetOutputCount(out outputs);
            Assert.AreEqual(0, hresult);
            Assert.AreEqual(1, outputs, "Output Count");           

            hresult = reader.Close();
            Assert.AreEqual(0, hresult);
            Marshal.ReleaseComObject(reader);

        }

        [Test]
        public void CanGetOutputFormat()
        {
            IWMSyncReader reader = OpenWmaFile();
            IWMOutputMediaProps props;
            uint hresult = reader.GetOutputFormat(0,0,out props);
            Assert.AreEqual(0, hresult);
            Assert.IsNotNull(props);

            hresult = reader.Close();
            Assert.AreEqual(0, hresult);
            Marshal.ReleaseComObject(reader);
        }

        [Test]
        public void CanGetNextSample()
        {
            IWMSyncReader reader = OpenWmaFile();
            INSSBuffer buffer;
            long sampleTime;
            long duration;
            uint flags;
            uint outputNum;
            ushort streamNum;
            uint hresult = reader.GetNextSample(0, out buffer, out sampleTime, out duration, out flags, out outputNum, out streamNum);
            Assert.AreEqual(0, hresult);
            //byte[] theBuffer;
            //buffer.GetBuffer(out theBuffer);


            hresult = reader.Close();
            Assert.AreEqual(0, hresult);
            Marshal.ReleaseComObject(reader);

            /*
            //char[] nameBuffer = new char[256];
            ushort length = 256;
            string name = new string(' ', 256);
            props.GetConnectionName(name, ref length);
            //string name = new string(nameBuffer, 0, length);
            Assert.IsFalse(String.IsNullOrEmpty(name));*/
        }

        private static IWMSyncReader OpenWmaFile()
        {
            IWMSyncReader reader;
            uint hresult = Functions.WMCreateSyncReader(IntPtr.Zero, WMT_RIGHTS.None, out reader);
            Assert.AreEqual(0, hresult);
            Assert.IsNotNull(reader);
            hresult = reader.Open(@"C:\Documents and Settings\All Users\Documents\My Music\Sample Music\New Stories (Highway Blues).wma");
            Assert.AreEqual(0, hresult);
            return reader;
        }
    }
}
