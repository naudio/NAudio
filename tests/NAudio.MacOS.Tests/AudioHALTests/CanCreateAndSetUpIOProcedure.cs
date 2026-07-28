
using System;

using NAudio.MacOS.CoreAudio;

using NUnit.Framework;

namespace NAudio.MacOS.Tests.AudioHALTests;

[TestFixture]
[Category("IntegrationTest")]
public class CanCreateAndSetUpIOProcedure
{
    private sealed class FakeIOProcedure : CoreAudioIOProcedure
    {
        public FakeIOProcedure(AudioDevice dev) : base(dev) { }

        protected override void IOProcedure(nint inNow, nint inInputData, nint inInputTime, nint outOutputData, nint inOutputTime)
        {
            // Does nothing.
        }
    }

    [Test]
    public void CanCreateInputIOProcedure()
    {
        AudioDevice dev = AudioSystemObject.Instance.DefaultInputDevice;
        FakeIOProcedure p = new(dev);
        AudioStream[] streams = dev.GetStreams(AudioObjectPropertyScopeConstants.Input);
        if (streams.Length == 0)
        {
            throw new InvalidOperationException("No streams were found in the default input device");
        }
        // Enable only the first input stream.
        bool[] enabledStreams = new bool[streams.Length];
        for (int I = 0; I < enabledStreams.Length; I++)
        {
            enabledStreams[I] = I == 0;
        }
        dev.SetStreamUsage(p, AudioObjectPropertyScopeConstants.Input, enabledStreams);
        p.Start();
        p.Stop();
        p.Dispose();
    }

    [Test]
    public void CanCreateOutputIOProcedure()
    {
        AudioDevice dev = AudioSystemObject.Instance.DefaultOutputDevice;
        FakeIOProcedure p = new(dev);
        AudioStream[] streams = dev.GetStreams(AudioObjectPropertyScopeConstants.Output);
        if (streams.Length == 0)
        {
            throw new InvalidOperationException("No streams were found in the default output device");
        }
        // Enable only the first output stream.
        bool[] enabledStreams = new bool[streams.Length];
        for (int I = 0; I < enabledStreams.Length; I++)
        {
            enabledStreams[I] = I == 0;
        }
        dev.SetStreamUsage(p, AudioObjectPropertyScopeConstants.Output, enabledStreams);
        p.Start();
        p.Stop();
        p.Dispose();
    }
}