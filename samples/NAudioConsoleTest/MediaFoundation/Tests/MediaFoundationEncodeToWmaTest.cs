using NAudio.Wave;
using NAudioConsoleTest.Shared.Testing;

namespace NAudioConsoleTest.MediaFoundation.Tests;

internal sealed class MediaFoundationEncodeToWmaTest : IConsoleTest
{
    public string Id => "MediaFoundation.EncodeToWma";
    public string Description => "Encode an audio file to WMA (Media Foundation)";
    public MenuPath? MenuLocation => new("Media Foundation", "Encode to WMA", Group: "Encoding", Order: 2);
    public IReadOnlyList<TestParameter> Parameters => MediaFoundationEncodeHelper.LossyParameters;

    public TestResult Run(TestContext ctx)
        => MediaFoundationEncodeHelper.RunLossy(ctx, "WMA", ".wma", MediaFoundationEncoder.EncodeToWma);
}
