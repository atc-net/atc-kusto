namespace Atc.Kusto.Tests.Providers.Internal;

public sealed class QueryIdProviderTests
{
    private readonly QueryIdProvider sut = new();

    [Theory, AutoNSubstituteData]
    public void Create_WithValidSessionId_ReturnsExpectedQueryId(
        Type queryType,
        string sessionId)
    {
        // Arrange & Act
        var actual = sut.Create(queryType, sessionId);

        // Assert
        actual
            .Should()
            .Be($"{queryType.Name}{sessionId.ToAlphaNumeric()}");
    }

    [Theory, AutoNSubstituteData]
    public void Create_WithNullSessionId_ReturnsQueryIdWithNewGuid(
        Type queryType)
    {
        // Arrange & Act
        var actual = sut.Create(queryType, sessionId: null);

        // Assert
        actual
            .Should().StartWith(queryType.Name)
            .And.HaveLength(queryType.Name.Length + Guid.NewGuid().ToString("N").Length);
    }
}