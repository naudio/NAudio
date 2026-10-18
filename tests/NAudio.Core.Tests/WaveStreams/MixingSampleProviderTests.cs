using System;
using System.Linq;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NUnit.Framework;

namespace NAudio.Core.Tests.WaveStreams;

[TestFixture]
public class MixingSampleProviderTests
{
    [Test]
    public void WithNoInputsFirstReadReturnsNoSamples()
    {
        var msp = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
        var buffer = new float[1000];
        Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(0));
    }

    [Test]
    public void WithReadFullySetNoInputsReturnsSampleCountRequested()
    {
        var msp = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
        msp.ReadFully = true;
        var buffer = new float[1000];
        Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(buffer.Length));
    }

    [Test]
    public void WithOneInputReadsToTheEnd()
    {
        var input1 = new TestSampleProvider(44100, 2, 2000);
        var msp = new MixingSampleProvider([input1]);
        var buffer = new float[1000];
        Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(buffer.Length));
        // randomly check one value
        Assert.That(buffer[567], Is.EqualTo(567));
    }

    [Test]
    public void WithOneInputReturnsSamplesReadIfNotEnoughToFullyRead()
    {
        var input1 = new TestSampleProvider(44100, 2, 800);
        var msp = new MixingSampleProvider([input1]);
        var buffer = new float[1000];
        Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(800));
        // randomly check one value
        Assert.That(buffer[567], Is.EqualTo(567));
    }

    [Test]
    public void FullyReadCausesPartialBufferToBeZeroedOut()
    {
        var input1 = new TestSampleProvider(44100, 2, 800);
        var msp = new MixingSampleProvider([input1]);
        msp.ReadFully = true;
        // buffer of 1000 floats of value 9999
        var buffer = Enumerable.Range(1, 1000).Select(n => 9999f).ToArray();

        Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(buffer.Length));
        // check we get 800 samples, followed by zeroed out data
        Assert.That(buffer[567], Is.EqualTo(567f));
        Assert.That(buffer[799], Is.EqualTo(799f));
        Assert.That(buffer[800], Is.EqualTo(0));
        Assert.That(buffer[999], Is.EqualTo(0));
    }

    [Test]
    public void AddingSameMixerInputTwiceThrows()
    {
        var input = new TestSampleProvider(44100, 2, 2000);
        var msp = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
        msp.AddMixerInput(input);
        Assert.Throws<ArgumentException>(() => msp.AddMixerInput(input));
        Assert.That(msp.MixerInputs.Count(), Is.EqualTo(1));
    }

    [Test]
    public void MixerInputEndedInvoked()
    {
        var input1 = new TestSampleProvider(44100, 2, 8000);
        var input2 = new TestSampleProvider(44100, 2, 800);
        var msp = new MixingSampleProvider([input1, input2]);
        ISampleProvider endedInput = null;
        msp.MixerInputEnded += (s, a) =>
        {
            Assert.That(endedInput, Is.Null);
            endedInput = a.SampleProvider;
        };
        // buffer of 1000 floats of value 9999
        var buffer = Enumerable.Range(1, 1000).Select(n => 9999f).ToArray();

        Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(buffer.Length));
        Assert.That(endedInput, Is.SameAs(input2));
        Assert.That(msp.MixerInputs.Count(), Is.EqualTo(1));
    }

    // ── CompleteAdding ────────────────────────────────────────────────────

    [TestCase(true)]
    [TestCase(false)]
    public void AfterCompleteAddingMixerEndsWithShortReadThenZero(bool readFully)
    {
        var input1 = new TestSampleProvider(44100, 2, 800);
        var input2 = new TestSampleProvider(44100, 2, 2500);
        var msp = new MixingSampleProvider([input1, input2]) { ReadFully = readFully };
        msp.CompleteAdding();
        Assert.That(msp.IsAddingCompleted, Is.True);

        var buffer = Enumerable.Repeat(9999f, 1000).ToArray();
        Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(1000)); // input1 ends here
        Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(1000));
        buffer.AsSpan().Fill(9999f);
        Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(500));
        Assert.That(buffer[499], Is.EqualTo(2499));
        // the short read is not padded
        Assert.That(buffer.Skip(500), Is.All.EqualTo(9999f));
        Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(0));
        Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(0));
    }

    [Test]
    public void ReadFullyMixerStillPadsBeforeCompleteAdding()
    {
        var msp = new MixingSampleProvider([new TestSampleProvider(44100, 2, 800)]) { ReadFully = true };
        var buffer = new float[1000];
        Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(1000));
        Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(1000));
        msp.CompleteAdding();
        Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(0));
    }

    [Test]
    public void CompleteAddingWithNoInputsReturnsZeroEvenWithReadFully()
    {
        var msp = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2)) { ReadFully = true };
        msp.CompleteAdding();
        Assert.That(msp.Read(new float[1000].AsSpan()), Is.EqualTo(0));
    }

    [Test]
    public void AddMixerInputAfterCompleteAddingThrows()
    {
        var msp = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
        msp.AddMixerInput(new TestSampleProvider(44100, 2, 800));
        msp.CompleteAdding();
        Assert.Throws<InvalidOperationException>(() => msp.AddMixerInput(new TestSampleProvider(44100, 2, 800)));
        Assert.That(msp.MixerInputs.Count(), Is.EqualTo(1));
    }

    [Test]
    public void CompletedBufferedWaveProviderInputEndsCompletedMixer()
    {
        var format = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
        var bwp = new BufferedWaveProvider(format);
        var msp = new MixingSampleProvider(format) { ReadFully = true };
        msp.AddMixerInput(bwp.ToSampleProvider());
        msp.CompleteAdding();

        var buffer = new float[1000];
        // before the producer completes, an empty buffer is padded and the chain keeps going
        Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(1000));
        bwp.AddSamples(new byte[1500 * 4]);
        bwp.CompleteAdding();
        Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(1000));
        Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(500));
        Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(0));
    }

    [Test]
    public void ConcurrentlyAddedInputsAreAllReadBeforeMixerEnds()
    {
        var msp = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2)) { ReadFully = true };
        var inputs = Enumerable.Range(0, 200).Select(n => new TestSampleProvider(44100, 2, 100 + n * 37)).ToArray();

        var adder = Task.Run(() =>
        {
            foreach (var input in inputs)
            {
                msp.AddMixerInput(input);
            }
            msp.CompleteAdding();
        });
        var reader = Task.Run(() =>
        {
            var buffer = new float[256];
            while (msp.Read(buffer.AsSpan()) > 0) { }
        });

        Assert.That(Task.WaitAll([adder, reader], TimeSpan.FromSeconds(30)), Is.True, "mixer did not end");
        Assert.That(inputs.Select(i => i.Position), Is.EqualTo(inputs.Select(i => 100 + Array.IndexOf(inputs, i) * 37)));
        Assert.That(msp.MixerInputs, Is.Empty);
    }
}
