namespace Atc.Kusto.Tests.AutoNSubstituteDataAttributes;

internal sealed class AtcKustoOptionsCustomization : ICustomization
{
    private readonly bool withCredential;

    public AtcKustoOptionsCustomization(bool withCredential)
    {
        this.withCredential = withCredential;
    }

    public void Customize(IFixture fixture)
    {
        fixture.Customize<AtcKustoOptions>(composer => composer
            .With(options => options.HostAddress, new Uri("https://example.kusto.windows.net"))
            .With(options => options.DatabaseName, "TestDatabase")
            .With(options => options.Credential, withCredential ? new DefaultAzureCredential() : null));
    }
}