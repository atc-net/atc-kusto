namespace Atc.Kusto.Tests.Serialization.Internal;

public sealed class NewtonsoftDecimalConverterTests
{
    [Theory]
    [InlineAutoNSubstituteData(typeof(decimal), true)]
    [InlineAutoNSubstituteData(typeof(decimal?), true)]
    [InlineAutoNSubstituteData(typeof(double), false)]
    [InlineAutoNSubstituteData(typeof(int), false)]
    [InlineAutoNSubstituteData(typeof(string), false)]
    internal void CanConvert_ShouldReturnExpectedResult(
        Type objectType,
        bool expected,
        NewtonsoftDecimalConverter sut)
    {
        // Act
        var actual = sut.CanConvert(objectType);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Theory, AutoNSubstituteData]
    internal void ReadJson_ShouldConvertJTokenToDecimal(
        decimal value,
        NewtonsoftDecimalConverter sut)
    {
        // Arrange

        // Simulate a JToken containing an object with a Value property similar to SqlDecimal serialization.
        var jToken = new Newtonsoft.Json.Linq.JObject
        {
            ["Value"] = value,

            // Extra noise to ensure we only pick the intended property.
            ["Other"] = 123,
        };

        using var reader = new Newtonsoft.Json.Linq.JTokenReader(jToken);
        reader.Read();

        // Act
        var actual = sut.ReadJson(
            reader: reader,
            objectType: typeof(decimal),
            existingValue: null,
            serializer: null!);

        // Assert
        actual
            .Should()
            .BeOfType<decimal>()
            .And
            .Be(value);
    }

    [Fact]
    public void ReadJson_ShouldReturnNullForInvalidReader()
    {
        // Arrange
        using var reader = new Newtonsoft.Json.JsonTextReader(new StringReader(""));
        var converter = new NewtonsoftDecimalConverter();

        // Act
        var actual = converter.ReadJson(reader, typeof(decimal), null, null!);

        // Assert
        Assert.Null(actual);
    }

    [Fact]
    public void WriteJson_ShouldThrowNotSupportedException()
    {
        // Arrange
        var converter = new NewtonsoftDecimalConverter();

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => converter.WriteJson(null!, null, null!));
    }
}