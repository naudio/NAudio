namespace NAudioConsoleTest.Shared.Testing;

/// <summary>
/// Where a test appears in the interactive menu. <see cref="Group"/> is optional — categories
/// with one or two tests can render flat by leaving it null. Tests with a null
/// <c>MenuLocation</c> are CLI-only and don't appear in any menu.
/// </summary>
public sealed record MenuPath(string Category, string Label, string? Group = null, int Order = 0);
