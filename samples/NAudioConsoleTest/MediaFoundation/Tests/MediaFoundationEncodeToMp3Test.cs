using NAudio.Wave;
using NAudioConsoleTest.Shared.Testing;

namespace NAudioConsoleTest.MediaFoundation.Tests;

internal sealed class MediaFoundationEncodeToMp3Test : IConsoleTest
{
    public string Id => "MediaFoundation.EncodeToMp3";
    public string Description => "Encode an audio file to MP3 (Media Foundation)";
    public MenuPath? MenuLocation => new("Media Foundation", "Encode to MP3", Group: "Encoding", Order: 0);
    public IReadOnlyList<TestParameter> Parameters => MediaFoundationEncodeHelper.LossyParameters;

    public TestResult Run(TestContext ctx)
        => MediaFoundationEncodeHelper.RunLossy(ctx, "MP3", ".mp3", MediaFoundationEncoder.EncodeToMp3);
}
