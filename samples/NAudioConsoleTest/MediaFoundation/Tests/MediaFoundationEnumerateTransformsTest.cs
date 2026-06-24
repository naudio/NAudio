using NAudio.MediaFoundation;
using NAudio.Utils;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.MediaFoundation.Tests;

/// <summary>
/// Enumerates audio decoders, encoders, and effects registered with Media Foundation on this
/// machine. Useful for diagnosing missing codecs (e.g. AAC encoder on N-edition Windows).
/// </summary>
sealed class MediaFoundationEnumerateTransformsTest : IConsoleTest
{
    public string Id => "MediaFoundation.EnumerateTransforms";
    public string Description => "Enumerate audio decoders, encoders, and effects registered with Media Foundation";
    public MenuPath? MenuLocation =>
        new("Media Foundation", "Enumerate transforms", Group: "Info", Order: 0);
    public IReadOnlyList<TestParameter> Parameters => [];

    public TestResult Run(TestContext ctx)
    {
        MediaFoundationApi.Startup();

        var categories = new (string Name, Guid Category)[]
        {
            ("Audio Decoders", MediaFoundationTransformCategories.AudioDecoder),
            ("Audio Encoders", MediaFoundationTransformCategories.AudioEncoder),
            ("Audio Effects", MediaFoundationTransformCategories.AudioEffect),
        };

        var diagnostics = new Dictionary<string, string>();

        foreach (var (name, category) in categories)
        {
            var transforms = MediaFoundationApi.EnumerateTransforms(category).ToList();
            diagnostics[$"{name.Replace(' ', '_')}_count"] = transforms.Count.ToString();

            if (ctx.Interactive)
            {
                var table = new Table()
                    .Title($"[bold]{name}[/] ({transforms.Count} found)")
                    .AddColumn("Name")
                    .AddColumn("Input Types")
                    .AddColumn("Output Types")
                    .Border(TableBorder.Rounded);
                foreach (var mft in transforms)
                {
                    table.AddRow(
                        Markup.Escape(GetTransformName(mft)),
                        Markup.Escape(GetTypeDescriptions(mft, MediaFoundationAttributes.MFT_INPUT_TYPES_Attributes)),
                        Markup.Escape(GetTypeDescriptions(mft, MediaFoundationAttributes.MFT_OUTPUT_TYPES_Attributes)));
                }
                AnsiConsole.Write(table);
                AnsiConsole.WriteLine();
            }
            else
            {
                Console.WriteLine($"-- {name} ({transforms.Count}) --");
                foreach (var mft in transforms)
                {
                    Console.WriteLine($"  {GetTransformName(mft)}");
                }
            }
        }

        return TestResult.Pass(
            $"Enumerated {diagnostics.Values.Sum(v => int.Parse(v))} transforms across 3 categories",
            diagnostics);
    }

    private static string GetTransformName(MfActivate mft)
    {
        try { return mft.GetString(MediaFoundationAttributes.MFT_FRIENDLY_NAME_Attribute); }
        catch { return "(unknown)"; }
    }

    private static string GetTypeDescriptions(MfActivate mft, Guid attributeKey)
    {
        try
        {
            var types = mft.GetBlobAsArrayOf<MftRegisterTypeInfo>(attributeKey);
            var descriptions = new List<string>();
            foreach (var t in types)
            {
                var subTypeName = FieldDescriptionHelper.Describe(typeof(AudioSubtypes), t.SubType);
                if (subTypeName.StartsWith("0000"))
                    subTypeName = t.SubType.ToString("D")[..8];
                descriptions.Add(subTypeName);
            }
            return string.Join(", ", descriptions);
        }
        catch
        {
            return "-";
        }
    }
}
