using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using NAudio.Utils;
using NAudio.Wave;

namespace AudioFileInspector
{
    [Export(typeof(IAudioFileInspector))]
    public class Mp3FileInspector : IAudioFileInspector
    {
        public string FileExtension
        {
            get { return ".mp3"; }
        }

        public string FileTypeDescription
        {
            get { return "MP3 File"; }
        }

        public string Describe(string fileName)
        {
            var stringBuilder = new StringBuilder();
            using (var reader = new Mp3FileReader(fileName))
            {
                Mp3WaveFormat wf = reader.Mp3WaveFormat;
                stringBuilder.AppendFormat("MP3 File WaveFormat: {0} {1}Hz {2} channels {3} bits per sample\r\n",
                    wf.Encoding, wf.SampleRate,
                    wf.Channels, wf.BitsPerSample);
                stringBuilder.AppendFormat("Extra Size: {0} Block Align: {1} Average Bytes Per Second: {2}\r\n",
                    wf.ExtraSize, wf.BlockAlign,
                    wf.AverageBytesPerSecond);
                stringBuilder.AppendFormat("ID: {0} Flags: {1} Block Size: {2} Frames per Block: {3}\r\n",
                    wf.id, wf.flags, wf.blockSize, wf.framesPerBlock
                    );

                stringBuilder.AppendFormat("Length: {0} bytes: {1} \r\n", reader.Length, reader.TotalTime);
                stringBuilder.AppendFormat("ID3v1 Tag: {0}\r\n", reader.Id3v1Tag == null ? "None" : reader.Id3v1Tag.ToString());
                stringBuilder.AppendFormat("ID3v2 Tag: {0}\r\n", reader.Id3v2Tag == null ? "None" : reader.Id3v2Tag.ToString());
                Mp3Frame frame;
                while ((frame = reader.ReadNextFrame()) != null)
                {
                    stringBuilder.AppendFormat("{0},{1},{2}Hz,{3},{4}bps, length {5}\r\n",
                        frame.MpegVersion, frame.MpegLayer,
                        frame.SampleRate, frame.ChannelMode,
                        frame.BitRate, frame.FrameLength);
                }
            }
            return stringBuilder.ToString();
        }
    }
}
