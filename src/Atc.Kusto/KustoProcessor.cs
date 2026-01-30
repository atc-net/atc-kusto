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
        => await factory
            .Create(
                command,
                ConnectionName,
                DatabaseName)
            .Execute(cancellationToken);

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
        AtcQueryOptions? options = null,
        CancellationToken cancellationToken = default)
        => await factory
            .Create(
                query,
                ConnectionName,
                DatabaseName,
                options)
            .Execute(cancellationToken);

    /// <inheritdoc />
    public async Task<PagedResult<T>?> ExecutePagedQuery<T>(
        IKustoQuery<IReadOnlyList<T>> query,
        string? sessionId,
        int? pageSize,
        string? continuationToken,
        CancellationToken cancellationToken = default)
        => pageSize is { } pageSizeValue
            ? await factory
                .Create(
                    query,
                    sessionId,
                    pageSizeValue,
                    continuationToken,
                    ConnectionName,
                    DatabaseName)
                .Execute(cancellationToken)
            : new PagedResult<T>(
                Items: await ExecuteQuery(query, options: null, cancellationToken) ?? [],
                ContinuationToken: null);

    /// <inheritdoc />
    public async Task<StreamingQueryResult<T>?> ExecuteBufferedStreamingQuery<T>(
        IKustoStreamingQuery<T> query,
        AtcStreamingQueryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
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
        AtcStreamingQueryOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
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