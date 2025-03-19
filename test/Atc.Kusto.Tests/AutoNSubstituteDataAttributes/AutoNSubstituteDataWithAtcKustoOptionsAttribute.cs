namespace Atc.Kusto.Tests.AutoNSubstituteDataAttributes;

[AttributeUsage(AttributeTargets.Method)]
internal sealed class AutoNSubstituteDataWithAtcKustoOptionsAttribute : AutoDataAttribute
{
    /// <summary>
    /// Gets the flag indicating if the generated AtcKustoOptions should include a credential.
    /// </summary>
    public bool WithCredential { get; }

    public AutoNSubstituteDataWithAtcKustoOptionsAttribute(bool withCredential = true)
        : base(() =>
            new Fixture()
                .Customize(new AutoNSubstituteCustomization())
                .Customize(new AtcKustoOptionsCustomization(withCredential)))
    {
        WithCredential = withCredential;
    }
}