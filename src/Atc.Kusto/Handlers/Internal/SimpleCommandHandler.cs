namespace Atc.Kusto.Handlers.Internal;

/// <summary>
/// A simple command handler that executes a Kusto command.
/// </summary>
internal sealed class SimpleCommandHandler : IScriptHandler
{
    private readonly ICslAdminProvider adminProvider;
    private readonly IKustoCommand command;

    public SimpleCommandHandler(
        ICslAdminProvider adminProvider,
        IKustoCommand command)
    {
        this.adminProvider = adminProvider;
        this.command = command;
    }

    /// <summary>
    /// Executes the Kusto command asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task Execute(
        CancellationToken cancellationToken)
    {
        using var reader = await adminProvider
            .ExecuteControlCommandAsync(
                databaseName: null,
                command.GetQueryText(),
                command.GetClientRequestProperties());
    }
}