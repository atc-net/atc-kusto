namespace Atc.Kusto.CLI;

public static class ConsoleHelper
{
    public static void WriteHeader()
        => Console.Spectre.Helpers.ConsoleHelper.WriteHeader("Kusto CLI");
}