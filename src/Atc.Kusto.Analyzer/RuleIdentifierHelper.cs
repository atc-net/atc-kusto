namespace Atc.Kusto.Analyzer;

public static class RuleIdentifierHelper
{
    [SuppressMessage("", "CA1055:Change the return type from 'string' to 'System.Uri'", Justification = "OK")]
    public static string GetHelpUri(string identifier)
        => string.Format(CultureInfo.InvariantCulture, "https://github.com/atc-net/atc-kusto/blob/main/docs/rules/{0}.md", identifier);
}
