namespace Atc.Kusto.Tests.Extensions;

/// <summary>
/// Covers the retry pipeline registered by <c>ConfigureAzureDataExplorer</c> for queries.
/// Only cases that end without a retry are tested here, as a retry waits several seconds;
/// which errors count as transient is covered by <see cref="Utilities.Internal.KustoTransientErrorsTests"/>.
/// </summary>
public sealed class ResiliencePipelineTests
{
    [Fact]
    public async Task A_Permanent_Error_Is_Not_Retried()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var attempts = 0;

        // Act
        var act = async () => await pipeline.ExecuteAsync(
            _ =>
            {
                attempts++;
                throw new SemanticException();
            },
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<SemanticException>();
        attempts.Should().Be(1);
    }

    [Fact]
    public async Task A_Transient_Error_Is_Not_Retried_Once_The_Caller_Has_Cancelled()
    {
        // Arrange
        var pipeline = CreatePipeline();
        using var cts = new CancellationTokenSource();
        var attempts = 0;

        // Act
        var act = async () => await pipeline.ExecuteAsync(
            async _ =>
            {
                attempts++;
                await cts.CancelAsync();
                throw new KustoServiceException();
            },
            cts.Token);

        // Assert
        await act.Should().ThrowAsync<KustoServiceException>();
        attempts.Should().Be(1);
    }

    [Fact]
    public async Task A_Mapping_Error_Is_Not_Retried()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var attempts = 0;

        // Act
        var act = async () => await pipeline.ExecuteAsync(
            _ =>
            {
                attempts++;
                throw new JsonException();
            },
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<JsonException>();
        attempts.Should().Be(1);
    }

    private static ResiliencePipeline CreatePipeline()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.ConfigureAzureDataExplorer(
            new Uri("https://example.kusto.windows.net"),
            "Db",
            Substitute.For<Azure.Core.TokenCredential>());

        return services
            .BuildServiceProvider()
            .GetRequiredKeyedService<ResiliencePipeline>(Constants.ResiliencePipelineKey);
    }
}