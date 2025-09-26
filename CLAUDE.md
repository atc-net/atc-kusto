# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Development Commands

### Building the project
```bash
# Build all projects in the repository
dotnet build

# Build in Release mode
dotnet build -c Release
```

### Running tests
```bash
# Run all tests
dotnet test

# Run tests with specific filter
dotnet test --filter "FullyQualifiedName~SimpleQueryHandler"

# Run tests from a specific project
dotnet test test/Atc.Kusto.Tests/Atc.Kusto.Tests.csproj
```

## Architecture Overview

This is a .NET library for executing Kusto (Azure Data Explorer) queries and commands. The library follows a handler-based architecture pattern with dependency injection.

### Core Concepts

1. **Kusto Scripts**: Queries and commands are defined as embedded `.kusto` resources paired with C# record classes that inherit from base types:
   - `KustoCommand` - For commands with no output
   - `KustoQuery<T>` - For queries returning results
   - `KustoStreamingQuery<T>` - For streaming large result sets

2. **Handler Pattern**: Different handlers process different query types:
   - `SimpleQueryHandler` - Standard query execution
   - `SimpleCommandHandler` - Command execution
   - `StreamingQueryHandler` - Direct streaming (immediate yield)
   - `BufferedStreamingQueryHandler` - Buffered streaming with metadata
   - `ExistingPagedStoredQueryHandler` / `NewPagedStoredQueryHandler` - Pagination support

3. **Factory Pattern**:
   - `ScriptHandlerFactory` creates appropriate handlers based on script type
   - `KustoProcessorFactory` creates processors for specific databases
   - `KustoClientProvider` manages Kusto client instances with caching

4. **Cancellation Support**: The library supports cooperative cancellation with optional server-side query cancellation via `CancellationTokenKustoExtensions`.

### Key Implementation Patterns

- **Embedded Resources**: `.kusto` files must have "Build Action" set to "Embedded resource"
- **Parameter Mapping**: Query parameters are extracted from record properties and converted to camelCase
- **Result Deserialization**: Results are mapped to DTOs using `DataReaderExtensions`
- **Dependency Injection**: Services are registered via `ServiceCollectionExtensions.ConfigureAzureDataExplorer()`

### Project Structure

- `src/Atc.Kusto/` - Main library implementation
  - `Handlers/Internal/` - Query/command handler implementations
  - `Factories/Internal/` - Factory implementations
  - `Providers/Internal/` - Client provider implementations
  - `Extensions/` - Extension methods for various operations
  - `Options/` - Configuration option classes
  - `HealthChecks/` - Health check integration for ADX clusters

- `sample/` - Example applications demonstrating library usage
  - `Atc.Kusto.Api.Sample/` - Web API sample
  - `Atc.Kusto.Sample/` - Console application sample

- `test/Atc.Kusto.Tests/` - Unit tests

### Configuration

The library uses `AtcKustoOptions` for configuration with these key properties:
- `HostAddress` - Kusto cluster URI
- `DatabaseName` - Target database
- `Credential` - Azure credential for authentication