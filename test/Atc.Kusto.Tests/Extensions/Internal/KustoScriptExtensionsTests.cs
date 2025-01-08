namespace Atc.Kusto.Tests.Extensions.Internal;

public sealed class KustoScriptExtensionsTests
{
    public static TheoryData<object, string> CslValueTestData => new()
    {
        { true, "true" },
        { false, "false" },
        { 123, "123" },
        { 123L, "123" },
        { 123.12M, "decimal(123.12)" },
        { 123.12D, "123.12" },
        { new TimeSpan(1, 2, 3), "time(01:02:03)" },
        { new DateTime(1, 2, 3, 4, 5, 6, DateTimeKind.Utc), "datetime(0001-02-03T04:05:06.0000000Z)" },
        { new DateTimeOffset(1, 2, 3, 4, 5, 6, TimeSpan.Zero), "datetime(0001-02-03T04:05:06.0000000Z)" },
        { "string", "\"string\"" },
        { new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "key", "value" } }, "dynamic({\"key\":\"value\"})" },
    };

    [Theory]
    [MemberAutoNSubstituteData(nameof(CslValueTestData))]
    public void GetClientRequestProperties_ShouldGenerateValidProperties(
        object data,
        string cslRepresentationData,
        IKustoQuery<TestRecord> query,
        string[] parameterNames)
    {
        query
            .GetParameters()
            .Returns(parameterNames
                .ToDictionary(
                    n => n,
                    _ => data));

        var actual = query.GetClientRequestProperties();

        actual
            .Should()
            .NotBeNull();

        actual.ClientRequestId
            .Should()
            .NotBeNullOrEmpty();

        actual.Parameters
            .Should()
            .BeEquivalentTo(parameterNames
                .ToDictionary(
                    n => n,
                    _ => cslRepresentationData));
    }

    [Theory]
    [MemberAutoNSubstituteData(nameof(CslValueTestData))]
    public void GetCslParameters_Returns_Cls_Converted_Query_Parameters(
        object data,
        string cslRepresentationData,
        IKustoQuery<TestRecord> query,
        string[] parameterNames)
    {
        query
            .GetParameters()
            .Returns(parameterNames
                .ToDictionary(
                    n => n,
                    _ => data));

        query
            .GetCslParameters()
            .Should()
            .BeEquivalentTo(parameterNames
                .ToDictionary(
                    n => n,
                    _ => cslRepresentationData));
    }

    [Theory]
    [MemberData(nameof(CslValueTestData))]
    public void GetClsValue_Returns_Cls_Formatted_String(
        object input,
        string output)
        => KustoScriptExtensions
            .GetCslValue(input)
            .Should()
            .Be(output);
}