namespace Atc.Kusto.Tests;

public sealed class KustoQueryTests
{
    [Fact]
    public void Implements()
        => typeof(KustoQuery<>)
            .Should()
            .BeAssignableTo<KustoScript>();
}