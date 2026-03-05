namespace Atc.Kusto.CLI.Commands.Settings;

public class ExportBaseCommandSettings : BaseCommandSettings
{
    [CommandOption("--tenant-id <GUID>")]
    [Description("Azure AD tenant ID")]
    public string TenantId { get; init; } = string.Empty;

    [CommandOption("--cluster-url <URL>")]
    [Description("Kusto cluster URL (e.g. https://mycluster.kusto.windows.net)")]
    public Uri? ClusterUrl { get; init; }

    [CommandOption("--database <NAME>")]
    [Description("Database name")]
    public string Database { get; init; } = string.Empty;

    [CommandOption("--output-dir <PATH>")]
    [Description("Output directory (defaults to current directory)")]
    public string OutputDir { get; init; } = ".";

    public override ValidationResult Validate()
    {
        var baseResult = base.Validate();
        if (!baseResult.Successful)
        {
            return baseResult;
        }

        if (string.IsNullOrWhiteSpace(TenantId))
        {
            return ValidationResult.Error("--tenant-id is required.");
        }

        if (ClusterUrl is null)
        {
            return ValidationResult.Error("--cluster-url is required.");
        }

        if (ClusterUrl.Scheme is not "https" and not "http")
        {
            return ValidationResult.Error("--cluster-url must be a valid HTTP(S) URL.");
        }

        if (string.IsNullOrWhiteSpace(Database))
        {
            return ValidationResult.Error("--database is required.");
        }

        return ValidationResult.Success();
    }
}