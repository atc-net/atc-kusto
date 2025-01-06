namespace Atc.Kusto.Tests.Serialization.Internal;

public sealed class NewtonsoftObjectConverterTests
{
    private readonly JsonSerializerOptions jsonSerializerOptions = JsonSerializerOptionsFactory.Create();

    [Theory]
    [InlineAutoNSubstituteData(typeof(object), true)]
    [InlineAutoNSubstituteData(typeof(string), false)]
    [InlineAutoNSubstituteData(typeof(int), false)]
    [InlineAutoNSubstituteData(typeof(NewtonsoftObjectConverterTests), false)]
    internal void CanConvert_ShouldReturnExpectedResult(
        Type objectType,
        bool expected,
        NewtonsoftObjectConverter sut)
    {
        // Arrange & Act
        var actual = sut.CanConvert(objectType);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Theory, AutoNSubstituteData]
    internal void ReadJson_ShouldConvertJTokenToJsonElement(
        Dictionary<string, string> data,
        NewtonsoftObjectConverter sut)
    {
        // Arrange
        var json = JsonSerializer.Serialize(
            data,
            jsonSerializerOptions);

        var jToken = Newtonsoft.Json.Linq.JToken.Parse(json);
        using var reader = new Newtonsoft.Json.Linq.JTokenReader(jToken);
        reader.Read();

        // Act
        var actual = sut.ReadJson(
            reader: reader,
            objectType: typeof(object),
            existingValue: null,
            serializer: null!);

        // Assert
        actual
            .Should()
            .BeAssignableTo<JsonElement>();

        ((JsonElement)actual!)
            .GetRawText()
            .Should()
            .BeEquivalentTo(json);
    }

    [Fact]
    public void ReadJson_ShouldReturnNullForInvalidReader()
    {
        // Arrange
        using var reader = new Newtonsoft.Json.JsonTextReader(new StringReader(""));
        var converter = new NewtonsoftObjectConverter();

        // Act
        var actual = converter.ReadJson(reader, typeof(object), null, null!);

        // Assert
        Assert.Null(actual);
    }

    [Fact]
    public void WriteJson_ShouldThrowNotSupportedException()
    {
        // Arrange
        var converter = new NewtonsoftObjectConverter();

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => converter.WriteJson(null!, null, null!));
    }
}