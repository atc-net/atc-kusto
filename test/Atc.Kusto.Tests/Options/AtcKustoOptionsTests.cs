namespace Atc.Kusto.Tests.Options;

public sealed class AtcKustoOptionsTests
{
    [Fact]
    public void DefaultIngestionMode_Defaults_To_ManagedStreaming()
        => new AtcKustoOptions().DefaultIngestionMode.Should().Be(IngestionMode.ManagedStreaming);

    [Fact]
    public void SerializerOptions_Defaults_To_CamelCase_Names_And_Case_Insensitive_Reading()
    {
        // Act
        var serializerOptions = new AtcKustoOptions().SerializerOptions;

        // Assert
        serializerOptions.PropertyNamingPolicy.Should().Be(JsonNamingPolicy.CamelCase);
        serializerOptions.PropertyNameCaseInsensitive.Should().BeTrue();
        serializerOptions.Converters.Should().ContainSingle(c => c is KustoBooleanJsonConverter);
        serializerOptions.Converters.Should().ContainSingle(c => c is KustoDateOnlyJsonConverter);
    }

    [Fact]
    public void SerializerOptions_Is_A_Separate_Instance_For_Each_Connection()
        => new AtcKustoOptions().SerializerOptions.Should().NotBeSameAs(new AtcKustoOptions().SerializerOptions);

    [Fact]
    public void ConfigureSerializerOptions_Changes_The_Settings_And_Keeps_The_Other_Defaults()
    {
        // Arrange
        var options = new AtcKustoOptions();

        // Act
        var returned = options.ConfigureSerializerOptions(json => json.PropertyNamingPolicy = null);

        // Assert
        returned.Should().BeSameAs(options);
        options.SerializerOptions.PropertyNamingPolicy.Should().BeNull();
        options.SerializerOptions.PropertyNameCaseInsensitive.Should().BeTrue();
    }

    [Fact]
    public void PagedResultExpiration_Defaults_To_One_Hour()
        => new AtcKustoOptions().PagedResultExpiration.Should().Be(TimeSpan.FromHours(1));

    [Fact]
    public void IngestUploadContainers_Defaults_To_An_Empty_Mutable_List()
    {
        // Arrange
        var options = new AtcKustoOptions();

        // Act
        options.IngestUploadContainers.Add(new Uri("https://account.blob.core.windows.net/ingest"));

        // Assert
        options.IngestUploadContainers.Should().ContainSingle();
        new AtcKustoOptions().IngestUploadContainers.Should().BeEmpty();
    }
}