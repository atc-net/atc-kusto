namespace Atc.Kusto.Analyzer.CodeFixes.Tests;

[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "Test class naming convention")]
[SuppressMessage("Blocker Code Smell", "S2699:Tests should include assertions", Justification = "Roslyn test framework handles assertions internally")]
public class MissingKustoScriptResourceCodeFixProviderTests
{
    [Fact]
    public async Task CodeFixProvider_HasCorrectFixableDiagnosticIds()
    {
        var codeFixProvider = new MissingKustoScriptResourceCodeFixProvider();
        var fixableIds = codeFixProvider.FixableDiagnosticIds;

        Assert.Single(fixableIds);
        Assert.Equal("ATCK301", fixableIds[0]);
    }

    [Fact]
    public async Task CodeFixProvider_HasFixAllProvider()
    {
        var codeFixProvider = new MissingKustoScriptResourceCodeFixProvider();
        var fixAllProvider = codeFixProvider.GetFixAllProvider();

        Assert.NotNull(fixAllProvider);
    }
}