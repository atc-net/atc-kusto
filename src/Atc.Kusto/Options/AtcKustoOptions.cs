namespace Atc.Kusto.Options;

public class AtcKustoOptions
{
    public string? HostAddress { get; set; }

    public string? DatabaseName { get; set; }

    public TokenCredential? Credential { get; set; }
}