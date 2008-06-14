using System;
using System.Collections.Generic;
using System.Text;
using NAudio.Wave;

namespace AudioFileInspector
{
    class WaveFileInspector : IAudioFileInspector
    {
        #region IAudioFileInspector Members

        public string FileExtension
        {
            get { return ".wav"; }
        }

        public string FileTypeDescription
        {
            get { return "Wave File"; }
        }

        public string Describe(string fileName)
        {
            StringBuilder stringBuilder = new StringBuilder();
            using (WaveFileReader wf = new WaveFileReader(fileName))
            {

                stringBuilder.AppendFormat("{0} {1}Hz {2} channels {3} bits per sample\r\n", 
                    wf.WaveFormat.Encoding, wf.WaveFormat.SampleRate,
                    wf.WaveFormat.Channels, wf.WaveFormat.BitsPerSample);
                stringBuilder.AppendFormat("Extra Size: {0} Block Align: {1} Average Bytes Per Second: {2}\r\n",
                    wf.WaveFormat.ExtraSize, wf.WaveFormat.BlockAlign,
                    wf.WaveFormat.AverageBytesPerSecond);
                stringBuilder.AppendFormat("WaveFormat: {0}\r\n",wf.WaveFormat);
                
                stringBuilder.AppendFormat("Length: {0} bytes: {1} \r\n", wf.Length, wf.TotalTime);
                foreach (RiffChunk chunk in wf.ExtraChunks)
                {
                    stringBuilder.AppendFormat("Chunk: {0}, length {1}\r\n", chunk.IdentifierAsString, chunk.Length);
                    byte[] data = wf.GetChunkData(chunk);
                    int n = 0;
                    foreach (byte b in data)
                    {
                        stringBuilder.AppendFormat("{0:X2} ", b);
                        if (++n % 8 == 0)
                            stringBuilder.Append("\r\n");
                    }
                    stringBuilder.Append("\r\n");
                }
            }
            return stringBuilder.ToString();
        }

        #endregion
    }
}
