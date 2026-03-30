namespace Atc.Kusto.CLI.Extensions;

public static class CommandAppExtensions
{
    public static void ConfigureCommands(this CommandApp app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.Configure(config =>
        {
            config.AddBranch("export", ConfigureExportCommands);
            ConfigureQueryCommand(config);
            config.AddBranch("cluster", ConfigureClusterCommands);
            config.AddBranch("database", ConfigureDatabaseCommands);
            config.AddBranch("table", ConfigureTableCommands);
        });
    }

    private static void ConfigureExportCommands(
        IConfigurator<CommandSettings> export)
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
    }

    private static void ConfigureQueryCommand(IConfigurator config)
    {
        config.AddCommand<QueryCommand>("query")
            .WithDescription("Execute a KQL query against a Kusto database")
            .WithExample("query", "\"StormEvents | take 5\"", "--tenant-id", "<GUID>", "--cluster-url", "https://mycluster.kusto.windows.net", "--database", "MyDb")
            .WithExample("query", "\"StormEvents | take 5\"", "--tenant-id", "<GUID>", "--cluster", "mycluster", "--database", "MyDb")
            .WithExample("query", "--file", "myquery.kql", "--tenant-id", "<GUID>", "--cluster", "mycluster", "--database", "MyDb")
            .WithExample("query", "--file", "myquery.kql:5-10", "--tenant-id", "<GUID>", "--cluster", "mycluster", "--database", "MyDb")
            .WithExample("query", "\"StormEvents | take 5\"", "--tenant-id", "<GUID>", "--cluster", "mycluster", "--database", "MyDb", "--format", "json")
            .WithExample("query", "\"StormEvents | take 5\"", "--tenant-id", "<GUID>", "--cluster", "mycluster", "--database", "MyDb", "--format", "csv");
    }

    private static void ConfigureClusterCommands(
        IConfigurator<CommandSettings> cluster)
    {
        cluster.SetDescription("Manage saved Kusto cluster connections");

        cluster.AddCommand<ClusterListCommand>("list")
            .WithDescription("List all saved clusters")
            .WithExample("cluster", "list");

        cluster.AddCommand<ClusterShowCommand>("show")
            .WithDescription("Show details for a saved cluster")
            .WithExample("cluster", "show", "mycluster");

        cluster.AddCommand<ClusterAddCommand>("add")
            .WithDescription("Save a new cluster connection")
            .WithExample("cluster", "add", "help", "https://help.kusto.windows.net")
            .WithExample("cluster", "add", "prod", "https://prod.kusto.windows.net", "--use");

        cluster.AddCommand<ClusterRemoveCommand>("remove")
            .WithDescription("Remove a saved cluster")
            .WithExample("cluster", "remove", "mycluster");

        cluster.AddCommand<ClusterSetDefaultCommand>("set-default")
            .WithDescription("Set the default cluster")
            .WithExample("cluster", "set-default", "prod");
    }

    private static void ConfigureDatabaseCommands(
        IConfigurator<CommandSettings> database)
    {
        database.SetDescription("Inspect databases on a Kusto cluster");

        database.AddCommand<DatabaseListCommand>("list")
            .WithDescription("List databases in a cluster")
            .WithExample("database", "list", "--tenant-id", "<GUID>", "--cluster-url", "https://mycluster.kusto.windows.net")
            .WithExample("database", "list", "--filter", "^prod", "--tenant-id", "<GUID>", "--cluster-url", "https://mycluster.kusto.windows.net");

        database.AddCommand<DatabaseShowCommand>("show")
            .WithDescription("Show details for a database")
            .WithExample("database", "show", "MyDb", "--tenant-id", "<GUID>", "--cluster-url", "https://mycluster.kusto.windows.net");
    }

    private static void ConfigureTableCommands(
        IConfigurator<CommandSettings> table)
    {
        table.SetDescription("Browse tables in a Kusto database");

        table.AddCommand<TableListCommand>("list")
            .WithDescription("List tables in a database")
            .WithExample("table", "list", "--tenant-id", "<GUID>", "--cluster-url", "https://mycluster.kusto.windows.net", "--database", "MyDb")
            .WithExample("table", "list", "--filter", "Storm$", "--take", "10", "--tenant-id", "<GUID>", "--cluster-url", "https://mycluster.kusto.windows.net", "--database", "MyDb");

        table.AddCommand<TableShowCommand>("show")
            .WithDescription("Show table schema and column details")
            .WithExample("table", "show", "StormEvents", "--tenant-id", "<GUID>", "--cluster-url", "https://mycluster.kusto.windows.net", "--database", "MyDb");
    }
}