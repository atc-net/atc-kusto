namespace Atc.Kusto.Tests;

/// <summary>
/// An empty async sequence that fails at a chosen step, optionally cancelling a token source first,
/// the way the Kusto SDK fails when the caller cancels mid-stream.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
internal sealed class FailingAsyncEnumerable<T> : IAsyncEnumerable<T>
{
    private readonly FailingStep step;
    private readonly Exception error;
    private readonly CancellationTokenSource? cancelBeforeFailing;

    public FailingAsyncEnumerable(
        FailingStep step,
        Exception error,
        CancellationTokenSource? cancelBeforeFailing)
    {
        this.step = step;
        this.error = error;
        this.cancelBeforeFailing = cancelBeforeFailing;
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(
        CancellationToken cancellationToken = default)
    {
        if (step == FailingStep.GetEnumerator)
        {
            cancelBeforeFailing?.Cancel();
            throw error;
        }

        return new Enumerator(this);
    }

    private sealed class Enumerator : IAsyncEnumerator<T>
    {
        private readonly FailingAsyncEnumerable<T> owner;

        public Enumerator(FailingAsyncEnumerable<T> owner)
            => this.owner = owner;

        public T Current => default!;

        public async ValueTask<bool> MoveNextAsync()
        {
            if (owner.step != FailingStep.MoveNext)
            {
                return false;
            }

            await CancelAsync();
            throw owner.error;
        }

        public async ValueTask DisposeAsync()
        {
            if (owner.step != FailingStep.Dispose)
            {
                return;
            }

            await CancelAsync();
            throw owner.error;
        }

        private Task CancelAsync()
            => owner.cancelBeforeFailing?.CancelAsync() ?? Task.CompletedTask;
    }
}