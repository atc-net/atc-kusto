namespace Atc.Kusto.Tests.Ingestion.Internal;

public sealed class KustoIngestFormatExtensionsTests
{
    [Theory]
    [InlineData(KustoIngestFormat.MultiJson, DataSourceFormat.multijson)]
    [InlineData(KustoIngestFormat.Json, DataSourceFormat.json)]
    [InlineData(KustoIngestFormat.Csv, DataSourceFormat.csv)]
    [InlineData(KustoIngestFormat.Tsv, DataSourceFormat.tsv)]
    public void ToDataSourceFormat_Maps_Each_Value(KustoIngestFormat input, DataSourceFormat expected)
        => input.ToDataSourceFormat().Should().Be(expected);

    [Fact]
    public void ToDataSourceFormat_Throws_For_Undefined_Value()
    {
        var act = () => ((KustoIngestFormat)9999).ToDataSourceFormat();

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(KustoIngestFormat.Json, true)]
    [InlineData(KustoIngestFormat.MultiJson, true)]
    [InlineData(KustoIngestFormat.Csv, false)]
    [InlineData(KustoIngestFormat.Tsv, false)]
    public void IsJson_True_Only_For_Json_Family(KustoIngestFormat input, bool expected)
        => input.IsJson().Should().Be(expected);
}