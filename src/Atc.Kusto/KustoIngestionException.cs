namespace Atc.Kusto;

/// <summary>
/// The exception thrown by <see cref="KustoIngestionResult.EnsureSuccess"/> when an ingestion failed.
/// </summary>
/// <remarks>
/// <see cref="IKustoIngestor"/> reports operational failures as a <see cref="KustoIngestionResult"/>
/// with <see cref="KustoIngestionStatus.Failed"/> rather than throwing. Call
/// <see cref="KustoIngestionResult.EnsureSuccess"/> to turn such a result into this exception, the
/// same way <c>HttpResponseMessage.EnsureSuccessStatusCode()</c> works for HTTP calls.
/// </remarks>
public sealed class KustoIngestionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KustoIngestionException"/> class.
    /// </summary>
    public KustoIngestionException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KustoIngestionException"/> class with a message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public KustoIngestionException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KustoIngestionException"/> class with a message
    /// and the exception that caused it.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public KustoIngestionException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KustoIngestionException"/> class for a failed result.
    /// </summary>
    /// <param name="result">The failed ingestion result.</param>
    public KustoIngestionException(KustoIngestionResult result)
        : base(CreateMessage(result))
    {
        Result = result;
    }

    /// <summary>
    /// Gets the failed result that caused the exception, when the exception was created from one.
    /// </summary>
    public KustoIngestionResult? Result { get; }

    private static string CreateMessage(KustoIngestionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return $"Kusto ingestion failed (mode: {result.Mode}). {result.ErrorMessage}".TrimEnd();
    }
}