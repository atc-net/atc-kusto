using Kusto.Cloud.Platform.Utils;

namespace Atc.Kusto.Handlers.Internal;

/// <summary>
/// Buffers a progressive Kusto query (executed through <c>QueryV2</c>) entirely in-memory
/// and returns a <see cref="StreamingQueryResult{T}" /> that exposes
/// <list type="bullet">
///   <item><description><see cref="StreamingQueryResult{T}.Rows" /> – an <see cref="IAsyncEnumerable{T}" /> of the mapped rows from every <c>PrimaryResult</c> table.</description></item>
///   <item><description><see cref="StreamingQueryResult{T}.Header" /> – the <c>DataSetHeader</c> frame (optional).</description></item>
///   <item><description><see cref="StreamingQueryResult{T}.TableSchemas" /> – table schemas (optional).</description></item>
///   <item><description><see cref="StreamingQueryResult{T}.Completion" /> – per-table and data-set completion details (optional).</description></item>
/// </list>
/// Choose this handler when you need those metadata artefacts and the consumer can tolerate the
/// buffering cost. To stream rows immediately, prefer <see cref="StreamingQueryHandler{T}"/>.
/// </summary>
/// <typeparam name="T">
/// The CLR type returned for each row produced by
/// <see cref="IKustoStreamingQuery{T}.MapDataRow" />.
/// </typeparam>
internal sealed partial class BufferedStreamingQueryHandler<T> : IScriptHandler<StreamingQueryResult<T>?>
{
    private readonly ICslAdminProvider? adminProvider;
    private readonly ICslQueryProvider queryProvider;
    private readonly IKustoStreamingQuery<T> query;
    private readonly AtcStreamingQueryOptions streamingQueryOptions;

    public BufferedStreamingQueryHandler(
        Microsoft.Extensions.Logging.ILogger<BufferedStreamingQueryHandler<T>> logger,
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
    public BufferedStreamingQueryHandler(
        Microsoft.Extensions.Logging.ILogger<BufferedStreamingQueryHandler<T>> logger,
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
    /// Executes the query via <c>ExecuteQueryV2Async</c>, collects all progressive frames,
    /// maps the <c>PrimaryResult</c> rows with <see cref="IKustoStreamingQuery{T}.MapDataRow" />,
    /// and returns a <see cref="StreamingQueryResult{T}" /> whose <see cref="StreamingQueryResult{T}.Rows" />
    /// can be enumerated by the caller.
    /// </summary>
    /// <param name="cancellationToken">Token to observe while waiting for completion.</param>
    /// <returns>
    /// A task that completes with a populated <see cref="StreamingQueryResult{T}" />.
    /// If execution fails <see cref="StreamingQueryResult{T}.Completion" /> will reflect the error.
    /// </returns>
    [SuppressMessage("Design", "MA0051:Method Length", Justification = "OK")]
    public async Task<StreamingQueryResult<T>?> Execute(CancellationToken cancellationToken)
    {
        var queryText = query.GetQueryText();

        var clientRequestProperties = query.GetClientRequestProperties();

        clientRequestProperties.SetQueryOptions(streamingQueryOptions);

        var channel = Channel.CreateUnbounded<T>();

        var result = new StreamingQueryResult<T>
        {
            Rows = ReadRowsAsync(channel.Reader, cancellationToken),
            Header = null,
            Completion = null,
        };

        using (CslAdminProviderExtensions.ShouldEnableServerSideCancellation(adminProvider, streamingQueryOptions.EnableServerSideCancellation)
                   ? adminProvider!.RegisterKustoServerSideCancellation(
                       logger,
                       databaseName: null,
                       clientRequestProperties,
                       cancellationToken)
                   : null)
        {
            await ProcessFramesAsync(queryText, clientRequestProperties, channel, result, cancellationToken);
        }

        return result;
    }

    private async Task ProcessFramesAsync(
        string queryText,
        ClientRequestProperties clientRequestProperties,
        Channel<T> channel,
        StreamingQueryResult<T> result,
        CancellationToken cancellationToken)
    {
        var tablesById = new Dictionary<int, DataTable>();
        var tableKindsById = new Dictionary<int, WellKnownDataSet>();

        try
        {
            var progressiveDataSet = await queryProvider.ExecuteQueryV2Async(
                databaseName: null,
                queryText,
                clientRequestProperties,
                cancellationToken);

            if (progressiveDataSet is null)
            {
                LogProgressiveDataSetIsNull(queryText, clientRequestProperties.ClientRequestId);
                return;
            }

            await foreach (var frame in progressiveDataSet.GetFrames().ToAsyncEnumerable(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                switch (frame.FrameType)
                {
                    case FrameType.DataSetHeader:
                        HandleDataSetHeader(frame, result);
                        break;
                    case FrameType.TableHeader:
                        await HandleTableHeader(result, frame, tablesById, tableKindsById, cancellationToken);
                        break;
                    case FrameType.TableFragment:
                        HandleTableFragment(frame, tablesById);
                        break;
                    case FrameType.TableCompletion:
                        HandleTableCompletion(frame, result, tablesById);
                        break;
                    case FrameType.TableProgress:
                        HandleTableProgress(frame);
                        break;
                    case FrameType.DataTable:
                        HandleDataTable(frame, tablesById, tableKindsById);
                        break;
                    case FrameType.DataSetCompletion:
                        HandleDataSetCompletion(frame, result);
                        break;
                    case FrameType.LastInvalid:
                    default:
                        break;
                }
            }

            // Once all frames are processed, yield rows from the PrimaryResult tables.
            if (tableKindsById.ContainsValue(WellKnownDataSet.PrimaryResult))
            {
                foreach (var kvp in tablesById)
                {
                    if (tableKindsById.TryGetValue(kvp.Key, out var kind) && kind == WellKnownDataSet.PrimaryResult)
                    {
                        foreach (DataRow row in kvp.Value.Rows)
                        {
                            var mapDataRow = query.MapDataRow(row);

                            if (mapDataRow is not null)
                            {
                                await channel.Writer.WriteAsync(mapDataRow, cancellationToken);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex) when (CancellationExceptionUtilities.IsCancellationException(ex))
        {
            throw CancellationExceptionUtilities.NormalizeCancellationException(ex, cancellationToken);
        }
        catch (Exception ex)
        {
            if (result.Completion is null)
            {
                result.Completion = new KustoResultCompletion
                {
                    HasErrors = true,
                    ErrorMessage = ex.Message,
                };
            }
            else
            {
                result.Completion.HasErrors = true;
                result.Completion.ErrorMessage = ex.Message;
            }

            LogUnhandledException(ex, clientRequestProperties.ClientRequestId, queryText);
        }
        finally
        {
            channel.Writer.Complete();
        }
    }

    /// <summary>
    /// Stores data-set header information when <see cref="FrameHeaders.DataSetHeader" /> was requested.
    /// </summary>
    private void HandleDataSetHeader(
        ProgressiveDataSetFrame frame,
        StreamingQueryResult<T> result)
    {
        if (!streamingQueryOptions.OptionalFrames.HasFlag(FrameHeaders.DataSetHeader))
        {
            return;
        }

        var dataSetHeaderFrame = (ProgressiveDataSetHeaderFrame)frame;

        result.Header = new KustoDataSetHeader
        {
            Version = dataSetHeaderFrame.Version,
            IsProgressive = dataSetHeaderFrame.IsProgressive,
        };
    }

    /// <summary>
    /// Caches table schema / kind and optionally adds a <see cref="KustoTableSchema" /> to <paramref name="result" />.
    /// </summary>
    private async Task HandleTableHeader(
        StreamingQueryResult<T> result,
        ProgressiveDataSetFrame frame,
        Dictionary<int, DataTable> tablesById,
        Dictionary<int, WellKnownDataSet> tableKindsById,
        CancellationToken cancellationToken)
    {
        var tableHeaderFrame = (ProgressiveDataSetDataTableSchemaFrame)frame;
        var dataTable = tableHeaderFrame.ToEmptyDataTable();
        dataTable.TableName = tableHeaderFrame.TableName;
        tablesById.TryAdd(tableHeaderFrame.TableId, dataTable);
        tableKindsById.TryAdd(tableHeaderFrame.TableId, tableHeaderFrame.TableKind);

        if (!streamingQueryOptions.OptionalFrames.HasFlag(FrameHeaders.TableHeader))
        {
            return;
        }

        var kustoColumns = await dataTable.Columns
            .Cast<DataColumn>()
            .Select(col => new KustoColumn(col.ColumnName, col.DataType.Name))
            .ToListAsync(cancellationToken);

        result.TableSchemas!.Add(
            new KustoTableSchema(
                dataTable.TableName,
                kustoColumns));
    }

    /// <summary>
    /// Appends incoming records to the <see cref="DataTable" /> matching the fragment’s TableId.
    /// </summary>
    private void HandleTableFragment(
        ProgressiveDataSetFrame frame,
        Dictionary<int, DataTable> tablesById)
    {
        var tableFragmentFrame = (ProgressiveDataSetDataTableFragmentFrame)frame;
        if (!tablesById.TryGetValue(tableFragmentFrame.TableId, out var dt))
        {
            LogReceivedTableFragmentForUnknownTable(tableFragmentFrame.TableId);
            return;
        }

        var record = new object[tableFragmentFrame.FieldCount];
        while (tableFragmentFrame.GetNextRecord(record))
        {
            var newRow = dt.NewRow();

            // Convert frame values to match column types (Kusto sends decimals as strings in progressive frames)
            for (var i = 0; i < record.Length; i++)
            {
                newRow[i] = FrameValueTypeConverter.ConvertToColumnType(record[i], dt.Columns[i].DataType);
            }

            dt.Rows.Add(newRow);
        }
    }

    /// <summary>
    /// Adds completion info for a single table.
    /// </summary>
    private void HandleTableCompletion(
        ProgressiveDataSetFrame frame,
        StreamingQueryResult<T> result,
        Dictionary<int, DataTable> tablesById)
    {
        if (!streamingQueryOptions.OptionalFrames.HasFlag(FrameHeaders.CompletionSummary))
        {
            return;
        }

        var tableCompletionFrame = (ProgressiveDataSetTableCompletionFrame)frame;

        result.Completion ??= new KustoResultCompletion
        {
            HasErrors = false,
            ErrorMessage = null,
        };

        result.Completion.TableCompletions!.Add(new KustoTableCompletionInfo
        {
            TableName = tablesById[tableCompletionFrame.TableId].TableName,
            RowCount = tableCompletionFrame.RowCount,
            Exception = tableCompletionFrame.Exception?.ToString(),
        });
    }

    /// <summary>
    /// Writes debug output for <c>TableProgress</c> frames when requested.
    /// </summary>
    private void HandleTableProgress(ProgressiveDataSetFrame frame)
    {
        if (!streamingQueryOptions.OptionalFrames.HasFlag(FrameHeaders.CompletionSummary))
        {
            return;
        }

        var tableProgressFrame = (ProgressiveDataSetTableProgressFrame)frame;

        System.Diagnostics.Debug.WriteLine($"TableProgress: TableId={tableProgressFrame.TableId}, Progress={tableProgressFrame.TableProgress}");
    }

    /// <summary>
    /// Materialises a <c>DataTable</c> frame and stores it by TableId.
    /// This frame represents one data table (in all, when progressive results are not used or there's no need for multiple-frames-per-table).
    /// There are usually multiple such tables in the response, differentiated by purpose (TableKind).
    /// </summary>
    private void HandleDataTable(
        ProgressiveDataSetFrame frame,
        Dictionary<int, DataTable> tablesById,
        Dictionary<int, WellKnownDataSet> tableKindsById)
    {
        var dataTableFrame = (ProgressiveDataSetDataTableFrame)frame;
        var schema = dataTableFrame.TableData.GetSchemaTable();

        if (schema is null)
        {
            LogSchemaNullInDataTableDataSetFrame(
                dataTableFrame.TableId,
                dataTableFrame.TableName,
                dataTableFrame.TableKind);

            return;
        }

        var table = new DataTable(dataTableFrame.TableName);
        foreach (DataRow row in schema.Rows)
        {
            var columnType = row.Field<string?>("ColumnType");
            var columnName = row.Field<string?>("ColumnName");
            var clrType = CslType.FromCslType(columnType).GetCorrespondingClrType();
            table.Columns.Add(columnName, clrType);
        }

        table.Load(dataTableFrame.TableData);
        tablesById[dataTableFrame.TableId] = table; // Always update the table with the new data
        tableKindsById[dataTableFrame.TableId] = dataTableFrame.TableKind;
    }

    /// <summary>
    /// Captures data-set completion (success / error / cancel) based on this last frame in the data set.
    /// It provides information on the overall success of the query:
    /// Whether there were any errors, whether it got cancelled mid-stream,
    /// and what exceptions were raised if either is true.
    /// </summary>
    private void HandleDataSetCompletion(
        ProgressiveDataSetFrame frame,
        StreamingQueryResult<T> result)
    {
        if (!streamingQueryOptions.OptionalFrames.HasFlag(FrameHeaders.CompletionSummary))
        {
            return;
        }

        var dataSetCompletionFrame = (ProgressiveDataSetCompletionFrame)frame;
        if (dataSetCompletionFrame.Cancelled)
        {
            throw new TaskCanceledException();
        }

        string? errorMessage = null;
        if (dataSetCompletionFrame is { HasErrors: true, Exception: not null })
        {
            errorMessage = ExtendedString.SafeToString(dataSetCompletionFrame.Exception);
        }

        result.Completion = new KustoResultCompletion
        {
            HasErrors = dataSetCompletionFrame.HasErrors,
            ErrorMessage = errorMessage,
        };
    }

    /// <summary>
    /// Drains the internal channel and yields mapped rows to the consumer.
    /// </summary>
    private static async IAsyncEnumerable<T> ReadRowsAsync(
        ChannelReader<T> reader,
        [EnumeratorCancellation] CancellationToken ct)
    {
        while (await reader.WaitToReadAsync(ct))
        {
            while (reader.TryRead(out var item))
            {
                yield return item;
            }
        }
    }
}