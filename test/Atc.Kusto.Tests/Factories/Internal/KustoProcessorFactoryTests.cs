namespace Atc.Kusto.Tests.Factories.Internal;

public sealed class KustoProcessorFactoryTests
{
    [Theory, AutoNSubstituteData]
    public void Can_Create_Processor(KustoProcessorFactory sut)
    {
        // Act
        var processor = sut.Create();

        // Assert
        processor.Should().BeOfType<KustoProcessor>();
        ((KustoProcessor)processor).ConnectionName.Should().BeNull();
        ((KustoProcessor)processor).DatabaseName.Should().BeNull();
    }

    [Theory, AutoNSubstituteData]
    public void Can_Create_Processor_With_Connection_And_Database(
        KustoProcessorFactory sut,
        string connectionName,
        string databaseName)
    {
        // Act
        var processor = sut.Create(connectionName, databaseName);

        // Assert
        processor.Should().BeOfType<KustoProcessor>();
        ((KustoProcessor)processor).ConnectionName.Should().Be(connectionName);
        ((KustoProcessor)processor).DatabaseName.Should().Be(databaseName);
    }
}