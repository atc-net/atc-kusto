# ATCK303: Parameter type mismatch between C# and Kusto

## Cause

A parameter type in a C# class or record that inherits from `KustoScript` does not match the corresponding parameter type declared in the `.kusto` file.

## Rule description

When creating Kusto queries or commands, the parameter types defined in the C# constructor must be compatible with the types declared using `declare query_parameters` in the corresponding `.kusto` file. The library performs type mapping between C# and Kusto types, and incompatible types will cause runtime errors or incorrect query results.

## Type mapping

The following table shows the expected C# to Kusto type mappings:

| C# Type    | Kusto Type |
|------------|------------|
| `long`     | `long`     |
| `int`      | `int`      |
| `string`   | `string`   |
| `DateTime` | `datetime` |
| `bool`     | `bool`     |
| `double`   | `real`     |
| `decimal`  | `decimal`  |
| `Guid`     | `guid`     |
| `TimeSpan` | `timespan` |

**Note**: Nullable C# types (e.g., `long?`, `string?`) are allowed and will be treated as their underlying types for validation purposes. Parameters with default values in Kusto are also acceptable regardless of C# nullability.

## How to fix violations

Update either the C# parameter type or the Kusto parameter type to match the expected mapping:

1. Check the C# parameter type in the constructor
2. Check the corresponding parameter type in the `.kusto` file
3. Change one to match the other using the type mapping table above

## Examples

### Violates ATCK303

```csharp
// CustomersQuery.cs
public record CustomersQuery(string CustomerId)
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

**Problem**: C# expects `string` but Kusto declares `long`.

### Does not violate ATCK303

```csharp
// CustomersQuery.cs
public record CustomersQuery(long CustomerId)
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

**Success**: Both use the correct type mapping (`long` in C# matches `long` in Kusto).

### Nullable types with defaults

```csharp
// SearchQuery.cs
public record SearchQuery(string? FilterName)
    : KustoScript, IKustoQuery<Customer>;
```

```kusto
// SearchQuery.kusto
declare query_parameters (
    filterName:string = ""
);
Customers
| where FilterName == filterName or filterName == ""
```

**Success**: Nullable C# type (`string?`) with Kusto default value is acceptable.

## When to suppress warnings

Do not suppress warnings from this rule. Parameter type mismatches will cause runtime errors or incorrect query results.

## Related rules

- [ATCK301](ATCK301.md): Missing .kusto embedded resource file
- [ATCK302](ATCK302.md): Parameter count mismatch between C# and Kusto
- [ATCK304](ATCK304.md): Parameter order mismatch between C# and Kusto
