namespace Atc.Kusto;

/// <inheritdoc />
[SuppressMessage("AsyncUsage", "AsyncFixer01:Unnecessary async/await usage", Justification = "OK.")]
public sealed class KustoProcessor : IKustoProcessor
{
    private readonly IScriptHandlerFactory factory;

    public KustoProcessor(
        IScriptHandlerFactory factory,
        string? connectionName,
        string? databaseName)
    {
        this.factory = factory;
        ConnectionName = connectionName;
        DatabaseName = databaseName;
    }

    public string? ConnectionName { get; }

    public string? DatabaseName { get; }

    /// <inheritdoc />
    public async Task ExecuteCommand(
        IKustoCommand command,
        CancellationToken cancellationToken)
        => await factory
            .Create(
                command,
                ConnectionName,
                DatabaseName)
            .Execute(cancellationToken);

    /// <inheritdoc />
    public async Task<T?> ExecuteQuery<T>(
        IKustoQuery<T> query,
        CancellationToken cancellationToken)
        => await factory
            .Create(
                query,
                ConnectionName,
                DatabaseName)
            .Execute(cancellationToken);

    /// <inheritdoc />
    public async Task<PagedResult<T>?> ExecutePagedQuery<T>(
        IKustoQuery<IReadOnlyList<T>> query,
        string? sessionId,
        int pageSize,
        string? continuationToken,
        CancellationToken cancellationToken)
        => await factory
            .Create(
                query,
                sessionId,
                pageSize,
                continuationToken,
                ConnectionName,
                DatabaseName)
            .Execute(cancellationToken);
}