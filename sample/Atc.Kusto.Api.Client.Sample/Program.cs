const string baseAddress = "https://localhost:46612/";
using var client = new HttpClient { BaseAddress = new Uri(baseAddress), Timeout = TimeSpan.FromMinutes(2) };
var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

Console.WriteLine("Calling /customers/stream-with-streaming-query-result endpoint...");
var responseWithStreamingQueryResult = await client.GetAsync("/customers/stream-with-streaming-query-result", CancellationToken.None);
responseWithStreamingQueryResult.EnsureSuccessStatusCode();

var resultWithStreamingQueryResult = await responseWithStreamingQueryResult.Content.ReadFromJsonAsync<StreamingQueryResult<Customer>>(options);
if (resultWithStreamingQueryResult is not null)
{
    if (resultWithStreamingQueryResult.Header is not null)
    {
        Console.WriteLine($"Header: Version={resultWithStreamingQueryResult.Header.Version}, Progressive={resultWithStreamingQueryResult.Header.IsProgressive}");
    }
    else
    {
        Console.WriteLine("No header received.");
    }

    if (resultWithStreamingQueryResult.TableSchemas.Any())
    {
        foreach (var schema in resultWithStreamingQueryResult.TableSchemas)
        {
            Console.WriteLine($"Schema for table '{schema.TableName}':");
            foreach (var col in schema.Columns)
            {
                Console.WriteLine($"\tColumn: {col.Name} (Type: {col.Type})");
            }
        }
    }
    else
    {
        Console.WriteLine("No table schema information received.");
    }

    Console.WriteLine("Streaming rows (with streaming query result):");
    foreach (var customer in resultWithStreamingQueryResult.Rows)
    {
        Console.WriteLine($"Customer: {customer.FirstName} {customer.LastName}");
    }

    if (resultWithStreamingQueryResult.Completion is not null)
    {
        Console.WriteLine($"Completion: HasErrors={resultWithStreamingQueryResult.Completion.HasErrors}");
        if (!string.IsNullOrEmpty(resultWithStreamingQueryResult.Completion.ErrorMessage))
        {
            Console.WriteLine($"Error Message: {resultWithStreamingQueryResult.Completion.ErrorMessage}");
        }
    }
    else
    {
        Console.WriteLine("No completion summary received.");
    }
}

Console.WriteLine();

Console.WriteLine("Calling /customers/stream endpoint...");

await foreach (var customer in client.GetFromJsonAsAsyncEnumerable<Customer>("/customers/stream", CancellationToken.None))
{
    if (customer is not null)
    {
        Console.WriteLine($"Customer: {customer.FirstName} {customer.LastName}");
    }
}

Console.WriteLine();

Console.WriteLine("Calling /nyctaxitrips/stream-with-streaming-query-result endpoint...");
var nycResponseWithStreamingQueryResult = await client.GetAsync("/nyctaxitrips/stream-with-streaming-query-result", CancellationToken.None);
nycResponseWithStreamingQueryResult.EnsureSuccessStatusCode();

var nycResultWithStreamingQueryResult = await nycResponseWithStreamingQueryResult.Content.ReadFromJsonAsync<StreamingQueryResult<NycTaxiTrip>>(options);
if (nycResultWithStreamingQueryResult is not null)
{
    if (nycResultWithStreamingQueryResult.Header is not null)
    {
        Console.WriteLine($"Header: Version={nycResultWithStreamingQueryResult.Header.Version}, Progressive={nycResultWithStreamingQueryResult.Header.IsProgressive}");
    }
    else
    {
        Console.WriteLine("No header received.");
    }

    if (nycResultWithStreamingQueryResult.TableSchemas.Any())
    {
        foreach (var schema in nycResultWithStreamingQueryResult.TableSchemas)
        {
            Console.WriteLine($"Schema for table '{schema.TableName}':");
            foreach (var col in schema.Columns)
            {
                Console.WriteLine($"\tColumn: {col.Name} (Type: {col.Type})");
            }
        }
    }
    else
    {
        Console.WriteLine("No table schema information received.");
    }

    Console.WriteLine("Streaming rows (with streaming query result):");
    var countWithStreamingQueryResult = 0;
    foreach (var nycTaxiTrip in nycResultWithStreamingQueryResult.Rows)
    {
        countWithStreamingQueryResult++;
        Console.WriteLine($"NYC Taxi Trip: Persons: {nycTaxiTrip.PassengerCount} - Amount: {nycTaxiTrip.TotalAmount}");
    }

    Console.WriteLine($"Streamed {countWithStreamingQueryResult} NYC Taxi Trip rows");

    if (nycResultWithStreamingQueryResult.Completion is not null)
    {
        Console.WriteLine($"Completion: HasErrors={nycResultWithStreamingQueryResult.Completion.HasErrors}");
        if (!string.IsNullOrEmpty(nycResultWithStreamingQueryResult.Completion.ErrorMessage))
        {
            Console.WriteLine($"Error Message: {nycResultWithStreamingQueryResult.Completion.ErrorMessage}");
        }
    }
    else
    {
        Console.WriteLine("No completion summary received.");
    }
}

Console.WriteLine();

Console.WriteLine("Calling /nyctaxitrips/stream endpoint...");

var timestamp = Stopwatch.GetTimestamp();

var count = 0;
await foreach (var nycTaxiTrip in client.GetFromJsonAsAsyncEnumerable<NycTaxiTrip>("/nyctaxitrips/stream", CancellationToken.None))
{
    if (nycTaxiTrip is not null)
    {
        count++;
        Console.WriteLine($"NYC Taxi Trip: Persons: {nycTaxiTrip.PassengerCount} - Amount: {nycTaxiTrip.TotalAmount}");
    }
}

Console.WriteLine($"Streamed {count} NYC Taxi Trip rows in {Stopwatch.GetElapsedTime(timestamp)}");

Console.WriteLine("Press any key to exit");
Console.ReadLine();