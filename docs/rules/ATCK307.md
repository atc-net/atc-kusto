# ATCK307: Result type property not projected

## Cause

A property in the result type `T` of `KustoQuery<T>` or `KustoStreamingQuery<T>` is not included in the `| project` statement of the `.kusto` file.

## Rule description

When creating Kusto queries that return typed results, it's best practice to project all properties defined in the result contract type. If a property is defined in the contract but not projected, it will have its default value (null, 0, etc.) at runtime, which may not be the intended behavior.

This is an **informational** rule because missing projections don't cause runtime errors - they simply result in default values.

This rule only applies when:
1. The class inherits from `KustoQuery<T>` or `KustoStreamingQuery<T>`
2. The `.kusto` file ends with a `| project` statement (final projection)

## How to fix violations

Either add the missing field to the projection or remove the property from the result type:

1. Add the property to the `| project` statement
2. Or remove the unnecessary property from the result type

## Examples

### Violates ATCK307

```csharp
// Customer.cs
public record Customer(
    long CustomerKey,
    string FirstName,
    string LastName,
    string Email);  // ATCK307: 'Email' not projected
```

```kusto
// CustomersQuery.kusto
Customers
| project
    CustomerKey,
    FirstName,
    LastName
    // Email is missing - will be null at runtime
```

**Problem**: `Email` is defined in `Customer` but not projected.

### Does not violate ATCK307

```csharp
// Customer.cs
public record Customer(
    long CustomerKey,
    string FirstName,
    string LastName,
    string Email);
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

**Success**: All contract properties are projected.

## When to suppress warnings

You may suppress this warning if:
- The property intentionally uses a default value in certain scenarios
- The property is populated by a custom `ReadResult` implementation
- You're deliberately using a larger contract type across multiple queries

Since this is an informational diagnostic, suppressing it is acceptable in appropriate scenarios.

## Related rules

- [ATCK306](ATCK306.md): Projection field not found in result type
- [ATCK308](ATCK308.md): Missing final project statement
- [ATCK309](ATCK309.md): Projection field naming mismatch
