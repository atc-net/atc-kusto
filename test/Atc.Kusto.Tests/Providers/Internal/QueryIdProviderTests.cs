namespace Atc.Kusto.Tests.Providers.Internal;

public sealed class QueryIdProviderTests
{
    private readonly QueryIdProvider sut = new();

    [Fact]
    public void Create_Returns_The_Query_Type_Name_Followed_By_A_Hash()
    {
        // Act
        var actual = sut.Create(new PartnerQuery("partner-a"), sessionId: null, nonce: "nonce1");

        // Assert
        actual.Should().StartWith(nameof(PartnerQuery));
        actual.Should().HaveLength(nameof(PartnerQuery).Length + 32);
        actual.Should().MatchRegex("^[a-zA-Z0-9]+$");
    }

    [Fact]
    public void Create_Returns_The_Same_Fixed_QueryId_On_Every_Instance_And_Version()
    {
        // Arrange - page 1 and page 2 can be served by different instances, so the same input must
        // always give the same queryId
        var query = new PartnerQuery("partner-a");

        // Act
        var actual = sut.Create(query, sessionId: "session-1", nonce: null);

        // Assert
        actual.Should().Be("PartnerQuery3abf96e56f8a2e9d5910a1b866b39124");
    }

    [Fact]
    public void Create_Returns_Another_QueryId_For_Another_Parameter_Value()
    {
        // Arrange - a token from partner A, replayed for partner B, must not read partner A's rows
        var partnerA = new PartnerQuery("partner-a");
        var partnerB = new PartnerQuery("partner-b");

        // Act
        var queryIdA = sut.Create(partnerA, sessionId: null, nonce: "nonce1");
        var queryIdB = sut.Create(partnerB, sessionId: null, nonce: "nonce1");

        // Assert
        queryIdB.Should().NotBe(queryIdA);
    }

    [Fact]
    public void Create_Returns_Another_QueryId_For_Another_Query_Text()
    {
        // Arrange
        var query = new PartnerQuery("partner-a", "Devices | where partner == partnerName");
        var changedQuery = new PartnerQuery("partner-a", "Devices | where partner == partnerName | take 10");

        // Act & Assert
        sut.Create(changedQuery, sessionId: null, nonce: "nonce1")
            .Should().NotBe(sut.Create(query, sessionId: null, nonce: "nonce1"));
    }

    [Theory]
    [InlineData("session-1", null, "session-2", null)]
    [InlineData(null, "nonce1", null, "nonce2")]
    [InlineData(null, null, "", null)]
    [InlineData("ab", "c", "a", "bc")]
    public void Create_Returns_Another_QueryId_When_The_Session_Or_Nonce_Differs(
        string? sessionId,
        string? nonce,
        string? otherSessionId,
        string? otherNonce)
    {
        // Arrange
        var query = new PartnerQuery("partner-a");

        // Act & Assert
        sut.Create(query, otherSessionId, otherNonce)
            .Should().NotBe(sut.Create(query, sessionId, nonce));
    }

    [Fact]
    public void Create_Returns_The_Same_QueryId_For_The_Same_Input()
    {
        // Act & Assert
        sut.Create(new PartnerQuery("partner-a"), "session-1", nonce: null)
            .Should().Be(sut.Create(new PartnerQuery("partner-a"), "session-1", nonce: null));
    }

    [Fact]
    public void Create_Returns_The_Same_QueryId_In_Any_Culture()
    {
        // Arrange - parameters whose text could depend on the culture
        var query = new MeasurementQuery(1234.5, new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc));
        var original = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            var invariantQueryId = sut.Create(query, "session-1", nonce: null);

            // Act
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("da-DK");
            var danishQueryId = sut.Create(query, "session-1", nonce: null);

            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("th-TH");
            var thaiQueryId = sut.Create(query, "session-1", nonce: null);

            // Assert
            danishQueryId.Should().Be(invariantQueryId);
            thaiQueryId.Should().Be(invariantQueryId);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    private sealed record PartnerQuery : KustoQuery<string>
    {
        private readonly string queryText;

        public PartnerQuery(
            string partnerName,
            string queryText = "Devices | where partner == partnerName")
        {
            PartnerName = partnerName;
            this.queryText = queryText;
        }

        public string PartnerName { get; }

        public override string GetQueryText() => queryText;
    }

    private sealed record MeasurementQuery(
        double Threshold,
        DateTime From) : KustoQuery<string>
    {
        public override string GetQueryText()
            => "Measurements | where value > threshold and time > from";
    }
}