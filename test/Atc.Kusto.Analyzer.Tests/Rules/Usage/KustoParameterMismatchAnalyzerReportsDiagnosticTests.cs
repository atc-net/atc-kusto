namespace Atc.Kusto.Analyzer.Tests.Rules.Usage;

[SuppressMessage("", "AsyncFixer01:The method does not need to use async/await", Justification = "OK - Test code")]
[SuppressMessage("Naming", "MA0048:File name must match type name", Justification = "OK - Partial class")]
[SuppressMessage("Critical Code Smell", "S2699:Add at least one assertion to this test case", Justification = "OK - test.RunAsync() is the assertion in Roslyn analyzer tests")]
public sealed partial class KustoParameterMismatchAnalyzerTests
{
    [Fact]
    public async Task ReportsDiagnostic_ATCK302_CSharpHasMoreParameters()
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
                                        declare query_parameters (
                                            customerId:long
                                        );
                                        Customers | take 10
                                        """;

        var expected = new DiagnosticResult(RuleIdentifierConstants.Usage.ParameterCountMismatch, DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("2", "1");

        var test = new CSharpAnalyzerTest<KustoParameterMismatchAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
            ExpectedDiagnostics = { expected },
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsDiagnostic_ATCK302_KustoHasMoreParameters()
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
                                            customerId:long,
                                            name:string
                                        );
                                        Customers | take 10
                                        """;

        var expected = new DiagnosticResult(RuleIdentifierConstants.Usage.ParameterCountMismatch, DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("1", "2");

        var test = new CSharpAnalyzerTest<KustoParameterMismatchAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
            ExpectedDiagnostics = { expected },
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsDiagnostic_ATCK302_CSharpHasParametersKustoHasNone()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public abstract class KustoScript
                            {
                            }

                            public class {|#0:MyQuery|}(long CustomerId, string Name, int Age) : KustoScript
                            {
                            }
                            """;

        const string kustoFileContent = """
                                        Customers | take 10
                                        """;

        var expected = new DiagnosticResult(RuleIdentifierConstants.Usage.ParameterCountMismatch, DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("3", "0");

        var test = new CSharpAnalyzerTest<KustoParameterMismatchAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
            ExpectedDiagnostics = { expected },
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsDiagnostic_ATCK302_KustoHasParametersCSharpHasNone()
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
                                        declare query_parameters (
                                            customerId:long,
                                            name:string
                                        );
                                        Customers | take 10
                                        """;

        var expected = new DiagnosticResult(RuleIdentifierConstants.Usage.ParameterCountMismatch, DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("0", "2");

        var test = new CSharpAnalyzerTest<KustoParameterMismatchAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
            ExpectedDiagnostics = { expected },
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsDiagnostic_ATCK303_TypeMismatch_LongVsString()
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
                                            customerId:string
                                        );
                                        Customers | take 10
                                        """;

        var expected = new DiagnosticResult(RuleIdentifierConstants.Usage.ParameterTypeMismatch, DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("CustomerId", "long", "string");

        var test = new CSharpAnalyzerTest<KustoParameterMismatchAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
            ExpectedDiagnostics = { expected },
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsDiagnostic_ATCK303_TypeMismatch_DateTimeVsInt()
    {
        const string code = """
                            namespace Atc.Kusto;
                            using System;

                            public abstract class KustoScript
                            {
                            }

                            public class {|#0:MyQuery|}(DateTime StartDate) : KustoScript
                            {
                            }
                            """;

        const string kustoFileContent = """
                                        declare query_parameters (
                                            startDate:int
                                        );
                                        Events | take 10
                                        """;

        var expected = new DiagnosticResult(RuleIdentifierConstants.Usage.ParameterTypeMismatch, DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("StartDate", "DateTime", "int");

        var test = new CSharpAnalyzerTest<KustoParameterMismatchAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
            ExpectedDiagnostics = { expected },
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsDiagnostic_ATCK304_ParameterOrderMismatch_TwoParameters()
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
                                        declare query_parameters (
                                            name:string,
                                            customerId:long
                                        );
                                        Customers | take 10
                                        """;

        var expected = new DiagnosticResult(RuleIdentifierConstants.Usage.ParameterOrderMismatch, DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("0", "CustomerId", "name");

        var test = new CSharpAnalyzerTest<KustoParameterMismatchAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
            ExpectedDiagnostics = { expected },
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsDiagnostic_ATCK304_ParameterOrderMismatch_MiddleSwapped()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public abstract class KustoScript
                            {
                            }

                            public class {|#0:MyQuery|}(int MaxItems, string FilterGuid, string FilterString) : KustoScript
                            {
                            }
                            """;

        const string kustoFileContent = """
                                        declare query_parameters (
                                            maxItems:int,
                                            filterString:string,
                                            filterGuid:string
                                        );
                                        Customers | take 10
                                        """;

        var expected = new DiagnosticResult(RuleIdentifierConstants.Usage.ParameterOrderMismatch, DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("1", "FilterGuid", "filterString");

        var test = new CSharpAnalyzerTest<KustoParameterMismatchAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
            ExpectedDiagnostics = { expected },
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }
}