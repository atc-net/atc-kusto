namespace Atc.Kusto.Analyzer.Tests.Rules.Usage;

[SuppressMessage("", "AsyncFixer01:The method does not need to use async/await", Justification = "OK - Test code")]
[SuppressMessage("Naming", "MA0048:File name must match type name", Justification = "OK - Partial class")]
public sealed partial class EmptyKustoScriptFileAnalyzerTests
{
    [Fact]
    public async Task ReportsDiagnostic_EmptyKustoFile()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public abstract class KustoScript
                            {
                            }

                            public class {|#0:MyQuery|} : KustoScript
                            {
                            }
                            """;

        const string kustoFileContent = "";

        var expected = new DiagnosticResult(RuleIdentifierConstants.Usage.EmptyKustoScriptFile, DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("MyQuery", "Test0.kusto");

        var test = new CSharpAnalyzerTest<EmptyKustoScriptFileAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
            ExpectedDiagnostics = { expected },
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsDiagnostic_OnlyWhitespaceInKustoFile()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public abstract class KustoScript
                            {
                            }

                            public class {|#0:MyQuery|} : KustoScript
                            {
                            }
                            """;

        const string kustoFileContent = """



                                        """;

        var expected = new DiagnosticResult(RuleIdentifierConstants.Usage.EmptyKustoScriptFile, DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("MyQuery", "Test0.kusto");

        var test = new CSharpAnalyzerTest<EmptyKustoScriptFileAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
            ExpectedDiagnostics = { expected },
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsDiagnostic_OnlySingleLineComments()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public abstract class KustoScript
                            {
                            }

                            public class {|#0:MyQuery|} : KustoScript
                            {
                            }
                            """;

        const string kustoFileContent = """
                                        // TODO: Add query here
                                        // This is a comment
                                        """;

        var expected = new DiagnosticResult(RuleIdentifierConstants.Usage.EmptyKustoScriptFile, DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("MyQuery", "Test0.kusto");

        var test = new CSharpAnalyzerTest<EmptyKustoScriptFileAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
            ExpectedDiagnostics = { expected },
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsDiagnostic_OnlyMultiLineComments()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public abstract class KustoScript
                            {
                            }

                            public class {|#0:MyQuery|} : KustoScript
                            {
                            }
                            """;

        const string kustoFileContent = """
                                        /* This is a multi-line comment
                                           that spans several lines
                                           but contains no actual query */
                                        """;

        var expected = new DiagnosticResult(RuleIdentifierConstants.Usage.EmptyKustoScriptFile, DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("MyQuery", "Test0.kusto");

        var test = new CSharpAnalyzerTest<EmptyKustoScriptFileAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
            ExpectedDiagnostics = { expected },
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsDiagnostic_OnlyParametersDeclaration()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public abstract class KustoScript
                            {
                            }

                            public class {|#0:MyQuery|}(long CustomerId) : KustoScript
                            {
                            }
                            """;

        const string kustoFileContent = """
                                        declare query_parameters (
                                            customerId:long
                                        );
                                        """;

        var expected = new DiagnosticResult(RuleIdentifierConstants.Usage.EmptyKustoScriptFile, DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("MyQuery", "Test0.kusto");

        var test = new CSharpAnalyzerTest<EmptyKustoScriptFileAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
            ExpectedDiagnostics = { expected },
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsDiagnostic_MixedCommentsAndParametersButNoQuery()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public abstract class KustoScript
                            {
                            }

                            public class {|#0:MyQuery|}(long CustomerId, string Name) : KustoScript
                            {
                            }
                            """;

        const string kustoFileContent = """
                                        // Query for customers
                                        /* Define parameters below */
                                        declare query_parameters (
                                            customerId:long,
                                            name:string
                                        );
                                        // TODO: Add query implementation
                                        """;

        var expected = new DiagnosticResult(RuleIdentifierConstants.Usage.EmptyKustoScriptFile, DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("MyQuery", "Test0.kusto");

        var test = new CSharpAnalyzerTest<EmptyKustoScriptFileAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
            ExpectedDiagnostics = { expected },
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsDiagnostic_CaseInsensitiveParameterDeclaration()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public abstract class KustoScript
                            {
                            }

                            public class {|#0:MyQuery|}(long CustomerId) : KustoScript
                            {
                            }
                            """;

        const string kustoFileContent = """
                                        DECLARE QUERY_PARAMETERS (
                                            customerId:long
                                        );
                                        """;

        var expected = new DiagnosticResult(RuleIdentifierConstants.Usage.EmptyKustoScriptFile, DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("MyQuery", "Test0.kusto");

        var test = new CSharpAnalyzerTest<EmptyKustoScriptFileAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
            ExpectedDiagnostics = { expected },
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }
}