using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NAudio.WindowsMediaFormat;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;

namespace NAudioTests.WindowsMedia
{
    [TestFixture]
    [Category("IntegrationTest")]
    public class WindowsMediaTests
    {
        private string testWmaFile = @"C:\Documents and Settings\All Users\Documents\My Music\Sample Music\New Stories (Highway Blues).wma";

        [Test]
        public void CanCreateSyncReader()
        {
            var reader = WM.CreateSyncReader(WMT_RIGHTS.WMT_RIGHT_NO_DRM);
            Assert.IsNotNull(reader);
            Marshal.ReleaseComObject(reader);            
        }

        [Test]
        public void CanOpenWmaFile()
        {
            IWMSyncReader reader = OpenWmaFile();
            reader.Close();
            Marshal.ReleaseComObject(reader);
        }

        [Test]
        public void CanGetOutputCount()
        {
            IWMSyncReader reader = OpenWmaFile();
            uint outputs;
            reader.GetOutputCount(out outputs);
            Assert.AreEqual(1, outputs, "Output Count");           

            reader.Close();
            Marshal.ReleaseComObject(reader);

        }

        [Test]
        public void CanGetOutputFormat()
        {
            IWMSyncReader reader = OpenWmaFile();
            IWMOutputMediaProps props;
            reader.GetOutputFormat(0,0,out props);
            Assert.IsNotNull(props);

            reader.Close();
            Marshal.ReleaseComObject(reader);
        }

        [Test]
        public void CanGetNextSample()
        {
            IWMSyncReader reader = OpenWmaFile();
            INSSBuffer buffer;
            ulong sampleTime;
            ulong duration;
            uint flags;
            uint outputNum;
            ushort streamNum;
            reader.GetNextSample(0, out buffer, out sampleTime, out duration, out flags, out outputNum, out streamNum);
            //byte[] theBuffer;
            //buffer.GetBuffer(out theBuffer);


            reader.Close();
            Marshal.ReleaseComObject(reader);

            /*
            //char[] nameBuffer = new char[256];
            ushort length = 256;
            string name = new string(' ', 256);
            props.GetConnectionName(name, ref length);
            //string name = new string(nameBuffer, 0, length);
            Assert.IsFalse(String.IsNullOrEmpty(name));*/
        }

        private IWMSyncReader OpenWmaFile()
        {
            if(!File.Exists(testWmaFile))
            {
                Assert.Ignore("Test WMA File Not Found");
            }
            IWMSyncReader reader = WM.CreateSyncReader(WMT_RIGHTS.WMT_RIGHT_NO_DRM);
            Assert.IsNotNull(reader);
            reader.Open(testWmaFile);
            return reader;
        }

        [Test]
        public void CanQueryAllCodecs()
        {
            foreach (var codec in Codec.GetCodecs(MediaTypes.WMMEDIATYPE_Audio))
            {
                Debug.WriteLine(codec.Name);
                foreach (var format in codec.CodecFormats)
                {
                    Debug.WriteLine(format.Description);
                    Debug.WriteLine(String.Format("---Bitrate: {0}", format.StreamConfig.Bitrate));
                    var waveFormat = format.StreamConfig.GetWaveFormat();
                    Debug.WriteLine(String.Format("---WaveFormat: {0}Hz {1} channels", waveFormat.SampleRate, waveFormat.Channels));
                }
            }
        }
    }
}
