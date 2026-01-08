using Spectre.Console;
using System.Text.Json;

namespace DDCSwitch.Commands;

internal static class GetCommand
{
    public static int Execute(string[] args, bool jsonOutput)
    {
        if (args.Length < 2)
        {
            if (jsonOutput)
            {
                var error = new ErrorResponse(false, "Monitor identifier required");
                Console.WriteLine(JsonSerializer.Serialize(error, JsonContext.Default.ErrorResponse));
            }
            else
            {
                ConsoleOutputFormatter.WriteError("Monitor identifier required.");
                AnsiConsole.WriteLine("Usage: ddcswitch get <monitor> [feature]");
                AnsiConsole.WriteLine("       ddcswitch get all");
            }

            return 1;
        }

        // Check if the monitor identifier is "all"
        if (args[1].Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            // "get all" should only scan all monitors, no specific feature
            if (args.Length > 2)
            {
                if (jsonOutput)
                {
                    var error = new ErrorResponse(false, "Feature specification not supported with 'get all'");
                    Console.WriteLine(JsonSerializer.Serialize(error, JsonContext.Default.ErrorResponse));
                }
                else
                {
                    ConsoleOutputFormatter.WriteError("Feature specification not supported with 'get all'.");
                    AnsiConsole.WriteLine("Usage: ddcswitch get all");
                }
                return 1;
            }

            return GetAllMonitors(jsonOutput);
        }

        // If no feature is specified, perform VCP scan
        if (args.Length == 2)
        {
            return VcpScanCommand.ScanSingleMonitor(args[1], jsonOutput);
        }

        string featureInput = args[2];
        
        if (!FeatureResolver.TryResolveFeature(featureInput, out VcpFeature? feature))
        {
            return HandleInvalidFeature(featureInput, jsonOutput);
        }

        var monitors = MonitorController.EnumerateMonitors();

        if (monitors.Count == 0)
        {
            return HandleNoMonitors(jsonOutput);
        }

        var monitor = MonitorController.FindMonitor(monitors, args[1]);

        if (monitor == null)
        {
            return HandleMonitorNotFound(monitors, args[1], jsonOutput);
        }

        int result = ReadFeatureValue(monitor, feature!, jsonOutput);

        // Cleanup
        foreach (var m in monitors)
        {
            m.Dispose();
        }

        return result;
    }

    private static int GetAllMonitors(bool jsonOutput)
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

        return VcpScanCommand.ScanAllMonitors(monitors, jsonOutput);
    }

    private static int HandleInvalidFeature(string featureInput, bool jsonOutput)
    {
        string errorMessage;
        
        // Provide specific error message based on input type
        if (FeatureResolver.TryParseVcpCode(featureInput, out _))
        {
            // Valid VCP code but not in our predefined list
            errorMessage = $"VCP code '{featureInput}' is valid but may not be supported by all monitors";
        }
        else
        {
            // Invalid feature name or VCP code
            errorMessage = $"Invalid feature '{featureInput}'. {FeatureResolver.GetVcpCodeValidationError(featureInput)}";
        }
        
        if (jsonOutput)
        {
            var error = new ErrorResponse(false, errorMessage);
            Console.WriteLine(JsonSerializer.Serialize(error, JsonContext.Default.ErrorResponse));
        }
        else
        {
            ConsoleOutputFormatter.WriteError(errorMessage);
            AnsiConsole.MarkupLine("Valid features: brightness, contrast, input, or VCP code (0x10, 0x12, etc.)");
        }

        return 1;
    }

    private static int HandleNoMonitors(bool jsonOutput)
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

    private static int HandleMonitorNotFound(List<Monitor> monitors, string monitorId, bool jsonOutput)
    {
        if (jsonOutput)
        {
            var error = new ErrorResponse(false, $"Monitor '{monitorId}' not found");
            Console.WriteLine(JsonSerializer.Serialize(error, JsonContext.Default.ErrorResponse));
        }
        else
        {
            ConsoleOutputFormatter.WriteError($"Monitor '{monitorId}' not found.");
            AnsiConsole.MarkupLine("Use [yellow]ddcswitch list[/] to see available monitors.");
        }

        // Cleanup
        foreach (var m in monitors)
        {
            m.Dispose();
        }

        return 1;
    }

    private static int ReadFeatureValue(Monitor monitor, VcpFeature feature, bool jsonOutput)
    {
        bool success = false;
        uint current = 0;
        uint max = 0;
        int errorCode = 0;
        
        if (!jsonOutput)
        {
            AnsiConsole.Status()
                .Start($"Reading {feature.Name} from {monitor.Name}...", ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    ctx.SpinnerStyle(Style.Parse("cyan"));
                    success = monitor.TryGetVcpFeature(feature.Code, out current, out max, out errorCode);
                });
        }
        else
        {
            success = monitor.TryGetVcpFeature(feature.Code, out current, out max, out errorCode);
        }

        if (!success)
        {
            return HandleReadFailure(monitor, feature, errorCode, jsonOutput);
        }

        OutputFeatureValue(monitor, feature, current, max, jsonOutput);
        return 0;
    }

    private static int HandleReadFailure(Monitor monitor, VcpFeature feature, int errorCode, bool jsonOutput)
    {
        string errorMessage;
        
        if (VcpErrorHandler.IsTimeoutError(errorCode))
        {
            errorMessage = VcpErrorHandler.CreateTimeoutMessage(monitor, feature, "read");
        }
        else if (VcpErrorHandler.IsUnsupportedFeatureError(errorCode))
        {
            errorMessage = VcpErrorHandler.CreateUnsupportedFeatureMessage(monitor, feature);
        }
        else if (errorCode == 0x00000006) // ERROR_INVALID_HANDLE
        {
            errorMessage = VcpErrorHandler.CreateCommunicationFailureMessage(monitor);
        }
        else
        {
            errorMessage = VcpErrorHandler.CreateReadFailureMessage(monitor, feature);
        }
        
        if (jsonOutput)
        {
            var monitorRef = new MonitorReference(monitor.Index, monitor.Name, monitor.DeviceName, monitor.IsPrimary);
            var error = new ErrorResponse(false, errorMessage, monitorRef);
            Console.WriteLine(JsonSerializer.Serialize(error, JsonContext.Default.ErrorResponse));
        }
        else
        {
            ConsoleOutputFormatter.WriteError(errorMessage);
        }

        return 1;
    }

    private static void OutputFeatureValue(Monitor monitor, VcpFeature feature, uint current, uint max, bool jsonOutput)
    {
        if (jsonOutput)
        {
            var monitorRef = new MonitorReference(monitor.Index, monitor.Name, monitor.DeviceName, monitor.IsPrimary);
            
            uint? percentageValue = feature.SupportsPercentage ? FeatureResolver.ConvertRawToPercentage(current, max) : null;
            var result = new GetVcpResponse(true, monitorRef, feature.Name, current, max, percentageValue);
            Console.WriteLine(JsonSerializer.Serialize(result, JsonContext.Default.GetVcpResponse));
        }
        else
        {
            var panel = new Panel(
                $"[bold cyan]Monitor:[/] {monitor.Name}\n" +
                $"[dim]Device:[/] [dim]{monitor.DeviceName}[/]")
            {
                Header = new PanelHeader($"[bold green]>> Feature Value[/]", Justify.Left),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Cyan)
            };
            AnsiConsole.Write(panel);
            
            if (feature.Code == InputSource.VcpInputSource)
            {
                // Display input with name resolution
                var inputName = InputSource.GetName(current);
                AnsiConsole.MarkupLine($"  [bold yellow]{feature.Name}:[/] [cyan]{inputName}[/] [dim](0x{current:X2})[/]");
            }
            else if (feature.SupportsPercentage)
            {
                // Display percentage for brightness/contrast
                uint percentage = FeatureResolver.ConvertRawToPercentage(current, max);
                var progressBar = new BarChart()
                    .Width(40)
                    .Label($"[bold yellow]{feature.Name}[/]")
                    .CenterLabel()
                    .AddItem("", percentage, Color.Green);
                
                AnsiConsole.Write(progressBar);
                AnsiConsole.MarkupLine($"  [bold green]{percentage}%[/] [dim](raw: {current}/{max})[/]");
            }
            else
            {
                // Display raw values for unknown VCP codes
                AnsiConsole.MarkupLine($"  [bold yellow]{feature.Name}:[/] [green]{current}[/] [dim](max: {max})[/]");
            }
        }
    }
}

