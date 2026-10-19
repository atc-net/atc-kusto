namespace Atc.Kusto.Tests.Extensions;

/// <summary>
/// Covers the retry pipeline registered by <c>ConfigureAzureDataExplorer</c> for queries.
/// The retry delays run on a <see cref="FakeTimeProvider"/>, so no test waits for real;
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

    [Fact]
    public async Task A_Transient_Error_Is_Retried_After_Three_Seconds()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();
        var pipeline = CreatePipeline(timeProvider);
        var attempts = 0;

        // Act
        var execution = pipeline
            .ExecuteAsync(
                _ =>
                {
                    attempts++;
                    return attempts == 1
                        ? throw new KustoServiceException()
                        : ValueTask.FromResult("result");
                },
                CancellationToken.None)
            .AsTask();

        // Assert
        timeProvider.Advance(TimeSpan.FromSeconds(3) - TimeSpan.FromTicks(1));
        attempts.Should().Be(1);

        timeProvider.Advance(TimeSpan.FromTicks(1));
        (await execution).Should().Be("result");
        attempts.Should().Be(2);
    }

    [Fact]
    public async Task A_Transient_Error_Is_Retried_Three_Times_Before_It_Is_Thrown()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();
        var pipeline = CreatePipeline(timeProvider);
        var attempts = 0;

        // Act
        var execution = pipeline
            .ExecuteAsync(
                _ =>
                {
                    attempts++;
                    throw new KustoServiceException();
                },
                CancellationToken.None)
            .AsTask();

        // Each retry starts its delay after the previous attempt, which runs on the thread pool,
        // so advance the clock in steps until the pipeline gives up.
        for (var step = 0; step < 1_000 && !execution.IsCompleted; step++)
        {
            timeProvider.Advance(TimeSpan.FromSeconds(1));
            await Task.Delay(TimeSpan.FromMilliseconds(1), cancellationToken: TestContext.Current.CancellationToken);
        }

        // Assert
        await execution.Invoking(e => e).Should().ThrowAsync<KustoServiceException>();
        attempts.Should().Be(4);
    }

    private static ResiliencePipeline CreatePipeline(
        TimeProvider? timeProvider = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        if (timeProvider is not null)
        {
            services.AddSingleton(timeProvider);
        }

        services.ConfigureAzureDataExplorer(
            options => options.ConnectionString = "Data Source=https://example.kusto.windows.net");

        return services
            .BuildServiceProvider()
            .GetRequiredKeyedService<ResiliencePipeline>(Constants.ResiliencePipelineKey);
    }
}