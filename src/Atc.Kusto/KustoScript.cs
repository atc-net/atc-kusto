namespace Atc.Kusto;

/// <summary>
/// Represents an abstract base class for Kusto scripts, providing mechanisms to
/// retrieve query text and parameters from embedded resources and object properties.
/// </summary>
public abstract record KustoScript : IKustoScript
{
    private readonly Type type;

    protected KustoScript()
    {
        type = GetType();
    }

    /// <inheritdoc />
    public string GetQueryText()
    {
        var resourcePath = $"{type.FullName}.kusto";

        using var stream = type.Assembly.GetManifestResourceStream(resourcePath)
                           ?? throw new FileNotFoundException("Could not load embedded resource.", resourcePath);

        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }

    /// <inheritdoc />
    public IDictionary<string, object> GetParameters()
        => new Dictionary<string, object>(GetPropertyValues(), StringComparer.Ordinal);

    /// <summary>
    /// Extracts the public instance properties of the derived class,
    /// converts their names to camelCase, and pairs them with their values.
    /// This method underpins the parameter extraction process, making it easier
    /// to map object properties to Kusto query parameters.
    /// </summary>
    /// <returns>An IEnumerable of key-value pairs representing the properties and their values.</returns>
    private IEnumerable<KeyValuePair<string, object>> GetPropertyValues()
    {
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

        foreach (var property in properties)
        {
            if (property.GetValue(this) is not { } value)
            {
                continue;
            }

            var name = property.Name.CamelCase();
            yield return KeyValuePair.Create(name, value);
        }
    }
}