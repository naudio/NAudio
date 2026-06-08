using System;
using NAudio.Sequencing;
using NAudio.Wave;

namespace NAudio.Sampler
{
    /// <summary>
    /// Renders a <see cref="MidiFileSequence"/> through a <see cref="SamplerEngine"/>
    /// offline (faster than real time), to an in-memory buffer or a WAV file. The
    /// rendering is sample-accurate (events are dispatched at their exact frame),
    /// deterministic, and needs no audio hardware.
    /// </summary>
    public static class OfflineMidiRenderer
    {
        private const int BlockFrames = 1024;

        /// <summary>
        /// Renders the sequence to an interleaved stereo float buffer, including a
        /// release tail. The sampler's sample rate drives the render.
        /// </summary>
        public static float[] Render(MidiFileSequence sequence, SamplerEngine sampler, double tailSeconds = 2.0)
        {
            if (sequence == null) throw new ArgumentNullException(nameof(sequence));
            if (sampler == null) throw new ArgumentNullException(nameof(sampler));

            int sampleRate = sampler.WaveFormat.SampleRate;
            int channels = sampler.WaveFormat.Channels;
            long totalFrames = sequence.DurationFrames(sampleRate, tailSeconds);

            var instrument = StartInstrument(sequence, sampler);
            var output = new float[totalFrames * channels];

            long done = 0;
            while (done < totalFrames)
            {
                int n = (int)Math.Min(BlockFrames, totalFrames - done);
                instrument.Read(output.AsSpan((int)(done * channels), n * channels));
                done += n;
            }
            return output;
        }

        /// <summary>
        /// Renders the sequence straight to a WAV file (32-bit float stereo),
        /// streaming in blocks so memory stays flat regardless of length.
        /// </summary>
        public static void RenderToWaveFile(MidiFileSequence sequence, SamplerEngine sampler,
            string outputPath, double tailSeconds = 2.0)
        {
            if (sequence == null) throw new ArgumentNullException(nameof(sequence));
            if (sampler == null) throw new ArgumentNullException(nameof(sampler));
            if (outputPath == null) throw new ArgumentNullException(nameof(outputPath));

            int sampleRate = sampler.WaveFormat.SampleRate;
            int channels = sampler.WaveFormat.Channels;
            long totalFrames = sequence.DurationFrames(sampleRate, tailSeconds);

            var instrument = StartInstrument(sequence, sampler);
            var block = new float[BlockFrames * channels];

            using var writer = new WaveFileWriter(outputPath, sampler.WaveFormat);
            long done = 0;
            while (done < totalFrames)
            {
                int n = (int)Math.Min(BlockFrames, totalFrames - done);
                instrument.Read(block.AsSpan(0, n * channels));
                writer.WriteSamples(block, 0, n * channels);
                done += n;
            }
        }

        private static SequencedMidiInstrument StartInstrument(MidiFileSequence sequence, SamplerEngine sampler)
        {
            var transport = new Transport(sequence.TempoMap, sampler.WaveFormat.SampleRate);
            var instrument = new SequencedMidiInstrument(transport, sequence.Timeline, sampler);
            transport.SeekTicks(0);
            transport.Play();
            return instrument;
        }
    }
}
