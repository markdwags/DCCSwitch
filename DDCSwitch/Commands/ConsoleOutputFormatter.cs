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
        WriteHeader($"Monitor {monitor.Index}: {monitor.Name}");
        
        WriteMonitorInfo("Device Name", monitor.DeviceName);
        WriteMonitorInfo("Is Primary", monitor.IsPrimary ? "Yes" : "No");
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("  [bold underline yellow]EDID Information[/]");
        
        if (monitor.EdidVersion != null)
            WriteMonitorInfo("EDID Version", monitor.EdidVersion.ToString());
        
        if (monitor.ManufacturerId != null)
            WriteMonitorInfo("Manufacturer ID", monitor.ManufacturerId);
        
        if (monitor.ManufacturerName != null)
            WriteMonitorInfo("Manufacturer", monitor.ManufacturerName);
        
        if (monitor.ModelName != null)
            WriteMonitorInfo("Model Name", monitor.ModelName);
        
        if (monitor.SerialNumber != null)
            WriteMonitorInfo("Serial Number", monitor.SerialNumber);
        
        if (monitor.ProductCode.HasValue)
            WriteMonitorInfo("Product Code", $"0x{monitor.ProductCode.Value:X4}");
        
        if (monitor.ManufactureYear.HasValue)
        {
            var date = monitor.ManufactureWeek.HasValue 
                ? $"{monitor.ManufactureYear.Value} Week {monitor.ManufactureWeek.Value}"
                : $"{monitor.ManufactureYear.Value}";
            WriteMonitorInfo("Manufacture Date", date);
        }
        
        if (monitor.VideoInputDefinition != null)
        {
            WriteMonitorInfo("Video Input Type", monitor.VideoInputDefinition.ToString());
        }
        
        if (monitor.SupportedFeatures != null)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("  [bold underline yellow]Supported Features[/]");
            WriteMonitorInfo("Display Type", monitor.SupportedFeatures.DisplayTypeDescription);
            WriteMonitorInfo("DPMS Standby", monitor.SupportedFeatures.DpmsStandby ? "Supported" : "Not supported");
            WriteMonitorInfo("DPMS Suspend", monitor.SupportedFeatures.DpmsSuspend ? "Supported" : "Not supported");
            WriteMonitorInfo("DPMS Active-Off", monitor.SupportedFeatures.DpmsActiveOff ? "Supported" : "Not supported");
            WriteMonitorInfo("Default Color Space", monitor.SupportedFeatures.DefaultColorSpace ? "Standard" : "Non-standard");
            WriteMonitorInfo("Preferred Timing Mode", monitor.SupportedFeatures.PreferredTimingMode ? "Included" : "Not included");
            WriteMonitorInfo("Continuous Frequency", monitor.SupportedFeatures.ContinuousFrequency ? "Supported" : "Not supported");
        }
        
        if (monitor.Chromaticity != null)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("  [bold underline yellow]Chromaticity Coordinates (CIE 1931)[/]");
            WriteMonitorInfo("Red", monitor.Chromaticity.Red.ToString());
            WriteMonitorInfo("Green", monitor.Chromaticity.Green.ToString());
            WriteMonitorInfo("Blue", monitor.Chromaticity.Blue.ToString());
            WriteMonitorInfo("White Point", monitor.Chromaticity.White.ToString());
        }
        
        AnsiConsole.WriteLine();
    }
}

