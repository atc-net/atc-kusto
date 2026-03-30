namespace Atc.Kusto.CLI.Commands.Settings;

/// <summary>
/// Settings for the cluster add command.
/// </summary>
public class ClusterAddCommandSettings : CommandSettings
{
    [CommandArgument(0, "<NAME>")]
    [Description("A friendly name for the cluster")]
    public string Name { get; init; } = string.Empty;

    [CommandArgument(1, "<URL>")]
    [Description("The Kusto cluster URL (e.g. https://mycluster.kusto.windows.net)")]
    [SuppressMessage("Design", "CA1056:URI-like properties should not be strings", Justification = "Spectre.Console command argument binds to string.")]
    public string Url { get; init; } = string.Empty;

    [CommandOption("--use")]
    [Description("Set this cluster as the default")]
    public bool SetAsDefault { get; init; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            return ValidationResult.Error("Cluster name is required.");
        }

        if (ClusterUtilities.NormalizeClusterUrl(Url) is null)
        {
            return ValidationResult.Error($"Invalid cluster URL: {Url}");
        }

        return ValidationResult.Success();
    }
}