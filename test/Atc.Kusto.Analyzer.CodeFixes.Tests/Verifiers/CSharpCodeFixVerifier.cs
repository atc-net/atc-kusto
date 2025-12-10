namespace Atc.Kusto.Analyzer.CodeFixes.Tests.Verifiers;

/// <summary>
/// Verifier for code fix providers.
/// </summary>
/// <typeparam name="TAnalyzer">The analyzer type.</typeparam>
/// <typeparam name="TCodeFix">The code fix provider type.</typeparam>
internal static class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    /// <summary>
    /// Verifies that the code fix produces the expected fixed code.
    /// </summary>
    /// <param name="source">The source code with diagnostics.</param>
    /// <param name="fixedSource">The expected fixed code.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static Task VerifyCodeFixAsync(
        string source,
        string fixedSource)
    {
        // Normalize line endings to LF for cross-platform compatibility
        var normalizedSource = source.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);
        var normalizedFixedSource = fixedSource.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);

        var test = new CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = normalizedSource,
            FixedCode = normalizedFixedSource,
        };

        // Configure consistent line endings for cross-platform compatibility
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", "root = true\n\n[*]\nend_of_line = lf\n"));

        return test.RunAsync();
    }

    /// <summary>
    /// Verifies that the code fix produces the expected fixed code with specific diagnostics.
    /// </summary>
    /// <param name="source">The source code with diagnostics.</param>
    /// <param name="expected">The expected diagnostic results.</param>
    /// <param name="fixedSource">The expected fixed code.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static Task VerifyCodeFixAsync(
        string source,
        DiagnosticResult[] expected,
        string fixedSource)
    {
        // Normalize line endings to LF for cross-platform compatibility
        var normalizedSource = source.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);
        var normalizedFixedSource = fixedSource.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);

        var test = new CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = normalizedSource,
            FixedCode = normalizedFixedSource,
        };

        // Configure consistent line endings for cross-platform compatibility
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", "root = true\n\n[*]\nend_of_line = lf\n"));

        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }
}