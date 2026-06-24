using Spectre.Console;

namespace NAudioConsoleTest.Shared;

static class AudioFileSelector
{
    private static string? lastPath;

    public static string? SelectAudioFile()
    {
        var prompt = new TextPrompt<string>("Audio file path:")
            .AllowEmpty()
            .Validate(path =>
            {
                if (string.IsNullOrWhiteSpace(path))
                    return ValidationResult.Error("Path cannot be empty");
                if (!File.Exists(path))
                    return ValidationResult.Error($"File not found: {path}");
                return ValidationResult.Success();
            });

        if (lastPath != null)
            prompt.DefaultValue(lastPath);

        var path = AnsiConsole.Prompt(prompt);
        if (string.IsNullOrWhiteSpace(path))
            return null;

        lastPath = path;
        return path;
    }
}
