# ATCK301: Missing .kusto embedded resource file

## Cause

A class or record that inherits from `KustoScript` does not have a corresponding `.kusto` file marked as an embedded resource, or the file cannot be found in the project's AdditionalFiles.

## Rule description

When creating Kusto queries or commands, each C# class or record that inherits from `KustoScript` must have a corresponding `.kusto` file that contains the actual Kusto query or command. This file must be:

1. Located in the same directory as the C# file
2. Named to match the C# class name (e.g., `CustomersQuery.cs` → `CustomersQuery.kusto`)
3. Marked as an **Embedded resource** in the project file
4. Added to **AdditionalFiles** in the project configuration

The analyzer validates this at compile time by checking the AdditionalFiles collection for the expected `.kusto` file path.

## How to fix violations

To fix this violation, ensure the `.kusto` file exists and is properly configured:

### Step 1: Create the .kusto file

Create a `.kusto` file in the same directory as your C# class with the same name:

```
MyProject/
├── Queries/
│   ├── CustomersQuery.cs
│   └── CustomersQuery.kusto    ← Must match the class name
```

### Step 2: Configure the project file

Add the following configuration to your `.csproj` file:

```xml
<ItemGroup>
  <!-- Required for runtime: embeds .kusto files in the assembly -->
  <EmbeddedResource Include="**/*.kusto" />

  <!-- Required for compile-time validation: allows the analyzer to verify .kusto files exist -->
  <AdditionalFiles Include="**/*.kusto" />
</ItemGroup>
```

**What this does**:
- `EmbeddedResource`: Embeds the `.kusto` file into the compiled assembly so it can be loaded at runtime
- `AdditionalFiles`: Makes the `.kusto` file available to analyzers for compile-time validation

The glob pattern `**/*.kusto` will automatically include all `.kusto` files in your project.

### Step 3: Install the analyzer NuGet package

Ensure your project references the analyzer:

```xml
<ItemGroup>
  <PackageReference Include="Atc.Kusto.Analyzer" Version="x.x.x">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

## Examples

### Violates ATCK301

**Scenario 1: Missing .kusto file**

```csharp
// CustomersQuery.cs
namespace MyApp.Queries;

public record CustomersQuery(long CustomerId)
    : KustoScript, IKustoQuery<Customer>;
```

**Problem**: No corresponding `CustomersQuery.kusto` file exists in the same directory.

---

**Scenario 2: File exists but not marked as embedded resource**

```csharp
// CustomersQuery.cs
public record CustomersQuery(long CustomerId)
    : KustoScript, IKustoQuery<Customer>;
```

```kusto
// CustomersQuery.kusto exists but has Build Action = "None" or "Content"
declare query_parameters (customerId:long);
Customers | where customerId == CustomerKey
```

**Problem**: The `.kusto` file exists but is not marked as an embedded resource.

---

**Scenario 3: File not in AdditionalFiles**

```csharp
// CustomersQuery.cs
public record CustomersQuery(long CustomerId)
    : KustoScript, IKustoQuery<Customer>;
```

```xml
<!-- .csproj -->
<ItemGroup>
  <!-- Required for runtime: embeds .kusto files in the assembly -->
  <EmbeddedResource Include="Queries\CustomersQuery.kusto" />

  <!-- Missing: No AdditionalFiles configuration for compile-time validation -->
</ItemGroup>
```

**Problem**: File is embedded but not included in AdditionalFiles for analyzer validation.

### Does not violate ATCK301

```csharp
// Queries/CustomersQuery.cs
namespace MyApp.Queries;

public record CustomersQuery(long CustomerId)
    : KustoScript, IKustoQuery<Customer>;
```

```kusto
// Queries/CustomersQuery.kusto
declare query_parameters (customerId:long);

Customers
| where customerId == CustomerKey
| project CustomerKey, FirstName, LastName
```

```xml
<!-- .csproj -->
<ItemGroup>
  <!-- Required for runtime: embeds .kusto files in the assembly -->
  <EmbeddedResource Include="**/*.kusto" />

  <!-- Required for compile-time validation: allows the analyzer to verify .kusto files exist -->
  <AdditionalFiles Include="**/*.kusto" />
</ItemGroup>
```

**Success**:
- `.kusto` file exists in same directory as `.cs` file
- File is marked as embedded resource
- File is included in AdditionalFiles
- File name matches class name

## When to suppress warnings

Do not suppress warnings from this rule. Missing `.kusto` files will cause runtime errors when the application tries to load the embedded resource for query execution.

If you have a base class that inherits from `KustoScript` but should not have its own `.kusto` file (because it's abstract or serves as a base for other queries), mark the class as `abstract`:

```csharp
// This will not trigger ATCK301
public abstract record BaseKustoQuery : KustoScript;

// Concrete implementations need .kusto files
public record CustomersQuery(long CustomerId)
    : BaseKustoQuery, IKustoQuery<Customer>;
```

## Related rules

- [ATCK302](ATCK302.md): Parameter count mismatch between C# and Kusto
- [ATCK303](ATCK303.md): Parameter type mismatch between C# and Kusto
- [ATCK304](ATCK304.md): Parameter order mismatch between C# and Kusto
