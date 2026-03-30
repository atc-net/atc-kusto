namespace Atc.Kusto.CLI.Commands.Settings;

/// <summary>
/// Settings for commands that require a cluster name argument.
/// </summary>
public class ClusterNameCommandSettings : CommandSettings
{
    [CommandArgument(0, "<NAME>")]
    [Description("The friendly name of the cluster")]
    public string Name { get; init; } = string.Empty;
}