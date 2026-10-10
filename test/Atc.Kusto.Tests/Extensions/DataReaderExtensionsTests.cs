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

    public record DateOnlyTestObject(
        string Name,
        DateOnly EventDate);

    [Fact]
    public void ReadObject_Returns_The_Row_The_Reader_Is_On()
    {
        // Arrange - a reader with two rows, moved to the second row
        using var table = new DataTable();
        table.Columns.Add("Property1", typeof(string));
        table.Columns.Add("Property2", typeof(string));
        table.Columns.Add("Property3", typeof(string));
        table.Rows.Add("first-1", "first-2", "first-3");
        table.Rows.Add("second-1", "second-2", "second-3");

        using var reader = table.CreateDataReader();
        reader.Read(); // now on the first row
        reader.Read(); // now on the second row

        // Act
        var actual = reader.ReadObject<TestObject>();

        // Assert
        actual.Should().Be(new TestObject("second-1", "second-2", "second-3"));
    }

    [Fact]
    public void ReadObject_Gives_The_Same_Result_As_ReadObjects()
    {
        // Arrange - one row where a bool arrives as the number 1, as Kusto's query reader sends it.
        // Both methods must convert it to true, because they share the same row conversion.
        using var table = new DataTable();
        table.Columns.Add("IsActive", typeof(sbyte));
        table.Columns.Add("IsEnabled", typeof(bool));
        table.Rows.Add((sbyte)1, false);

        using var readerForReadObjects = table.CreateDataReader();
        using var readerForReadObject = table.CreateDataReader();
        readerForReadObject.Read(); // ReadObject maps the row the reader is on, so move to it first

        var expected = new BooleanTestObject(IsActive: true, IsEnabled: false);

        // Act
        var fromReadObjects = readerForReadObjects.ReadObjects<BooleanTestObject>();
        var fromReadObject = readerForReadObject.ReadObject<BooleanTestObject>();

        // Assert
        fromReadObjects.Should().Equal(expected);
        fromReadObject.Should().Be(expected);
    }

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
            .ReturnsForAnyArgs(x => fieldNames[x.Arg<int>()]);

        dataReader
            .Read()
            .Returns(_ => ++index < data.Count);

        dataReader
            .GetValues(null!)
            .ReturnsForAnyArgs(x => x.Arg<object[]>().CopyFrom(values[index], 0));

        dataReader
            .GetValue(0)
            .ReturnsForAnyArgs(x => values[index][x.Arg<int>()]);

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
            .ReturnsForAnyArgs(x => fieldNames[x.Arg<int>()]);

        dataReader
            .Read()
            .Returns(_ => ++index < data.Count);

        dataReader
            .GetValues(null!)
            .ReturnsForAnyArgs(x => x.Arg<object[]>().CopyFrom(values[index], 0));

        dataReader
            .GetValue(0)
            .ReturnsForAnyArgs(x => values[index][x.Arg<int>()]);

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
            .Returns(x => fieldNames[x.Arg<int>()]);

        var readCount = 0;
        dataReader
            .Read()
            .Returns(_ => readCount++ < 1); // Return one row

        dataReader
            .GetValue(Arg.Any<int>())
            .Returns(x => rowValues[x.Arg<int>()]);

        // Act
        var actual = dataReader.ReadObjects<BooleanTestObject>();

        // Assert
        Assert.Single(actual);
        Assert.Equal(expectedTrue, actual[0].IsActive);
        Assert.Equal(expectedFalse, actual[0].IsEnabled);
    }

    [Fact]
    public void ReadObjects_Should_Convert_DateTime_To_DateOnly()
    {
        // Arrange - simulates Kusto returning datetime values for date-only columns (e.g. startofday())
        var dataReader = Substitute.For<IDataReader>();

        var fieldNames = new[] { "Name", "EventDate" };
        var expectedDate = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        object[] rowValues = ["TestEvent", expectedDate];

        dataReader.FieldCount
            .Returns(2);

        dataReader
            .GetName(Arg.Any<int>())
            .Returns(x => fieldNames[x.Arg<int>()]);

        var readCount = 0;
        dataReader
            .Read()
            .Returns(_ => readCount++ < 1);

        dataReader
            .GetValue(Arg.Any<int>())
            .Returns(x => rowValues[x.Arg<int>()]);

        // Act
        var actual = dataReader.ReadObjects<DateOnlyTestObject>();

        // Assert
        Assert.Single(actual);
        Assert.Equal("TestEvent", actual[0].Name);
        Assert.Equal(new DateOnly(2024, 1, 15), actual[0].EventDate);
    }
}