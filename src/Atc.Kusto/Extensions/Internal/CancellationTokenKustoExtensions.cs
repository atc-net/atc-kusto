// ReSharper disable MethodSupportsCancellation
namespace Atc.Kusto.Extensions.Internal;

/// <summary>
/// Utilities to hook up server-side cancellation for Kusto operations by issuing a cancel control command
/// when a CancellationToken is triggered.
/// </summary>
internal static class CancellationTokenKustoExtensions
{
    /// <summary>
    /// Determines whether server-side cancellation should be enabled based on the admin provider and options.
    /// </summary>
    /// <param name="adminProvider">The admin provider to check.</param>
    /// <param name="enableServerSideCancellation">The option flag for server-side cancellation.</param>
    /// <returns>True if server-side cancellation should be enabled; otherwise, false.</returns>
    public static bool ShouldEnableServerSideCancellation(
        ICslAdminProvider? adminProvider,
        bool enableServerSideCancellation)
        => adminProvider is not null &&
           enableServerSideCancellation;

    /// <summary>
    /// Registers a callback that issues a server-side cancel command using the original <see cref="ClientRequestProperties.ClientRequestId"/>.
    /// </summary>
    /// <param name="adminProvider">Admin provider used to execute control commands.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    /// <param name="databaseName">Optional database name; pass null to use the provider's default.</param>
    /// <param name="clientRequestProperties">The original request properties used for the running query.</param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <returns>An <see cref="IDisposable"/> that should be disposed to unregister the callback.</returns>
    public static IDisposable RegisterKustoServerSideCancellation(
        this ICslAdminProvider adminProvider,
        ILogger logger,
        string? databaseName,
        ClientRequestProperties clientRequestProperties,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(adminProvider);
        ArgumentNullException.ThrowIfNull(clientRequestProperties);

        if (!cancellationToken.CanBeCanceled)
        {
            return NullDisposable.Instance;
        }

        clientRequestProperties.ClientRequestId ??= Guid.NewGuid().ToString();

        var state = (adminProvider, databaseName, originalClientRequestId: clientRequestProperties.ClientRequestId, logger);

        return cancellationToken.Register(
            static s =>
            {
                var (admin, db, originalClientRequestId, logger) = ((ICslAdminProvider, string?, string, ILogger))s!;

                try
                {
                    var cancelCommand = CslCommandGenerator.GenerateQueryCancelCommand(originalClientRequestId);

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var cancelProps = new ClientRequestProperties
                            {
                                ClientRequestId = Guid.NewGuid().ToString(),
                            };

                            // Set request options to not obfuscate client request parameters for debugging purposes
                            cancelProps.SetOption(ClientRequestProperties.OptionQueryLogQueryParameters, value: true);

                            using var reader = await admin.ExecuteControlCommandAsync(db, cancelCommand, cancelProps).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            logger.LogKustoCancelCommandFailed(ex);
                        }
                    });
                }
                catch (Exception ex)
                {
                    logger.LogFailedToScheduleKustoCancel(ex);
                }
            },
            state);
    }

    private sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new();

        private NullDisposable()
        {
        }

        public void Dispose()
        {
        }
    }
}