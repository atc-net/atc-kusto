namespace Atc.Kusto.Analyzer.Tests.Rules.Usage;

#pragma warning disable SA1135 // Using directives must be qualified
using AnalyzerVerifier = CSharpAnalyzerVerifier<MissingKustoScriptResourceAnalyzer>;
#pragma warning restore SA1135 // Using directives must be qualified

[SuppressMessage("", "AsyncFixer01:The method does not need to use async/await", Justification = "OK - Test code")]
[SuppressMessage("Naming", "MA0048:File name must match type name", Justification = "OK - Partial class")]
public sealed partial class MissingKustoScriptResourceAnalyzerTests
{
    [Fact]
    public async Task ReportsDiagnostic_ConcreteClassInheritingKustoScript()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public abstract class KustoScript
                            {
                            }

                            public class [|MyQuery|] : KustoScript
                            {
                            }
                            """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task ReportsDiagnostic_NestedInheritance()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public abstract class KustoScript
                            {
                            }

                            public abstract class KustoQuery<T> : KustoScript
                            {
                            }

                            public class [|CustomersQuery|] : KustoQuery<Customer>
                            {
                            }

                            public class Customer
                            {
                            }
                            """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task ReportsDiagnostic_InternalClass()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public abstract class KustoScript
                            {
                            }

                            internal class [|InternalQuery|] : KustoScript
                            {
                            }
                            """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task ReportsDiagnostic_ClassInDifferentNamespace()
    {
        const string code = """
                            namespace Atc.Kusto
                            {
                                public abstract class KustoScript
                                {
                                }
                            }

                            namespace MyApp.Queries
                            {
                                public class [|GetUserQuery|] : Atc.Kusto.KustoScript
                                {
                                }
                            }
                            """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(code);
    }
}