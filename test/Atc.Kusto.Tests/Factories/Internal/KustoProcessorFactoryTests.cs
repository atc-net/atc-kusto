namespace Atc.Kusto.Tests.Factories.Internal;

public sealed class KustoProcessorFactoryTests
{
    [Theory, AutoNSubstituteData]
    public void Create_Without_Arguments_Returns_A_Processor_Without_Connection_Or_Database(
        KustoProcessorFactory sut)
    {
        // Act
        var processor = sut.Create();

        // Assert
        processor.Should().BeOfType<KustoProcessor>();
        ((KustoProcessor)processor).ConnectionName.Should().BeNull();
        ((KustoProcessor)processor).DatabaseName.Should().BeNull();
    }

    [Theory, AutoNSubstituteData]
    public void Create_Returns_A_Processor_For_The_Given_Connection_And_Database(
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