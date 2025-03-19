namespace Atc.Kusto.Tests.Extensions;

public sealed class ClientRequestPropertiesExtensionsTests
{
    [Fact]
    public void SetQueryOptions_WithStreamingOptions_SetsProgressiveFlag()
    {
        // Arrange
        var props = new ClientRequestProperties();
        var options = new AtcStreamingQueryOptions { ProgressiveEnabled = true };

        // Act
        props.SetQueryOptions(options);

        // Assert
        Assert.True(props.TryGetOptionValue(ClientRequestProperties.OptionResultsProgressiveEnabled, out bool value));
        Assert.True(value);
    }

    [Fact]
    public void SetQueryOptions_WithBaseOptions_MapsExpectedValues()
    {
        // Arrange
        var props = new ClientRequestProperties();
        var options = new AtcQueryOptions
        {
            QueryTakeMaxRecords = 123,
            NoTruncation = true,
            TruncationMaxRecords = 999,
            TruncationMaxSize = 2048,
        };

        // Act
        props.SetQueryOptions(options);

        // Assert
        Assert.True(props.TryGetOptionValue<int>(ClientRequestProperties.OptionTakeMaxRecords, out var takeMaxRecords));
        Assert.Equal(123, takeMaxRecords);

        Assert.True(props.TryGetOptionValue<bool>(ClientRequestProperties.OptionNoTruncation, out var noTruncation));
        Assert.True(noTruncation);

        Assert.True(props.TryGetOptionValue<int>(ClientRequestProperties.OptionTruncationMaxRecords, out var truncationMaxRecords));
        Assert.Equal(999, truncationMaxRecords);

        Assert.True(props.TryGetOptionValue<int>(ClientRequestProperties.OptionTruncationMaxSize, out var truncationMaxSize));
        Assert.Equal(2048, truncationMaxSize);
    }
}