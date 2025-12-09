namespace Atc.Kusto.Analyzer.Tests.Rules.Usage;

#pragma warning disable SA1135 // Using directives must be qualified
using AnalyzerVerifier = CSharpAnalyzerVerifier<ProjectionMismatchAnalyzer>;
#pragma warning restore SA1135 // Using directives must be qualified

[SuppressMessage("", "AsyncFixer01:The method does not need to use async/await", Justification = "OK - Test code")]
[SuppressMessage("Critical Code Smell", "S2699:Add at least one assertion to this test case", Justification = "OK - test.RunAsync() is the assertion in Roslyn analyzer tests")]
public sealed class ProjectionMismatchAnalyzerTests
{
    [Fact]
    public async Task NoDiagnostic_WhenProjectionMatchesContract()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public abstract record KustoScript { }
                            public abstract record KustoQuery<T> : KustoScript { }

                            public record Customer(string FirstName, string LastName);

                            public record MyQuery : KustoQuery<Customer>
                            {
                            }
                            """;

        const string kustoFileContent = """
                                        Customers
                                        | project
                                            FirstName,
                                            LastName
                                        """;

        var test = new CSharpAnalyzerTest<ProjectionMismatchAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenProjectionMatchesContract_CaseInsensitive()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public abstract record KustoScript { }
                            public abstract record KustoQuery<T> : KustoScript { }

                            public record Customer(string FirstName, string LastName);

                            public record MyQuery : KustoQuery<Customer>
                            {
                            }
                            """;

        const string kustoFileContent = """
                                        Customers
                                        | project
                                            firstname,
                                            lastname
                                        """;

        var test = new CSharpAnalyzerTest<ProjectionMismatchAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenNoResultType_KustoCommand()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public abstract record KustoScript { }

                            public record MyCommand : KustoScript
                            {
                            }
                            """;

        const string kustoFileContent = """
                                        .drop table Customers
                                        """;

        var test = new CSharpAnalyzerTest<ProjectionMismatchAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenAbstractClass()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public abstract record KustoScript { }
                            public abstract record KustoQuery<T> : KustoScript { }

                            public record Customer(string FirstName);

                            public abstract record MyQuery : KustoQuery<Customer>
                            {
                            }
                            """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task NoDiagnostic_WhenProjectionHasAliases()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public abstract record KustoScript { }
                            public abstract record KustoQuery<T> : KustoScript { }

                            public record Customer(string FullName, string Email);

                            public record MyQuery : KustoQuery<Customer>
                            {
                            }
                            """;

        const string kustoFileContent = """
                                        Customers
                                        | project
                                            FullName = strcat(FirstName, ' ', LastName),
                                            Email = EmailAddress
                                        """;

        var test = new CSharpAnalyzerTest<ProjectionMismatchAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsDiagnostic_ATCK306_WhenProjectedFieldNotInContract()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public abstract record KustoScript { }
                            public abstract record KustoQuery<T> : KustoScript { }

                            public record Customer(string FirstName, string LastName);

                            public record {|#0:MyQuery|} : KustoQuery<Customer>
                            {
                            }
                            """;

        const string kustoFileContent = """
                                        Customers
                                        | project
                                            FirstName,
                                            LastName,
                                            UnknownField
                                        """;

        var expected = new DiagnosticResult(RuleIdentifierConstants.Usage.ProjectionFieldNotFound, DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("UnknownField", "Customer");

        var test = new CSharpAnalyzerTest<ProjectionMismatchAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
            ExpectedDiagnostics = { expected },
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsDiagnostic_ATCK307_WhenContractPropertyNotProjected()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public abstract record KustoScript { }
                            public abstract record KustoQuery<T> : KustoScript { }

                            public record Customer(string FirstName, string LastName, string Email);

                            public record {|#0:MyQuery|} : KustoQuery<Customer>
                            {
                            }
                            """;

        const string kustoFileContent = """
                                        Customers
                                        | project
                                            FirstName,
                                            LastName
                                        """;

        var expected = new DiagnosticResult(RuleIdentifierConstants.Usage.ResultPropertyNotProjected, DiagnosticSeverity.Info)
            .WithLocation(0)
            .WithArguments("Email", "Customer");

        var test = new CSharpAnalyzerTest<ProjectionMismatchAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
            ExpectedDiagnostics = { expected },
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsDiagnostic_ATCK308_WhenNoFinalProjectStatement()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public abstract record KustoScript { }
                            public abstract record KustoQuery<T> : KustoScript { }

                            public record Customer(string FirstName, string LastName);

                            public record {|#0:MyQuery|} : KustoQuery<Customer>
                            {
                            }
                            """;

        const string kustoFileContent = """
                                        Customers
                                        | where Active == true
                                        | take 100
                                        """;

        var expected = new DiagnosticResult(RuleIdentifierConstants.Usage.MissingFinalProjection, DiagnosticSeverity.Info)
            .WithLocation(0)
            .WithArguments("MyQuery");

        var test = new CSharpAnalyzerTest<ProjectionMismatchAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
            ExpectedDiagnostics = { expected },
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsDiagnostic_ATCK308_WhenProjectIsNotFinal()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public abstract record KustoScript { }
                            public abstract record KustoQuery<T> : KustoScript { }

                            public record Customer(string FirstName, string LastName);

                            public record {|#0:MyQuery|} : KustoQuery<Customer>
                            {
                            }
                            """;

        const string kustoFileContent = """
                                        Customers
                                        | project FirstName, LastName
                                        | take 100
                                        """;

        var expected = new DiagnosticResult(RuleIdentifierConstants.Usage.MissingFinalProjection, DiagnosticSeverity.Info)
            .WithLocation(0)
            .WithArguments("MyQuery");

        var test = new CSharpAnalyzerTest<ProjectionMismatchAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
            ExpectedDiagnostics = { expected },
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsMultipleDiagnostics_WhenMultipleFieldsMismatch()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public abstract record KustoScript { }
                            public abstract record KustoQuery<T> : KustoScript { }

                            public record Customer(string FirstName, string LastName, string Email);

                            public record {|#0:MyQuery|} : KustoQuery<Customer>
                            {
                            }
                            """;

        const string kustoFileContent = """
                                        Customers
                                        | project
                                            FirstName,
                                            Unknown1,
                                            Unknown2
                                        """;

        var expected1 = new DiagnosticResult(RuleIdentifierConstants.Usage.ProjectionFieldNotFound, DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("Unknown1", "Customer");

        var expected2 = new DiagnosticResult(RuleIdentifierConstants.Usage.ProjectionFieldNotFound, DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("Unknown2", "Customer");

        var expected3 = new DiagnosticResult(RuleIdentifierConstants.Usage.ResultPropertyNotProjected, DiagnosticSeverity.Info)
            .WithLocation(0)
            .WithArguments("LastName", "Customer");

        var expected4 = new DiagnosticResult(RuleIdentifierConstants.Usage.ResultPropertyNotProjected, DiagnosticSeverity.Info)
            .WithLocation(0)
            .WithArguments("Email", "Customer");

        var test = new CSharpAnalyzerTest<ProjectionMismatchAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
            ExpectedDiagnostics = { expected1, expected2, expected3, expected4 },
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenKustoStreamingQuery()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public abstract record KustoScript { }
                            public abstract record KustoStreamingQuery<T> : KustoScript { }

                            public record Customer(string FirstName, string LastName);

                            public record MyQuery : KustoStreamingQuery<Customer>
                            {
                            }
                            """;

        const string kustoFileContent = """
                                        Customers
                                        | project
                                            FirstName,
                                            LastName
                                        """;

        var test = new CSharpAnalyzerTest<ProjectionMismatchAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsDiagnostic_ATCK309_WhenSnakeCaseFieldMatchesPascalCaseProperty()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public abstract record KustoScript { }
                            public abstract record KustoQuery<T> : KustoScript { }

                            public record Customer(long CustomerKey, string FirstName, string LastName);

                            public record {|#0:MyQuery|} : KustoQuery<Customer>
                            {
                            }
                            """;

        const string kustoFileContent = """
                                        Customers
                                        | project
                                            customer_key,
                                            first_name,
                                            last_name
                                        """;

        var expected1 = new DiagnosticResult(RuleIdentifierConstants.Usage.ProjectionFieldNamingMismatch, DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("customer_key", "Customer", "CustomerKey");

        var expected2 = new DiagnosticResult(RuleIdentifierConstants.Usage.ProjectionFieldNamingMismatch, DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("first_name", "Customer", "FirstName");

        var expected3 = new DiagnosticResult(RuleIdentifierConstants.Usage.ProjectionFieldNamingMismatch, DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("last_name", "Customer", "LastName");

        var test = new CSharpAnalyzerTest<ProjectionMismatchAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
            ExpectedDiagnostics = { expected1, expected2, expected3 },
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsDiagnostic_ATCK306_WhenSnakeCaseFieldHasNoMatch()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public abstract record KustoScript { }
                            public abstract record KustoQuery<T> : KustoScript { }

                            public record Customer(string FirstName, string LastName);

                            public record {|#0:MyQuery|} : KustoQuery<Customer>
                            {
                            }
                            """;

        const string kustoFileContent = """
                                        Customers
                                        | project
                                            FirstName,
                                            LastName,
                                            unknown_field
                                        """;

        // unknown_field has no potential PascalCase match, so it's ATCK306
        var expected = new DiagnosticResult(RuleIdentifierConstants.Usage.ProjectionFieldNotFound, DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("unknown_field", "Customer");

        var test = new CSharpAnalyzerTest<ProjectionMismatchAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
            ExpectedDiagnostics = { expected },
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_ATCK307_WhenSnakeCaseFieldMatchesProperty()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public abstract record KustoScript { }
                            public abstract record KustoQuery<T> : KustoScript { }

                            public record Customer(string FirstName, string LastName);

                            public record {|#0:MyQuery|} : KustoQuery<Customer>
                            {
                            }
                            """;

        const string kustoFileContent = """
                                        Customers
                                        | project
                                            first_name,
                                            last_name
                                        """;

        // Should report ATCK309 for snake_case mismatch, but NOT ATCK307 for missing properties
        // because the snake_case fields conceptually match the PascalCase properties
        var expected1 = new DiagnosticResult(RuleIdentifierConstants.Usage.ProjectionFieldNamingMismatch, DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("first_name", "Customer", "FirstName");

        var expected2 = new DiagnosticResult(RuleIdentifierConstants.Usage.ProjectionFieldNamingMismatch, DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("last_name", "Customer", "LastName");

        var test = new CSharpAnalyzerTest<ProjectionMismatchAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
            ExpectedDiagnostics = { expected1, expected2 },
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }

    [Fact]
    public async Task MixedDiagnostics_WhenSomeFieldsMatchAndSomeDont()
    {
        const string code = """
                            namespace Atc.Kusto;

                            public abstract record KustoScript { }
                            public abstract record KustoQuery<T> : KustoScript { }

                            public record Customer(string FirstName, string LastName, string Email);

                            public record {|#0:MyQuery|} : KustoQuery<Customer>
                            {
                            }
                            """;

        const string kustoFileContent = """
                                        Customers
                                        | project
                                            FirstName,
                                            last_name,
                                            unknown_field
                                        """;

        // FirstName matches exactly - no diagnostic
        // last_name matches LastName as snake_case -> ATCK309
        // unknown_field has no match -> ATCK306
        // Email not projected but last_name covers LastName -> only Email gets ATCK307
        var expected1 = new DiagnosticResult(RuleIdentifierConstants.Usage.ProjectionFieldNamingMismatch, DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("last_name", "Customer", "LastName");

        var expected2 = new DiagnosticResult(RuleIdentifierConstants.Usage.ProjectionFieldNotFound, DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("unknown_field", "Customer");

        var expected3 = new DiagnosticResult(RuleIdentifierConstants.Usage.ResultPropertyNotProjected, DiagnosticSeverity.Info)
            .WithLocation(0)
            .WithArguments("Email", "Customer");

        var test = new CSharpAnalyzerTest<ProjectionMismatchAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = code,
            ExpectedDiagnostics = { expected1, expected2, expected3 },
        };

        test.TestState.AdditionalFiles.Add(("/0/Test0.kusto", kustoFileContent));

        await test.RunAsync();
    }
}