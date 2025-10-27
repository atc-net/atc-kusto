namespace Atc.Kusto.Tests.Extensions;

public sealed class DataReaderExtensionsTests
{
    public record TestObject(
        string Property1,
        string Property2,
        string Property3);

    public record BooleanTestObject(
        bool IsActive,
        bool IsEnabled);

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
            .GetName(0)
            .ReturnsForAnyArgs(c => fieldNames[c.Arg<int>()]);

        dataReader
            .Read()
            .Returns(_ => ++index < data.Count);

        dataReader
            .GetValues(null!)
            .ReturnsForAnyArgs(c => c.Arg<object[]>().CopyFrom(values[index], 0));

        dataReader
            .GetValue(0)
            .ReturnsForAnyArgs(c => values[index][c.Arg<int>()]);

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
            .GetName(0)
            .ReturnsForAnyArgs(c => fieldNames[c.Arg<int>()]);

        dataReader
            .Read()
            .Returns(c => ++index < data.Count);

        dataReader
            .GetValues(null!)
            .ReturnsForAnyArgs(c => c.Arg<object[]>().CopyFrom(values[index], 0));

        dataReader
            .GetValue(0)
            .ReturnsForAnyArgs(c => values[index][c.Arg<int>()]);

        dataReader
            .NextResult()
            .Returns(true);

        // Act
        var actual = dataReader.ReadObjectsFromNextResult<TestObject>();

        // Assert
        actual.Should().BeEquivalentTo(data);
        dataReader.Received(1).NextResult();
    }

    [Theory]
    [InlineData(1, 0, true, false)] // Kusto returns 1 for true, 0 for false
    [InlineData(42, 0, true, false)] // Any non-zero value should be true
    [InlineData(-1, 0, true, false)] // Negative non-zero should also be true
    public void ReadObjects_Should_Convert_Numeric_Boolean_Values_From_Kusto(
        int numericTrue,
        int numericFalse,
        bool expectedTrue,
        bool expectedFalse)
    {
        // Arrange - simulates Kusto returning numeric values for boolean columns (tobool() returns sbyte)
        var dataReader = Substitute.For<IDataReader>();

        var fieldNames = new[] { "IsActive", "IsEnabled" };
        object[] rowValues = [numericTrue, numericFalse];

        dataReader.FieldCount
            .Returns(2);

        dataReader
            .GetName(Arg.Any<int>())
            .Returns(c => fieldNames[c.Arg<int>()]);

        var readCount = 0;
        dataReader
            .Read()
            .Returns(_ => readCount++ < 1); // Return one row

        dataReader
            .GetValue(Arg.Any<int>())
            .Returns(c => rowValues[c.Arg<int>()]);

        // Act
        var actual = dataReader.ReadObjects<BooleanTestObject>();

        // Assert
        Assert.Single(actual);
        Assert.Equal(expectedTrue, actual[0].IsActive);
        Assert.Equal(expectedFalse, actual[0].IsEnabled);
    }
}