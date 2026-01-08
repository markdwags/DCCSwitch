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
                AnsiConsole.MarkupLine("Use [yellow]DDCSwitch list[/] to see available monitors.");
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
                        features = monitor.ScanVcpFeatures();
                    });
            }
            else
            {
                features = monitor.ScanVcpFeatures();
            }
            
            // Filter only supported features for cleaner output
            var supportedFeatures = features.Values
                .Where(f => f.IsSupported)
                .OrderBy(f => f.Code)
                .ToList();

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
                AnsiConsole.MarkupLine($"\n[bold blue]Monitor {monitor.Index}: {monitor.Name}[/] ({monitor.DeviceName})");
                
                Dictionary<byte, VcpFeatureInfo> features = null!;
                AnsiConsole.Status()
                    .Start($"Scanning VCP features for {monitor.Name}...", ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        features = monitor.ScanVcpFeatures();
                    });
                
                var supportedFeatures = features.Values
                    .Where(f => f.IsSupported)
                    .OrderBy(f => f.Code)
                    .ToList();

                if (supportedFeatures.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]  No supported VCP features found[/]");
                    continue;
                }

                OutputFeatureTable(supportedFeatures);
            }
            catch (Exception ex)
            {
                ConsoleOutputFormatter.WriteError($"Error scanning monitor {monitor.Index} ({monitor.Name}): {ex.Message}");
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
        AnsiConsole.MarkupLine($"[bold blue]Monitor {monitor.Index}: {monitor.Name}[/] ({monitor.DeviceName})");
        
        if (supportedFeatures.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]  No supported VCP features found[/]");
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
            .AddColumn("VCP Code")
            .AddColumn("Feature Name")
            .AddColumn("Access Type")
            .AddColumn("Current Value")
            .AddColumn("Max Value")
            .AddColumn("Percentage");

        foreach (var feature in supportedFeatures)
        {
            string vcpCode = $"0x{feature.Code:X2}";
            string accessType = feature.Type switch
            {
                VcpFeatureType.ReadOnly => "[yellow]Read-Only[/]",
                VcpFeatureType.WriteOnly => "[red]Write-Only[/]",
                VcpFeatureType.ReadWrite => "[green]Read-Write[/]",
                _ => "[dim]Unknown[/]"
            };

            string currentValue = feature.CurrentValue.ToString();
            string maxValue = feature.MaxValue.ToString();
            
            // Calculate percentage for known percentage-based features
            string percentage = "N/A";
            if ((feature.Code == VcpFeature.Brightness.Code || feature.Code == VcpFeature.Contrast.Code) && feature.MaxValue > 0)
            {
                uint percentageValue = FeatureResolver.ConvertRawToPercentage(feature.CurrentValue, feature.MaxValue);
                percentage = $"{percentageValue}%";
            }

            table.AddRow(vcpCode, feature.Name, accessType, currentValue, maxValue, percentage);
        }

        AnsiConsole.Write(table);
    }
}

