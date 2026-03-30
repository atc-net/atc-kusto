namespace Atc.Kusto.CLI.Commands.Settings;

/// <summary>
/// Settings for the database set-default command.
/// </summary>
public class DatabaseSetDefaultCommandSettings : ClusterCommandSettings
{
    [CommandArgument(0, "<NAME>")]
    [Description("The database name to set as default for the cluster")]
    public string DatabaseName { get; init; } = string.Empty;

    public override ValidationResult Validate()
    {
        var baseResult = base.Validate();
        if (!baseResult.Successful)
        {
            return baseResult;
        }

        if (string.IsNullOrWhiteSpace(DatabaseName))
        {
            return ValidationResult.Error("Database name is required.");
        }

        return ValidationResult.Success();
    }
}