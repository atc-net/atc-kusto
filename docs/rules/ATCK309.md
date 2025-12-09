# ATCK309: Projection field naming mismatch

## Cause

A field in the `| project` statement uses snake_case naming (e.g., `customer_key`) that appears to correspond to a PascalCase property in the result type (e.g., `CustomerKey`), but the JSON deserializer won't match them automatically.

## Rule description

The Kusto library deserializes query results using `System.Text.Json` with case-insensitive property matching. However, this only handles case differences like `customerkey` vs `CustomerKey`. It does **not** handle naming convention differences like snake_case to PascalCase conversion.

When you project a field like `customer_key`, the deserializer looks for a property literally named `customer_key` (case-insensitive). It will not automatically map it to `CustomerKey` because the underscore makes it a different name entirely.

This is a **warning** because:
- The field won't deserialize to the intended property
- The property will have its default value at runtime
- This is almost always unintentional when the names are so similar

## How to fix violations

Either rename the projected field to match the property, or use an alias:

### Option 1: Use the property name directly

```kusto
Customers
| project
    CustomerKey,
    FirstName,
    LastName
```

### Option 2: Use an alias to rename the field

```kusto
Customers
| project
    CustomerKey = customer_key,
    FirstName = first_name,
    LastName = last_name
```

## Examples

### Violates ATCK309

```csharp
// Customer.cs
public record Customer(
    long CustomerKey,
    string FirstName,
    string LastName);
```

```kusto
// CustomersQuery.kusto
Customers
| project
    customer_key,   // ATCK309: Won't match CustomerKey
    first_name,     // ATCK309: Won't match FirstName
    last_name       // ATCK309: Won't match LastName
```

**Problem**: The snake_case field names won't deserialize to the PascalCase properties.

### Does not violate ATCK309

```kusto
// CustomersQuery.kusto - Using aliases
Customers
| project
    CustomerKey = customer_key,
    FirstName = first_name,
    LastName = last_name
```

**Success**: Aliases explicitly map snake_case source columns to PascalCase output fields.

## When to suppress warnings

You may suppress this warning if:
- You intentionally want the property to have its default value
- You have a custom deserialization mechanism that handles the naming conversion
- The snake_case field is not intended to map to the similarly-named PascalCase property

However, in most cases this warning indicates a bug that should be fixed.

## Related rules

- [ATCK306](ATCK306.md): Projection field not found in result type
- [ATCK307](ATCK307.md): Result type property not projected
- [ATCK308](ATCK308.md): Missing final project statement
