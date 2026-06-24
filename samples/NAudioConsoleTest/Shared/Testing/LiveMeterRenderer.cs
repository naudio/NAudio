namespace NAudioConsoleTest.Shared.Testing;

/// <summary>
/// In-place per-channel bar meter for interactive tests (RMS or peak level monitoring). Reserves
/// <c>rowCount</c> console rows on construction, and each <see cref="Update"/> rewrites a fixed
/// row without scrolling. Disposing moves the cursor past the meter block so subsequent output
/// doesn't trample it.
/// </summary>
/// <remarks>
/// Use only when <c>ctx.Interactive</c> is true — cursor positioning misbehaves under
/// redirected output. CLI/non-interactive tests should fall back to a summary table after the
/// measurement loop finishes.
/// </remarks>
sealed class LiveMeterRenderer : IDisposable
{
    private const int BarWidth = 16;

    private readonly int topRow;
    private readonly int rowCount;
    private bool disposed;

    public LiveMeterRenderer(int rowCount)
    {
        this.rowCount = rowCount;
        topRow = Console.CursorTop;
        // Reserve `rowCount` rows so SetCursorPosition has somewhere to land.
        for (var i = 0; i < rowCount; i++) Console.WriteLine();
    }

    /// <summary>
    /// Updates one meter row. <paramref name="scale"/> stretches the bar — pass <c>1f</c> for
    /// peak meters (full-scale = 1.0) and <c>4f</c> for RMS meters (typical RMS rarely exceeds
    /// 0.25 even on loud signal, so amplify to make the bar useful).
    /// </summary>
    public void Update(int rowIndex, string label, float level, float scale = 1f)
    {
        if (disposed) return;
        if (rowIndex < 0 || rowIndex >= rowCount) return;

        var filled = Math.Clamp((int)(level * scale * BarWidth), 0, BarWidth);
        var bar = new string('█', filled) + new string('░', BarWidth - filled);
        var db = level > 1e-6f ? 20f * MathF.Log10(level) : float.NegativeInfinity;
        var dbText = float.IsNegativeInfinity(db) ? "   -∞" : $"{db,6:0.0}";

        Console.SetCursorPosition(0, topRow + rowIndex);
        // Trailing spaces ensure any wider previous text gets overwritten.
        Console.Write($"  {label,-14} {bar} {dbText} dBFS   ");
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Console.SetCursorPosition(0, Math.Min(topRow + rowCount, Console.BufferHeight - 1));
        Console.WriteLine();
    }
}
