using System.Runtime.InteropServices;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.MediaFoundation;
using NAudio.Utils;
using Spectre.Console;

namespace NAudioConsoleTest.MediaFoundation;

static class TransformEnumerationTests
{
    public static void EnumerateAll()
    {
        AnsiConsole.MarkupLine("[bold]Enumerate Media Foundation Transforms[/]\n");

        MediaFoundationApi.Startup();

        var categories = new (string Name, Guid Category)[]
        {
            ("Audio Decoders", MediaFoundationTransformCategories.AudioDecoder),
            ("Audio Encoders", MediaFoundationTransformCategories.AudioEncoder),
            ("Audio Effects", MediaFoundationTransformCategories.AudioEffect),
        };

        foreach (var (name, category) in categories)
        {
            var transforms = MediaFoundationApi.EnumerateTransforms(category).ToList();

            var table = new Table()
                .Title($"[bold]{name}[/] ({transforms.Count} found)")
                .AddColumn("Name")
                .AddColumn("Input Types")
                .AddColumn("Output Types")
                .Border(TableBorder.Rounded);

            foreach (var mft in transforms)
            {
                var mftName = GetTransformName(mft);
                var inputTypes = GetTypeDescriptions(mft, MediaFoundationAttributes.MFT_INPUT_TYPES_Attributes);
                var outputTypes = GetTypeDescriptions(mft, MediaFoundationAttributes.MFT_OUTPUT_TYPES_Attributes);

                table.AddRow(
                    Markup.Escape(mftName),
                    Markup.Escape(inputTypes),
                    Markup.Escape(outputTypes));
            }

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine("");
        }

        AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
        Console.ReadKey(intercept: true);
    }

    private static string GetTransformName(MfActivate mft)
    {
        try
        {
            return mft.GetString(MediaFoundationAttributes.MFT_FRIENDLY_NAME_Attribute);
        }
        catch
        {
            return "(unknown)";
        }
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
                    subTypeName = t.SubType.ToString("D")[..8]; // shortened GUID for unknown types
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
