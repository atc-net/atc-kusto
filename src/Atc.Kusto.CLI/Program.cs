namespace Atc.Kusto.CLI;

public static class Program
{
    public static Task<int> Main(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        args = SetHelpArgumentIfNeeded(args);

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        var consoleLoggerConfiguration = new ConsoleLoggerConfiguration();
        configuration.GetSection("ConsoleLogger").Bind(consoleLoggerConfiguration);

        ProgramCsHelper.SetMinimumLogLevelIfNeeded(args, consoleLoggerConfiguration);

        var serviceCollection = ServiceCollectionFactory.Create(consoleLoggerConfiguration);
        serviceCollection.AddTransient<IKustoSchemaExporter, KustoSchemaExporter>();

        var app = CommandAppFactory.Create(serviceCollection);
        app.ConfigureCommands();

        return app.RunAsync(args);
    }

    private static string[] SetHelpArgumentIfNeeded(string[] args)
        => args.Length == 0
            ? [CommandConstants.ArgumentShortHelp]
            : args;
}