namespace Atc.Kusto.Factories.Internal;

/// <inheritdoc />
public class KustoProcessorFactory : IKustoProcessorFactory
{
    private readonly IScriptHandlerFactory factory;

    public KustoProcessorFactory(IScriptHandlerFactory factory)
    {
        this.factory = factory;
    }

    /// <inheritdoc />
    public IKustoProcessor Create(
        string? connectionName = null,
        string? databaseName = null)
        => new KustoProcessor(
            factory,
            connectionName,
            databaseName);
}