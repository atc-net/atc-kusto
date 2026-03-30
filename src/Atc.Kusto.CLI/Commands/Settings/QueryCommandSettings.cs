namespace Atc.Kusto.CLI.Commands.Settings;

/// <summary>
/// Settings for the query command.
/// </summary>
public class QueryCommandSettings : DatabaseCommandSettings
{
    [CommandArgument(0, "[QUERY]")]
    [Description("Inline KQL query text, or '-' to read from stdin")]
    public string? Query { get; init; }

    [CommandOption("--file|-f <PATH>")]
    [Description("Read query from a file (supports :start-end line range, e.g. query.kql:5-10)")]
    public string? FilePath { get; init; }

    [CommandOption("--format <FORMAT>")]
    [Description("Output format: human, json, markdown, or csv (default: human)")]
    public string Format { get; init; } = "human";

    [CommandOption("--show-stats")]
    [Description("Include query execution statistics in output")]
    public bool ShowStats { get; init; }

    public override ValidationResult Validate()
    {
        var baseResult = base.Validate();
        if (!baseResult.Successful)
        {
            return baseResult;
        }

        if (Query is not null && FilePath is not null)
        {
            return ValidationResult.Error("Cannot specify both an inline query and --file.");
        }

        if (Query is null && FilePath is null && !System.Console.IsInputRedirected)
        {
            return ValidationResult.Error("Provide a query as an argument, via --file, or pipe to stdin.");
        }

        if (FilePath is not null)
        {
            try
            {
                var fileRef = QueryFileReferenceParser.Parse(FilePath);
                if (!File.Exists(fileRef.Path))
                {
                    return ValidationResult.Error($"File not found: {fileRef.Path}");
                }
            }
            catch (ArgumentException ex)
            {
                return ValidationResult.Error(ex.Message);
            }
        }

        if (Format is not "human" and not "json" and not "markdown" and not "md" and not "csv")
        {
            return ValidationResult.Error("--format must be one of: human, json, markdown, csv.");
        }

        if (ShowStats && Format is "csv")
        {
            return ValidationResult.Error("--show-stats cannot be used with --format csv.");
        }

        return ValidationResult.Success();
    }
}