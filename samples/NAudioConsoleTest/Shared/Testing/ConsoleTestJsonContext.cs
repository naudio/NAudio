using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace NAudioConsoleTest.Shared.Testing;

/// <summary>
/// Source-generated JSON metadata for every type the console test app reads or writes (batch plans
/// and reports, host diagnostics, the recent-files store), so the app works when published with
/// Native AOT, where reflection-based serialization is unavailable. Each call site keeps its own
/// <see cref="JsonSerializerOptions"/> (naming policy, comment handling and so on) and resolves
/// metadata from here via <see cref="JsonOptionsExtensions.TypeInfo{T}"/>.
/// </summary>
[JsonSerializable(typeof(BatchPlan))]
[JsonSerializable(typeof(BatchReport))]
[JsonSerializable(typeof(HostDiagnostics))]
[JsonSerializable(typeof(Dictionary<string, List<string>>))]
internal sealed partial class ConsoleTestJsonContext : JsonSerializerContext;

internal static class JsonOptionsExtensions
{
    /// <summary>
    /// Typed metadata for <typeparamref name="T"/> under these options. The options must have
    /// <see cref="ConsoleTestJsonContext"/> as their <see cref="JsonSerializerOptions.TypeInfoResolver"/>.
    /// </summary>
    public static JsonTypeInfo<T> TypeInfo<T>(this JsonSerializerOptions options) =>
        (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T));
}
