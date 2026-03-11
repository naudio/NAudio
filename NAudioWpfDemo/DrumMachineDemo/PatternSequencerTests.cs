using System.Linq;
using NUnit.Framework;
using NAudio.Wave;

namespace NAudioWpfDemo.DrumMachineDemo
{
    [TestFixture]
    public class PatternSequencerTests
    {
        class TestKit : DrumKit
        {
            private readonly WaveFormat wf;
            public TestKit()
            {
            }

            public TestKit(int sampleRate)
            {
                wf = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
            }

            public override WaveFormat WaveFormat => wf ?? base.WaveFormat;
        }

        [Test]
        public void Pattern_Sequencer_Should_Return_No_Mixer_Inputs_For_An_Empty_Pattern()
        {
            var pattern = new DrumPattern(new[] { "Bass Drum" }, 16);
            var sequencer = new PatternSequencer(pattern, new TestKit());
            var mixerInputs = sequencer.GetNextMixerInputs(100);
            Assert.That(mixerInputs.Count(), Is.EqualTo(0));
        }

        [Test]
        public void Pattern_Sequencer_Should_Return_A_Non_Delayed_Mixer_Input_For_A_Beat_At_Position_Zero()
        {
            var pattern = new DrumPattern(new[] { "Bass Drum" }, 16);
            pattern[0, 0] = 127;
            var sequencer = new PatternSequencer(pattern, new TestKit());
            var mixerInputs = sequencer.GetNextMixerInputs(100);
            
            Assert.That(mixerInputs.Count(), Is.EqualTo(1));
            Assert.That(mixerInputs.First().DelayBy, Is.EqualTo(0));
        }

        [Test]
        public void Pattern_Sequencer_Should_Set_DelayBy_On_Mixer_Inputs_That_Are_Not_At_The_Start()
        {
            var pattern = new DrumPattern(new[] { "Bass Drum" }, 16);
            pattern[0, 1] = 127;
            
            var sequencer = new PatternSequencer(pattern, new TestKit(CalculateSampleRateForTempo(120)));
                       
            var mixerInputs = sequencer.GetNextMixerInputs(2);

            Assert.That(mixerInputs.Count(), Is.EqualTo(1));
            Assert.That(mixerInputs.First().DelayBy, Is.EqualTo(1));
        }

        private int CalculateSampleRateForTempo(int tempo, int samplesPerStep = 1)
        {
            int stepsPerBeat = 4;
            int stepsPerMinute = tempo * stepsPerBeat;
            int stepsPerSecond = stepsPerMinute / 60;
            return stepsPerSecond * samplesPerStep; // an imaginary low sample rate where there is one sample per beat
        }

        [Test]
        public void Pattern_Sequencer_Should_Not_Return_Mixer_Inputs_For_Steps_That_Are_Outside_The_Requested_Range()
        {
            var pattern = new DrumPattern(new[] { "Bass Drum" }, 16);
            pattern[0, 2] = 127;
            var sequencer = new PatternSequencer(pattern, new TestKit(CalculateSampleRateForTempo(120)));

            var mixerInputs = sequencer.GetNextMixerInputs(2);

            Assert.That(mixerInputs.Count(), Is.EqualTo(0));
        }

        [Test]
        public void Pattern_Sequencer_Should_Loop_Around_After_Reaching_The_End_Of_The_Pattern()
        {
            var pattern = new DrumPattern(new[] { "Bass Drum" }, 16);
            pattern[0, 2] = 127;
            var sequencer = new PatternSequencer(pattern, new TestKit(CalculateSampleRateForTempo(120)));

            var mixerInputs = sequencer.GetNextMixerInputs(32); // twice through

            Assert.That(mixerInputs.Count(), Is.EqualTo(2));
        }

        [Test]
        public void Pattern_Sequencer_Should_Carry_On_From_Where_It_Left_Off_On_Second_Call()
        {
            var pattern = new DrumPattern(new[] { "Bass Drum" }, 16);
            pattern[0, 1] = 127;
            var sequencer = new PatternSequencer(pattern, new TestKit(CalculateSampleRateForTempo(120)));

            // first read gets nothing
            var mixerInputs = sequencer.GetNextMixerInputs(1);
            Assert.That(mixerInputs.Count(), Is.EqualTo(0), "First read");
            
            // second read gets something
            mixerInputs = sequencer.GetNextMixerInputs(1);
            Assert.That(mixerInputs.Count(), Is.EqualTo(1), "Second Read");
        }

        [Test]
        public void DelayBy_Values_Are_Relative_To_Current_Position_On_Subsequent_Calls()
        {
            var pattern = new DrumPattern(new[] { "Bass Drum" }, 16);
            pattern[0, 6] = 127;
            var sequencer = new PatternSequencer(pattern, new TestKit(CalculateSampleRateForTempo(120)));

            // first read gets nothing
            var mixerInputs = sequencer.GetNextMixerInputs(3);
            Assert.That(mixerInputs.Count(), Is.EqualTo(0), "First read");

            // second read gets something
            mixerInputs = sequencer.GetNextMixerInputs(4);
            Assert.That(mixerInputs.Count(), Is.EqualTo(1), "Second Read");
            Assert.That(mixerInputs.First().DelayBy, Is.EqualTo(3), "DelayBy");
        }
        
        [Test]
        public void Multiple_DelayBy_Values_Are_All_Relative_To_Current_Position_Before_Calling_GetNextMixerInputs()
        {
            var pattern = new DrumPattern(new[] { "Bass Drum" }, 16);
            pattern[0, 6] = 127;
            pattern[0, 7] = 127;
            pattern[0, 8] = 127;
            var sequencer = new PatternSequencer(pattern, new TestKit(CalculateSampleRateForTempo(120)));

            // first read gets nothing
            var mixerInputs = sequencer.GetNextMixerInputs(3);
            Assert.That(mixerInputs.Count, Is.EqualTo(0), "First read");

            // second read gets something
            mixerInputs = sequencer.GetNextMixerInputs(10);
            Assert.That(mixerInputs.Count, Is.EqualTo(3), "Second Read");
            Assert.That(mixerInputs[0].DelayBy, Is.EqualTo(3), "Inputs[0].DelayBy");
            Assert.That(mixerInputs[1].DelayBy, Is.EqualTo(4), "Inputs[1].DelayBy");
            Assert.That(mixerInputs[2].DelayBy, Is.EqualTo(5), "Inputs[2].DelayBy");
        }

        [Test]
        public void DelayBy_Values_Should_Be_Correct_On_Wraparound()
        {
            var pattern = new DrumPattern(new[] { "Bass Drum" }, 16);
            pattern[0, 0] = 127;
            var sequencer = new PatternSequencer(pattern, new TestKit(CalculateSampleRateForTempo(120)));

            // read 12 of the 16 steps
            var mixerInputs = sequencer.GetNextMixerInputs(12);
            Assert.That(mixerInputs.Count, Is.EqualTo(1), "First read");

            // read 12 more - will wrap around
            mixerInputs = sequencer.GetNextMixerInputs(12);
            Assert.That(mixerInputs.Count, Is.EqualTo(1), "Second Read");
            Assert.That(mixerInputs[0].DelayBy, Is.EqualTo(4), "Inputs[0].DelayBy");
        }

        [Test]
        public void DelayBy_Values_Should_Be_Correct_On_Subsequent_Read_After_Wraparound()
        {
            var pattern = new DrumPattern(new[] { "Bass Drum" }, 16);
            pattern[0, 0] = 127;
            pattern[0, 10] = 127;
            var sequencer = new PatternSequencer(pattern, new TestKit(CalculateSampleRateForTempo(120)));

            // read 12 of the 16 steps (ends at pos 12)
            var mixerInputs = sequencer.GetNextMixerInputs(12);
            Assert.That(mixerInputs.Count, Is.EqualTo(2), "First read");

            // read 12 more - will wrap around (ends at pos 8)
            mixerInputs = sequencer.GetNextMixerInputs(12);
            Assert.That(mixerInputs.Count, Is.EqualTo(1), "Second Read");
            Assert.That(mixerInputs[0].DelayBy, Is.EqualTo(4), "Inputs[0].DelayBy");

            // read 12 more - (start from pos 8, ends at pos 4)
            mixerInputs = sequencer.GetNextMixerInputs(12);
            Assert.That(mixerInputs[0].DelayBy, Is.EqualTo(2), "3rd Read Inputs[0].DelayBy");
            Assert.That(mixerInputs[1].DelayBy, Is.EqualTo(8), "3rd Read Inputs[1].DelayBy");
        }

        [Test]
        public void Tempo_Can_Be_Changed()
        {
            var pattern = new DrumPattern(new[] { "Bass Drum" }, 16);
            for (int n = 0; n < pattern.Steps; n++)
                pattern[0, n] = 127;

            var sequencer = new PatternSequencer(pattern, new TestKit(CalculateSampleRateForTempo(120)));
            sequencer.Tempo = 60; // half tempo

            var mixerInputs = sequencer.GetNextMixerInputs(16);
            // tempo is half, so only half the beats should get read
            Assert.That(mixerInputs.Count, Is.EqualTo(8), "First read");
        }

        [Test]
        public void When_Tempo_Is_Halved_DelayBy_Is_Doubled()
        {
            var pattern = new DrumPattern(new[] { "Bass Drum" }, 16);
            for (int n = 0; n < pattern.Steps; n++)
                pattern[0, n] = 127;

            var sequencer = new PatternSequencer(pattern, new TestKit(CalculateSampleRateForTempo(120)));
            sequencer.Tempo = 60; // half tempo

            var mixerInputs = sequencer.GetNextMixerInputs(16);
            // tempo is half, so only half the beats should get read
            Assert.That(mixerInputs[1].DelayBy, Is.EqualTo(2), "First beat DelayBy");
        }

        [Test]
        public void Pattern_Sequencer_Should_Return_Mixer_Inputs_for_Beats_On_Any_Note()
        {
            var pattern = new DrumPattern(new[] { "Bass Drum", "Snare Drum" }, 16);
            pattern[1, 5] = 127;
            var sequencer = new PatternSequencer(pattern, new TestKit(CalculateSampleRateForTempo(120)));
            var mixerInputs = sequencer.GetNextMixerInputs(16);
            // tempo is half, so only half the beats should get read
            Assert.That(mixerInputs.Count, Is.EqualTo(1));
        }
    }
}
