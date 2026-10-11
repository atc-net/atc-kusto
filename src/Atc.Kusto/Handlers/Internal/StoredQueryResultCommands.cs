namespace Atc.Kusto.Handlers.Internal;

/// <summary>
/// Builds the Kusto texts that create and read the stored query result behind a paged query.
/// </summary>
internal static class StoredQueryResultCommands
{
    /// <summary>
    /// Creates the command that runs <paramref name="query"/> and stores its result under <paramref name="queryId"/>
    /// for an hour, with the rows numbered from 1 in a <c>row_number</c> column.
    /// </summary>
    /// <param name="query">The paged query.</param>
    /// <param name="queryId">The queryId (the stored query result name).</param>
    /// <param name="pageSize">The number of rows the command returns, which is the first page.</param>
    /// <returns>The <c>.set-or-replace stored_query_result</c> command.</returns>
    public static string CreateSetOrReplace(
        IKustoScript query,
        string queryId,
        int pageSize)
    {
        var header = string.Create(
            CultureInfo.InvariantCulture,
            $".set-or-replace stored_query_result ['{queryId}'] with (previewCount = {pageSize}, expiresAfter = 1h) <|");
        const string footer = "| serialize row_number = row_number()";

        return $"{header}\n{query.GetQueryText().Trim(' ', '\n', '\t', ';')}\n{footer}";
    }

    /// <summary>
    /// Creates the query that reads the page after the first <paramref name="itemsReturned"/> rows.
    /// </summary>
    /// <param name="queryId">The queryId (the stored query result name).</param>
    /// <param name="itemsReturned">The number of rows returned so far.</param>
    /// <param name="pageSize">The number of rows in a page.</param>
    /// <returns>The <c>stored_query_result</c> query.</returns>
    public static string CreatePageQuery(
        string queryId,
        long itemsReturned,
        int pageSize)
        => string.Create(
            CultureInfo.InvariantCulture,
            $"stored_query_result('{queryId}') | where row_number between({itemsReturned + 1} .. {itemsReturned + pageSize})");
}