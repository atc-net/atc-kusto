namespace Atc.Kusto.Sample;

public sealed class SingleLineConsoleFormatter() : ConsoleFormatter("singleline")
{
    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        var logLevel = logEntry.LogLevel.ToString();
        var category = logEntry.Category;
        var message = logEntry.Formatter(logEntry.State, logEntry.Exception);

        if (logEntry.Exception is not null)
        {
            message += $" {logEntry.Exception.Message}";
        }

        textWriter.WriteLine($"{logLevel}: {category} - {message}");
    }
}