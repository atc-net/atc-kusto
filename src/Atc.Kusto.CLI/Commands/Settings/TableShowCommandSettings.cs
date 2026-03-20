namespace Atc.Kusto.CLI.Commands.Settings;

/// <summary>
/// Settings for the table show command.
/// </summary>
public class TableShowCommandSettings : DatabaseCommandSettings
{
    [CommandArgument(0, "<NAME>")]
    [Description("Table name to show schema for")]
    public string Name { get; init; } = string.Empty;

    [CommandOption("--format <FORMAT>")]
    [Description("Output format: human, json, or markdown (default: human)")]
    public string Format { get; init; } = "human";

    public override ValidationResult Validate()
    {
        var baseResult = base.Validate();
        if (!baseResult.Successful)
        {
            return baseResult;
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            return ValidationResult.Error("Table name is required.");
        }

        if (Format is not "human" and not "json" and not "markdown" and not "md")
        {
            return ValidationResult.Error("--format must be one of: human, json, markdown.");
        }

        return ValidationResult.Success();
    }
}