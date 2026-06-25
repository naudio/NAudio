using NAudio.Wave;
using NAudioConsoleTest.Shared.Testing;

namespace NAudioConsoleTest.MediaFoundation.Tests;

internal sealed class MediaFoundationEncodeToAacTest : IConsoleTest
{
    public string Id => "MediaFoundation.EncodeToAac";
    public string Description => "Encode an audio file to AAC (Media Foundation)";
    public MenuPath? MenuLocation => new("Media Foundation", "Encode to AAC", Group: "Encoding", Order: 1);
    public IReadOnlyList<TestParameter> Parameters => MediaFoundationEncodeHelper.LossyParameters;

    public TestResult Run(TestContext ctx)
        => MediaFoundationEncodeHelper.RunLossy(ctx, "AAC", ".mp4", MediaFoundationEncoder.EncodeToAac);
}
