namespace Atc.Kusto.Tests.Ingestion.Internal;

public sealed class MultiJsonSerializerTests
{
    private sealed record Person(string FirstName, int Age);

    [Fact]
    public void Serialize_Writes_One_Json_Object_Per_Line_With_Default_Options()
    {
        // Arrange
        var rows = new[] { new Person("Ada", 36), new Person("Linus", 54) };

        // Act
        using var stream = MultiJsonSerializer.Serialize(rows, serializerOptions: null);

        // Assert
        var text = Encoding.UTF8.GetString(stream.ToArray());
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        lines.Should().HaveCount(2);
        lines[0].Should().Be("{\"FirstName\":\"Ada\",\"Age\":36}");
        lines[1].Should().Be("{\"FirstName\":\"Linus\",\"Age\":54}");
        stream.Position.Should().Be(0);
    }

    [Fact]
    public void Serialize_Honours_Supplied_Options()
    {
        // Arrange
        var rows = new[] { new Person("Ada", 36) };
        var camel = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        // Act
        using var stream = MultiJsonSerializer.Serialize(rows, camel);

        // Assert
        var text = Encoding.UTF8.GetString(stream.ToArray()).TrimEnd('\n');

        text.Should().Be("{\"firstName\":\"Ada\",\"age\":36}");
    }

    [Fact]
    public void Serialize_Empty_Sequence_Yields_Empty_Stream()
    {
        // Act
        using var stream = MultiJsonSerializer.Serialize(Array.Empty<Person>(), serializerOptions: null);

        // Assert
        stream.Length.Should().Be(0);
        stream.Position.Should().Be(0);
    }
}