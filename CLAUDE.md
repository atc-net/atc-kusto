# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Development Commands

```bash
# Build all projects
dotnet build

# Run all tests (Microsoft.Testing.Platform with xUnit v3, selected in global.json)
dotnet test

# Run tests from specific project
dotnet test --project test/Atc.Kusto.Tests/Atc.Kusto.Tests.csproj
dotnet test --project test/Atc.Kusto.Analyzer.Tests/Atc.Kusto.Analyzer.Tests.csproj

# Run specific test class or method - pass --project, because a filtered run over the whole
# solution exits with code 8 when another test project runs no tests
dotnet test --project test/Atc.Kusto.Tests/Atc.Kusto.Tests.csproj --filter "FullyQualifiedName~SimpleQueryHandler"
```

The solution targets .NET 10 (`net10.0`, set in `Directory.Build.props`); the analyzer projects target `netstandard2.0`.

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

4. **Ingestion**: `IKustoIngestor` writes in-memory rows (as multijson), streams and blobs to a table, in `Streaming`, `Queued` or `ManagedStreaming` mode, and checks the status of a tracked queued ingestion. Implementation in `src/Atc.Kusto/Ingestion/Internal/`:
   - `KustoIngestor` - validation, serialization, logging and turning SDK errors into a `KustoIngestionResult` (failures are returned, not thrown)
   - `KustoIngestClient` - the only file that uses Kusto Ingest V2 types (`Microsoft.Azure.Kusto.Ingest.V2`, pinned to an exact pre-1.0 version). Its class remarks list the SDK behaviours it works around; re-check them when upgrading the package
   - A test guards that no `Kusto.Ingest` types appear in the public API
   - Unit tests replace the SDK with a fake, so SDK behaviour is only verified by running `sample/Atc.Kusto.Ingestion.Sample` against a writable cluster

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
- `src/Atc.Kusto.CLI/` - The `atc-kusto` dotnet tool (queries, cluster/database/table browsing, schema export), released separately; it uses the Kusto SDK directly, not the library's processor or ingestor
- `sample/Atc.Kusto.Analyzer.Sample/` - Analyzer rule demonstrations
- `sample/Atc.Kusto.Ingestion.Sample/` - Every ingestion scenario against a real cluster (see its README and `setup.kql`)
- `test/Atc.Kusto.Tests/` - Library unit tests
- `test/Atc.Kusto.Analyzer.Tests/` - Analyzer unit tests