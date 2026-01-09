using Spectre.Console;
using System.Text.Json;

namespace DDCSwitch.Commands;

internal static class VcpScanCommand
{
    public static int ScanAllMonitors(List<Monitor> monitors, bool jsonOutput)
    {
        if (jsonOutput)
        {
            OutputJsonScanAll(monitors);
        }
        else
        {
            OutputTableScanAll(monitors);
        }

        // Cleanup
        foreach (var monitor in monitors)
        {
            monitor.Dispose();
        }

        return 0;
    }

    public static int ScanSingleMonitor(string monitorIdentifier, bool jsonOutput)
    {
        List<Monitor> monitors;

        if (!jsonOutput)
        {
            monitors = null!;
            AnsiConsole.Status()
                .Start("Enumerating monitors...", ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    ctx.SpinnerStyle(Style.Parse("cyan"));
                    monitors = MonitorController.EnumerateMonitors();
                });
        }
        else
        {
            monitors = MonitorController.EnumerateMonitors();
        }

        if (monitors.Count == 0)
        {
            if (jsonOutput)
            {
                var error = new ErrorResponse(false, "No DDC/CI capable monitors found");
                Console.WriteLine(JsonSerializer.Serialize(error, JsonContext.Default.ErrorResponse));
            }
            else
            {
                ConsoleOutputFormatter.WriteError("No DDC/CI capable monitors found.");
            }

            return 1;
        }

        var monitor = MonitorController.FindMonitor(monitors, monitorIdentifier);

        if (monitor == null)
        {
            if (jsonOutput)
            {
                var error = new ErrorResponse(false, $"Monitor '{monitorIdentifier}' not found");
                Console.WriteLine(JsonSerializer.Serialize(error, JsonContext.Default.ErrorResponse));
            }
            else
            {
                ConsoleOutputFormatter.WriteError($"Monitor '{monitorIdentifier}' not found.");
                AnsiConsole.MarkupLine("Use [yellow]ddcswitch list[/] to see available monitors.");
            }

            // Cleanup
            foreach (var m in monitors)
            {
                m.Dispose();
            }

            return 1;
        }

        int result;
        
        try
        {
            var monitorRef = new MonitorReference(monitor.Index, monitor.Name, monitor.DeviceName, monitor.IsPrimary);
            Dictionary<byte, VcpFeatureInfo> features;
            
            if (!jsonOutput)
            {
                features = null!;
                AnsiConsole.Status()
                    .Start($"Scanning VCP features for {monitor.Name}...", ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("cyan"));
                        features = monitor.ScanVcpFeatures();
                    });
            }
            else
            {
                features = monitor.ScanVcpFeatures();
            }
            
            // Filter only supported features for cleaner output
            var supportedFeatures = new List<VcpFeatureInfo>(features.Count);
            foreach (var feature in features.Values)
            {
                if (feature.IsSupported)
                {
                    supportedFeatures.Add(feature);
                }
            }
            supportedFeatures.Sort((a, b) => a.Code.CompareTo(b.Code));

            if (jsonOutput)
            {
                OutputJsonScanSingle(monitorRef, supportedFeatures);
            }
            else
            {
                OutputTableScanSingle(monitor, supportedFeatures);
            }

            result = 0;
        }
        catch (Exception ex)
        {
            if (jsonOutput)
            {
                var monitorRef = new MonitorReference(monitor.Index, monitor.Name, monitor.DeviceName, monitor.IsPrimary);
                var scanResult = new VcpScanResponse(false, monitorRef, new List<VcpFeatureInfo>(), ex.Message);
                Console.WriteLine(JsonSerializer.Serialize(scanResult, JsonContext.Default.VcpScanResponse));
            }
            else
            {
                ConsoleOutputFormatter.WriteError($"Error scanning monitor {monitor.Index} ({monitor.Name}): {ex.Message}");
            }

            result = 1;
        }

        // Cleanup
        foreach (var m in monitors)
        {
            m.Dispose();
        }

        return result;
    }

    private static void OutputJsonScanAll(List<Monitor> monitors)
    {
        var scanResults = new List<VcpScanResponse>();

        foreach (var monitor in monitors)
        {
            try
            {
                var monitorRef = new MonitorReference(monitor.Index, monitor.Name, monitor.DeviceName, monitor.IsPrimary);
                var features = monitor.ScanVcpFeatures();
                
                // Convert to list and filter only supported features for cleaner output
                var supportedFeatures = features.Values
                    .Where(f => f.IsSupported)
                    .OrderBy(f => f.Code)
                    .ToList();

                scanResults.Add(new VcpScanResponse(true, monitorRef, supportedFeatures));
            }
            catch (Exception ex)
            {
                var monitorRef = new MonitorReference(monitor.Index, monitor.Name, monitor.DeviceName, monitor.IsPrimary);
                scanResults.Add(new VcpScanResponse(false, monitorRef, new List<VcpFeatureInfo>(), ex.Message));
            }
        }

        // Output all scan results
        foreach (var result in scanResults)
        {
            Console.WriteLine(JsonSerializer.Serialize(result, JsonContext.Default.VcpScanResponse));
        }
    }

    private static void OutputTableScanAll(List<Monitor> monitors)
    {
        foreach (var monitor in monitors)
        {
            try
            {
                var rule = new Rule($"[bold cyan]Monitor {monitor.Index}: {monitor.Name}[/] [dim]({monitor.DeviceName})[/]")
                {
                    Justification = Justify.Left,
                    Style = new Style(Color.Cyan)
                };
                AnsiConsole.Write(rule);
                AnsiConsole.WriteLine();

                Dictionary<byte, VcpFeatureInfo> features = null!;
                AnsiConsole.Status()
                    .Start($"Scanning VCP features for {monitor.Name}...", ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("cyan"));
                        features = monitor.ScanVcpFeatures();
                    });
                
                var supportedFeatures = features.Values
                    .Where(f => f.IsSupported)
                    .OrderBy(f => f.Code)
                    .ToList();

                if (supportedFeatures.Count == 0)
                {
                    ConsoleOutputFormatter.WriteWarning("No supported VCP features found");
                    AnsiConsole.WriteLine();
                    continue;
                }

                // Show feature count summary
                int readWriteCount = supportedFeatures.Count(f => f.Type == VcpFeatureType.ReadWrite);
                int readOnlyCount = supportedFeatures.Count(f => f.Type == VcpFeatureType.ReadOnly);
                int writeOnlyCount = supportedFeatures.Count(f => f.Type == VcpFeatureType.WriteOnly);
                
                AnsiConsole.MarkupLine(
                    $"[bold green]✓ Found {supportedFeatures.Count} supported features[/] " +
                    $"[dim]([green]{readWriteCount}[/] R/W, [yellow]{readOnlyCount}[/] R, [red]{writeOnlyCount}[/] W)[/]\n");
                
                OutputFeatureTable(supportedFeatures);
                AnsiConsole.WriteLine();
            }
            catch (Exception ex)
            {
                ConsoleOutputFormatter.WriteError($"Error scanning monitor {monitor.Index} ({monitor.Name}): {ex.Message}");
                AnsiConsole.WriteLine();
            }
        }
    }

    private static void OutputJsonScanSingle(MonitorReference monitorRef, List<VcpFeatureInfo> supportedFeatures)
    {
        var scanResult = new VcpScanResponse(true, monitorRef, supportedFeatures);
        Console.WriteLine(JsonSerializer.Serialize(scanResult, JsonContext.Default.VcpScanResponse));
    }

    private static void OutputTableScanSingle(Monitor monitor, List<VcpFeatureInfo> supportedFeatures)
    {
        // Summary panel with feature count breakdown
        int readWriteCount = supportedFeatures.Count(f => f.Type == VcpFeatureType.ReadWrite);
        int readOnlyCount = supportedFeatures.Count(f => f.Type == VcpFeatureType.ReadOnly);
        int writeOnlyCount = supportedFeatures.Count(f => f.Type == VcpFeatureType.WriteOnly);
        
        var summaryPanel = new Panel(
            $"[bold white]{monitor.Name}[/]\n" +
            $"[dim]Device:[/] [dim]{monitor.DeviceName}[/]\n\n" +
            $"[bold yellow]Total Features:[/] [green]{supportedFeatures.Count}[/]  " +
            $"[dim]([green]{readWriteCount}[/] R/W, [yellow]{readOnlyCount}[/] R, [red]{writeOnlyCount}[/] W)[/]")
        {
            Header = new PanelHeader($"[bold cyan]🔍 VCP Feature Scan Results[/]", Justify.Left),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Cyan)
        };
        AnsiConsole.Write(summaryPanel);
        AnsiConsole.WriteLine();
        
        if (supportedFeatures.Count == 0)
        {
            ConsoleOutputFormatter.WriteWarning("No supported VCP features found");
        }
        else
        {
            OutputFeatureTable(supportedFeatures);
        }
    }

    private static void OutputFeatureTable(List<VcpFeatureInfo> supportedFeatures)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[bold yellow]VCP Code[/]").Centered())
            .AddColumn(new TableColumn("[bold yellow]Feature Name[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold yellow]Access[/]").Centered())
            .AddColumn(new TableColumn("[bold yellow]Current[/]").RightAligned())
            .AddColumn(new TableColumn("[bold yellow]Max[/]").RightAligned());

        foreach (var feature in supportedFeatures)
        {
            string vcpCode = $"[cyan]0x{feature.Code:X2}[/]";
            string accessType = feature.Type switch
            {
                VcpFeatureType.ReadOnly => "[yellow]✓[/] [dim]Read[/]",
                VcpFeatureType.WriteOnly => "[red]✎[/] [dim]Write[/]",
                VcpFeatureType.ReadWrite => "[green]✓✎[/] [dim]R/W[/]",
                _ => "[dim]?[/]"
            };

            string currentValue = $"[green]{feature.CurrentValue}[/]";
            string maxValue = $"[cyan]{feature.MaxValue}[/]";
            
            var name = feature.Name;
            
            // If the feature name isn't known, make it dim
            if (feature.Name.StartsWith("VCP_"))
            {
                name = $"[dim]{feature.Name}[/]";
            }
            else
            {
                // Add icon for common features
                name = feature.Code switch
                {
                    0x10 => $"☀️  {feature.Name}",
                    0x12 => $"🎨 {feature.Name}",
                    0x60 => $"📺 {feature.Name}",
                    0x62 => $"🔊 {feature.Name}",
                    0x8D => $"🔇 {feature.Name}",
                    _ => feature.Name
                };
            }

            table.AddRow(vcpCode, name, accessType, currentValue, maxValue);
        }

        AnsiConsole.Write(table);
    }
}

