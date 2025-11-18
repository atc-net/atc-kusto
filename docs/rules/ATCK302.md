# ATCK302: Parameter count mismatch between C# and Kusto

## Cause

The number of parameters in a C# class or record that inherits from `KustoScript` does not match the number of parameters declared in the corresponding `.kusto` file.

## Rule description

When creating Kusto queries or commands, the parameters defined in the C# constructor must exactly match the number of parameters declared using `declare query_parameters` in the corresponding `.kusto` file. This ensures that all parameters can be correctly passed from C# to the Kusto query.

## How to fix violations

Ensure that the number of parameters in your C# class/record matches the number in the `.kusto` file:

1. Check the C# constructor parameters
2. Check the `declare query_parameters` section in the `.kusto` file
3. Add or remove parameters to make them match

## Examples

### Violates ATCK302

```csharp
// CustomersQuery.cs
public record CustomersQuery(long CustomerId, string Name)
    : KustoScript, IKustoQuery<Customer>;
```

```kusto
// CustomersQuery.kusto
declare query_parameters (
    customerId:long
);
Customers
| where customerId == CustomerKey
```

**Problem**: C# has 2 parameters (`CustomerId`, `Name`) but Kusto only declares 1 (`customerId`).

### Does not violate ATCK302

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

**Success**: Both C# and Kusto have 2 parameters.

## When to suppress warnings

Do not suppress warnings from this rule. Parameter count mismatches will cause runtime errors when executing the query.

## Related rules

- [ATCK301](ATCK301.md): Missing .kusto embedded resource file
- [ATCK303](ATCK303.md): Parameter type mismatch between C# and Kusto
- [ATCK304](ATCK304.md): Parameter order mismatch between C# and Kusto
