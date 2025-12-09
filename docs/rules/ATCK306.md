# ATCK306: Projection field not found in result type

## Cause

A field in the `| project` statement of a `.kusto` file does not match any property in the result type `T` of `KustoQuery<T>` or `KustoStreamingQuery<T>`.

## Rule description

When creating Kusto queries that return typed results, the fields in the `| project` statement must correspond to properties in the result contract type. If a projected field doesn't exist in the contract, deserialization will fail at runtime because the library won't be able to map the Kusto column to a property.

This rule only applies when:
1. The class inherits from `KustoQuery<T>` or `KustoStreamingQuery<T>`
2. The `.kusto` file ends with a `| project` statement (final projection)

## How to fix violations

Either add the missing property to the result type or remove the field from the projection:

1. Add a property with the same name to the result type (case-insensitive match)
2. Or remove the unneeded field from the `| project` statement

## Examples

### Violates ATCK306

```csharp
// Customer.cs
public record Customer(
    long CustomerKey,
    string FirstName,
    string LastName);
```

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
| project
    CustomerKey,
    FirstName,
    LastName,
    Email  // ATCK306: 'Email' not found in Customer
```

**Problem**: `Email` is projected but doesn't exist in `Customer`.

### Does not violate ATCK306

```csharp
// Customer.cs
public record Customer(
    long CustomerKey,
    string FirstName,
    string LastName,
    string Email);  // Added Email property
```

```kusto
// CustomersQuery.kusto
Customers
| project
    CustomerKey,
    FirstName,
    LastName,
    Email
```

**Success**: All projected fields exist in the result type.

## When to suppress warnings

You may suppress this warning if:
- The field is intentionally projected but handled by a custom `ReadResult` implementation
- You're migrating code and plan to fix the contract

However, unsuppressed ATCK306 violations will cause runtime exceptions.

## Related rules

- [ATCK307](ATCK307.md): Result type property not projected
- [ATCK308](ATCK308.md): Missing final project statement
- [ATCK309](ATCK309.md): Projection field naming mismatch
