using Spectre.Console;

namespace DDCSwitch.Commands;

internal static class ConsoleOutputFormatter
{
    public static void WriteError(string message)
    {
        AnsiConsole.MarkupLine($"[red]Error:[/] {message}");
    }

    public static void WriteInfo(string message)
    {
        AnsiConsole.MarkupLine(message);
    }

    public static void WriteSuccess(string message)
    {
        AnsiConsole.MarkupLine($"[green]✓[/] {message}");
    }
}

