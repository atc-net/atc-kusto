namespace Atc.Kusto.CLI.Rendering;

/// <summary>
/// Renders results as a JSON array for scripting and automation.
/// </summary>
public sealed class JsonResultRenderer : IResultRenderer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    /// <inheritdoc />
    public void Render(
        IReadOnlyList<string> columns,
        IReadOnlyList<string[]> rows)
    {
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(rows);

        var result = new List<Dictionary<string, string>>(rows.Count);
        foreach (var row in rows)
        {
            var obj = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var c = 0; c < columns.Count; c++)
            {
                obj[columns[c]] = row[c];
            }

            result.Add(obj);
        }

        System.Console.WriteLine(JsonSerializer.Serialize(result, SerializerOptions));
    }

    /// <inheritdoc />
    public void RenderStatistics(IDictionary<string, string> statistics)
    {
        ArgumentNullException.ThrowIfNull(statistics);

        var wrapper = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["statistics"] = statistics,
        };

        System.Console.WriteLine();
        System.Console.WriteLine(JsonSerializer.Serialize(wrapper, SerializerOptions));
    }

    /// <inheritdoc />
    public void RenderWebExplorerUrl(Uri url)
    {
        ArgumentNullException.ThrowIfNull(url);

        var wrapper = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["webExplorerUrl"] = url.AbsoluteUri,
        };

        System.Console.WriteLine();
        System.Console.WriteLine(JsonSerializer.Serialize(wrapper, SerializerOptions));
    }
}