namespace Atc.Kusto.Tests.Utilities.Internal;

public sealed class KustoBooleanJsonConverterTests
{
    [Fact]
    public void Read_Should_Return_True_When_Token_Is_JsonTrue()
    {
        // Arrange
        var json = "true"u8.ToArray();
        var reader = new Utf8JsonReader(json);
        reader.Read();
        var converter = new KustoBooleanJsonConverter();

        // Act
        var result = converter.Read(ref reader, typeof(bool), JsonSerializerOptions.Default);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Read_Should_Return_False_When_Token_Is_JsonFalse()
    {
        // Arrange
        var json = "false"u8.ToArray();
        var reader = new Utf8JsonReader(json);
        reader.Read();
        var converter = new KustoBooleanJsonConverter();

        // Act
        var result = converter.Read(ref reader, typeof(bool), JsonSerializerOptions.Default);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Read_Should_Return_False_When_Number_Is_Zero()
    {
        // Arrange - simulates Kusto returning 0 for false
        var json = "0"u8.ToArray();
        var reader = new Utf8JsonReader(json);
        reader.Read();
        var converter = new KustoBooleanJsonConverter();

        // Act
        var result = converter.Read(ref reader, typeof(bool), JsonSerializerOptions.Default);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Read_Should_Return_True_When_Number_Is_One()
    {
        // Arrange - simulates Kusto returning 1 for true
        var json = "1"u8.ToArray();
        var reader = new Utf8JsonReader(json);
        reader.Read();
        var converter = new KustoBooleanJsonConverter();

        // Act
        var result = converter.Read(ref reader, typeof(bool), JsonSerializerOptions.Default);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(42)]
    [InlineData(100)]
    [InlineData(255)]
    [InlineData(-1)]
    [InlineData(-42)]
    [InlineData(-100)]
    public void Read_Should_Return_True_When_Number_Is_NonZero(int value)
    {
        // Arrange - any non-zero value (positive or negative) should be treated as true
        var json = Encoding.UTF8.GetBytes(value.ToString(CultureInfo.InvariantCulture));
        var reader = new Utf8JsonReader(json);
        reader.Read();
        var converter = new KustoBooleanJsonConverter();

        // Act
        var result = converter.Read(ref reader, typeof(bool), JsonSerializerOptions.Default);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Read_Should_Throw_JsonException_When_Token_Is_String()
    {
        // Arrange
        var json = "\"invalid\""u8.ToArray();
        var converter = new KustoBooleanJsonConverter();

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() =>
        {
            var localReader = new Utf8JsonReader(json);
            localReader.Read();
            return converter.Read(ref localReader, typeof(bool), JsonSerializerOptions.Default);
        });

        Assert.Contains("Cannot convert", exception.Message, StringComparison.Ordinal);
        Assert.Contains("String", exception.Message, StringComparison.Ordinal);
        Assert.Contains("boolean", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_Should_Throw_JsonException_When_Token_Is_Null()
    {
        // Arrange
        var json = "null"u8.ToArray();
        var converter = new KustoBooleanJsonConverter();

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() =>
        {
            var localReader = new Utf8JsonReader(json);
            localReader.Read();
            return converter.Read(ref localReader, typeof(bool), JsonSerializerOptions.Default);
        });

        Assert.Contains("Cannot convert", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Null", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Write_Should_Write_True_As_JsonBoolean()
    {
        // Arrange
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        var converter = new KustoBooleanJsonConverter();

        // Act
        converter.Write(writer, value: true, JsonSerializerOptions.Default);
        writer.Flush();

        // Assert
        var json = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Equal("true", json);
    }

    [Fact]
    public void Write_Should_Write_False_As_JsonBoolean()
    {
        // Arrange
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        var converter = new KustoBooleanJsonConverter();

        // Act
        converter.Write(writer, value: false, JsonSerializerOptions.Default);
        writer.Flush();

        // Assert
        var json = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Equal("false", json);
    }

    [Fact]
    public void Write_Should_Throw_ArgumentNullException_When_Writer_Is_Null()
    {
        // Arrange
        var converter = new KustoBooleanJsonConverter();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => converter.Write(writer: null!, value: true, JsonSerializerOptions.Default));
    }

    [Fact]
    public void Converter_Should_Work_With_JsonSerializer_For_Numeric_Boolean()
    {
        // Arrange - end-to-end test with JsonSerializer using the converter
        const string json = "{\"IsActive\":1,\"IsEnabled\":0}";
        var options = new JsonSerializerOptions
        {
            Converters = { new KustoBooleanJsonConverter() },
        };

        // Act
        var result = JsonSerializer.Deserialize<TestBooleanDto>(json, options);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsActive);
        Assert.False(result.IsEnabled);
    }

    [Fact]
    public void Converter_Should_Work_With_JsonSerializer_For_Standard_Boolean()
    {
        // Arrange - verify it still works with standard JSON booleans
        const string json = "{\"IsActive\":true,\"IsEnabled\":false}";
        var options = new JsonSerializerOptions
        {
            Converters = { new KustoBooleanJsonConverter() },
        };

        // Act
        var result = JsonSerializer.Deserialize<TestBooleanDto>(json, options);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsActive);
        Assert.False(result.IsEnabled);
    }

    [Fact]
    public void Converter_Should_Serialize_Booleans_As_Standard_Json()
    {
        // Arrange
        var dto = new TestBooleanDto { IsActive = true, IsEnabled = false };
        var options = new JsonSerializerOptions
        {
            Converters = { new KustoBooleanJsonConverter() },
        };

        // Act
        var json = JsonSerializer.Serialize(dto, options);

        // Assert
        Assert.Contains("\"IsActive\":true", json, StringComparison.Ordinal);
        Assert.Contains("\"IsEnabled\":false", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"IsActive\":1", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"IsEnabled\":0", json, StringComparison.Ordinal);
    }

    private sealed record TestBooleanDto
    {
        public bool IsActive { get; init; }

        public bool IsEnabled { get; init; }
    }
}