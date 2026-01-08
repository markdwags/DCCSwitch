using Spectre.Console;

namespace DDCSwitch.Commands;

internal static class ConsoleOutputFormatter
{
    public static void WriteError(string message)
    {
        AnsiConsole.MarkupLine($"[bold red]X Error:[/] [red]{message}[/]");
    }

    public static void WriteInfo(string message)
    {
        AnsiConsole.MarkupLine($"[cyan]i[/] {message}");
    }

    public static void WriteSuccess(string message)
    {
        AnsiConsole.MarkupLine($"[bold green]> Success:[/] [green]{message}[/]");
    }

    public static void WriteWarning(string message)
    {
        AnsiConsole.MarkupLine($"[bold yellow]! Warning:[/] [yellow]{message}[/]");
    }

    public static void WriteHeader(string text)
    {
        var rule = new Rule($"[bold cyan]{text}[/]")
        {
            Justification = Justify.Left
        };
        AnsiConsole.Write(rule);
    }

    public static void WriteMonitorInfo(string label, string value, bool highlight = false)
    {
        var color = highlight ? "yellow" : "cyan";
        AnsiConsole.MarkupLine($"  [bold {color}]{label}:[/] {value}");
    }
}

