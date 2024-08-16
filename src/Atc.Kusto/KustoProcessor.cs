namespace Atc.Kusto;

/// <inheritdoc />
[SuppressMessage("AsyncUsage", "AsyncFixer01:Unnecessary async/await usage", Justification = "OK.")]
public sealed class KustoProcessor : IKustoProcessor
{
    private readonly IScriptHandlerFactory factory;

    public KustoProcessor(
        IScriptHandlerFactory factory)
    {
        this.factory = factory;
    }

    /// <inheritdoc />
    public async Task ExecuteCommand(
        IKustoCommand command,
        CancellationToken cancellationToken)
        => await factory
            .Create(command)
            .Execute(cancellationToken);

    /// <inheritdoc />
    public async Task<T?> ExecuteQuery<T>(
        IKustoQuery<T> query,
        CancellationToken cancellationToken)
        => await factory
            .Create(query)
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
                continuationToken)
            .Execute(cancellationToken);
}