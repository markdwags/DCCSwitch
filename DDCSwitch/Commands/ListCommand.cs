using Spectre.Console;
using System.Text.Json;

namespace DDCSwitch.Commands;

internal static class ListCommand
{
    public static int Execute(bool jsonOutput, bool verboseOutput)
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
                var result = new ListMonitorsResponse(false, null, "No DDC/CI capable monitors found");
                Console.WriteLine(JsonSerializer.Serialize(result, JsonContext.Default.ListMonitorsResponse));
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]No DDC/CI capable monitors found.[/]");
            }

            return 1;
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
            .BorderColor(Color.White)
            .AddColumn(new TableColumn("[bold yellow]Index[/]").Centered())
            .AddColumn(new TableColumn("[bold yellow]Monitor Name[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold yellow]Device[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold yellow]Current Input[/]").LeftAligned());

        // Add brightness and contrast columns if verbose mode is enabled
        if (verboseOutput)
        {
            table.AddColumn(new TableColumn("[bold yellow]Brightness[/]").Centered());
            table.AddColumn(new TableColumn("[bold yellow]Contrast[/]").Centered());
        }

        table.AddColumn(new TableColumn("[bold yellow]Status[/]").Centered());

        foreach (var monitor in monitors)
        {
            string inputInfo = "[dim]N/A[/]";
            string status = "[green]+[/] [bold green]OK[/]";
            string brightnessInfo = "[dim]N/A[/]";
            string contrastInfo = "[dim]N/A[/]";

            try
            {
                if (monitor.TryGetInputSource(out uint current, out _))
                {
                    var inputName = InputSource.GetName(current);
                    inputInfo = $"[cyan]{inputName}[/] [dim](0x{current:X2})[/]";
                }
                else
                {
                    status = "[yellow]~[/] [bold yellow]No DDC/CI[/]";
                }

                // Get brightness and contrast if verbose mode is enabled and monitor supports DDC/CI
                if (verboseOutput && status == "[green]+[/] [bold green]OK[/]")
                {
                    // Try to get brightness (VCP 0x10)
                    if (monitor.TryGetVcpFeature(VcpFeature.Brightness.Code, out uint brightnessCurrent, out uint brightnessMax))
                    {
                        uint brightnessPercentage = FeatureResolver.ConvertRawToPercentage(brightnessCurrent, brightnessMax);
                        brightnessInfo = $"[green]{brightnessPercentage}%[/]";
                    }
                    else
                    {
                        brightnessInfo = "[dim]N/A[/]";
                    }

                    // Try to get contrast (VCP 0x12)
                    if (monitor.TryGetVcpFeature(VcpFeature.Contrast.Code, out uint contrastCurrent, out uint contrastMax))
                    {
                        uint contrastPercentage = FeatureResolver.ConvertRawToPercentage(contrastCurrent, contrastMax);
                        contrastInfo = $"[green]{contrastPercentage}%[/]";
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
                status = "[red]X[/] [bold red]Error[/]";
                if (verboseOutput)
                {
                    brightnessInfo = "[dim]N/A[/]";
                    contrastInfo = "[dim]N/A[/]";
                }
            }

            var row = new List<string>
            {
                monitor.IsPrimary ? $"[bold cyan]{monitor.Index}[/] [yellow]*[/]" : $"[cyan]{monitor.Index}[/]",
                monitor.Name,
                $"[dim]{monitor.DeviceName}[/]",
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

