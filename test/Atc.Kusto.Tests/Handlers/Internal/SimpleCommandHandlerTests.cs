namespace Atc.Kusto.Tests.Handlers.Internal;

public sealed class SimpleCommandHandlerTests
{
    [Theory, AutoNSubstituteData]
    internal async Task Execute_ShouldInvokeAdminProvider_WhenCommandExecutesSuccessfully(
        [Frozen] ICslAdminProvider adminProvider,
        [Frozen] IKustoCommand command,
        [Frozen] IDataReader reader,
        SimpleCommandHandler sut,
        string queryText,
        CancellationToken cancellationToken)
    {
        // Arrange
        command.GetQueryText().Returns(queryText);

        adminProvider
            .ExecuteControlCommandAsync(
                databaseName: null,
                command.GetQueryText(),
                command.GetClientRequestProperties())
            .ReturnsForAnyArgs(reader);

        // Act
        await sut.Execute(cancellationToken);

        // Assert
        _ = adminProvider
            .Received(1)
            .ExecuteControlCommandAsync(
                databaseName: null,
                command.GetQueryText(),
                Arg.Is<ClientRequestProperties>(p
                    => p.ClientRequestId != null &&
                       p.Parameters.SequenceEqual(command.GetCslParameters())));
    }

    [Theory, AutoNSubstituteData]
    internal async Task Execute_Does_Not_Send_The_Command_When_Already_Cancelled(
        [Frozen] ICslAdminProvider adminProvider,
        SimpleCommandHandler sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var act = () => sut.Execute(cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();

        await adminProvider
            .DidNotReceive()
            .ExecuteControlCommandAsync(
                Arg.Any<string?>(),
                Arg.Any<string>(),
                Arg.Any<ClientRequestProperties>());
    }

    [Theory]
    [MemberAutoNSubstituteData(nameof(CancellationTestData.SdkCancellationErrors), MemberType = typeof(CancellationTestData))]
    internal async Task Execute_Throws_OperationCanceledException_When_The_Sdk_Fails_After_The_Caller_Cancelled(
        Exception sdkError,
        [Frozen] ICslAdminProvider adminProvider,
        SimpleCommandHandler sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        adminProvider
            .ExecuteControlCommandAsync(
                Arg.Any<string?>(),
                Arg.Any<string>(),
                Arg.Any<ClientRequestProperties>())
            .Returns(_ => CancellationTestData.CancelThenThrow<IDataReader>(cts.CancelAsync, sdkError));

        // Act
        var act = () => sut.Execute(cts.Token);

        // Assert
        var thrown = await act.Should().ThrowAsync<OperationCanceledException>();
        thrown.Which.InnerException.Should().BeSameAs(sdkError);
        thrown.Which.CancellationToken.Should().Be(cts.Token);
    }

    [Theory, AutoNSubstituteData]
    internal async Task Execute_Rethrows_The_Sdk_Error_When_The_Caller_Did_Not_Cancel(
        [Frozen] ICslAdminProvider adminProvider,
        SimpleCommandHandler sut)
    {
        // Arrange
        var sdkError = new KustoClientRequestCanceledByUserException();

        adminProvider
            .ExecuteControlCommandAsync(
                Arg.Any<string?>(),
                Arg.Any<string>(),
                Arg.Any<ClientRequestProperties>())
            .ThrowsAsync(sdkError);

        // Act
        var act = () => sut.Execute(CancellationToken.None);

        // Assert
        (await act.Should().ThrowAsync<KustoClientRequestCanceledByUserException>())
            .Which.Should().BeSameAs(sdkError);
    }
}