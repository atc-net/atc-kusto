# ATCK305: Empty .kusto embedded resource file

## Cause

A class or record that inherits from `KustoScript` has a corresponding `.kusto` file, but the file is empty or contains only comments and parameter declarations without any actual query or command content.

## Rule description

When creating Kusto queries or commands, the `.kusto` file must contain actual query or command logic. An empty file or a file containing only the following is considered invalid:

- Whitespace
- Single-line comments (`//`)
- Multi-line comments (`/* */`)
- Parameter declarations (`declare query_parameters`)

While parameter declarations and comments are useful, they must be accompanied by a valid Kusto query or command. A file containing only these elements provides no executable logic and will fail at runtime.

## How to fix violations

To fix this violation, add a valid Kusto query or command to your `.kusto` file.

### Example fixes

**Option 1: Add a simple query**

```kusto
declare query_parameters (
    customerId:long
);

Customers
| where CustomerKey == customerId
| project CustomerKey, FirstName, LastName
```

**Option 2: Add a table reference**

```kusto
Customers
```

**Option 3: Add a command**

```kusto
.show tables
```

## Examples

### Violates ATCK305

**Scenario 1: Completely empty file**

```csharp
// CustomersQuery.cs
namespace MyApp.Queries;

public record CustomersQuery(long CustomerId)
    : KustoScript, IKustoQuery<Customer>;
```

```kusto
// CustomersQuery.kusto - EMPTY FILE
```

**Problem**: The `.kusto` file exists but contains no content.

---

**Scenario 2: Only comments**

```csharp
// CustomersQuery.cs
namespace MyApp.Queries;

public record CustomersQuery(long CustomerId)
    : KustoScript, IKustoQuery<Customer>;
```

```kusto
// CustomersQuery.kusto
// TODO: Implement customer query
/* This query will fetch customers
   based on their ID */
```

**Problem**: The file contains only comments with no executable query.

---

**Scenario 3: Only parameter declarations**

```csharp
// CustomersQuery.cs
namespace MyApp.Queries;

public record CustomersQuery(long CustomerId, string Name)
    : KustoScript, IKustoQuery<Customer>;
```

```kusto
// CustomersQuery.kusto
declare query_parameters (
    customerId:long,
    name:string
);
```

**Problem**: Parameter declaration exists but no query implementation follows.

---

**Scenario 4: Comments and parameters but no query**

```csharp
// CustomersQuery.cs
namespace MyApp.Queries;

public record CustomersQuery(long CustomerId)
    : KustoScript, IKustoQuery<Customer>;
```

```kusto
// CustomersQuery.kusto
// Query for retrieving customer information
declare query_parameters (
    customerId:long
);
// TODO: Add query logic here
```

**Problem**: The file has structure but no actual query implementation.

### Does not violate ATCK305

**Example 1: Complete query with parameters**

```csharp
// Queries/CustomersQuery.cs
namespace MyApp.Queries;

public record CustomersQuery(long CustomerId)
    : KustoScript, IKustoQuery<Customer>;
```

```kusto
// Queries/CustomersQuery.kusto
declare query_parameters (
    customerId:long
);

Customers
| where CustomerKey == customerId
| project CustomerKey, FirstName, LastName
```

**Success**: File contains parameter declaration and a valid query.

---

**Example 2: Query with comments**

```csharp
// Queries/TopCustomersQuery.cs
namespace MyApp.Queries;

public record TopCustomersQuery()
    : KustoScript, IKustoQuery<Customer>;
```

```kusto
// Queries/TopCustomersQuery.kusto
// Retrieve top 100 customers by order count
/* This query helps identify our most valuable customers
   for marketing campaigns */
Customers
| summarize OrderCount = count() by CustomerKey
| top 100 by OrderCount desc
```

**Success**: Comments provide context, and the file contains a valid query.

---

**Example 3: Simple table reference**

```csharp
// Queries/AllCustomersQuery.cs
namespace MyApp.Queries;

public record AllCustomersQuery()
    : KustoScript, IKustoQuery<Customer>;
```

```kusto
// Queries/AllCustomersQuery.kusto
Customers
```

**Success**: A simple table name is a valid query that returns all rows.

---

**Example 4: Command without parameters**

```csharp
// Commands/ShowTablesCommand.cs
namespace MyApp.Commands;

public record ShowTablesCommand()
    : KustoScript, IKustoCommand;
```

```kusto
// Commands/ShowTablesCommand.kusto
.show tables
```

**Success**: A valid Kusto control command.

## When to suppress warnings

Generally, you should not suppress this warning. An empty `.kusto` file indicates incomplete implementation that will fail at runtime.

However, you may temporarily suppress this warning during development if:

1. You are scaffolding multiple query files and plan to implement them soon
2. You are refactoring and temporarily have empty files

To suppress the warning, use:

```csharp
#pragma warning disable ATCK305
public record CustomersQuery(long CustomerId)
    : KustoScript, IKustoQuery<Customer>;
#pragma warning restore ATCK305
```

**Important**: Remove the suppression once you've added the query implementation. Empty queries will fail at runtime.

## Related rules

- [ATCK301](ATCK301.md): Missing .kusto embedded resource file
- [ATCK302](ATCK302.md): Parameter count mismatch between C# and Kusto
- [ATCK303](ATCK303.md): Parameter type mismatch between C# and Kusto
- [ATCK304](ATCK304.md): Parameter order mismatch between C# and Kusto
