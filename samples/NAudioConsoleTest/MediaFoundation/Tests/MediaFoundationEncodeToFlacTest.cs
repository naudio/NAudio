using NAudio.MediaFoundation;
using NAudio.Wave;
using NAudioConsoleTest.Shared.Testing;

namespace NAudioConsoleTest.MediaFoundation.Tests;

sealed class MediaFoundationEncodeToFlacTest : IConsoleTest
{
    public string Id => "MediaFoundation.EncodeToFlac";
    public string Description => "Encode an audio file to FLAC (Media Foundation, lossless)";
    public MenuPath? MenuLocation => new("Media Foundation", "Encode to FLAC", Group: "Encoding", Order: 3);
    public IReadOnlyList<TestParameter> Parameters => MediaFoundationEncodeHelper.LosslessParameters;

    public TestResult Run(TestContext ctx)
        => MediaFoundationEncodeHelper.RunLossless(ctx, "FLAC", ".flac", MediaFoundationEncoder.EncodeToFlac);
}
