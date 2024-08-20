namespace Atc.Kusto.Tests.Extensions;

public sealed class DataReaderExtensionsTests
{
    public record TestObject(
        string Property1,
        string Property2,
        string Property3);

    [Theory, AutoNSubstituteData]
    public void ReadObjects_Will_Return_Objects_Read_From_DataReader(
        List<TestObject> data,
        IDataReader dataReader)
    {
        // Arrange
        var properties = typeof(TestObject).GetProperties();
        var fieldNames = properties.Select(p => p.Name).ToArray();
        var values = data.Select(d => properties.Select(p => p.GetValue(d)!).ToArray()).ToArray();
        var index = -1;

        dataReader.FieldCount
            .Returns(fieldNames.Length);

        dataReader
            .GetName(default)
            .ReturnsForAnyArgs(c => fieldNames[c.Arg<int>()]);

        dataReader
            .Read()
            .Returns(c => ++index < data.Count);

        dataReader
            .GetValues(default!)
            .ReturnsForAnyArgs(c => c.Arg<object[]>().CopyFrom(values[index], 0));

        // Act
        var actual = dataReader.ReadObjects<TestObject>();

        // Assert
        actual.Should().BeEquivalentTo(data);
    }

    [Theory, AutoNSubstituteData]
    public void CanReadObjects(
        List<TestObject> data,
        IDataReader dataReader)
    {
        // Arrange
        var properties = typeof(TestObject).GetProperties();
        var fieldNames = properties.Select(p => p.Name).ToArray();
        var values = data.Select(d => properties.Select(p => p.GetValue(d)!).ToArray()).ToArray();
        var index = -1;

        dataReader.FieldCount
            .Returns(fieldNames.Length);

        dataReader
            .GetName(default)
            .ReturnsForAnyArgs(c => fieldNames[c.Arg<int>()]);

        dataReader
            .Read()
            .Returns(c => ++index < data.Count);

        dataReader
            .GetValues(default!)
            .ReturnsForAnyArgs(c => c.Arg<object[]>().CopyFrom(values[index], 0));

        dataReader
            .NextResult()
            .Returns(true);

        // Act
        var actual = dataReader.ReadObjectsFromNextResult<TestObject>();

        // Assert
        actual.Should().BeEquivalentTo(data);
        dataReader.Received(1).NextResult();
    }
}