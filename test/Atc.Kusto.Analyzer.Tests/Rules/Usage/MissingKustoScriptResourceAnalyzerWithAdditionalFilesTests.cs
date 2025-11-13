namespace Atc.Kusto.Analyzer.Tests.Rules.Usage;

[SuppressMessage("", "AsyncFixer01:The method does not need to use async/await", Justification = "OK - Test code")]
public sealed class MissingKustoScriptResourceAnalyzerWithAdditionalFilesTests
{
    [Fact]
    public async Task NoDiagnostic_WhenKustoFileExistsInAdditionalFiles()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public abstract class KustoScript
                            {
                            }

                            public class SampleQuery : KustoScript
                            {
                            }
                            """;

        const string kustoFileContent = """
                                        // Sample Kusto query
                                        Customers
                                        | take 100
                                        """;

        var test = new CSharpAnalyzerTest<MissingKustoScriptResourceAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
        };

        // Add the .kusto file as an AdditionalFile
        // Must match the test file name which defaults to Test0.cs, so we need Test0.kusto
        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsDiagnostic_WhenKustoFileNotInAdditionalFiles()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public abstract class KustoScript
                            {
                            }

                            public class [|SampleQuery|] : KustoScript
                            {
                            }
                            """;

        var test = new CSharpAnalyzerTest<MissingKustoScriptResourceAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
        };

        // No AdditionalFiles added - should report diagnostic

        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenMultipleKustoFilesExist()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public abstract class KustoScript
                            {
                            }

                            public class FirstQuery : KustoScript
                            {
                            }

                            public class SecondQuery : KustoScript
                            {
                            }
                            """;

        const string kustoFileContent = "Customers | take 10";

        var test = new CSharpAnalyzerTest<MissingKustoScriptResourceAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
        };

        // Test file is Test0.cs, so we need Test0.kusto for both classes in the same file
        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsDiagnostic_WhenOnlyOneKustoFileMissing()
    {
        // This test needs two separate files to have different .kusto file requirements
        // We'll use two test files: Test0.cs has FirstQuery, Test1.cs has SecondQuery
        const string firstFile = """
                            namespace Atc.Kusto;

                            public abstract class KustoScript
                            {
                            }

                            public class FirstQuery : KustoScript
                            {
                            }
                            """;

        const string secondFile = """
                            namespace Atc.Kusto;

                            public class [|SecondQuery|] : KustoScript
                            {
                            }
                            """;

        const string firstKustoFile = "Customers | take 10";

        var test = new CSharpAnalyzerTest<MissingKustoScriptResourceAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = firstFile,
        };

        // Add second test file
        test.TestState.Sources.Add(secondFile);

        // Add only Test0.kusto (for FirstQuery) - Test1.kusto (for SecondQuery) is missing
        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", firstKustoFile));

        await test.RunAsync();
    }
}
