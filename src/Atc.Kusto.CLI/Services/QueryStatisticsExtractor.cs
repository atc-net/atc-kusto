namespace Atc.Kusto.CLI.Services;

/// <summary>
/// Extracts query execution statistics from Kusto V2 response frames.
/// The statistics are found in the QueryCompletionInformation result set,
/// in a row where SeverityName is "Stats" and the StatusDescription column
/// contains a JSON payload with resource usage data.
/// </summary>
public static class QueryStatisticsExtractor
{
    private const double BytesPerMb = 1024.0 * 1024.0;
    private const double BytesPerKb = 1024.0;

    /// <summary>
    /// Attempts to extract statistics from a data reader that may contain
    /// multiple result sets (V2 query response).
    /// Call this after reading the primary result set by advancing with NextResult().
    /// </summary>
    /// <param name="reader">The data reader positioned after the primary results.</param>
    /// <returns>Extracted statistics, or null if not available.</returns>
    public static QueryStatistics? Extract(System.Data.IDataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        // Navigate through result sets looking for the statistics frame
        do
        {
            var stats = TryExtractFromCurrentResultSet(reader);
            if (stats is not null)
            {
                return stats;
            }
        }
        while (reader.NextResult());

        return null;
    }

    private static QueryStatistics? TryExtractFromCurrentResultSet(
        System.Data.IDataReader reader)
    {
        // Look for columns that indicate this is a QueryCompletionInformation frame
        var statusDescriptionOrdinal = TryGetOrdinal(reader, "StatusDescription");
        var severityNameOrdinal = TryGetOrdinal(reader, "SeverityName");
        var payloadOrdinal = TryGetOrdinal(reader, "Payload");

        if (statusDescriptionOrdinal < 0 && payloadOrdinal < 0)
        {
            return null;
        }

        while (reader.Read())
        {
            // Try the newer format: SeverityName = "Stats" with StatusDescription containing JSON
            if (severityNameOrdinal >= 0 && statusDescriptionOrdinal >= 0)
            {
                var severity = reader.IsDBNull(severityNameOrdinal) ? null : reader.GetString(severityNameOrdinal);
                if (string.Equals(severity, "Stats", StringComparison.OrdinalIgnoreCase))
                {
                    var json = reader.IsDBNull(statusDescriptionOrdinal) ? null : reader.GetString(statusDescriptionOrdinal);
                    if (json is not null)
                    {
                        return ParseStatisticsJson(json);
                    }
                }
            }

            // Try the legacy format: Payload column with stats JSON
            if (payloadOrdinal >= 0)
            {
                var payload = reader.IsDBNull(payloadOrdinal) ? null : reader.GetString(payloadOrdinal);
                if (payload is not null && payload.Contains("resource_usage", StringComparison.OrdinalIgnoreCase))
                {
                    return ParseStatisticsJson(payload);
                }
            }
        }

        return null;
    }

    private static QueryStatistics? ParseStatisticsJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new QueryStatistics
            {
                ExecutionTimeSec = GetDoubleProperty(root, "ExecutionTime"),
                Cpu = ParseCpuStats(root),
                MemoryPeakPerNodeMb = GetBytesAsMb(root, "resource_usage", "memory", "peak_per_node"),
                Cache = ParseCacheStats(root),
                Extents = ParseCountStats(root, "input_dataset_statistics", "extents"),
                Rows = ParseCountStats(root, "input_dataset_statistics", "rows"),
                Result = ParseResultStats(root),
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static QueryCpuStatistics? ParseCpuStats(JsonElement root)
    {
        if (!TryGetNestedElement(root, out var cpu, "resource_usage", "cpu"))
        {
            return null;
        }

        return new QueryCpuStatistics
        {
            Total = GetStringProperty(cpu, "total cpu"),
            QueryExecution = GetNestedStringProperty(cpu, "breakdown", "query execution"),
            QueryPlanning = GetNestedStringProperty(cpu, "breakdown", "query planning"),
        };
    }

    private static QueryCacheStatistics? ParseCacheStats(JsonElement root)
    {
        if (!TryGetNestedElement(root, out var hot, "resource_usage", "cache", "shards", "hot"))
        {
            return null;
        }

        var hitBytes = GetDoubleProperty(hot, "hitbytes");
        var missBytes = GetDoubleProperty(hot, "missbytes");

        if (hitBytes is null && missBytes is null)
        {
            return null;
        }

        return new QueryCacheStatistics
        {
            HotHitMb = hitBytes.HasValue ? System.Math.Round(hitBytes.Value / BytesPerMb, 2) : null,
            HotMissMb = missBytes.HasValue ? System.Math.Round(missBytes.Value / BytesPerMb, 2) : null,
        };
    }

    private static QueryCountStatistics? ParseCountStats(
        JsonElement root,
        string section,
        string subsection)
    {
        if (!TryGetNestedElement(root, out var element, section, subsection))
        {
            return null;
        }

        var scanned = GetLongProperty(element, "scanned");
        var total = GetLongProperty(element, "total");

        if (scanned is null && total is null)
        {
            return null;
        }

        return new QueryCountStatistics
        {
            Scanned = scanned,
            Total = total,
        };
    }

    private static QueryResultStatistics? ParseResultStats(JsonElement root)
    {
        if (!root.TryGetProperty("dataset_statistics", out var datasets) || datasets.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var enumerator = datasets.EnumerateArray();
        if (!enumerator.MoveNext())
        {
            return null;
        }

        var first = enumerator.Current;
        var rowCount = GetLongProperty(first, "table_row_count");
        var tableSize = GetDoubleProperty(first, "table_size");

        if (rowCount is null && tableSize is null)
        {
            return null;
        }

        return new QueryResultStatistics
        {
            RowCount = rowCount,
            SizeKb = tableSize.HasValue ? System.Math.Round(tableSize.Value / BytesPerKb, 2) : null,
        };
    }

    private static double? GetBytesAsMb(
        JsonElement root,
        params string[] path)
    {
        if (!TryGetNestedElement(root, out var element, path))
        {
            return null;
        }

        if (element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out var bytes))
        {
            return System.Math.Round(bytes / BytesPerMb, 2);
        }

        return null;
    }

    private static bool TryGetNestedElement(
        JsonElement root,
        out JsonElement result,
        params string[] path)
    {
        result = root;
        foreach (var key in path)
        {
            if (!result.TryGetProperty(key, out result))
            {
                result = default;
                return false;
            }
        }

        return true;
    }

    private static double? GetDoubleProperty(
        JsonElement element,
        string name)
    {
        if (element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.Number)
        {
            return prop.GetDouble();
        }

        return null;
    }

    private static long? GetLongProperty(
        JsonElement element,
        string name)
    {
        if (element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.Number)
        {
            return prop.GetInt64();
        }

        return null;
    }

    private static string? GetStringProperty(
        JsonElement element,
        string name)
    {
        if (element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString();
        }

        return null;
    }

    private static string? GetNestedStringProperty(
        JsonElement element,
        string parent,
        string child)
    {
        if (element.TryGetProperty(parent, out var parentElement) &&
            parentElement.TryGetProperty(child, out var childElement) &&
            childElement.ValueKind == JsonValueKind.String)
        {
            return childElement.GetString();
        }

        return null;
    }

    private static int TryGetOrdinal(
        System.Data.IDataReader reader,
        string name)
    {
        try
        {
            return reader.GetOrdinal(name);
        }
        catch (IndexOutOfRangeException)
        {
            return -1;
        }
    }
}