#pragma warning disable ATC220 // Moving to GlobalUsings causes ILogger<> ambiguity with Kusto.Cloud.Platform.Utils.ILogger<T>
using Kusto.Cloud.Platform.Utils;
#pragma warning restore ATC220

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
/// <see cref="IKustoStreamingQuery{T}.MapRecord" />.
/// </typeparam>
internal sealed partial class BufferedStreamingQueryHandler<T> : IScriptHandler<StreamingQueryResult<T>?>
{
    private readonly ICslAdminProvider? adminProvider;
    private readonly ICslQueryProvider queryProvider;
    private readonly IKustoStreamingQuery<T> query;
    private readonly JsonSerializerOptions serializerOptions;
    private readonly AtcStreamingQueryOptions streamingQueryOptions;

    public BufferedStreamingQueryHandler(
        Microsoft.Extensions.Logging.ILogger<BufferedStreamingQueryHandler<T>> logger,
        ICslAdminProvider adminProvider,
        ICslQueryProvider queryProvider,
        IKustoStreamingQuery<T> query,
        JsonSerializerOptions serializerOptions,
        AtcStreamingQueryOptions streamingQueryOptions)
    {
        this.logger = logger;
        this.adminProvider = adminProvider;
        this.queryProvider = queryProvider;
        this.query = query;
        this.serializerOptions = serializerOptions;
        this.streamingQueryOptions = streamingQueryOptions;
    }

    // Backward-compatible overload (pre-cancellation change)
    public BufferedStreamingQueryHandler(
        Microsoft.Extensions.Logging.ILogger<BufferedStreamingQueryHandler<T>> logger,
        ICslQueryProvider queryProvider,
        IKustoStreamingQuery<T> query,
        JsonSerializerOptions serializerOptions,
        AtcStreamingQueryOptions streamingQueryOptions)
        : this(logger, adminProvider: null!, queryProvider, query, serializerOptions, streamingQueryOptions)
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
    /// maps the <c>PrimaryResult</c> rows with <see cref="IKustoStreamingQuery{T}.MapRecord" />,
    /// and returns a <see cref="StreamingQueryResult{T}" /> whose <see cref="StreamingQueryResult{T}.Rows" />
    /// can be enumerated by the caller.
    /// </summary>
    /// <param name="cancellationToken">Token to observe while waiting for completion.</param>
    /// <returns>
    /// A task that completes with a populated <see cref="StreamingQueryResult{T}" />.
    /// If execution fails <see cref="StreamingQueryResult{T}.Completion" /> will reflect the error.
    /// </returns>
    [SuppressMessage("Design", "MA0051:Method Length", Justification = "OK")]
    public async Task<StreamingQueryResult<T>?> Execute(
        CancellationToken cancellationToken)
    {
        using var activity = KustoDiagnostics.Source.StartActivity(
            KustoDiagnostics.ActivityNames.StreamingQuery,
            ActivityKind.Client,
            default(ActivityContext));

        var queryText = query.GetQueryText();

        activity?.SetTag(KustoDiagnostics.TagNames.DbStatement, queryText);

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
            await ProcessFramesAsync(queryText, clientRequestProperties, channel, result, activity, cancellationToken);
        }

        return result;
    }

    [SuppressMessage("Design", "MA0051:Method Length", Justification = "OK")]
    private async Task ProcessFramesAsync(
        string queryText,
        ClientRequestProperties clientRequestProperties,
        Channel<T> channel,
        StreamingQueryResult<T> result,
        System.Diagnostics.Activity? activity,
        CancellationToken cancellationToken)
    {
        var tablesById = new Dictionary<int, FrameTable>();
        var primaryResultRecords = new List<FrameRecord>();

        try
        {
            // Both are disposed, also when an error occurs: once GetFrames() has been called the data
            // set no longer disposes the frames, and they hold the HTTP response open.
            using var progressiveDataSet = await queryProvider.ExecuteQueryV2Async(
                databaseName: null,
                queryText,
                clientRequestProperties,
                cancellationToken);

            if (progressiveDataSet is null)
            {
                LogProgressiveDataSetIsNull(queryText, clientRequestProperties.ClientRequestId);
                return;
            }

            using var frames = progressiveDataSet.GetFrames();

            await foreach (var frame in frames.ToAsyncEnumerable(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                switch (frame.FrameType)
                {
                    case FrameType.DataSetHeader:
                        HandleDataSetHeader(frame, result);
                        break;
                    case FrameType.TableHeader:
                        HandleTableHeader(result, frame, tablesById);
                        break;
                    case FrameType.TableFragment:
                        HandleTableFragment(frame, tablesById, primaryResultRecords);
                        break;
                    case FrameType.TableCompletion:
                        HandleTableCompletion(frame, result, tablesById);
                        break;
                    case FrameType.TableProgress:
                        HandleTableProgress(frame);
                        break;
                    case FrameType.DataTable:
                        HandleDataTable(frame, tablesById, primaryResultRecords);
                        break;
                    case FrameType.DataSetCompletion:
                        HandleDataSetCompletion(frame, result);
                        break;
                    case FrameType.LastInvalid:
                    default:
                        break;
                }
            }

            // Once all frames are processed, map the PrimaryResult rows and hand them to the consumer.
            foreach (var record in primaryResultRecords)
            {
                var mapped = query.MapRecord(record, serializerOptions);

                if (mapped is not null)
                {
                    await channel.Writer.WriteAsync(mapped, cancellationToken);
                }
            }

            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex) when (CancellationExceptionUtilities.IsCancellation(ex, cancellationToken))
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw CancellationExceptionUtilities.NormalizeCancellationException(ex, cancellationToken);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

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
    /// Keeps the table by TableId and optionally adds a <see cref="KustoTableSchema" /> to <paramref name="result" />.
    /// </summary>
    private void HandleTableHeader(
        StreamingQueryResult<T> result,
        ProgressiveDataSetFrame frame,
        Dictionary<int, FrameTable> tablesById)
    {
        var tableHeaderFrame = (ProgressiveDataSetDataTableSchemaFrame)frame;
        var table = FrameTable.FromSchemaFrame(tableHeaderFrame);
        tablesById.TryAdd(tableHeaderFrame.TableId, table);

        if (!streamingQueryOptions.OptionalFrames.HasFlag(FrameHeaders.TableHeader))
        {
            return;
        }

        var kustoColumns = new List<KustoColumn>();
        for (var i = 0; i < table.ColumnNames.Count; i++)
        {
            kustoColumns.Add(new KustoColumn(table.ColumnNames[i], table.ColumnTypes[i].Name));
        }

        result.TableSchemas!.Add(
            new KustoTableSchema(
                table.Name,
                kustoColumns));
    }

    /// <summary>
    /// Keeps the <c>PrimaryResult</c> records of the fragment, with each value converted to its column type.
    /// A <c>DataReplace</c> fragment first drops the records the table sent so far.
    /// </summary>
    /// <exception cref="NotSupportedException">Thrown for a <c>DataRemove</c> fragment.</exception>
    private void HandleTableFragment(
        ProgressiveDataSetFrame frame,
        Dictionary<int, FrameTable> tablesById,
        List<FrameRecord> primaryResultRecords)
    {
        var tableFragmentFrame = (ProgressiveDataSetDataTableFragmentFrame)frame;
        if (!tablesById.TryGetValue(tableFragmentFrame.TableId, out var table))
        {
            LogReceivedTableFragmentForUnknownTable(tableFragmentFrame.TableId);
            return;
        }

        if (table.Kind == WellKnownDataSet.PrimaryResult)
        {
            switch (tableFragmentFrame.FrameSubType)
            {
                case TableFragmentType.DataAppend:
                    break;
                case TableFragmentType.DataReplace:
                    // Rows are only handed out after the last frame, so the replace can be applied:
                    // drop what this table sent so far and keep the new rows instead.
                    primaryResultRecords.RemoveAll(record => record.Table == table);
                    break;
                default:
                    // DataRemove is not documented by Microsoft and has never been seen: refuse clearly instead of guessing.
                    throw new NotSupportedException(
                        $"Kusto sent a progressive '{tableFragmentFrame.FrameSubType}' update for table '{table.Name}', " +
                        $"which Atc.Kusto doesn't support. Turn off {nameof(AtcStreamingQueryOptions.ProgressiveEnabled)} for this query.");
            }
        }

        var values = new object[tableFragmentFrame.FieldCount];
        while (tableFragmentFrame.GetNextRecord(values))
        {
            if (table.Kind == WellKnownDataSet.PrimaryResult)
            {
                primaryResultRecords.Add(table.CreateRecord(values));
            }
        }
    }

    /// <summary>
    /// Adds completion info for a single table.
    /// </summary>
    private void HandleTableCompletion(
        ProgressiveDataSetFrame frame,
        StreamingQueryResult<T> result,
        Dictionary<int, FrameTable> tablesById)
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
            TableName = tablesById[tableCompletionFrame.TableId].Name,
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
    /// Reads a <c>DataTable</c> frame, keeps the table by TableId and keeps its <c>PrimaryResult</c> records.
    /// This frame represents one data table (in all, when progressive results are not used or there's no need for multiple-frames-per-table).
    /// There are usually multiple such tables in the response, differentiated by purpose (TableKind).
    /// </summary>
    private static void HandleDataTable(
        ProgressiveDataSetFrame frame,
        Dictionary<int, FrameTable> tablesById,
        List<FrameRecord> primaryResultRecords)
    {
        var dataTableFrame = (ProgressiveDataSetDataTableFrame)frame;
        var table = FrameTable.FromDataTableFrame(dataTableFrame);
        tablesById[dataTableFrame.TableId] = table; // Always update the table with the new data

        // Every table, also the ones we don't keep, is read to the end and then closed: the SDK can't move
        // to the next frame before that, and closing a table without reading it silently ends the result.
        var reader = dataTableFrame.TableData;
        while (reader.Read())
        {
            if (table.Kind != WellKnownDataSet.PrimaryResult)
            {
                continue;
            }

            var values = new object[reader.FieldCount];
            reader.GetValues(values);
            primaryResultRecords.Add(new FrameRecord(table, values));
        }

        reader.Close();
    }

    /// <summary>
    /// Captures data-set completion (success / error / cancel) based on this last frame in the data set.
    /// It provides information on the overall success of the query:
    /// Whether there were any errors, whether it got cancelled mid-stream,
    /// and what exceptions were raised if either is true.
    /// The table completions collected before are kept.
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

        result.Completion ??= new KustoResultCompletion();
        result.Completion.HasErrors = dataSetCompletionFrame.HasErrors;
        result.Completion.ErrorMessage = errorMessage;
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