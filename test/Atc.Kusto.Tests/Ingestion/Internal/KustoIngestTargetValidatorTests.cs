namespace Atc.Kusto.Tests.Ingestion.Internal;

public sealed class KustoIngestTargetValidatorTests
{
    private static KustoIngestTarget Target(
        KustoIngestFormat format = KustoIngestFormat.MultiJson,
        string? mapping = "Mapping1",
        bool enableTracking = false)
        => new()
        {
            TableName = "T",
            Format = format,
            MappingReference = mapping,
            EnableTracking = enableTracking,
        };

    [Fact]
    public void Validate_Throws_When_Json_Without_Mapping()
    {
        var act = () => KustoIngestTargetValidator.Validate(
            Target(KustoIngestFormat.Json, mapping: null),
            IngestionMode.ManagedStreaming,
            knownPayloadByteLength: null,
            isInlineRows: false);

        act.Should().Throw<ArgumentException>().WithMessage("*MappingReference*");
    }

    [Fact]
    public void Validate_Throws_When_TableName_Is_Blank()
    {
        var target = new KustoIngestTarget { TableName = "  ", Format = KustoIngestFormat.Csv };

        var act = () => KustoIngestTargetValidator.Validate(
            target,
            IngestionMode.Queued,
            knownPayloadByteLength: null,
            isInlineRows: false);

        act.Should().Throw<ArgumentException>().WithMessage("*TableName*");
    }

    [Fact]
    public void Validate_Allows_Csv_Without_Mapping()
    {
        var act = () => KustoIngestTargetValidator.Validate(
            Target(KustoIngestFormat.Csv, mapping: null),
            IngestionMode.Queued,
            knownPayloadByteLength: null,
            isInlineRows: false);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(KustoIngestFormat.Csv)]
    [InlineData(KustoIngestFormat.Tsv)]
    public void Validate_Throws_When_Inline_Rows_Not_Json(
        KustoIngestFormat format)
    {
        var act = () => KustoIngestTargetValidator.Validate(
            Target(format),
            IngestionMode.ManagedStreaming,
            knownPayloadByteLength: null,
            isInlineRows: true);

        act.Should().Throw<ArgumentException>().WithMessage("*inline rows*");
    }

    [Fact]
    public void Validate_Throws_When_Streaming_Payload_Over_Cap()
    {
        var act = () => KustoIngestTargetValidator.Validate(
            Target(),
            IngestionMode.Streaming,
            knownPayloadByteLength: KustoIngestTargetValidator.StreamingIngestionMaxBytes + 1,
            isInlineRows: true);

        act.Should().Throw<ArgumentException>().WithMessage("*10 MB*");
    }

    [Fact]
    public void Validate_Allows_Streaming_Payload_At_Cap()
    {
        var act = () => KustoIngestTargetValidator.Validate(
            Target(),
            IngestionMode.Streaming,
            knownPayloadByteLength: KustoIngestTargetValidator.StreamingIngestionMaxBytes,
            isInlineRows: true);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(IngestionMode.ManagedStreaming)]
    [InlineData(IngestionMode.Queued)]
    public void Validate_Allows_Oversize_Payload_For_Non_Streaming_Modes(
        IngestionMode mode)
    {
        var act = () => KustoIngestTargetValidator.Validate(
            Target(),
            mode,
            knownPayloadByteLength: KustoIngestTargetValidator.StreamingIngestionMaxBytes + 1,
            isInlineRows: true);

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_Allows_Streaming_When_Payload_Length_Unknown()
    {
        var act = () => KustoIngestTargetValidator.Validate(
            Target(),
            IngestionMode.Streaming,
            knownPayloadByteLength: null,
            isInlineRows: false);

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_Throws_When_Tracking_On_Pure_Streaming()
    {
        var act = () => KustoIngestTargetValidator.Validate(
            Target(enableTracking: true),
            IngestionMode.Streaming,
            knownPayloadByteLength: null,
            isInlineRows: false);

        act.Should().Throw<ArgumentException>().WithMessage("*EnableTracking*");
    }

    [Fact]
    public void ValidatePayloadSize_Reports_The_Callers_Parameter_Name()
    {
        var act = () => KustoIngestTargetValidator.ValidatePayloadSize(
            IngestionMode.Streaming,
            KustoIngestTargetValidator.StreamingIngestionMaxBytes + 1,
            "data");

        act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("data");
    }

    [Fact]
    public void ValidateConnection_Throws_When_Credential_Missing()
    {
        var options = new AtcKustoOptions { HostAddress = new Uri("https://example.kusto.windows.net") };

        var act = () => KustoIngestTargetValidator.ValidateConnection(options, "Sales");

        act.Should().Throw<InvalidOperationException>().WithMessage("*HostAddress and Credential*Sales*");
    }

    [Fact]
    public void ValidateConnection_Throws_For_ConnectionString_Only()
    {
        var options = new AtcKustoOptions { ConnectionString = "Data Source=https://example.kusto.windows.net" };

        var act = () => KustoIngestTargetValidator.ValidateConnection(options, connectionName: null);

        act.Should().Throw<InvalidOperationException>().WithMessage("*(default)*");
    }

    [Fact]
    public void ValidateConnection_Allows_HostAddress_And_Credential()
    {
        var options = new AtcKustoOptions
        {
            HostAddress = new Uri("https://example.kusto.windows.net"),
            Credential = Substitute.For<Azure.Core.TokenCredential>(),
        };

        var act = () => KustoIngestTargetValidator.ValidateConnection(options, connectionName: null);

        act.Should().NotThrow();
    }
}