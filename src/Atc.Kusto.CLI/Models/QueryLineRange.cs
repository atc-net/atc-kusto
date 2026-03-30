namespace Atc.Kusto.CLI.Models;

/// <summary>
/// Represents a line range within a query file.
/// </summary>
/// <param name="StartLine">The 1-based start line number.</param>
/// <param name="EndLine">The 1-based end line number.</param>
[StructLayout(LayoutKind.Auto)]
public readonly record struct QueryLineRange(int StartLine, int EndLine)
{
    public int LineCount => EndLine - StartLine + 1;
}