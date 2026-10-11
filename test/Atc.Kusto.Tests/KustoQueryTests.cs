namespace Atc.Kusto.Tests;

public sealed class KustoQueryTests
{
    [Fact]
    public void KustoQuery_Inherits_From_KustoScript()
        => typeof(KustoQuery<>)
            .Should()
            .BeAssignableTo<KustoScript>();
}