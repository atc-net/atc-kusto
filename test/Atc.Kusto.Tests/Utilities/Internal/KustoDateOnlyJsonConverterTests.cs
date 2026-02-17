namespace Atc.Kusto.Tests.Utilities.Internal;

public sealed class KustoDateOnlyJsonConverterTests
{
    [Theory]
    [InlineData("2024-01-15", 2024, 1, 15)] // Date-only ISO 8601 string
    [InlineData("2024-01-15T00:00:00Z", 2024, 1, 15)] // Kusto datetime from startofday(), bin()
    [InlineData("2024-06-20T14:30:00+02:00", 2024, 6, 20)] // DateTime with timezone offset
    public void Read_Should_Parse_Date_String(
        string input,
        int expectedYear,
        int expectedMonth,
        int expectedDay)
    {
        // Arrange
        var json = Encoding.UTF8.GetBytes($"\"{input}\"");
        var reader = new Utf8JsonReader(json);
        reader.Read();
        var converter = new KustoDateOnlyJsonConverter();

        // Act
        var result = converter.Read(ref reader, typeof(DateOnly), JsonSerializerOptions.Default);

        // Assert
        Assert.Equal(new DateOnly(expectedYear, expectedMonth, expectedDay), result);
    }

    [Fact]
    public void Read_Should_Throw_JsonException_When_Value_Is_Null()
    {
        // Arrange
        var json = "null"u8.ToArray();
        var converter = new KustoDateOnlyJsonConverter();

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() =>
        {
            var localReader = new Utf8JsonReader(json);
            localReader.Read();
            return converter.Read(ref localReader, typeof(DateOnly), JsonSerializerOptions.Default);
        });

        Assert.Contains("Unable to convert null", exception.Message, StringComparison.Ordinal);
        Assert.Contains("DateOnly", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_Should_Throw_JsonException_When_Value_Is_Not_A_Date()
    {
        // Arrange
        var json = "\"not-a-date\""u8.ToArray();
        var converter = new KustoDateOnlyJsonConverter();

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() =>
        {
            var localReader = new Utf8JsonReader(json);
            localReader.Read();
            return converter.Read(ref localReader, typeof(DateOnly), JsonSerializerOptions.Default);
        });

        Assert.Contains("Unable to convert", exception.Message, StringComparison.Ordinal);
        Assert.Contains("not-a-date", exception.Message, StringComparison.Ordinal);
        Assert.Contains("DateOnly", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Write_Should_Write_DateOnly_As_Iso8601_String()
    {
        // Arrange
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        var converter = new KustoDateOnlyJsonConverter();

        // Act
        converter.Write(writer, new DateOnly(2024, 1, 15), JsonSerializerOptions.Default);
        writer.Flush();

        // Assert
        var json = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Equal("\"2024-01-15\"", json);
    }

    [Fact]
    public void Write_Should_Throw_ArgumentNullException_When_Writer_Is_Null()
    {
        // Arrange
        var converter = new KustoDateOnlyJsonConverter();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => converter.Write(writer: null!, new DateOnly(2024, 1, 15), JsonSerializerOptions.Default));
    }

    [Theory]
    [InlineData("""{"EventDate":"2024-01-15T00:00:00Z","Count":42}""", 2024, 1, 15, 42)] // Kusto datetime string
    [InlineData("""{"EventDate":"2024-03-20","Count":7}""", 2024, 3, 20, 7)] // Date-only string
    public void Converter_Should_Work_With_JsonSerializer(
        string json,
        int expectedYear,
        int expectedMonth,
        int expectedDay,
        int expectedCount)
    {
        // Arrange
        var options = new JsonSerializerOptions
        {
            Converters = { new KustoDateOnlyJsonConverter() },
        };

        // Act
        var result = JsonSerializer.Deserialize<TestDateOnlyDto>(json, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(new DateOnly(expectedYear, expectedMonth, expectedDay), result.EventDate);
        Assert.Equal(expectedCount, result.Count);
    }

    [Fact]
    public void Converter_Should_Serialize_DateOnly_As_Iso8601_String()
    {
        // Arrange
        var dto = new TestDateOnlyDto { EventDate = new DateOnly(2024, 1, 15), Count = 10 };
        var options = new JsonSerializerOptions
        {
            Converters = { new KustoDateOnlyJsonConverter() },
        };

        // Act
        var json = JsonSerializer.Serialize(dto, options);

        // Assert
        Assert.Contains("\"2024-01-15\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("T00:00:00", json, StringComparison.Ordinal);
    }

    private sealed record TestDateOnlyDto
    {
        public DateOnly EventDate { get; init; }

        public int Count { get; init; }
    }
}