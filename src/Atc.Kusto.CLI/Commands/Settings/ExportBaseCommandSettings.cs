namespace Atc.Kusto.CLI.Commands.Settings;

public class ExportBaseCommandSettings : DatabaseCommandSettings
{
    [CommandOption("--output-dir <PATH>")]
    [Description("Output directory (defaults to current directory)")]
    public string OutputDir { get; init; } = ".";
}