namespace Atc.Kusto.CLI.Commands.Settings;

/// <summary>
/// Base settings for commands that require a specific database.
/// </summary>
public class DatabaseCommandSettings : ClusterCommandSettings
{
    [CommandOption("--database <NAME>")]
    [Description("Database name")]
    public string Database { get; init; } = string.Empty;

    public override ValidationResult Validate()
    {
        var baseResult = base.Validate();
        if (!baseResult.Successful)
        {
            return baseResult;
        }

        if (string.IsNullOrWhiteSpace(Database))
        {
            return ValidationResult.Error("--database is required.");
        }

        return ValidationResult.Success();
    }
}