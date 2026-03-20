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

            config.AddBranch("database", database =>
            {
                database.SetDescription("Inspect databases on a Kusto cluster");

                database.AddCommand<DatabaseListCommand>("list")
                    .WithDescription("List databases in a cluster")
                    .WithExample("database", "list", "--tenant-id", "<GUID>", "--cluster-url", "https://mycluster.kusto.windows.net")
                    .WithExample("database", "list", "--filter", "^prod", "--tenant-id", "<GUID>", "--cluster-url", "https://mycluster.kusto.windows.net");

                database.AddCommand<DatabaseShowCommand>("show")
                    .WithDescription("Show details for a database")
                    .WithExample("database", "show", "MyDb", "--tenant-id", "<GUID>", "--cluster-url", "https://mycluster.kusto.windows.net");
            });

            config.AddBranch("table", table =>
            {
                table.SetDescription("Browse tables in a Kusto database");

                table.AddCommand<TableListCommand>("list")
                    .WithDescription("List tables in a database")
                    .WithExample("table", "list", "--tenant-id", "<GUID>", "--cluster-url", "https://mycluster.kusto.windows.net", "--database", "MyDb")
                    .WithExample("table", "list", "--filter", "Storm$", "--take", "10", "--tenant-id", "<GUID>", "--cluster-url", "https://mycluster.kusto.windows.net", "--database", "MyDb");

                table.AddCommand<TableShowCommand>("show")
                    .WithDescription("Show table schema and column details")
                    .WithExample("table", "show", "StormEvents", "--tenant-id", "<GUID>", "--cluster-url", "https://mycluster.kusto.windows.net", "--database", "MyDb");
            });
        });
    }
}