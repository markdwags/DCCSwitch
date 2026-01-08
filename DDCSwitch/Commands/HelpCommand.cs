using Spectre.Console;

namespace DDCSwitch.Commands;

internal static class HelpCommand
{
    public static string GetVersion()
    {
        var version = typeof(HelpCommand).Assembly
            .GetName().Version?.ToString(3) ?? "0.0.0";
        return version;
    }

    public static int ShowVersion(bool jsonOutput)
    {
        var version = GetVersion();

        if (jsonOutput)
        {
            Console.WriteLine($"{{\"version\":\"{version}\"}}");
        }
        else
        {
            AnsiConsole.Write(new FigletText("ddcswitch").Color(Color.Cyan1));
            AnsiConsole.MarkupLine($"[bold white] v{version}[/]");
            AnsiConsole.MarkupLine("[dim italic]>> Windows DDC/CI Monitor Input Switcher[/]");
        }

        return 0;
    }

    public static int ShowUsage()
    {
        var version = GetVersion();

        AnsiConsole.Write(new FigletText("ddcswitch").Color(Color.Cyan1));
        AnsiConsole.MarkupLine($"[bold white]v{version}[/]");
        AnsiConsole.MarkupLine($"[dim italic]>> A Windows command-line utility to control monitors using DDC/CI[/]\n");

        var commandsTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.White)
            .AddColumn(new TableColumn("[bold yellow]Command[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold yellow]Description[/]").LeftAligned());

        commandsTable.AddRow(
            "[cyan]list[/] [dim]or[/] [cyan]ls[/]",
            "List all DDC/CI capable monitors with current input sources");
        commandsTable.AddRow(
            "[cyan]list --verbose[/]",
            "Include brightness and contrast information");
        commandsTable.AddRow(
            "[cyan]get all[/]",
            "Enumerate all supported VCP features for all monitors");
        commandsTable.AddRow(
            "[cyan]get[/] [green]<monitor>[/] [blue][[feature]][/]",
            "Get current value for a monitor feature or get all features");
        commandsTable.AddRow(
            "[cyan]set[/] [green]<monitor>[/] [blue]<feature>[/] [magenta]<value>[/]",
            "Set value for a monitor feature");
        commandsTable.AddRow(
            "[cyan]version[/] [dim]or[/] [cyan]-v[/]",
            "Display version information");
        commandsTable.AddRow(
            "[cyan]help[/] [dim]or[/] [cyan]-h[/]",
            "Show this help message");

        AnsiConsole.Write(commandsTable);

        AnsiConsole.WriteLine();
        
        var panel = new Panel(
            "[bold yellow]Features:[/] brightness, contrast, input, or VCP codes like [cyan]0x10[/]\n" +
            "[bold yellow]Flags:[/]\n" +
            "  [cyan]--json[/]    Machine-readable JSON output for automation\n" +
            "  [cyan]--verbose[/] Include detailed information in list command")
        {
            Header = new PanelHeader("[bold green]>> Quick Reference[/]", Justify.Left),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.White)
        };
        
        AnsiConsole.Write(panel);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Examples:[/]");
        AnsiConsole.MarkupLine("  [grey]$[/] ddcswitch list");
        AnsiConsole.MarkupLine("  [grey]$[/] ddcswitch get all");
        AnsiConsole.MarkupLine("  [grey]$[/] ddcswitch get 0");
        AnsiConsole.MarkupLine("  [grey]$[/] ddcswitch get 0 brightness");
        AnsiConsole.MarkupLine("  [grey]$[/] ddcswitch set 0 input HDMI1");
        AnsiConsole.MarkupLine("  [grey]$[/] ddcswitch set 1 brightness 75%");

        return 0;
    }
}

