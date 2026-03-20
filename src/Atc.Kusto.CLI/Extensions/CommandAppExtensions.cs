namespace Atc.Kusto.CLI.Extensions;

public static class CommandAppExtensions
{
    public static void ConfigureCommands(this CommandApp app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.Configure(config =>
        {
            config.AddBranch("export", export =>
            {
                export.SetDescription("Export Kusto database schemas");

                export.AddCommand<ExportSchemaCommand>("schema")
                    .WithDescription("Export the full database schema (tables, functions, materialized views, external tables, and policies)")
                    .WithExample("export", "schema", "--tenant-id", "<GUID>", "--cluster-url", "https://mycluster.kusto.windows.net", "--database", "MyDb")
                    .WithExample("export", "schema", "--tenant-id", "<GUID>", "--cluster-url", "https://mycluster.kusto.windows.net", "--database", "MyDb", "--output-dir", "./kusto-export");

                export.AddCommand<ExportTablesCommand>("tables")
                    .WithDescription("Export table schemas")
                    .WithExample("export", "tables", "--tenant-id", "<GUID>", "--cluster-url", "https://mycluster.kusto.windows.net", "--database", "MyDb")
                    .WithExample("export", "tables", "--tenant-id", "<GUID>", "--cluster-url", "https://mycluster.kusto.windows.net", "--database", "MyDb", "--output-dir", "./kusto-export");

                export.AddCommand<ExportFunctionsCommand>("functions")
                    .WithDescription("Export functions")
                    .WithExample("export", "functions", "--tenant-id", "<GUID>", "--cluster-url", "https://mycluster.kusto.windows.net", "--database", "MyDb")
                    .WithExample("export", "functions", "--tenant-id", "<GUID>", "--cluster-url", "https://mycluster.kusto.windows.net", "--database", "MyDb", "--output-dir", "./kusto-export");

                export.AddCommand<ExportMaterializedViewsCommand>("materialized-views")
                    .WithDescription("Export materialized views")
                    .WithExample("export", "materialized-views", "--tenant-id", "<GUID>", "--cluster-url", "https://mycluster.kusto.windows.net", "--database", "MyDb")
                    .WithExample("export", "materialized-views", "--tenant-id", "<GUID>", "--cluster-url", "https://mycluster.kusto.windows.net", "--database", "MyDb", "--output-dir", "./kusto-export");

                export.AddCommand<ExportExternalTablesCommand>("external-tables")
                    .WithDescription("Export external tables")
                    .WithExample("export", "external-tables", "--tenant-id", "<GUID>", "--cluster-url", "https://mycluster.kusto.windows.net", "--database", "MyDb")
                    .WithExample("export", "external-tables", "--tenant-id", "<GUID>", "--cluster-url", "https://mycluster.kusto.windows.net", "--database", "MyDb", "--output-dir", "./kusto-export");

                export.AddCommand<ExportPoliciesCommand>("policies")
                    .WithDescription("Export retention and caching policies")
                    .WithExample("export", "policies", "--tenant-id", "<GUID>", "--cluster-url", "https://mycluster.kusto.windows.net", "--database", "MyDb")
                    .WithExample("export", "policies", "--tenant-id", "<GUID>", "--cluster-url", "https://mycluster.kusto.windows.net", "--database", "MyDb", "--output-dir", "./kusto-export");
            });

            config.AddCommand<QueryCommand>("query")
                .WithDescription("Execute a KQL query against a Kusto database")
                .WithExample("query", "\"StormEvents | take 5\"", "--tenant-id", "<GUID>", "--cluster-url", "https://mycluster.kusto.windows.net", "--database", "MyDb")
                .WithExample("query", "--file", "myquery.kql", "--tenant-id", "<GUID>", "--cluster-url", "https://mycluster.kusto.windows.net", "--database", "MyDb")
                .WithExample("query", "\"StormEvents | take 5\"", "--tenant-id", "<GUID>", "--cluster-url", "https://mycluster.kusto.windows.net", "--database", "MyDb", "--format", "json")
                .WithExample("query", "\"StormEvents | take 5\"", "--tenant-id", "<GUID>", "--cluster-url", "https://mycluster.kusto.windows.net", "--database", "MyDb", "--format", "markdown");
        });
    }
}