namespace Atc.Kusto.Tests;

public sealed class KustoIngestionResultTests
{
    [Theory]
    [InlineData(KustoIngestionStatus.Succeeded, true)]
    [InlineData(KustoIngestionStatus.Queued, true)]
    [InlineData(KustoIngestionStatus.Skipped, true)]
    [InlineData(KustoIngestionStatus.Failed, false)]
    public void IsSuccess_Is_False_Only_For_Failed(
        KustoIngestionStatus status,
        bool expected)
        => Result(status).IsSuccess.Should().Be(expected);

    [Theory]
    [InlineData(KustoIngestionStatus.Succeeded)]
    [InlineData(KustoIngestionStatus.Queued)]
    [InlineData(KustoIngestionStatus.Skipped)]
    public void EnsureSuccess_Returns_The_Result_When_Not_Failed(
        KustoIngestionStatus status)
    {
        // Arrange
        var result = Result(status);

        // Act & Assert
        result.EnsureSuccess().Should().BeSameAs(result);
    }

    [Fact]
    public void EnsureSuccess_Throws_With_The_Result_And_Message_When_Failed()
    {
        // Arrange
        var result = Result(KustoIngestionStatus.Failed) with { ErrorMessage = "Mapping 'Events_mapping' not found." };

        // Act
        var act = () => result.EnsureSuccess();

        // Assert
        var exception = act.Should().Throw<KustoIngestionException>().Which;
        exception.Result.Should().BeSameAs(result);
        exception.Message.Should().Contain("ManagedStreaming").And.Contain("Mapping 'Events_mapping' not found.");
    }

    private static KustoIngestionResult Result(KustoIngestionStatus status)
        => new() { Status = status, Mode = IngestionMode.ManagedStreaming };
}