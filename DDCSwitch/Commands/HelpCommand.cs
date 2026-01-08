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
            AnsiConsole.Write(new FigletText("DDCSwitch").Color(Color.Blue));
            AnsiConsole.MarkupLine($"[bold]Version:[/] [green]{version}[/]");
            AnsiConsole.MarkupLine("[dim]Windows DDC/CI Monitor Input Switcher[/]");
        }

        return 0;
    }

    public static int ShowUsage()
    {
        var version = GetVersion();

        AnsiConsole.Write(new FigletText("DDCSwitch").Color(Color.Blue));
        AnsiConsole.MarkupLine($"[dim]Windows DDC/CI Monitor Input Switcher v{version}[/]\n");

        AnsiConsole.MarkupLine("[yellow]Commands:[/]");
        AnsiConsole.WriteLine("  list [--verbose] [--scan] - List all DDC/CI capable monitors");
        AnsiConsole.WriteLine("  get monitor [feature] - Get current value for a monitor feature or scan all features");
        AnsiConsole.MarkupLine("  set monitor feature value - Set value for a monitor feature");
        AnsiConsole.MarkupLine("  version - Display version information");
        
        AnsiConsole.MarkupLine("\nSupported features: brightness, contrast, input, or VCP codes like 0x10");
        AnsiConsole.MarkupLine("Use [yellow]--json[/] flag for JSON output");
        AnsiConsole.MarkupLine("Use [yellow]--verbose[/] flag with list to include brightness and contrast");
        AnsiConsole.MarkupLine("Use [yellow]--scan[/] flag with list to enumerate all VCP codes");

        return 0;
    }
}

