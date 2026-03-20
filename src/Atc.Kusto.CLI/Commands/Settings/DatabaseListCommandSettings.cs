namespace Atc.Kusto.CLI.Commands.Settings;

/// <summary>
/// Settings for the database list command.
/// </summary>
public class DatabaseListCommandSettings : ClusterCommandSettings
{
    [CommandOption("--filter <VALUE>")]
    [Description("Filter databases by name (^prefix, suffix$, ^exact$, or contains)")]
    public string? Filter { get; init; }

    [CommandOption("--take <COUNT>")]
    [Description("Maximum number of results to return")]
    public int? Take { get; init; }

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

        if (Take <= 0)
        {
            return ValidationResult.Error("--take must be a positive integer.");
        }

        if (Format is not "human" and not "json" and not "markdown" and not "md")
        {
            return ValidationResult.Error("--format must be one of: human, json, markdown.");
        }

        return ValidationResult.Success();
    }
}