// Run Atc.Kusto.Api.Sample first; this client calls its endpoints.
const string baseAddress = "https://localhost:46612/";
using var client = new HttpClient { BaseAddress = new Uri(baseAddress), Timeout = TimeSpan.FromMinutes(5) };
var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

await ReadAllPages(client, options);
await ReadBuffered<Customer>(client, options, "/customers/stream-with-streaming-query-result", c => $"Customer: {c.FirstName} {c.LastName}");
await ReadStream<Customer>(client, "/customers/stream", c => $"Customer: {c.FirstName} {c.LastName}");
await ReadBuffered<NycTaxiTrip>(client, options, "/nyctaxitrips/stream-with-streaming-query-result", t => $"NYC Taxi Trip: Persons: {t.PassengerCount} - Amount: {t.TotalAmount}");
await ReadStream<NycTaxiTrip>(client, "/nyctaxitrips/stream", t => $"NYC Taxi Trip: Persons: {t.PassengerCount} - Amount: {t.TotalAmount}");
await StopReadingEarly(client);

Console.WriteLine("Press Enter to exit");
Console.ReadLine();

// Follows the continuation token from page to page, and asks for the total count.
static async Task ReadAllPages(
    HttpClient client,
    JsonSerializerOptions options)
{
    Console.WriteLine("Calling /customers page by page...");

    string? continuationToken = null;
    var pages = 0;
    var items = 0;

    do
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/customers?includeTotalCount=true");
        request.Headers.Add("x-pageSize", "5000");
        if (continuationToken is not null)
        {
            request.Headers.Add("x-continuation-token", continuationToken);
        }

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var page = await response.Content.ReadFromJsonAsync<PagedResult<Customer>>(options);
        pages++;
        items += page!.Items.Count;
        Console.WriteLine($"Page {pages}: {page.Items.Count} customers ({items} of {page.TotalCount})");

        continuationToken = page.ContinuationToken;
    }
    while (continuationToken is not null);

    Console.WriteLine();
}

// Reads a buffered streaming result: the header, the table schemas, the rows and the completion.
static async Task ReadBuffered<T>(
    HttpClient client,
    JsonSerializerOptions options,
    string path,
    Func<T, string> describe)
{
    Console.WriteLine($"Calling {path}...");

    using var response = await client.GetAsync(path);
    if (!response.IsSuccessStatusCode)
    {
        // The API returns 502 when the buffered query reported an error in its completion.
        Console.WriteLine($"The query failed ({(int)response.StatusCode}): {await response.Content.ReadAsStringAsync()}");
        Console.WriteLine();
        return;
    }

    var result = await response.Content.ReadFromJsonAsync<StreamingQueryResult<T>>(options);

    Console.WriteLine(result!.Header is { } header
        ? $"Header: Version={header.Version}, Progressive={header.IsProgressive}"
        : "No header received.");

    foreach (var schema in result.TableSchemas)
    {
        Console.WriteLine($"Schema for table '{schema.TableName}': {string.Join(", ", schema.Columns.Select(c => $"{c.Name} ({c.Type})"))}");
    }

    PrintRows(result.Rows, describe);

    Console.WriteLine(result.Completion is { } completion
        ? $"Completion: HasErrors={completion.HasErrors} {completion.ErrorMessage}"
        : "No completion summary received.");
    Console.WriteLine();
}

// Reads a directly streamed result: the rows arrive while the API is still reading them from Kusto.
static async Task ReadStream<T>(
    HttpClient client,
    string path,
    Func<T, string> describe)
{
    Console.WriteLine($"Calling {path}...");

    var timestamp = Stopwatch.GetTimestamp();
    var rows = new List<T>();
    await foreach (var row in client.GetFromJsonAsAsyncEnumerable<T>(path))
    {
        rows.Add(row!);
    }

    PrintRows(rows, describe);
    Console.WriteLine($"Read in {Stopwatch.GetElapsedTime(timestamp)}");
    Console.WriteLine();
}

// Stops reading a stream after 1000 rows. The connection closes, ASP.NET Core cancels the request,
// and Atc.Kusto cancels the query on the cluster, without an error in the API's log.
static async Task StopReadingEarly(HttpClient client)
{
    Console.WriteLine("Calling /nyctaxitrips/stream and stopping after 1000 rows...");

    using var cts = new CancellationTokenSource();
    var rows = 0;

    try
    {
        await foreach (var trip in client.GetFromJsonAsAsyncEnumerable<NycTaxiTrip>("/nyctaxitrips/stream", cts.Token))
        {
            if (++rows == 1000)
            {
                await cts.CancelAsync();
            }
        }
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine($"Stopped after {rows} rows.");
    }

    Console.WriteLine();
}

// Prints the first rows and the number of rows, instead of every row.
static void PrintRows<T>(
    IReadOnlyCollection<T> rows,
    Func<T, string> describe)
{
    foreach (var row in rows.Take(3))
    {
        Console.WriteLine(describe(row));
    }

    Console.WriteLine($"... {rows.Count} rows");
}