using Spectre.Console;
using System.Text.Json;

namespace DDCSwitch.Commands;

internal static class InfoCommand
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
                AnsiConsole.WriteLine("Usage: ddcswitch info <monitor>");
            }

            return 1;
        }

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
            return HandleNoMonitors(jsonOutput);
        }

        var monitor = MonitorController.FindMonitor(monitors, args[1]);

        if (monitor == null)
        {
            return HandleMonitorNotFound(monitors, args[1], jsonOutput);
        }

        int result = DisplayMonitorInfo(monitor, jsonOutput);

        // Cleanup
        foreach (var m in monitors)
        {
            m.Dispose();
        }

        return result;
    }

    private static int DisplayMonitorInfo(Monitor monitor, bool jsonOutput)
    {
        try
        {
            // Get current input source
            string? currentInput = null;
            uint? currentInputCode = null;
            string status = "ok";

            if (monitor.TryGetInputSource(out uint current, out _))
            {
                currentInput = InputSource.GetName(current);
                currentInputCode = current;
            }
            else
            {
                status = "no_ddc_ci";
            }

            if (jsonOutput)
            {
                var monitorRef = new MonitorReference(
                    monitor.Index,
                    monitor.ResolvedName,
                    monitor.DeviceName,
                    monitor.IsPrimary);

                var edidInfo = new EdidInfo(
                    monitor.EdidVersion?.Major,
                    monitor.EdidVersion?.Minor,
                    monitor.ManufacturerId,
                    monitor.ManufacturerName,
                    monitor.ModelName,
                    monitor.SerialNumber,
                    monitor.ProductCode.HasValue ? $"0x{monitor.ProductCode.Value:X4}" : null,
                    monitor.ManufactureYear,
                    monitor.ManufactureWeek,
                    monitor.VideoInputDefinition?.IsDigital,
                    monitor.VideoInputDefinition?.ToString(),
                    monitor.SupportedFeatures != null ? new FeaturesInfo(
                        monitor.SupportedFeatures.DisplayTypeDescription,
                        monitor.SupportedFeatures.DpmsStandby,
                        monitor.SupportedFeatures.DpmsSuspend,
                        monitor.SupportedFeatures.DpmsActiveOff,
                        monitor.SupportedFeatures.DefaultColorSpace,
                        monitor.SupportedFeatures.PreferredTimingMode,
                        monitor.SupportedFeatures.ContinuousFrequency) : null,
                    monitor.Chromaticity != null ? new ChromaticityInfo(
                        new ColorPointInfo(monitor.Chromaticity.Red.X, monitor.Chromaticity.Red.Y),
                        new ColorPointInfo(monitor.Chromaticity.Green.X, monitor.Chromaticity.Green.Y),
                        new ColorPointInfo(monitor.Chromaticity.Blue.X, monitor.Chromaticity.Blue.Y),
                        new ColorPointInfo(monitor.Chromaticity.White.X, monitor.Chromaticity.White.Y)) : null);

                var response = new MonitorInfoResponse(
                    true,
                    monitorRef,
                    status,
                    currentInput,
                    currentInputCode.HasValue ? $"0x{currentInputCode.Value:X2}" : null,
                    edidInfo);

                Console.WriteLine(JsonSerializer.Serialize(response, JsonContext.Default.MonitorInfoResponse));
            }
            else
            {
                ConsoleOutputFormatter.WriteMonitorDetails(monitor);

                // Current Status Panel
                if (status == "ok")
                {
                    var statusPanel = new Panel(
                        $"[green]✓ DDC/CI Active[/]\n" +
                        $"[cyan]Current Input:[/] [white]{currentInput}[/] [dim](0x{currentInputCode:X2})[/]")
                    {
                        Header = new PanelHeader("[bold green]📡 Current Status[/]", Justify.Left),
                        Border = BoxBorder.Rounded,
                        BorderStyle = new Style(Color.Green)
                    };
                    AnsiConsole.Write(statusPanel);
                }
                else
                {
                    var warningPanel = new Panel(
                        "[yellow]DDC/CI communication not available[/]\n" +
                        "[dim]Input source switching may not be supported on this monitor[/]")
                    {
                        Header = new PanelHeader("[bold yellow]⚠ Warning[/]", Justify.Left),
                        Border = BoxBorder.Rounded,
                        BorderStyle = new Style(Color.Yellow)
                    };
                    AnsiConsole.Write(warningPanel);
                }

                AnsiConsole.WriteLine();
            }

            return 0;
        }
        catch (Exception ex)
        {
            if (jsonOutput)
            {
                var monitorRef = new MonitorReference(monitor.Index, monitor.ResolvedName, monitor.DeviceName, monitor.IsPrimary);
                var error = new ErrorResponse(false, $"Failed to retrieve monitor information: {ex.Message}", monitorRef);
                Console.WriteLine(JsonSerializer.Serialize(error, JsonContext.Default.ErrorResponse));
            }
            else
            {
                ConsoleOutputFormatter.WriteError($"Failed to retrieve monitor information: {ex.Message}");
            }

            return 1;
        }
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

    private static int HandleMonitorNotFound(List<Monitor> monitors, string identifier, bool jsonOutput)
    {
        if (jsonOutput)
        {
            var error = new ErrorResponse(false, $"Monitor '{identifier}' not found");
            Console.WriteLine(JsonSerializer.Serialize(error, JsonContext.Default.ErrorResponse));
        }
        else
        {
            ConsoleOutputFormatter.WriteError($"Monitor '{identifier}' not found.");
            AnsiConsole.MarkupLine("[dim]Use [/][yellow]ddcswitch list[/][dim] to see available monitors.[/]");
        }

        foreach (var m in monitors)
        {
            m.Dispose();
        }

        return 1;
    }
}
