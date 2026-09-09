namespace Atc.Kusto.Tests;

public sealed class KustoIngestTargetTests
{
    [Fact]
    public void Mode_Default_Is_Null_So_Options_Default_Can_Apply()
    {
        // Arrange & Act
        var target = new KustoIngestTarget
        {
            TableName = "Events",
            Format = KustoIngestFormat.MultiJson,
        };

        // Assert
        target.Mode.Should().BeNull();
        target.MappingReference.Should().BeNull();
        target.EnableTracking.Should().BeFalse();
    }
}