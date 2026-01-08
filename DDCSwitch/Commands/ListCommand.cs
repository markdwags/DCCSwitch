using Spectre.Console;
using System.Text.Json;

namespace DDCSwitch.Commands;

internal static class ListCommand
{
    public static int Execute(bool jsonOutput, bool verboseOutput, bool scanOutput)
    {
        List<Monitor> monitors;

        if (!jsonOutput)
        {
            monitors = null!;
            AnsiConsole.Status()
                .Start(scanOutput ? "Scanning VCP features..." : "Enumerating monitors...", ctx =>
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
                var result = new ListMonitorsResponse(false, null, "No DDC/CI capable monitors found");
                Console.WriteLine(JsonSerializer.Serialize(result, JsonContext.Default.ListMonitorsResponse));
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]No DDC/CI capable monitors found.[/]");
            }

            return 1;
        }

        // If scan mode is enabled, perform VCP scanning for each monitor
        if (scanOutput)
        {
            return VcpScanCommand.ScanAllMonitors(monitors, jsonOutput);
        }

        if (jsonOutput)
        {
            OutputJsonList(monitors, verboseOutput);
        }
        else
        {
            OutputTableList(monitors, verboseOutput);
        }

        // Cleanup
        foreach (var monitor in monitors)
        {
            monitor.Dispose();
        }

        return 0;
    }

    private static void OutputJsonList(List<Monitor> monitors, bool verboseOutput)
    {
        var monitorList = monitors.Select(monitor =>
        {
            string? inputName = null;
            uint? inputCode = null;
            string status = "ok";
            string? brightness = null;
            string? contrast = null;

            try
            {
                if (monitor.TryGetInputSource(out uint current, out _))
                {
                    inputName = InputSource.GetName(current);
                    inputCode = current;
                }
                else
                {
                    status = "no_ddc_ci";
                }

                // Get brightness and contrast if verbose mode is enabled
                if (verboseOutput && status == "ok")
                {
                    // Try to get brightness (VCP 0x10)
                    if (monitor.TryGetVcpFeature(VcpFeature.Brightness.Code, out uint brightnessCurrent, out uint brightnessMax))
                    {
                        uint brightnessPercentage = FeatureResolver.ConvertRawToPercentage(brightnessCurrent, brightnessMax);
                        brightness = $"{brightnessPercentage}%";
                    }
                    else
                    {
                        brightness = "N/A";
                    }

                    // Try to get contrast (VCP 0x12)
                    if (monitor.TryGetVcpFeature(VcpFeature.Contrast.Code, out uint contrastCurrent, out uint contrastMax))
                    {
                        uint contrastPercentage = FeatureResolver.ConvertRawToPercentage(contrastCurrent, contrastMax);
                        contrast = $"{contrastPercentage}%";
                    }
                    else
                    {
                        contrast = "N/A";
                    }
                }
            }
            catch
            {
                status = "error";
                if (verboseOutput)
                {
                    brightness = "N/A";
                    contrast = "N/A";
                }
            }

            return new MonitorInfo(
                monitor.Index,
                monitor.Name,
                monitor.DeviceName,
                monitor.IsPrimary,
                inputName,
                inputCode != null ? $"0x{inputCode:X2}" : null,
                status,
                brightness,
                contrast);
        }).ToList();

        var result = new ListMonitorsResponse(true, monitorList);
        Console.WriteLine(JsonSerializer.Serialize(result, JsonContext.Default.ListMonitorsResponse));
    }

    private static void OutputTableList(List<Monitor> monitors, bool verboseOutput)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Index")
            .AddColumn("Monitor Name")
            .AddColumn("Device")
            .AddColumn("Current Input");

        // Add brightness and contrast columns if verbose mode is enabled
        if (verboseOutput)
        {
            table.AddColumn("Brightness");
            table.AddColumn("Contrast");
        }

        table.AddColumn("Status");

        foreach (var monitor in monitors)
        {
            string inputInfo = "N/A";
            string status = "[green]OK[/]";
            string brightnessInfo = "N/A";
            string contrastInfo = "N/A";

            try
            {
                if (monitor.TryGetInputSource(out uint current, out _))
                {
                    inputInfo = $"{InputSource.GetName(current)} (0x{current:X2})";
                }
                else
                {
                    status = "[yellow]No DDC/CI[/]";
                }

                // Get brightness and contrast if verbose mode is enabled and monitor supports DDC/CI
                if (verboseOutput && status == "[green]OK[/]")
                {
                    // Try to get brightness (VCP 0x10)
                    if (monitor.TryGetVcpFeature(VcpFeature.Brightness.Code, out uint brightnessCurrent, out uint brightnessMax))
                    {
                        uint brightnessPercentage = FeatureResolver.ConvertRawToPercentage(brightnessCurrent, brightnessMax);
                        brightnessInfo = $"{brightnessPercentage}%";
                    }
                    else
                    {
                        brightnessInfo = "[dim]N/A[/]";
                    }

                    // Try to get contrast (VCP 0x12)
                    if (monitor.TryGetVcpFeature(VcpFeature.Contrast.Code, out uint contrastCurrent, out uint contrastMax))
                    {
                        uint contrastPercentage = FeatureResolver.ConvertRawToPercentage(contrastCurrent, contrastMax);
                        contrastInfo = $"{contrastPercentage}%";
                    }
                    else
                    {
                        contrastInfo = "[dim]N/A[/]";
                    }
                }
                else if (verboseOutput)
                {
                    brightnessInfo = "[dim]N/A[/]";
                    contrastInfo = "[dim]N/A[/]";
                }
            }
            catch
            {
                status = "[red]Error[/]";
                if (verboseOutput)
                {
                    brightnessInfo = "[dim]N/A[/]";
                    contrastInfo = "[dim]N/A[/]";
                }
            }

            var row = new List<string>
            {
                monitor.IsPrimary ? $"{monitor.Index} [yellow]*[/]" : monitor.Index.ToString(),
                monitor.Name,
                monitor.DeviceName,
                inputInfo
            };

            // Add brightness and contrast columns if verbose mode is enabled
            if (verboseOutput)
            {
                row.Add(brightnessInfo);
                row.Add(contrastInfo);
            }

            row.Add(status);

            table.AddRow(row.ToArray());
        }

        AnsiConsole.Write(table);
    }
}

