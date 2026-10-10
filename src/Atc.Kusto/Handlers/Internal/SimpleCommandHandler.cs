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
    /// <remarks>
    /// The Kusto SDK cannot cancel a control command, so <paramref name="cancellationToken"/> is checked
    /// before the command is sent, and a command that has started always runs to completion. That way
    /// the outcome the caller sees always matches what happened on the cluster.
    /// </remarks>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="cancellationToken"/> is cancelled before the command is sent, or the
    /// command fails while it is cancelled.
    /// </exception>
    public async Task Execute(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var activity = KustoDiagnostics.Source.StartActivity(
            KustoDiagnostics.ActivityNames.Command,
            ActivityKind.Client,
            default(ActivityContext));

        var commandText = command.GetQueryText();

        activity?.SetTag(KustoDiagnostics.TagNames.DbStatement, commandText);

        try
        {
            using var reader = await adminProvider
                .ExecuteControlCommandAsync(
                    databaseName: null,
                    commandText,
                    command.GetClientRequestProperties());

            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex) when (CancellationExceptionUtilities.IsCancellation(ex, cancellationToken))
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw CancellationExceptionUtilities.NormalizeCancellationException(ex, cancellationToken);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}