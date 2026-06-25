using System.Globalization;

namespace NAudioConsoleTest.Shared.Testing;

/// <summary>
/// Shared string → typed value conversion used by both the CLI argv parser and the batch
/// runner's plan-file param section. Keeps timespan parsing, enum case-insensitivity, and
/// invariant culture in one place.
/// </summary>
public static class ParameterCoercion
{
    public static bool TryConvert(string raw, Type target, out object? value)
    {
        try
        {
            if (target == typeof(string)) { value = raw; return true; }
            if (target == typeof(int)) { value = int.Parse(raw, CultureInfo.InvariantCulture); return true; }
            if (target == typeof(long)) { value = long.Parse(raw, CultureInfo.InvariantCulture); return true; }
            if (target == typeof(double)) { value = double.Parse(raw, CultureInfo.InvariantCulture); return true; }
            if (target == typeof(float)) { value = float.Parse(raw, CultureInfo.InvariantCulture); return true; }
            if (target == typeof(bool)) { value = bool.Parse(raw); return true; }
            if (target == typeof(TimeSpan))
            {
                if (!ParameterPrompter.TryParseTimeSpan(raw, out var ts)) { value = null; return false; }
                value = ts;
                return true;
            }
            if (target.IsEnum) { value = Enum.Parse(target, raw, ignoreCase: true); return true; }
            value = Convert.ChangeType(raw, target, CultureInfo.InvariantCulture);
            return true;
        }
        catch
        {
            value = null;
            return false;
        }
    }
}
