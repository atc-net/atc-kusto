namespace Atc.Kusto.Analyzer.Tests.Rules.Usage;

[SuppressMessage("", "AsyncFixer01:The method does not need to use async/await", Justification = "OK - Test code")]
[SuppressMessage("Naming", "MA0048:File name must match type name", Justification = "OK - Partial class")]
[SuppressMessage("Critical Code Smell", "S2699:Add at least one assertion to this test case", Justification = "OK - test.RunAsync() is the assertion in Roslyn analyzer tests")]
public sealed partial class KustoParameterMismatchAnalyzerTests
{
    [Fact]
    public async Task NoDiagnostic_NoParameters_BothEmpty()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public abstract class KustoScript
                            {
                            }

                            public class MyQuery : KustoScript
                            {
                            }
                            """;

        const string kustoFileContent = """
                                        Customers | take 10
                                        """;

        var test = new CSharpAnalyzerTest<KustoParameterMismatchAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_SingleParameter_Match()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public abstract class KustoScript
                            {
                            }

                            public class MyQuery(long CustomerId) : KustoScript
                            {
                            }
                            """;

        const string kustoFileContent = """
                                        declare query_parameters (
                                            customerId:long
                                        );
                                        Customers | take 10
                                        """;

        var test = new CSharpAnalyzerTest<KustoParameterMismatchAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_NullableParameter_WithDefault()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public abstract class KustoScript
                            {
                            }

                            public class MyQuery(string? PartnerName) : KustoScript
                            {
                            }
                            """;

        const string kustoFileContent = """
                                        declare query_parameters (
                                            partnerName:string = ""
                                        );
                                        Partners | take 10
                                        """;

        var test = new CSharpAnalyzerTest<KustoParameterMismatchAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_NonNullableParameter_WithDefault()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public abstract class KustoScript
                            {
                            }

                            public class MyQuery(string PartnerName) : KustoScript
                            {
                            }
                            """;

        const string kustoFileContent = """
                                        declare query_parameters (
                                            partnerName:string = ""
                                        );
                                        Partners | take 10
                                        """;

        var test = new CSharpAnalyzerTest<KustoParameterMismatchAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_MultipleParameters_AllMatch()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public abstract class KustoScript
                            {
                            }

                            public class MyQuery(int MaxItems, string? FilterGuid, string? FilterString) : KustoScript
                            {
                            }
                            """;

        const string kustoFileContent = """
                                        declare query_parameters (
                                            maxItems:int,
                                            filterGuid:string = "",
                                            filterString:string = ""
                                        );
                                        Customers | take 10
                                        """;

        var test = new CSharpAnalyzerTest<KustoParameterMismatchAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_AllSupportedTypes()
    {
        const string code = """
                            namespace Atc.Kusto;
                            using System;

                            public abstract class KustoScript
                            {
                            }

                            public class MyQuery(
                                long LongParam,
                                int IntParam,
                                string StringParam,
                                DateTime DateTimeParam,
                                bool BoolParam,
                                double DoubleParam,
                                Guid GuidParam,
                                TimeSpan TimeSpanParam) : KustoScript
                            {
                            }
                            """;

        const string kustoFileContent = """
                                        declare query_parameters (
                                            longParam:long,
                                            intParam:int,
                                            stringParam:string,
                                            dateTimeParam:datetime,
                                            boolParam:bool,
                                            doubleParam:real,
                                            guidParam:guid,
                                            timeSpanParam:timespan
                                        );
                                        Customers | take 10
                                        """;

        var test = new CSharpAnalyzerTest<KustoParameterMismatchAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_CaseInsensitiveMatch()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public abstract class KustoScript
                            {
                            }

                            public class MyQuery(long CustomerId) : KustoScript
                            {
                            }
                            """;

        const string kustoFileContent = """
                                        declare query_parameters (
                                            CUSTOMERID:LONG
                                        );
                                        Customers | take 10
                                        """;

        var test = new CSharpAnalyzerTest<KustoParameterMismatchAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_RecordDeclaration()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public abstract record KustoScript;

                            public record MyQuery(long? CustomerId) : KustoScript;
                            """;

        const string kustoFileContent = """
                                        declare query_parameters (
                                            customerId:long = long(null)
                                        );
                                        Customers | take 10
                                        """;

        var test = new CSharpAnalyzerTest<KustoParameterMismatchAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }
}