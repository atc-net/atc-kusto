namespace Atc.Kusto.CLI.Rendering;

/// <summary>
/// Supported output formats for CLI results.
/// </summary>
public enum OutputFormat
{
    /// <summary>
    /// Human-readable Spectre.Console table with ANSI colors and borders.
    /// </summary>
    Human,

    /// <summary>
    /// Structured JSON array output for scripting.
    /// </summary>
    Json,

    /// <summary>
    /// GitHub Flavored Markdown table output.
    /// </summary>
    Markdown,

    /// <summary>
    /// Comma-separated values output.
    /// </summary>
    Csv,
}