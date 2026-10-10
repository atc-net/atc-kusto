namespace Atc.Kusto.Tests;

/// <summary>
/// Guards the public API: consumers must never need to reference the (pre-1.0) Kusto Ingest V2 SDK,
/// and the SDK's <see cref="DataSourceFormat"/> stays behind the Atc-owned <see cref="KustoIngestFormat"/>.
/// </summary>
public sealed class PublicApiTests
{
    private const System.Reflection.BindingFlags PublicMembers =
        System.Reflection.BindingFlags.Public |
        System.Reflection.BindingFlags.Instance |
        System.Reflection.BindingFlags.Static |
        System.Reflection.BindingFlags.DeclaredOnly;

    [Fact]
    public void Public_Api_Exposes_No_Kusto_Ingest_Types()
    {
        // Arrange
        var publicTypes = typeof(IKustoIngestor).Assembly.GetExportedTypes();

        // Act
        var leaks = publicTypes
            .SelectMany(type => ReferencedTypes(type).Select(referenced => (Owner: type, Referenced: referenced)))
            .Where(x => IsForbidden(x.Referenced))
            .Select(x => $"{x.Owner.FullName} -> {x.Referenced.FullName}")
            .Distinct(StringComparer.Ordinal)
            .ToList();

        // Assert
        leaks.Should().BeEmpty("no Kusto.Ingest.* or DataSourceFormat type may appear in Atc.Kusto's public API");
    }

    private static bool IsForbidden(Type type)
        => type == typeof(DataSourceFormat) ||
           (type.Namespace?.StartsWith("Kusto.Ingest", StringComparison.Ordinal) ?? false);

    private static IEnumerable<Type> ReferencedTypes(Type type)
    {
        var direct = new List<Type?> { type.BaseType };
        direct.AddRange(type.GetInterfaces().Where(i => i.IsPublic || i.IsNestedPublic));

        foreach (var member in type.GetMembers(PublicMembers))
        {
            switch (member)
            {
                case System.Reflection.MethodInfo method:
                    direct.Add(method.ReturnType);
                    direct.AddRange(method.GetParameters().Select(p => p.ParameterType));
                    break;
                case System.Reflection.ConstructorInfo constructor:
                    direct.AddRange(constructor.GetParameters().Select(p => p.ParameterType));
                    break;
                case System.Reflection.PropertyInfo property:
                    direct.Add(property.PropertyType);
                    break;
                case System.Reflection.FieldInfo field:
                    direct.Add(field.FieldType);
                    break;
                case System.Reflection.EventInfo @event:
                    direct.Add(@event.EventHandlerType);
                    break;
            }
        }

        return direct
            .Where(t => t is not null)
            .SelectMany(t => Expand(t!));
    }

    private static IEnumerable<Type> Expand(Type type)
    {
        if (type.HasElementType && type.GetElementType() is { } element)
        {
            return Expand(element);
        }

        return type.IsGenericType
            ? new[] { type }.Concat(type.GetGenericArguments().SelectMany(Expand))
            : [type];
    }
}