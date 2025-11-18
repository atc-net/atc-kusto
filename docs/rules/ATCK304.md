# ATCK304: Parameter order mismatch between C# and Kusto

## Cause

The order of parameters in a C# class or record that inherits from `KustoScript` does not match the order of parameters declared in the corresponding `.kusto` file.

## Rule description

When creating Kusto queries or commands, the parameters defined in the C# constructor must appear in the same order as they are declared using `declare query_parameters` in the corresponding `.kusto` file. The library maps parameters positionally, so incorrect ordering will result in parameter values being assigned to the wrong Kusto variables.

**Note**: Parameter names are compared case-insensitively, and the analyzer automatically converts C# PascalCase names to camelCase when comparing with Kusto parameter names.

## How to fix violations

Reorder the parameters in either the C# constructor or the `.kusto` file to match:

1. Check the order of parameters in the C# constructor
2. Check the order of parameters in the `declare query_parameters` section
3. Reorder parameters to match (preferably update the C# side to avoid breaking existing queries)

## Examples

### Violates ATCK304

```csharp
// CustomersQuery.cs
public record CustomersQuery(long CustomerId, string Name)
    : KustoScript, IKustoQuery<Customer>;
```

```kusto
// CustomersQuery.kusto
declare query_parameters (
    name:string,
    customerId:long
);
Customers
| where customerId == CustomerKey and name == FirstName
```

**Problem**: C# has `CustomerId` first, but Kusto has `name` first. The parameters are in different orders.

### Does not violate ATCK304

```csharp
// CustomersQuery.cs
public record CustomersQuery(long CustomerId, string Name)
    : KustoScript, IKustoQuery<Customer>;
```

```kusto
// CustomersQuery.kusto
declare query_parameters (
    customerId:long,
    name:string
);
Customers
| where customerId == CustomerKey and name == FirstName
```

**Success**: Both C# and Kusto have parameters in the same order: `customerId` then `name`.

### Case-insensitive matching

```csharp
// CustomersQuery.cs
public record CustomersQuery(long CustomerId)
    : KustoScript, IKustoQuery<Customer>;
```

```kusto
// CustomersQuery.kusto
declare query_parameters (
    CUSTOMERID:LONG
);
Customers
| where CUSTOMERID == CustomerKey
```

**Success**: `CustomerId` (PascalCase in C#) matches `CUSTOMERID` (uppercase in Kusto) due to case-insensitive comparison.

## When to suppress warnings

Do not suppress warnings from this rule. Parameter order mismatches will cause parameter values to be assigned incorrectly, leading to wrong query results or runtime errors.

## Related rules

- [ATCK301](ATCK301.md): Missing .kusto embedded resource file
- [ATCK302](ATCK302.md): Parameter count mismatch between C# and Kusto
- [ATCK303](ATCK303.md): Parameter type mismatch between C# and Kusto
