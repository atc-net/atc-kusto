namespace Atc.Kusto.CLI.Services;

/// <summary>
/// Loads and saves CLI configuration from a JSON file.
/// Default path: {LocalApplicationData}/atc-kusto/config.json (overridable via KUSTO_CLI_CONFIG_PATH).
/// </summary>
public sealed class FileCliConfigStore : ICliConfigStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string configPath;

    public FileCliConfigStore()
    {
        var envPath = Environment.GetEnvironmentVariable("KUSTO_CLI_CONFIG_PATH");
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            configPath = envPath;
        }
        else
        {
            configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "atc-kusto",
                "config.json");
        }
    }

    /// <inheritdoc />
    public async Task<KustoCliConfig> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(configPath))
        {
            return new KustoCliConfig();
        }

        var json = await File.ReadAllTextAsync(configPath, Encoding.UTF8, cancellationToken);
        return JsonSerializer.Deserialize<KustoCliConfig>(json, SerializerOptions)
               ?? new KustoCliConfig();
    }

    /// <inheritdoc />
    public Task SaveAsync(
        KustoCliConfig config,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);

        var directory = Path.GetDirectoryName(configPath);
        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(config, SerializerOptions);
        return File.WriteAllTextAsync(configPath, json, Encoding.UTF8, cancellationToken);
    }
}