namespace Atc.Kusto.Tests.Extensions.Internal;

public sealed class CslAdminProviderExtensionsTests
{
    [Theory, AutoNSubstituteData]
    public async Task RegisterKustoServerSideCancellation_Sends_The_Cancel_Command_Even_When_The_Registration_Is_Disposed_While_Cancelling(
        ICslAdminProvider adminProvider,
        IDataReader cancelReader)
    {
        // Arrange
        var cancelCommandSent = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        adminProvider
            .ExecuteControlCommandAsync(
                Arg.Any<string?>(),
                Arg.Any<string>(),
                Arg.Any<ClientRequestProperties>())
            .Returns(ci =>
            {
                cancelCommandSent.TrySetResult(ci.ArgAt<string>(1));
                return cancelReader;
            });

        var clientRequestProperties = new ClientRequestProperties { ClientRequestId = "query-1" };

        using var cts = new CancellationTokenSource();

        var registration = adminProvider.RegisterKustoServerSideCancellation(
            NullLogger.Instance,
            databaseName: null,
            clientRequestProperties,
            cts.Token);

        // The SDK registers its own callback on the token later, and a token runs its callbacks newest first.
        // So the SDK can end the query first, and the handler then disposes the registration before ours has run.
        using var queryEndsFirst = cts.Token.Register(() => registration.Dispose());

        // Act
        await cts.CancelAsync();

        // Assert - not cts.Token: it is already cancelled, and this waits for the effect of cancelling it
        var cancelCommand = await cancelCommandSent.Task.WaitAsync(
            TimeSpan.FromSeconds(10),
            CancellationToken.None);

        cancelCommand.Should().ContainEquivalentOf("cancel");
        cancelCommand.Should().Contain("query-1");
    }
}