# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Development Commands

```bash
# Build all projects
dotnet build

# Run all tests
dotnet test

# Run specific test class or method
dotnet test --filter "FullyQualifiedName~SimpleQueryHandler"

# Run tests from specific project
dotnet test test/Atc.Kusto.Tests/Atc.Kusto.Tests.csproj
dotnet test test/Atc.Kusto.Analyzer.Tests/Atc.Kusto.Analyzer.Tests.csproj
```

## Architecture Overview

.NET library for executing Kusto (Azure Data Explorer) queries and commands with a handler-based architecture and dependency injection.

### Core Concepts

1. **Kusto Scripts**: Queries/commands are embedded `.kusto` resources paired with C# records inheriting from:
   - `KustoCommand` - Commands with no output
   - `KustoQuery<T>` - Queries returning results
   - `KustoStreamingQuery<T>` - Streaming large result sets

2. **Handler Pattern**: `src/Atc.Kusto/Handlers/Internal/`
   - `SimpleQueryHandler` / `SimpleCommandHandler` - Standard execution
   - `StreamingQueryHandler` / `BufferedStreamingQueryHandler` - Streaming support
   - `ExistingPagedStoredQueryHandler` / `NewPagedStoredQueryHandler` - Pagination

3. **Factory Pattern**: `ScriptHandlerFactory` creates handlers, `KustoProcessorFactory` creates processors, `KustoClientProvider` manages client instances

### Roslyn Analyzer (`src/Atc.Kusto.Analyzer/`)

Bundled analyzer providing compile-time validation. Targets `netstandard2.0` for Roslyn compatibility.

**Key components:**
- `Rules/Usage/` - Analyzer implementations (e.g., `MissingKustoScriptResourceAnalyzer`, `ProjectionMismatchAnalyzer`)
- `Parsing/` - Kusto file parsers (`KustoParameterParser`, `KustoProjectionParser`)
- `Helpers/KustoAnalyzerHelper.cs` - Shared analyzer utilities

**Rules (ATCK301-309):** Validate `.kusto` files exist, parameters match between C# and Kusto, and projections match result types.

**Testing analyzers:** Uses `Microsoft.CodeAnalysis.CSharp.Testing` with `CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>` pattern.

### Key Implementation Patterns

- **Embedded Resources**: `.kusto` files need both `<EmbeddedResource>` and `<AdditionalFiles>` in csproj
- **Parameter Mapping**: Record properties → camelCase Kusto parameters
- **Result Deserialization**: Via `DataReaderExtensions`
- **DI Registration**: `ServiceCollectionExtensions.ConfigureAzureDataExplorer()`

### Project Structure

- `src/Atc.Kusto/` - Main library
- `src/Atc.Kusto.Analyzer/` - Roslyn analyzer (bundled in NuGet package)
- `sample/Atc.Kusto.Analyzer.Sample/` - Analyzer rule demonstrations
- `test/Atc.Kusto.Tests/` - Library unit tests
- `test/Atc.Kusto.Analyzer.Tests/` - Analyzer unit tests