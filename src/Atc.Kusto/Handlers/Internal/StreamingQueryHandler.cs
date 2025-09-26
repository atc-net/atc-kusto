namespace Atc.Kusto.Handlers.Internal;

/// <summary>
/// Streams data from a progressive Kusto query (executed through <c>QueryV2</c>) directly to the consumer
/// as rows are processed, without buffering the entire result set in memory.
/// <para>
/// This handler yields mapped rows as soon as they become available, making it ideal for
/// processing large datasets where low-latency access to the first results is important.
/// </para>
/// <para>
/// Unlike <see cref="BufferedStreamingQueryHandler{T}"/>, this handler does not provide
/// access to metadata like table schemas or completion information. For access to those
/// artifacts, use <see cref="BufferedStreamingQueryHandler{T}"/> instead.
/// </para>
/// </summary>
/// <typeparam name="T">The type of objects streamed from the query results.</typeparam>
internal sealed partial class StreamingQueryHandler<T> : IStreamingScriptHandler<T?>
{
    private readonly ICslAdminProvider? adminProvider;
    private readonly ICslQueryProvider queryProvider;
    private readonly IKustoStreamingQuery<T> query;
    private readonly AtcStreamingQueryOptions streamingQueryOptions;

    public StreamingQueryHandler(
        ILogger<StreamingQueryHandler<T>> logger,
        ICslAdminProvider adminProvider,
        ICslQueryProvider queryProvider,
        IKustoStreamingQuery<T> query,
        AtcStreamingQueryOptions streamingQueryOptions)
    {
        this.logger = logger;
        this.adminProvider = adminProvider;
        this.queryProvider = queryProvider;
        this.query = query;
        this.streamingQueryOptions = streamingQueryOptions;
    }

    // Backward-compatible overload (pre-cancellation change)
    public StreamingQueryHandler(
        ILogger<StreamingQueryHandler<T>> logger,
        ICslQueryProvider queryProvider,
        IKustoStreamingQuery<T> query,
        AtcStreamingQueryOptions streamingQueryOptions)
        : this(logger, adminProvider: null!, queryProvider, query, streamingQueryOptions)
    {
        if (streamingQueryOptions.EnableServerSideCancellation)
        {
            throw new ArgumentException(
                "Server-side cancellation cannot be enabled when adminProvider is not supplied.",
                nameof(streamingQueryOptions));
        }

        adminProvider = null;
    }

    /// <summary>
    /// Executes the Kusto query asynchronously using QueryV2 and streams back mapped rows as soon as they become available>.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    [SuppressMessage("Design", "MA0051:Method Length", Justification = "OK")]
    public async IAsyncEnumerable<T> Execute([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var queryText = query.GetQueryText();

        var clientRequestProperties = query.GetClientRequestProperties();

        clientRequestProperties.SetQueryOptions(streamingQueryOptions);

        using var serverSideCancellationRegistration = CancellationTokenKustoExtensions.ShouldEnableServerSideCancellation(adminProvider, streamingQueryOptions.EnableServerSideCancellation)
            ? adminProvider!.RegisterKustoServerSideCancellation(
                logger,
                databaseName: null,
                clientRequestProperties,
                cancellationToken)
            : null;

        var progressiveDataSet = await queryProvider.ExecuteQueryV2Async(
            databaseName: null,
            queryText,
            clientRequestProperties,
            cancellationToken);

        var tablesById = new Dictionary<int, DataTable>();
        var tableKindsById = new Dictionary<int, WellKnownDataSet>();

        await foreach (var frame in progressiveDataSet.GetFrames().ToAsyncEnumerable(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (frame.FrameType)
            {
                case FrameType.TableHeader:
                    HandleTableHeader(frame, tablesById, tableKindsById);
                    break;
                case FrameType.TableFragment:
                    await foreach (var row in HandleTableFragment(frame, tablesById, tableKindsById, cancellationToken))
                    {
                        yield return row;
                    }

                    break;
                case FrameType.DataTable:
                    await foreach (var row in HandleDataTable(frame, tablesById, tableKindsById, cancellationToken))
                    {
                        yield return row;
                    }

                    break;
                case FrameType.DataSetHeader:
                case FrameType.TableCompletion:
                case FrameType.TableProgress:
                case FrameType.DataSetCompletion:
                case FrameType.LastInvalid:
                default:
                    break;
            }
        }
    }

    /// <summary>
    /// Contains the ID, name, kind and schema for a table being returned from ADX.
    /// The schema and kind should be cached by ID so that they can be associated with the table rows later
    /// </summary>
    /// <param name="frame">The frame.</param>
    /// <param name="tablesById">The tables by identifier.</param>
    /// <param name="tableKindsById">The table kinds by identifier.</param>
    private static void HandleTableHeader(
        ProgressiveDataSetFrame frame,
        Dictionary<int, DataTable> tablesById,
        Dictionary<int, WellKnownDataSet> tableKindsById)
    {
        var tableHeaderFrame = (ProgressiveDataSetDataTableSchemaFrame)frame;
        var dataTable = tableHeaderFrame.ToEmptyDataTable();
        dataTable.TableName = tableHeaderFrame.TableName;
        tablesById.Add(tableHeaderFrame.TableId, dataTable);
        tableKindsById.Add(tableHeaderFrame.TableId, tableHeaderFrame.TableKind);
    }

    /// <summary>
    /// Contains the table ID, field count, frame subtype, and a subset of data for the associated data table.
    /// The records for the table can be accessed via the ProgressiveDataSetDataTableFragmentFrame.GetNextRecord method
    /// </summary>
    /// <param name="frame">The frame.</param>
    /// <param name="tablesById">The tables by identifier.</param>
    private async IAsyncEnumerable<T> HandleTableFragment(
        ProgressiveDataSetFrame frame,
        Dictionary<int, DataTable> tablesById,
        Dictionary<int, WellKnownDataSet> tableKindsById,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var tableFragmentFrame = (ProgressiveDataSetDataTableFragmentFrame)frame;
        if (!tablesById.TryGetValue(tableFragmentFrame.TableId, out var dt))
        {
            LogReceivedTableFragmentForUnknownTable(tableFragmentFrame.TableId);
            yield break;
        }

        var tableKind = tableKindsById[tableFragmentFrame.TableId];
        var record = new object[tableFragmentFrame.FieldCount];

        while (tableFragmentFrame.GetNextRecord(record))
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Create a new row from the known schema.
            var newRow = dt.NewRow();
            newRow.ItemArray = (object[])record.Clone(); // ???
            dt.Rows.Add(newRow);

            if (tableKind == WellKnownDataSet.PrimaryResult)
            {
                var mapped = query.MapDataRow(newRow);
                if (mapped is not null)
                {
                    yield return mapped;
                }
            }

            await Task.Yield();
        }
    }

    /// <summary>
    /// This frame represents one data table (in all, when progressive results are not used or there's no need for multiple-frames-per-table).
    /// There are usually multiple such tables in the response, differentiated by purpose (TableKind).
    /// </summary>
    /// <param name="frame">The frame.</param>
    /// <param name="tablesById">The tables by identifier.</param>
    /// <param name="tableKindsById">The table kinds by identifier.</param>
    private async IAsyncEnumerable<T> HandleDataTable(
        ProgressiveDataSetFrame frame,
        Dictionary<int, DataTable> tablesById,
        Dictionary<int, WellKnownDataSet> tableKindsById,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var dataTableFrame = (ProgressiveDataSetDataTableFrame)frame;
        var schema = dataTableFrame.TableData.GetSchemaTable();

        if (schema is null)
        {
            LogSchemaNullInDataTableDataSetFrame(
                dataTableFrame.TableId,
                dataTableFrame.TableName,
                dataTableFrame.TableKind);

            yield break;
        }

        var dt = new DataTable(dataTableFrame.TableName);
        foreach (DataRow row in schema.Rows)
        {
            var columnType = row.Field<string?>("ColumnType");
            var columnName = row.Field<string?>("ColumnName");
            var clrType = CslType.FromCslType(columnType).GetCorrespondingClrType();
            dt.Columns.Add(columnName, clrType);
        }

        dt.Load(dataTableFrame.TableData);
        tablesById[dataTableFrame.TableId] = dt; // Always update the table with the new data
        tableKindsById[dataTableFrame.TableId] = dataTableFrame.TableKind;

        if (dataTableFrame.TableKind != WellKnownDataSet.PrimaryResult)
        {
            yield break;
        }

        foreach (DataRow row in dt.Rows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var mapped = query.MapDataRow(row);
            if (mapped is not null)
            {
                yield return mapped;
            }

            await Task.Yield();
        }
    }
}