namespace Atc.Kusto.Tests.Extensions.Internal;

public sealed class AsyncEnumerableExtensionsTests
{
    [Theory]
    [InlineData(FailingStep.GetEnumerator)]
    [InlineData(FailingStep.MoveNext)]
    [InlineData(FailingStep.Dispose)]
    internal async Task NormalizeCancellationExceptions_Translates_A_Failure_After_The_Caller_Cancelled(
        FailingStep step)
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var sdkError = new KustoServiceException();
        var source = new FailingAsyncEnumerable<string>(step, sdkError, cancelBeforeFailing: cts);

        // Act
        var act = () => Enumerate(source.NormalizeCancellationExceptions(cts.Token));

        // Assert
        var thrown = await act.Should().ThrowAsync<OperationCanceledException>();
        thrown.Which.InnerException.Should().BeSameAs(sdkError);
        thrown.Which.CancellationToken.Should().Be(cts.Token);
    }

    [Theory]
    [InlineData(FailingStep.GetEnumerator)]
    [InlineData(FailingStep.MoveNext)]
    [InlineData(FailingStep.Dispose)]
    internal async Task NormalizeCancellationExceptions_Passes_A_Failure_Through_When_The_Caller_Did_Not_Cancel(
        FailingStep step)
    {
        // Arrange
        var sdkError = new KustoClientRequestCanceledByUserException();
        var source = new FailingAsyncEnumerable<string>(step, sdkError, cancelBeforeFailing: null);

        // Act
        var act = () => Enumerate(source.NormalizeCancellationExceptions(CancellationToken.None));

        // Assert
        (await act.Should().ThrowAsync<KustoClientRequestCanceledByUserException>())
            .Which.Should().BeSameAs(sdkError);
    }

    [Fact]
    public async Task NormalizeCancellationExceptions_Rethrows_An_OperationCanceledException_Unchanged()
    {
        // Arrange
        var original = new OperationCanceledException("original");
        var source = new FailingAsyncEnumerable<string>(FailingStep.MoveNext, original, cancelBeforeFailing: null);

        // Act
        var act = () => Enumerate(source.NormalizeCancellationExceptions(CancellationToken.None));

        // Assert
        (await act.Should().ThrowAsync<OperationCanceledException>())
            .Which.Should().BeSameAs(original);
    }

    private static async Task Enumerate(IAsyncEnumerable<string> source)
    {
        await foreach (var item in source)
        {
            _ = item;
        }
    }
}