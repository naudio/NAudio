using System;
using NAudio.Sequencing;
using NAudio.Wave;

namespace NAudio.Midi
{
    /// <summary>
    /// Renders a <see cref="MidiFileSequence"/> through an
    /// <see cref="IMidiInstrument"/> offline (faster than real time), to an
    /// in-memory buffer or a WAV file. The rendering is sample-accurate (events
    /// are dispatched at their exact frame), deterministic, and needs no audio
    /// hardware.
    /// </summary>
    public static class OfflineMidiRenderer
    {
        private const int BlockFrames = 1024;

        /// <summary>
        /// Renders the sequence to an interleaved float buffer, including a
        /// release tail. The instrument's sample rate drives the render.
        /// </summary>
        public static float[] Render(MidiFileSequence sequence, IMidiInstrument instrument, double tailSeconds = 2.0)
        {
            if (sequence == null) throw new ArgumentNullException(nameof(sequence));
            if (instrument == null) throw new ArgumentNullException(nameof(instrument));

            int sampleRate = instrument.WaveFormat.SampleRate;
            int channels = instrument.WaveFormat.Channels;
            long totalFrames = sequence.DurationFrames(sampleRate, tailSeconds);

            var player = StartPlayer(sequence, instrument);
            var output = new float[totalFrames * channels];

            long done = 0;
            while (done < totalFrames)
            {
                int n = (int)Math.Min(BlockFrames, totalFrames - done);
                player.Read(output.AsSpan((int)(done * channels), n * channels));
                done += n;
            }
            return output;
        }

        /// <summary>
        /// Renders the sequence straight to a WAV file (32-bit float), streaming
        /// in blocks so memory stays flat regardless of length.
        /// </summary>
        public static void RenderToWaveFile(MidiFileSequence sequence, IMidiInstrument instrument,
            string outputPath, double tailSeconds = 2.0)
        {
            if (sequence == null) throw new ArgumentNullException(nameof(sequence));
            if (instrument == null) throw new ArgumentNullException(nameof(instrument));
            if (outputPath == null) throw new ArgumentNullException(nameof(outputPath));

            int sampleRate = instrument.WaveFormat.SampleRate;
            int channels = instrument.WaveFormat.Channels;
            long totalFrames = sequence.DurationFrames(sampleRate, tailSeconds);

            var player = StartPlayer(sequence, instrument);
            var block = new float[BlockFrames * channels];

            using var writer = new WaveFileWriter(outputPath, instrument.WaveFormat);
            long done = 0;
            while (done < totalFrames)
            {
                int n = (int)Math.Min(BlockFrames, totalFrames - done);
                player.Read(block.AsSpan(0, n * channels));
                writer.WriteSamples(block, 0, n * channels);
                done += n;
            }
        }

        private static SequencedMidiPlayer StartPlayer(MidiFileSequence sequence, IMidiInstrument instrument)
        {
            var transport = new Transport(sequence.TempoMap, instrument.WaveFormat.SampleRate);
            var player = new SequencedMidiPlayer(transport, sequence.Timeline, instrument);
            transport.SeekTicks(0);
            transport.Play();
            return player;
        }
    }
}
