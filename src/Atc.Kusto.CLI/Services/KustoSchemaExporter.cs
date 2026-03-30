namespace Atc.Kusto.CLI.Services;

public sealed class KustoSchemaExporter(
    ILoggerFactory loggerFactory,
    ICliKustoClientFactory clientFactory)
    : IKustoSchemaExporter
{
    private readonly ILogger<KustoSchemaExporter> logger = loggerFactory.CreateLogger<KustoSchemaExporter>();

    /// <inheritdoc />
    public async Task ExportTablesAsync(
        string tenantId,
        Uri clusterUrl,
        string database,
        string outputDir,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clusterUrl);

        var client = clientFactory.GetOrCreateAdminClient(tenantId, clusterUrl, database);
        var tablesDir = EnsureDirectory(outputDir, "Tables");

        using var reader = await client.ExecuteControlCommandAsync(database, ".show tables");
        var tableNames = new List<string>();

        var nameOrdinal = reader.GetOrdinal("TableName");
        while (reader.Read())
        {
            tableNames.Add(reader.GetString(nameOrdinal));
        }

        logger.LogInformation("Found {Count} tables", tableNames.Count);

        foreach (var tableName in tableNames)
        {
            await ExportTableAsync(client, database, tablesDir, tableName, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task ExportFunctionsAsync(
        string tenantId,
        Uri clusterUrl,
        string database,
        string outputDir,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clusterUrl);

        var client = clientFactory.GetOrCreateAdminClient(tenantId, clusterUrl, database);
        var functionsDir = EnsureDirectory(outputDir, "Functions");

        using var reader = await client.ExecuteControlCommandAsync(database, ".show functions");

        var nameOrdinal = reader.GetOrdinal("Name");
        var parametersOrdinal = reader.GetOrdinal("Parameters");
        var bodyOrdinal = reader.GetOrdinal("Body");
        var folderOrdinal = reader.GetOrdinal("Folder");
        var docStringOrdinal = reader.GetOrdinal("DocString");

        var functions = new List<(string Name, string Parameters, string Body, string Folder, string DocString)>();
        while (reader.Read())
        {
            functions.Add((
                reader.GetString(nameOrdinal),
                reader.GetString(parametersOrdinal),
                reader.GetString(bodyOrdinal),
                reader.IsDBNull(folderOrdinal) ? string.Empty : reader.GetString(folderOrdinal),
                reader.IsDBNull(docStringOrdinal) ? string.Empty : reader.GetString(docStringOrdinal)));
        }

        logger.LogInformation("Found {Count} functions", functions.Count);

        foreach (var (name, parameters, body, folder, docString) in functions)
        {
            var kql = FormatFunction(name, parameters, body, folder, docString);
            var filePath = Path.Combine(functionsDir, SanitizeFileName(name) + ".kql");
            await File.WriteAllTextAsync(filePath, kql, Encoding.UTF8, cancellationToken);
            logger.LogDebug("Exported function {FunctionName}", name);
        }
    }

    /// <inheritdoc />
    public async Task ExportMaterializedViewsAsync(
        string tenantId,
        Uri clusterUrl,
        string database,
        string outputDir,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clusterUrl);

        var client = clientFactory.GetOrCreateAdminClient(tenantId, clusterUrl, database);
        var viewsDir = EnsureDirectory(outputDir, "MaterializedViews");

        using var reader = await client.ExecuteControlCommandAsync(database, ".show materialized-views");

        var viewNames = new List<string>();
        var nameOrdinal = reader.GetOrdinal("Name");
        while (reader.Read())
        {
            viewNames.Add(reader.GetString(nameOrdinal));
        }

        logger.LogInformation("Found {Count} materialized views", viewNames.Count);

        foreach (var viewName in viewNames)
        {
            using var viewReader = await client.ExecuteControlCommandAsync(
                database,
                $".show materialized-view ['{viewName}']");

            if (viewReader.Read())
            {
                var sourceTableOrdinal = viewReader.GetOrdinal("SourceTable");
                var queryOrdinal = viewReader.GetOrdinal("Query");

                var sourceTable = viewReader.GetString(sourceTableOrdinal);
                var query = viewReader.GetString(queryOrdinal);

                var sb = new StringBuilder();
                sb.Append(".create-or-alter materialized-view ")
                    .Append(viewName)
                    .Append(" on table ")
                    .Append(sourceTable);
                AppendKustoBody(sb, query.Trim());
                var kql = sb.ToString();
                var filePath = Path.Combine(viewsDir, SanitizeFileName(viewName) + ".kql");
                await File.WriteAllTextAsync(filePath, kql, Encoding.UTF8, cancellationToken);
                logger.LogDebug("Exported materialized view {ViewName}", viewName);
            }
        }
    }

    /// <inheritdoc />
    public async Task ExportExternalTablesAsync(
        string tenantId,
        Uri clusterUrl,
        string database,
        string outputDir,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clusterUrl);

        var client = clientFactory.GetOrCreateAdminClient(tenantId, clusterUrl, database);
        var externalDir = EnsureDirectory(outputDir, "ExternalTables");

        using var reader = await client.ExecuteControlCommandAsync(database, ".show external tables");

        var tableNames = new List<string>();
        var nameOrdinal = reader.GetOrdinal("TableName");
        while (reader.Read())
        {
            tableNames.Add(reader.GetString(nameOrdinal));
        }

        logger.LogInformation("Found {Count} external tables", tableNames.Count);

        foreach (var tableName in tableNames)
        {
            using var schemaReader = await client.ExecuteControlCommandAsync(
                database,
                $".show external table ['{tableName}'] cslschema");

            if (schemaReader.Read())
            {
                var schemaOrdinal = schemaReader.GetOrdinal("Schema");
                var schema = schemaReader.GetString(schemaOrdinal);

                var sb = new StringBuilder();
                sb.Append(".create-or-alter external table ").Append(tableName).Append(" (");
                AppendColumnList(sb, schema);

                var kql = sb.ToString();
                var filePath = Path.Combine(externalDir, SanitizeFileName(tableName) + ".kql");
                await File.WriteAllTextAsync(filePath, kql, Encoding.UTF8, cancellationToken);
                logger.LogDebug("Exported external table {TableName}", tableName);
            }
        }
    }

    /// <inheritdoc />
    public async Task ExportPoliciesAsync(
        string tenantId,
        Uri clusterUrl,
        string database,
        string outputDir,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clusterUrl);

        var client = clientFactory.GetOrCreateAdminClient(tenantId, clusterUrl, database);
        var policiesDir = EnsureDirectory(outputDir, "Policies");

        await ExportDatabasePoliciesAsync(client, database, policiesDir, cancellationToken);
        await ExportTablePoliciesAsync(client, database, policiesDir, cancellationToken);
    }

    /// <summary>
    /// Exports database-level retention and caching policies as KQL scripts.
    /// </summary>
    private static async Task ExportDatabasePoliciesAsync(
        ICslAdminProvider client,
        string database,
        string policiesDir,
        CancellationToken cancellationToken)
    {
        // Database retention policy
        using (var reader = await client.ExecuteControlCommandAsync(
            database,
            $".show database ['{database}'] policy retention"))
        {
            if (reader.Read())
            {
                var policyOrdinal = reader.GetOrdinal("Policy");
                var policy = reader.IsDBNull(policyOrdinal) ? null : reader.GetString(policyOrdinal);
                if (!string.IsNullOrEmpty(policy))
                {
                    var kql = $".alter database {database} policy retention\n```\n{policy}\n```";
                    await File.WriteAllTextAsync(
                        Path.Combine(policiesDir, "Database_RetentionPolicy.kql"),
                        kql,
                        Encoding.UTF8,
                        cancellationToken);
                }
            }
        }

        // Database caching policy
        using (var reader = await client.ExecuteControlCommandAsync(
            database,
            $".show database ['{database}'] policy caching"))
        {
            if (reader.Read())
            {
                var policyOrdinal = reader.GetOrdinal("Policy");
                var policy = reader.IsDBNull(policyOrdinal) ? null : reader.GetString(policyOrdinal);
                if (!string.IsNullOrEmpty(policy))
                {
                    var kql = $".alter database {database} policy caching\n```\n{policy}\n```";
                    await File.WriteAllTextAsync(
                        Path.Combine(policiesDir, "Database_CachingPolicy.kql"),
                        kql,
                        Encoding.UTF8,
                        cancellationToken);
                }
            }
        }
    }

    /// <summary>
    /// Exports table-level retention and caching policies as KQL scripts.
    /// </summary>
    private static async Task ExportTablePoliciesAsync(
        ICslAdminProvider client,
        string database,
        string policiesDir,
        CancellationToken cancellationToken)
    {
        // Table retention policies
        using (var reader = await client.ExecuteControlCommandAsync(
            database,
            ".show table * policy retention"))
        {
            var entityOrdinal = reader.GetOrdinal("EntityName");
            var policyOrdinal = reader.GetOrdinal("Policy");
            while (reader.Read())
            {
                var entity = reader.GetString(entityOrdinal);
                var policy = reader.IsDBNull(policyOrdinal) ? null : reader.GetString(policyOrdinal);
                if (string.IsNullOrEmpty(policy))
                {
                    continue;
                }

                var tableName = ExtractTableName(entity);
                var kql = $".alter table {tableName} policy retention\n```\n{policy}\n```";
                await File.WriteAllTextAsync(
                    Path.Combine(policiesDir, SanitizeFileName(tableName) + "_RetentionPolicy.kql"),
                    kql,
                    Encoding.UTF8,
                    cancellationToken);
            }
        }

        // Table caching policies
        using (var reader = await client.ExecuteControlCommandAsync(
            database,
            ".show table * policy caching"))
        {
            var entityOrdinal = reader.GetOrdinal("EntityName");
            var policyOrdinal = reader.GetOrdinal("Policy");
            while (reader.Read())
            {
                var entity = reader.GetString(entityOrdinal);
                var policy = reader.IsDBNull(policyOrdinal) ? null : reader.GetString(policyOrdinal);
                if (string.IsNullOrEmpty(policy))
                {
                    continue;
                }

                var tableName = ExtractTableName(entity);
                var kql = $".alter table {tableName} policy caching\n```\n{policy}\n```";
                await File.WriteAllTextAsync(
                    Path.Combine(policiesDir, SanitizeFileName(tableName) + "_CachingPolicy.kql"),
                    kql,
                    Encoding.UTF8,
                    cancellationToken);
            }
        }
    }

    /// <summary>
    /// Exports a single table definition including schema and properties as a KQL script.
    /// </summary>
    private static async Task ExportTableAsync(
        ICslAdminProvider client,
        string database,
        string tablesDir,
        string tableName,
        CancellationToken cancellationToken)
    {
        using var schemaReader = await client.ExecuteControlCommandAsync(
            database,
            $".show table ['{tableName}'] cslschema");

        if (!schemaReader.Read())
        {
            return;
        }

        var schemaOrdinal = schemaReader.GetOrdinal("Schema");
        var schema = schemaReader.GetString(schemaOrdinal);

        var sb = new StringBuilder();
        sb.Append(".create-merge table ").Append(tableName).Append(" (");

        AppendColumnList(sb, schema);

        using var detailsReader = await client.ExecuteControlCommandAsync(
            database,
            $".show table ['{tableName}'] details");
        if (detailsReader.Read())
        {
            AppendWithProperties(sb, detailsReader);
        }

        await File.WriteAllTextAsync(
            Path.Combine(tablesDir, SanitizeFileName(tableName) + ".kql"),
            sb.ToString(),
            Encoding.UTF8,
            cancellationToken);
    }

    /// <summary>
    /// Formats a Kusto function definition as a .create-or-alter KQL statement.
    /// </summary>
    private static string FormatFunction(
        string name,
        string parameters,
        string body,
        string folder,
        string docString)
    {
        var sb = new StringBuilder();
        sb.Append(".create-or-alter function");

        var properties = new List<string>();
        if (!string.IsNullOrEmpty(folder))
        {
            properties.Add($"folder = \"{folder}\"");
        }

        if (!string.IsNullOrEmpty(docString))
        {
            properties.Add($"docstring = \"{docString}\"");
        }

        if (properties.Count > 0)
        {
            sb.Append(" with (");
            sb.Append(string.Join(", ", properties));
            sb.Append(')');
        }

        sb.Append(' ').Append(name);

        var rawParams = parameters;
        if (rawParams.StartsWith('(') && rawParams.EndsWith(')'))
        {
            rawParams = rawParams[1..^1];
        }

        var paramList = string.IsNullOrWhiteSpace(rawParams)
            ? []
            : rawParams.Split(',', StringSplitOptions.TrimEntries);

        if (paramList.Length <= 1)
        {
            sb.Append('(');
            if (paramList.Length == 1)
            {
                sb.Append(FormatParameter(paramList[0]));
            }

            sb.Append(')');
        }
        else
        {
            sb.AppendLine("(");
            for (var i = 0; i < paramList.Length; i++)
            {
                sb.Append("    ").Append(FormatParameter(paramList[i]));
                sb.AppendLine(i < paramList.Length - 1 ? "," : ")");
            }
        }

        var trimmedBody = body.Trim();
        if (trimmedBody.StartsWith('{') && trimmedBody.EndsWith('}'))
        {
            trimmedBody = trimmedBody[1..^1].Trim();
        }

        AppendKustoBody(sb, trimmedBody, preserveIndentation: true);

        return sb.ToString();
    }

    /// <summary>
    /// Appends a formatted column list to the string builder, with one column per line for multiple columns.
    /// </summary>
    private static void AppendColumnList(
        StringBuilder sb,
        string schema)
    {
        var columns = schema.Split(',', StringSplitOptions.TrimEntries);
        if (columns.Length <= 1)
        {
            if (columns.Length == 1)
            {
                sb.Append(FormatParameter(columns[0]));
            }

            sb.Append(')');
        }
        else
        {
            sb.AppendLine();
            for (var i = 0; i < columns.Length; i++)
            {
                sb.Append("    ").Append(FormatParameter(columns[i]));
                sb.AppendLine(i < columns.Length - 1 ? "," : ")");
            }
        }
    }

    /// <summary>
    /// Appends a 'with' clause containing folder and docstring properties if present.
    /// </summary>
    private static void AppendWithProperties(
        StringBuilder sb,
        System.Data.IDataReader reader)
    {
        var properties = new List<string>();

        var folderOrdinal = reader.GetOrdinal("Folder");
        var folder = reader.IsDBNull(folderOrdinal) ? null : reader.GetString(folderOrdinal);
        if (!string.IsNullOrEmpty(folder))
        {
            properties.Add($"folder = \"{folder}\"");
        }

        var docStringOrdinal = reader.GetOrdinal("DocString");
        var docString = reader.IsDBNull(docStringOrdinal) ? null : reader.GetString(docStringOrdinal);
        if (!string.IsNullOrEmpty(docString))
        {
            properties.Add($"docstring = \"{docString}\"");
        }

        if (properties.Count > 0)
        {
            sb.Append(" with (").Append(string.Join(", ", properties)).Append(')');
        }
    }

    /// <summary>
    /// Appends a Kusto body block with consistent indentation for pipe operators and their continuations.
    /// </summary>
    private static void AppendKustoBody(
        StringBuilder sb,
        string body,
        bool preserveIndentation = false)
    {
        if (sb.Length > 0 && !sb.ToString().EndsWith(Environment.NewLine, StringComparison.Ordinal))
        {
            sb.AppendLine();
        }

        sb.AppendLine("{");
        var lines = body.Split('\n');
        var afterPipeOperator = false;

        foreach (var rawLine in lines)
        {
            var trimmed = rawLine.TrimEnd();
            if (string.IsNullOrEmpty(trimmed))
            {
                sb.AppendLine();
                afterPipeOperator = false;
                continue;
            }

            var stripped = trimmed.TrimStart();

            if (stripped.StartsWith('|'))
            {
                if (!preserveIndentation && TrySplitPipeOperatorFields(stripped, out var pipeKeyword, out var fields))
                {
                    sb.Append("    ").AppendLine(pipeKeyword);
                    for (var f = 0; f < fields.Length; f++)
                    {
                        sb.Append("        ").Append(fields[f]);
                        sb.AppendLine(f < fields.Length - 1 ? "," : string.Empty);
                    }
                }
                else
                {
                    sb.Append("    ").AppendLine(stripped);
                }

                afterPipeOperator = true;
            }
            else if (afterPipeOperator)
            {
                sb.Append("        ").AppendLine(stripped);
            }
            else if (preserveIndentation && trimmed.Length > stripped.Length)
            {
                // Non-pipe line with existing indentation — preserve it with base indent
                sb.Append("    ").AppendLine(trimmed);
            }
            else
            {
                sb.Append("    ").AppendLine(stripped);
            }
        }

        sb.Append('}');
    }

    /// <summary>
    /// Attempts to split a pipe operator line with inline fields (e.g., "| project a, b, c") into the keyword and individual fields.
    /// </summary>
    private static bool TrySplitPipeOperatorFields(
        string line,
        [NotNullWhen(true)] out string? pipeKeyword,
        [NotNullWhen(true)] out string[]? fields)
    {
        pipeKeyword = null;
        fields = null;

        // Match patterns like "| project field1, field2" or "| extend field1, field2"
        var spaceIndex = line.IndexOf(' ', StringComparison.Ordinal);
        if (spaceIndex < 0)
        {
            return false;
        }

        // Find end of the keyword (e.g., "| project" or "| extend")
        var afterPipe = line[spaceIndex..].TrimStart();
        var keywordEnd = afterPipe.IndexOf(' ', StringComparison.Ordinal);
        if (keywordEnd < 0)
        {
            return false;
        }

        var keyword = afterPipe[..keywordEnd];
        var remainder = afterPipe[keywordEnd..].Trim();

        // Only split known operators that take field lists
        if (keyword is not ("project" or "extend" or "project-away" or "project-keep"
            or "project-rename" or "project-reorder"))
        {
            return false;
        }

        // Only split if there are multiple fields (contains comma)
        if (!remainder.Contains(',', StringComparison.Ordinal))
        {
            return false;
        }

        pipeKeyword = $"| {keyword}";
        fields = remainder.Split(',', StringSplitOptions.TrimEntries);
        return true;
    }

    /// <summary>
    /// Formats a parameter or column definition by ensuring a space after the colon separator.
    /// </summary>
    private static string FormatParameter(string param)
    {
        var colonIndex = param.IndexOf(':', StringComparison.Ordinal);
        if (colonIndex > 0 && colonIndex < param.Length - 1 && param[colonIndex + 1] != ' ')
        {
            return param[..(colonIndex + 1)] + " " + param[(colonIndex + 1)..];
        }

        return param;
    }

    /// <summary>
    /// Ensures the specified subdirectory exists under the output directory and returns its path.
    /// </summary>
    private static string EnsureDirectory(
        string outputDir,
        string subDir)
    {
        var dir = Path.Combine(outputDir, subDir);
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>
    /// Replaces invalid filename characters with underscores.
    /// </summary>
    private static string SanitizeFileName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            sb.Append(Array.IndexOf(invalidChars, c) >= 0 ? '_' : c);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Extracts the table name from a fully qualified entity name like "[database].[table]".
    /// </summary>
    private static string ExtractTableName(string entityName)
    {
        var lastDot = entityName.LastIndexOf('.');
        if (lastDot < 0)
        {
            return entityName.Trim('[', ']', '\'');
        }

        return entityName[(lastDot + 1)..].Trim('[', ']', '\'');
    }
}