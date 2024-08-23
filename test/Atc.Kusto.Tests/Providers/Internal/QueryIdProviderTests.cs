namespace Atc.Kusto.Tests.Providers.Internal;

public sealed class QueryIdProviderTests
{
    [Theory, AutoNSubstituteData]
    internal void Create_WithValidSessionId_ReturnsExpectedQueryId(
        QueryIdProvider sut,
        Type queryType,
        string sessionId)
    {
        // Arrange & Act
        var actual = sut.Create(queryType, sessionId);

        // Assert
        actual
            .Should()
            .Be($"{queryType.Name}{sessionId.ToAlphanumeric()}");
    }

    [Theory, AutoNSubstituteData]
    internal void Create_WithNullSessionId_ReturnsQueryIdWithNewGuid(
        QueryIdProvider sut,
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