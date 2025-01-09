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
        await adminProvider
            .Received(1)
            .ExecuteControlCommandAsync(
                databaseName: null,
                command.GetQueryText(),
                Arg.Is<ClientRequestProperties>(p
                    => p.ClientRequestId != null &&
                       p.Parameters.SequenceEqual(command.GetCslParameters())));
    }
}