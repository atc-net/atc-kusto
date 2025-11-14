namespace Atc.Kusto.Analyzer.Tests.Verifiers;

/// <summary>
/// Verifier for C# analyzers - provides test infrastructure for analyzer verification.
/// </summary>
/// <typeparam name="TAnalyzer">The analyzer type.</typeparam>
[SuppressMessage("", "AsyncFixer01:The method does not need to use async/await", Justification = "OK - Test code")]
internal static class CSharpAnalyzerVerifier<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    /// <summary>
    /// Verifies that the analyzer produces no diagnostics for the given source code.
    /// </summary>
    /// <param name="source">The source code to analyze.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task VerifyAnalyzerAsync(string source)
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = source,
        };

        await test.RunAsync();
    }

    /// <summary>
    /// Verifies that the analyzer produces the expected diagnostics for the given source code.
    /// Use [| and |] markers in the source to indicate expected diagnostic locations.
    /// </summary>
    /// <param name="source">The source code to analyze.</param>
    /// <param name="expected">The expected diagnostic results.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task VerifyAnalyzerAsync(
        string source,
        params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = source,
        };

        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync();
    }
}