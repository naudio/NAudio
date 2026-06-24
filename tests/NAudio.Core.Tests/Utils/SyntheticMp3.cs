using System;

namespace NAudio.Core.Tests.Utils
{
    /// <summary>
    /// Generates valid MPEG-1 Layer III frame bytes in memory: 96 kbps, 44.1 kHz, stereo,
    /// no padding, no CRC. Frame headers parse cleanly via <c>NAudio.Wave.Mp3Frame</c>;
    /// the audio payload is zero-filled. Used by tests that exercise
    /// <c>Mp3FileReaderBase</c> reposition / TOC / length logic without invoking a real
    /// codec or MediaFoundation encoder. See acm-av-investigation.md for context.
    /// </summary>
    internal static class SyntheticMp3
    {
        // MPEG-1 Layer III, 96 kbps, 44.1 kHz, stereo:
        //   sync 0xFFF, version 11, layer 01, protection bit 1 (no CRC) → bytes 0..1 = 0xFF, 0xFB
        //   bitrate index 0111 (= 96 kbps), samplerate 00 (= 44.1 kHz), padding 0, private 0 → byte 2 = 0x70
        //   channel mode 00 (= stereo), mode-ext 00, copyright 0, original 0, emphasis 00 → byte 3 = 0x00
        // Frame size = floor(144 * bitrate / samplerate) = floor(144 * 96000 / 44100) = 313 bytes (no padding).
        // 1152 samples / 44100 Hz ≈ 26.12 ms per frame.
        private const int FrameSize = 313;
        private const int SamplesPerFrame = 1152;
        public const int SampleRate = 44100;
        public const int Channels = 2;
        public const int BitRate = 96000;

        public static int FramesForSeconds(double seconds) =>
            (int)Math.Round(seconds * SampleRate / SamplesPerFrame);

        /// <summary>Bytes of a synthetic CBR MP3 with <paramref name="seconds"/> of duration.</summary>
        public static byte[] CreateBytes(double seconds) =>
            CreateFrames(FramesForSeconds(seconds));

        public static byte[] CreateFrames(int frameCount)
        {
            byte[] bytes = new byte[frameCount * FrameSize];
            for (int i = 0; i < frameCount; i++)
            {
                int o = i * FrameSize;
                bytes[o + 0] = 0xFF;
                bytes[o + 1] = 0xFB;
                bytes[o + 2] = 0x70;
                bytes[o + 3] = 0x00;
                // bytes 4..312 are zero (side-info + audio data)
            }
            return bytes;
        }

        /// <summary>
        /// Bytes of a synthetic MP3 with an injected Xing/Info tag in the first frame's
        /// payload. The reader sees the tag, treats it as a non-audio frame, and reads
        /// <c>frames</c> as the exact frame count for length calculation.
        /// </summary>
        public static byte[] CreateBytesWithInfoHeader(double audioSeconds)
        {
            int audioFrames = FramesForSeconds(audioSeconds);
            // The Info-tag frame is itself a valid (silent) frame, prepended to the audio.
            byte[] bytes = CreateFrames(audioFrames + 1);
            // Xing/Info offset within frame for MPEG-1 stereo: 32 (side-info) + 4 (header) = 36.
            int xingOffset = 36;
            // "Info" signature
            bytes[xingOffset + 0] = (byte)'I';
            bytes[xingOffset + 1] = (byte)'n';
            bytes[xingOffset + 2] = (byte)'f';
            bytes[xingOffset + 3] = (byte)'o';
            // Flags: 0x00000001 = Frames present
            bytes[xingOffset + 4] = 0x00;
            bytes[xingOffset + 5] = 0x00;
            bytes[xingOffset + 6] = 0x00;
            bytes[xingOffset + 7] = 0x01;
            // Audio frame count, big-endian (the audio frames following the Info frame)
            bytes[xingOffset +  8] = (byte)((audioFrames >> 24) & 0xFF);
            bytes[xingOffset +  9] = (byte)((audioFrames >> 16) & 0xFF);
            bytes[xingOffset + 10] = (byte)((audioFrames >>  8) & 0xFF);
            bytes[xingOffset + 11] = (byte)( audioFrames        & 0xFF);
            return bytes;
        }
    }
}
