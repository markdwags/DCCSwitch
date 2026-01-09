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

    public static void WriteMonitorDetails(Monitor monitor)
    {
        // Header with monitor identification
        var headerPanel = new Panel(
            $"[bold white]{monitor.Name}[/]\n" +
            $"[dim]Device:[/] [cyan]{monitor.DeviceName}[/]  " +
            $"[dim]Primary:[/] {(monitor.IsPrimary ? "[green]Yes[/]" : "[dim]No[/]")}")
        {
            Header = new PanelHeader($"[bold cyan]Monitor {monitor.Index}[/]", Justify.Left),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Cyan1)
        };
        AnsiConsole.Write(headerPanel);
        AnsiConsole.WriteLine();

        // EDID Information in a table
        var edidTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[bold yellow]Property[/]").Width(20))
            .AddColumn(new TableColumn("[bold yellow]Value[/]"));

        if (monitor.EdidVersion != null)
            edidTable.AddRow("[cyan]EDID Version[/]", $"[white]{monitor.EdidVersion}[/]");
        
        if (monitor.ManufacturerName != null)
            edidTable.AddRow("[cyan]Manufacturer[/]", $"[white]{monitor.ManufacturerName}[/] [dim]({monitor.ManufacturerId})[/]");
        else if (monitor.ManufacturerId != null)
            edidTable.AddRow("[cyan]Manufacturer ID[/]", $"[white]{monitor.ManufacturerId}[/]");
        
        if (monitor.ModelName != null)
            edidTable.AddRow("[cyan]Model Name[/]", $"[white]{monitor.ModelName}[/]");
        
        if (monitor.SerialNumber != null)
            edidTable.AddRow("[cyan]Serial Number[/]", $"[white]{monitor.SerialNumber}[/]");
        
        if (monitor.ProductCode.HasValue)
            edidTable.AddRow("[cyan]Product Code[/]", $"[white]0x{monitor.ProductCode.Value:X4}[/]");
        
        if (monitor.ManufactureYear.HasValue)
        {
            var date = monitor.ManufactureWeek.HasValue 
                ? FormatManufactureDate(monitor.ManufactureYear.Value, monitor.ManufactureWeek.Value)
                : $"{monitor.ManufactureYear.Value}";
            edidTable.AddRow("[cyan]Manufacture Date[/]", $"[white]{date}[/]");
        }
        
        if (monitor.VideoInputDefinition != null)
        {
            var inputColor = monitor.VideoInputDefinition.IsDigital ? "green" : "yellow";
            edidTable.AddRow("[cyan]Video Input[/]", $"[{inputColor}]{monitor.VideoInputDefinition}[/]");
        }

        var edidPanel = new Panel(edidTable)
        {
            Header = new PanelHeader("[bold yellow]📋 EDID Information[/]", Justify.Left),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Yellow)
        };
        AnsiConsole.Write(edidPanel);
        AnsiConsole.WriteLine();

        // Supported Features
        if (monitor.SupportedFeatures != null)
        {
            var featuresGrid = new Grid();
            featuresGrid.AddColumn(new GridColumn().Width(25));
            featuresGrid.AddColumn(new GridColumn());

            featuresGrid.AddRow(
                "[cyan]Display Type:[/]",
                $"[white]{monitor.SupportedFeatures.DisplayTypeDescription}[/]");
            
            featuresGrid.AddRow(
                "[cyan]DPMS Standby:[/]",
                FormatFeatureSupport(monitor.SupportedFeatures.DpmsStandby));
            
            featuresGrid.AddRow(
                "[cyan]DPMS Suspend:[/]",
                FormatFeatureSupport(monitor.SupportedFeatures.DpmsSuspend));
            
            featuresGrid.AddRow(
                "[cyan]DPMS Active-Off:[/]",
                FormatFeatureSupport(monitor.SupportedFeatures.DpmsActiveOff));
            
            featuresGrid.AddRow(
                "[cyan]Default Color Space:[/]",
                monitor.SupportedFeatures.DefaultColorSpace ? "[green]Standard[/]" : "[dim]Non-standard[/]");
            
            featuresGrid.AddRow(
                "[cyan]Preferred Timing:[/]",
                monitor.SupportedFeatures.PreferredTimingMode ? "[green]✓ Included[/]" : "[dim]Not included[/]");
            
            featuresGrid.AddRow(
                "[cyan]Continuous Frequency:[/]",
                FormatFeatureSupport(monitor.SupportedFeatures.ContinuousFrequency));

            var featuresPanel = new Panel(featuresGrid)
            {
                Header = new PanelHeader("[bold magenta]⚡ Supported Features[/]", Justify.Left),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Magenta)
            };
            AnsiConsole.Write(featuresPanel);
            AnsiConsole.WriteLine();
        }

        // Chromaticity Coordinates
        if (monitor.Chromaticity != null)
        {
            var chromaTable = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Grey)
                .AddColumn(new TableColumn("[bold]Color[/]").Centered().Width(12))
                .AddColumn(new TableColumn("[bold]X[/]").Centered().Width(12))
                .AddColumn(new TableColumn("[bold]Y[/]").Centered().Width(12));

            chromaTable.AddRow(
                "[red]● Red[/]",
                $"[white]{monitor.Chromaticity.Red.X:F4}[/]",
                $"[white]{monitor.Chromaticity.Red.Y:F4}[/]");
            
            chromaTable.AddRow(
                "[green]● Green[/]",
                $"[white]{monitor.Chromaticity.Green.X:F4}[/]",
                $"[white]{monitor.Chromaticity.Green.Y:F4}[/]");
            
            chromaTable.AddRow(
                "[blue]● Blue[/]",
                $"[white]{monitor.Chromaticity.Blue.X:F4}[/]",
                $"[white]{monitor.Chromaticity.Blue.Y:F4}[/]");
            
            chromaTable.AddRow(
                "[grey]● White[/]",
                $"[white]{monitor.Chromaticity.White.X:F4}[/]",
                $"[white]{monitor.Chromaticity.White.Y:F4}[/]");

            var chromaPanel = new Panel(chromaTable)
            {
                Header = new PanelHeader("[bold blue]🎨 Chromaticity Coordinates (CIE 1931)[/]", Justify.Left),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Blue)
            };
            AnsiConsole.Write(chromaPanel);
            AnsiConsole.WriteLine();
        }
    }

    private static string FormatFeatureSupport(bool supported)
    {
        return supported ? "[green]✓ Supported[/]" : "[dim]✗ Not supported[/]";
    }

    private static string FormatManufactureDate(int year, int week)
    {
        // Calculate approximate month from week number
        // Week 1-4: January, Week 5-8: February, etc.
        string? monthName = week switch
        {
            >= 1 and <= 4 => "January",
            >= 5 and <= 8 => "February",
            >= 9 and <= 13 => "March",
            >= 14 and <= 17 => "April",
            >= 18 and <= 22 => "May",
            >= 23 and <= 26 => "June",
            >= 27 and <= 30 => "July",
            >= 31 and <= 35 => "August",
            >= 36 and <= 39 => "September",
            >= 40 and <= 43 => "October",
            >= 44 and <= 48 => "November",
            >= 49 and <= 53 => "December",
            _ => null
        };

        if (monthName != null)
        {
            return $"{monthName} {year} (Week {week})";
        }

        return $"{year} Week {week}";
    }
}

