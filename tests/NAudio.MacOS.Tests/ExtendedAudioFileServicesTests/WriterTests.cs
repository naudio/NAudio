
using System;
using System.IO;

using NAudio.Wave;
using NAudio.Wave.SampleProviders;

using NAudio.MacOS.AudioToolbox;

using NUnit.Framework;

namespace NAudio.MacOS.Tests.ExtendedAudioFileServicesTests;

[TestFixture]
public class WriterTests
{
    private static string CreateRandomFileName() => Path.Join(Environment.CurrentDirectory, Path.GetRandomFileName());

    [OneTimeSetUp]
    public void VerifyMacOS() => MacOSVerify.VerifyIsOSMacOSFloorAtLeast(10, 3, 1);

    [Test]
    public void WriteAAC() => UseURLWriter(new() { FileType = "audio/aac" });

    [Test]
    public void WriteMPEG4() => UseURLWriter(new() { FileType = "audio/mp4" });

    [Test]
    public void WriteMPEG4ByStreamCallbacks() => UseWriterWithStream(new() { FileType = "audio/mp4" });

    [Test]
    public void WriteAACByStreamCallbacks() => UseWriterWithStream(new() { FileType = "audio/aac" });

    private static void UseURLWriter(ExtendedAudioFileWriterSettings settings)
    {
        var sg = new SignalGenerator(48000, 2).Take(TimeSpan.FromSeconds(5)).ToWaveProvider();
        settings ??= new();
        settings.ProvidingFormat = sg.WaveFormat;
        var path = CreateRandomFileName();
        var wr = ExtendedAudioFileWriter.CreateFromFilePath(path, settings, true);
        WriteAndDisposeWriter(wr, sg);
        TryDeleteTheCreatedFile(path);
    }

    private static void UseWriterWithStream(ExtendedAudioFileWriterSettings settings)
    {
        var sg = new SignalGenerator(48000, 2).Take(TimeSpan.FromSeconds(5)).ToWaveProvider();
        settings ??= new();
        settings.ProvidingFormat = sg.WaveFormat;

        var path = CreateRandomFileName();

        var fs = new FileStream(
            path,
            FileMode.Create,
            FileAccess.ReadWrite
        );

        try
        {
            var wr = ExtendedAudioFileWriter.CreateFromStream(fs, settings);
            WriteAndDisposeWriter(wr, sg);
        }
        finally
        {
            fs.Dispose();
            TryDeleteTheCreatedFile(path);
        }
    }

    private static void TryDeleteTheCreatedFile(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (IOException)
        {
            // best-effort temp cleanup
        }
    }

    private static void WriteAndDisposeWriter(ExtendedAudioFileWriter writer, IWaveProvider provider)
    {
        byte[] buffer = new byte[provider.WaveFormat.ConvertLatencyToByteSize(300)];

        long writtenBytes = 0L;
        int providerRead;

        while ((providerRead = provider.Read(buffer)) > 0)
        {
            writer.Write(buffer.AsSpan(0, providerRead));
            writtenBytes += providerRead;
        }

        System.Console.WriteLine("Written bytes: {0}", writtenBytes);

        // Make a buffer less than BlockAlign to verify that it throws when the buffer is not at least a full sample frame.
        buffer = new byte[provider.WaveFormat.BlockAlign - 1];

        Assert.Throws<ArgumentException>(() => writer.Write(buffer), "Buffers that have a size less than BlockAlign should throw in the write call.");

        Assert.DoesNotThrow(writer.Dispose);

        // We can't write after the writer object is disposed of
        Assert.IsFalse(writer.CanWrite);

        // This should throw once the writer object is disposed of
        Assert.Throws<ObjectDisposedException>(() => _ = writer.Position);
    }
}