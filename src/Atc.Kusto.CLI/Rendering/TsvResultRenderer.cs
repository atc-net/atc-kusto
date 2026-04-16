namespace Atc.Kusto.CLI.Rendering;

/// <summary>
/// Renders results as tab-separated values (TSV).
/// </summary>
public sealed class TsvResultRenderer : CsvResultRenderer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TsvResultRenderer"/> class.
    /// </summary>
    public TsvResultRenderer()
        : base('\t')
    {
    }
}