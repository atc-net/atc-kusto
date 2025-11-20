namespace Atc.Kusto.Analyzer.Tests.Rules.Usage;

#pragma warning disable SA1135 // Using directives must be qualified
using AnalyzerVerifier = CSharpAnalyzerVerifier<EmptyKustoScriptFileAnalyzer>;
#pragma warning restore SA1135 // Using directives must be qualified

[SuppressMessage("", "AsyncFixer01:The method does not need to use async/await", Justification = "OK - Test code")]
[SuppressMessage("Naming", "MA0048:File name must match type name", Justification = "OK - Partial class")]
[SuppressMessage("Critical Code Smell", "S2699:Add at least one assertion to this test case", Justification = "OK - test.RunAsync() is the assertion in Roslyn analyzer tests")]
public sealed partial class EmptyKustoScriptFileAnalyzerTests
{
    [Fact]
    public async Task NoDiagnostic_AbstractClass()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public abstract class KustoScript
                            {
                            }

                            public abstract class MyQuery : KustoScript
                            {
                            }
                            """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task NoDiagnostic_InterfaceType()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public interface IKustoScript
                            {
                            }
                            """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task NoDiagnostic_ClassNotInheritingFromKustoScript()
    {
        const string code = """
                            public class MyClass
                            {
                            }
                            """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task NoDiagnostic_NoKustoFile()
    {
        // When there's no .kusto file, ATCK301 handles it, not ATCK305
        const string code = """
                            namespace Atc.Kusto;

                            public abstract class KustoScript
                            {
                            }

                            public class MyQuery : KustoScript
                            {
                            }
                            """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task NoDiagnostic_ValidQueryInKustoFile()
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

        var test = new CSharpAnalyzerTest<EmptyKustoScriptFileAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_QueryWithParameters()
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
                                        Customers | where Id == customerId
                                        """;

        var test = new CSharpAnalyzerTest<EmptyKustoScriptFileAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_QueryWithCommentsAndContent()
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
                                        // Get top 10 customers
                                        /* This is a sample query
                                           for testing purposes */
                                        Customers | take 10
                                        """;

        var test = new CSharpAnalyzerTest<EmptyKustoScriptFileAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_MultilineQuery()
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
                                        Customers
                                        | where Country == "USA"
                                        | project Name, Email
                                        | take 100
                                        """;

        var test = new CSharpAnalyzerTest<EmptyKustoScriptFileAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_TableNameOnly()
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
                                        Customers
                                        """;

        var test = new CSharpAnalyzerTest<EmptyKustoScriptFileAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }
}