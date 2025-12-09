# ATCK308: Missing final project statement

## Cause

A `.kusto` file for a `KustoQuery<T>` or `KustoStreamingQuery<T>` does not end with an explicit `| project` statement.

## Rule description

It's best practice to end queries with an explicit `| project` statement that defines exactly which columns map to the result contract type. Without an explicit final projection:

- The query returns all columns from the source table or previous operation
- It's unclear which columns are actually needed
- Changes to the source table schema could unexpectedly break deserialization
- The contract between the query and the result type is implicit rather than explicit

This is an **informational** rule that encourages best practices. Queries without final projections will still work if the result type properties match the actual column names.

## How to fix violations

Add a `| project` statement as the final operation in the query:

```kusto
Customers
| where Active == true
| project
    CustomerKey,
    FirstName,
    LastName
```

## Examples

### Violates ATCK308

```csharp
// CustomersQuery.cs
public record CustomersQuery(long? CustomerId = null)
    : KustoQuery<Customer>;
```

```kusto
// CustomersQuery.kusto
declare query_parameters (
    customerId:long = long(null)
);
Customers
| where isnull(customerId) or customerId == CustomerKey
| take 100
// ATCK308: No final project statement
```

**Problem**: Query ends with `| take 100` instead of `| project`.

### Does not violate ATCK308

```kusto
// CustomersQuery.kusto
declare query_parameters (
    customerId:long = long(null)
);
Customers
| where isnull(customerId) or customerId == CustomerKey
| take 100
| project
    CustomerKey,
    FirstName,
    LastName
```

**Success**: Query ends with an explicit `| project` statement.

## When to suppress warnings

You may suppress this warning if:
- The query intentionally uses `| summarize` to define output columns
- The query uses `| extend` to add computed columns and all source columns are needed
- You're using a very simple query where the table structure exactly matches the contract
- You're working with dynamic or flexible result types

Since this is an informational diagnostic for best practices, suppressing it is acceptable when appropriate.

## Related rules

- [ATCK306](ATCK306.md): Projection field not found in result type
- [ATCK307](ATCK307.md): Result type property not projected
- [ATCK309](ATCK309.md): Projection field naming mismatch
