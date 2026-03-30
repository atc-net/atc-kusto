namespace Atc.Kusto.CLI.Models;

/// <summary>
/// Represents a reference to a query file with an optional line range.
/// </summary>
/// <param name="Path">The file path.</param>
/// <param name="LineRange">Optional line range to extract.</param>
[StructLayout(LayoutKind.Auto)]
public readonly record struct QueryFileReference(string Path, QueryLineRange? LineRange);