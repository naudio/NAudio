using System;
using System.IO;
using NAudio.MediaFoundation;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace NAudioTests.Utils
{
    static class TestFileBuilder
    {
        public static string CreateMp3File(int durationSeconds, int sampleRate=44100, int channels=2)
        {
            return CreateMp3File(durationSeconds, sampleRate, channels, fileName: "TestSignal.mp3");
        }

        public static string CreateMp3File(int durationSeconds, int sampleRate, int channels, string fileName)
        {
            var testSignal = new SignalGenerator(sampleRate, channels) { Frequency = 1000, Gain = 0.25 }
                .Take(TimeSpan.FromSeconds(durationSeconds));
            var path = Path.Combine(Path.GetTempPath(), "NAudioTests");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            var file = Path.Combine(path, fileName);
            MediaFoundationApi.Startup();
            MediaFoundationEncoder.EncodeToMp3(testSignal.ToWaveProvider(), file, 96000);
            return file;
        }

        /// <summary>
        /// Generates an MP3 file (CBR via the MediaFoundation encoder, which does NOT write a
        /// Xing/Info tag) and then injects a synthetic Info tag into the first frame, with the
        /// <c>Frames</c> flag set to the true frame count. Used to exercise the "Xing present →
        /// exact length immediately" code path in Mp3FileReaderBase.
        /// </summary>
        public static string CreateMp3FileWithInfoHeader(int durationSeconds, int sampleRate = 44100, int channels = 2, string fileName = "TestSignalWithInfo.mp3")
        {
            var file = CreateMp3File(durationSeconds, sampleRate, channels, fileName);

            // Discover the first frame's offset, frame count, and version/channel mode by
            // walking the file with the existing parser. Then write an "Info" signature +
            // flags=0x01 (Frames present) + 4-byte big-endian frame count into the first
            // frame's payload at the version-and-channel-dependent offset.
            long firstFrameOffset;
            int xingOffsetInFrame;
            int audioFrameCount = 0;

            using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                Id3v2Tag.ReadTag(fs);
                var first = Mp3Frame.LoadFromStream(fs);
                if (first == null) throw new InvalidOperationException($"No MP3 frame in {file}");
                firstFrameOffset = first.FileOffset;
                xingOffsetInFrame = ComputeXingOffsetInFrame(first);

                // Count *audio* frames (subsequent to what will become the Info frame).
                // Mp3FileReaderBase, on detecting the Xing/Info tag, sets dataStartPosition past
                // it — so totalSamples = xingHeader.Frames * sampleCount must reflect only the
                // remaining audio frames, not the Info frame itself.
                while (true)
                {
                    Mp3Frame next;
                    try { next = Mp3Frame.LoadFromStream(fs, readData: false); }
                    catch (EndOfStreamException) { break; }
                    if (next == null) break;
                    audioFrameCount++;
                }
            }

            using (var fs = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                fs.Position = firstFrameOffset + xingOffsetInFrame;
                // "Info" signature
                fs.Write(new byte[] { (byte)'I', (byte)'n', (byte)'f', (byte)'o' }, 0, 4);
                // Flags: 0x00000001 = Frames present
                fs.Write(new byte[] { 0x00, 0x00, 0x00, 0x01 }, 0, 4);
                // Audio frame count, big-endian
                fs.Write(new byte[]
                {
                    (byte)((audioFrameCount >> 24) & 0xFF),
                    (byte)((audioFrameCount >> 16) & 0xFF),
                    (byte)((audioFrameCount >> 8) & 0xFF),
                    (byte)(audioFrameCount & 0xFF),
                }, 0, 4);
            }
            return file;
        }

        private static int ComputeXingOffsetInFrame(Mp3Frame frame)
        {
            // From XingHeader.LoadXingHeader: offset within frame.RawData (which includes the
            // 4-byte audio header at index 0..3). MPEG version × channel-mode lookup.
            if (frame.MpegVersion == MpegVersion.Version1)
            {
                return frame.ChannelMode != ChannelMode.Mono ? (32 + 4) : (17 + 4);
            }
            if (frame.MpegVersion == MpegVersion.Version2)
            {
                return frame.ChannelMode != ChannelMode.Mono ? (17 + 4) : (9 + 4);
            }
            throw new InvalidOperationException($"Unsupported MPEG version: {frame.MpegVersion}");
        }
    }
}
