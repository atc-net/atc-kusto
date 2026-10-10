namespace Atc.Kusto.Tests;

/// <summary>
/// The step at which a <see cref="FailingAsyncEnumerable{T}"/> fails.
/// </summary>
internal enum FailingStep
{
    GetEnumerator,
    MoveNext,
    Dispose,
}