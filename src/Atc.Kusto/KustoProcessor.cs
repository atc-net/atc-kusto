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
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await factory
            .Create(
                command,
                ConnectionName,
                DatabaseName)
            .Execute(cancellationToken);
    }

    /// <inheritdoc />
    public Task<T?> ExecuteQuery<T>(
        IKustoQuery<T> query,
        CancellationToken cancellationToken = default)
        => ExecuteQuery(
            query,
            options: null,
            cancellationToken);

    /// <inheritdoc />
    public async Task<T?> ExecuteQuery<T>(
        IKustoQuery<T> query,
        AtcQueryOptions? options,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await factory
            .Create(
                query,
                ConnectionName,
                DatabaseName,
                options)
            .Execute(cancellationToken);
    }

    /// <inheritdoc />
    public Task<PagedResult<T>?> ExecutePagedQuery<T>(
        IKustoQuery<IReadOnlyList<T>> query,
        string? sessionId,
        int? pageSize,
        string? continuationToken,
        CancellationToken cancellationToken = default)
        => ExecutePagedQuery(
            query,
            sessionId,
            pageSize,
            continuationToken,
            options: null,
            cancellationToken);

    /// <inheritdoc />
    public async Task<PagedResult<T>?> ExecutePagedQuery<T>(
        IKustoQuery<IReadOnlyList<T>> query,
        string? sessionId,
        int? pageSize,
        string? continuationToken,
        AtcPagedQueryOptions? options,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // A page size of 0 would return empty pages forever.
        if (pageSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "The page size must be at least 1.");
        }

        if (pageSize is { } pageSizeValue)
        {
            return await factory
                .Create(
                    query,
                    sessionId,
                    pageSizeValue,
                    continuationToken,
                    ConnectionName,
                    DatabaseName,
                    options)
                .Execute(cancellationToken);
        }

        // No page size: one page with every item.
        var items = await ExecuteQuery(query, options: null, cancellationToken) ?? [];

        return new PagedResult<T>(items, ContinuationToken: null)
        {
            TotalCount = options?.IncludeTotalCount == true ? items.Count : null,
        };
    }

    /// <inheritdoc />
    public async Task<StreamingQueryResult<T>?> ExecuteBufferedStreamingQuery<T>(
        IKustoStreamingQuery<T> query,
        AtcStreamingQueryOptions? options,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        options ??= new AtcStreamingQueryOptions();

        var localOptions = new AtcStreamingQueryOptions
        {
            QueryTimeout = options.QueryTimeout,
            QueryTakeMaxRecords = options.QueryTakeMaxRecords,
            NoTruncation = options.NoTruncation,
            TruncationMaxRecords = options.TruncationMaxRecords,
            TruncationMaxSize = options.TruncationMaxSize,
            EnableServerSideCancellation = options.EnableServerSideCancellation,
            ProgressiveEnabled = options.ProgressiveEnabled,
            OptionalFrames = options.OptionalFrames == FrameHeaders.None
                ? FrameHeaders.All
                : options.OptionalFrames,
        };

        return await factory
            .CreateBuffered(
                query,
                ConnectionName,
                DatabaseName,
                localOptions)
            .Execute(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<StreamingQueryResult<T>?> ExecuteBufferedStreamingQuery<T>(
        IKustoStreamingQuery<T> query,
        CancellationToken cancellationToken = default)
        => await ExecuteBufferedStreamingQuery(
            query,
            new AtcStreamingQueryOptions(),
            cancellationToken);

    /// <inheritdoc />
    public async IAsyncEnumerable<T> ExecuteStreamingQuery<T>(
        IKustoStreamingQuery<T> query,
        AtcStreamingQueryOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        options ??= new AtcStreamingQueryOptions();

        var localOptions = new AtcStreamingQueryOptions
        {
            QueryTimeout = options.QueryTimeout,
            QueryTakeMaxRecords = options.QueryTakeMaxRecords,
            NoTruncation = options.NoTruncation,
            TruncationMaxRecords = options.TruncationMaxRecords,
            TruncationMaxSize = options.TruncationMaxSize,
            EnableServerSideCancellation = options.EnableServerSideCancellation,
            ProgressiveEnabled = options.ProgressiveEnabled,
            OptionalFrames = FrameHeaders.None,
        };

        var stream = factory
            .Create(query, ConnectionName, DatabaseName, localOptions)
            .Execute(cancellationToken)
            .NormalizeCancellationExceptions(cancellationToken);

        await foreach (var row in stream.WithCancellation(cancellationToken))
        {
            if (row is not null)
            {
                yield return row;
            }
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<T> ExecuteStreamingQuery<T>(
        IKustoStreamingQuery<T> query,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var row in ExecuteStreamingQuery(query, options: null, cancellationToken))
        {
            yield return row;
        }
    }
}