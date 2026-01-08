using DDCSwitch;
using Spectre.Console;
using System.Text.Json;
using System.Text.Json.Serialization;

return DDCSwitchProgram.Run(args);

static class DDCSwitchProgram
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        TypeInfoResolver = JsonContext.Default
    };

    public static int Run(string[] args)
    {
        if (args.Length == 0)
        {
            ShowUsage();
            return 1;
        }

        // Check for --json flag
        bool jsonOutput = args.Contains("--json", StringComparer.OrdinalIgnoreCase);
        var filteredArgs = args.Where(a => !a.Equals("--json", StringComparison.OrdinalIgnoreCase)).ToArray();

        // Check for --verbose flag
        bool verboseOutput = filteredArgs.Contains("--verbose", StringComparer.OrdinalIgnoreCase);
        filteredArgs = filteredArgs.Where(a => !a.Equals("--verbose", StringComparison.OrdinalIgnoreCase)).ToArray();

        // Check for --scan flag
        bool scanOutput = filteredArgs.Contains("--scan", StringComparer.OrdinalIgnoreCase);
        filteredArgs = filteredArgs.Where(a => !a.Equals("--scan", StringComparison.OrdinalIgnoreCase)).ToArray();

        if (filteredArgs.Length == 0)
        {
            ShowUsage();
            return 1;
        }

        var command = filteredArgs[0].ToLowerInvariant();

        try
        {
            return command switch
            {
                "list" or "ls" => ListMonitors(jsonOutput, verboseOutput, scanOutput),
                "get" => GetCurrentInput(filteredArgs, jsonOutput),
                "set" => SetInput(filteredArgs, jsonOutput),
                "version" or "-v" or "--version" => ShowVersion(jsonOutput),
                "help" or "-h" or "--help" or "/?" => ShowUsage(),
                _ => InvalidCommand(filteredArgs[0], jsonOutput)
            };
        }
        catch (Exception ex)
        {
            if (jsonOutput)
            {
                var error = new ErrorResponse(false, ex.Message);
                Console.WriteLine(JsonSerializer.Serialize(error, JsonContext.Default.ErrorResponse));
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            }

            return 1;
        }
    }

    private static string GetVersion()
    {
        var version = typeof(DDCSwitchProgram).Assembly
            .GetName().Version?.ToString(3) ?? "0.0.0";
        return version;
    }

    private static int ShowVersion(bool jsonOutput)
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

    private static int ShowUsage()
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

    private static int InvalidCommand(string command, bool jsonOutput)
    {
        if (jsonOutput)
        {
            var error = new ErrorResponse(false, $"Unknown command: {command}");
            Console.WriteLine(JsonSerializer.Serialize(error, JsonContext.Default.ErrorResponse));
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]Unknown command:[/] {command}");
            AnsiConsole.MarkupLine("Run [yellow]DDCSwitch help[/] for usage information.");
        }

        return 1;
    }

    private static int ListMonitors(bool jsonOutput, bool verboseOutput = false, bool scanOutput = false)
    {
        List<DDCSwitch.Monitor> monitors;

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
            return HandleVcpScan(monitors, jsonOutput);
        }

        if (jsonOutput)
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
                    if (monitor.TryGetInputSource(out uint current, out uint max))
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
        else
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
                    if (monitor.TryGetInputSource(out uint current, out uint max))
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

        // Cleanup
        foreach (var monitor in monitors)
        {
            monitor.Dispose();
        }

        return 0;
    }

    private static int GetCurrentInput(string[] args, bool jsonOutput)
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
                AnsiConsole.MarkupLine("[red]Error:[/] Monitor identifier required.");
                AnsiConsole.WriteLine("Usage: DDCSwitch get <monitor> [feature]");
            }

            return 1;
        }

        // If no feature is specified, perform VCP scan
        if (args.Length == 2)
        {
            return HandleVcpScanForMonitor(args[1], jsonOutput);
        }

        string featureInput = args[2];
        
        if (!FeatureResolver.TryResolveFeature(featureInput, out VcpFeature? feature))
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
                AnsiConsole.MarkupLine($"[red]Error:[/] {errorMessage}");
                AnsiConsole.MarkupLine("Valid features: brightness, contrast, input, or VCP code (0x10, 0x12, etc.)");
            }

            return 1;
        }

        var monitors = MonitorController.EnumerateMonitors();

        if (monitors.Count == 0)
        {
            if (jsonOutput)
            {
                var error = new ErrorResponse(false, "No DDC/CI capable monitors found");
                Console.WriteLine(JsonSerializer.Serialize(error, JsonContext.Default.ErrorResponse));
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Error:[/] No DDC/CI capable monitors found.");
            }

            return 1;
        }

        var monitor = MonitorController.FindMonitor(monitors, args[1]);

        if (monitor == null)
        {
            if (jsonOutput)
            {
                var error = new ErrorResponse(false, $"Monitor '{args[1]}' not found");
                Console.WriteLine(JsonSerializer.Serialize(error, JsonContext.Default.ErrorResponse));
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Monitor '{args[1]}' not found.");
                AnsiConsole.MarkupLine("Use [yellow]DDCSwitch list[/] to see available monitors.");
            }

            // Cleanup
            foreach (var m in monitors)
            {
                m.Dispose();
            }

            return 1;
        }

        // Use generic VCP method for all features with enhanced error handling
        bool success = false;
        uint current = 0;
        uint max = 0;
        int errorCode = 0;
        
        if (!jsonOutput)
        {
            AnsiConsole.Status()
                .Start($"Reading {feature!.Name} from {monitor.Name}...", ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    success = monitor.TryGetVcpFeature(feature.Code, out current, out max, out errorCode);
                });
        }
        else
        {
            success = monitor.TryGetVcpFeature(feature!.Code, out current, out max, out errorCode);
        }

        if (!success)
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
                var monitorRef =
                    new MonitorReference(monitor.Index, monitor.Name, monitor.DeviceName, monitor.IsPrimary);
                var error = new ErrorResponse(false, errorMessage, monitorRef);
                Console.WriteLine(JsonSerializer.Serialize(error, JsonContext.Default.ErrorResponse));
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {errorMessage}");
            }

            // Cleanup
            foreach (var m in monitors)
            {
                m.Dispose();
            }

            return 1;
        }

        if (jsonOutput)
        {
            var monitorRef = new MonitorReference(monitor.Index, monitor.Name, monitor.DeviceName, monitor.IsPrimary);
            
            uint? percentageValue = feature.SupportsPercentage ? FeatureResolver.ConvertRawToPercentage(current, max) : null;
            var result = new GetVcpResponse(true, monitorRef, feature.Name, current, max, percentageValue);
            Console.WriteLine(JsonSerializer.Serialize(result, JsonContext.Default.GetVcpResponse));
        }
        else
        {
            AnsiConsole.MarkupLine($"[green]Monitor:[/] {monitor.Name} ({monitor.DeviceName})");
            
            if (feature.Code == InputSource.VcpInputSource)
            {
                // Display input with name resolution
                AnsiConsole.MarkupLine($"[green]Current {feature.Name}:[/] {InputSource.GetName(current)} (0x{current:X2})");
            }
            else if (feature.SupportsPercentage)
            {
                // Display percentage for brightness/contrast
                uint percentage = FeatureResolver.ConvertRawToPercentage(current, max);
                AnsiConsole.MarkupLine($"[green]Current {feature.Name}:[/] {percentage}% (raw: {current}/{max})");
            }
            else
            {
                // Display raw values for unknown VCP codes
                AnsiConsole.MarkupLine($"[green]Current {feature.Name}:[/] {current} (max: {max})");
            }
        }

        // Cleanup
        foreach (var m in monitors)
        {
            m.Dispose();
        }

        return 0;
    }

    private static int SetInput(string[] args, bool jsonOutput)
    {
        // Require 4 arguments: set <monitor> <feature> <value>
        if (args.Length < 4)
        {
            if (jsonOutput)
            {
                var error = new ErrorResponse(false, "Monitor, feature, and value required");
                Console.WriteLine(JsonSerializer.Serialize(error, JsonContext.Default.ErrorResponse));
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Monitor, feature, and value required.");
                AnsiConsole.MarkupLine("Usage: [yellow]DDCSwitch set <monitor> <feature> <value>[/]");
            }

            return 1;
        }

        string featureInput = args[2];
        string valueInput = args[3];

        if (!FeatureResolver.TryResolveFeature(featureInput, out VcpFeature? feature))
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
                AnsiConsole.MarkupLine($"[red]Error:[/] {errorMessage}");
                AnsiConsole.MarkupLine("Valid features: brightness, contrast, input, or VCP code (0x10, 0x12, etc.)");
            }

            return 1;
        }

        // Parse and validate the value based on feature type
        uint setValue = 0; // Initialize to avoid compiler error
        uint? percentageValue = null;
        string? validationError = null;
        
        if (feature!.Code == InputSource.VcpInputSource)
        {
            // Use existing input source parsing for input feature
            if (!InputSource.TryParse(valueInput, out setValue))
            {
                validationError = $"Invalid input source '{valueInput}'. Valid inputs: HDMI1, HDMI2, DP1, DP2, DVI1, DVI2, VGA1, VGA2, or hex code (0x11)";
            }
        }
        else if (feature.SupportsPercentage && FeatureResolver.TryParsePercentage(valueInput, out uint percentage))
        {
            // Parse as percentage for brightness/contrast - validate percentage range
            if (!FeatureResolver.IsValidPercentage(percentage))
            {
                validationError = VcpErrorHandler.CreateRangeValidationMessage(feature, percentage, 100, true);
            }
            else
            {
                percentageValue = percentage;
                // We'll convert to raw value after getting monitor's max value
                setValue = 0; // Placeholder
            }
        }
        else if (uint.TryParse(valueInput, out uint rawValue))
        {
            // Parse as raw value - we'll validate range after getting monitor's max value
            setValue = rawValue;
        }
        else
        {
            // Invalid value format
            if (feature.SupportsPercentage)
            {
                validationError = FeatureResolver.GetPercentageValidationError(valueInput);
            }
            else
            {
                validationError = $"Invalid value '{valueInput}' for feature '{feature.Name}'. Expected: numeric value within monitor's supported range";
            }
        }

        // If we have a validation error, return it now
        if (validationError != null)
        {
            if (jsonOutput)
            {
                var error = new ErrorResponse(false, validationError);
                Console.WriteLine(JsonSerializer.Serialize(error, JsonContext.Default.ErrorResponse));
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {validationError}");
            }

            return 1;
        }

        var monitors = MonitorController.EnumerateMonitors();

        if (monitors.Count == 0)
        {
            if (jsonOutput)
            {
                var error = new ErrorResponse(false, "No DDC/CI capable monitors found");
                Console.WriteLine(JsonSerializer.Serialize(error, JsonContext.Default.ErrorResponse));
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Error:[/] No DDC/CI capable monitors found.");
            }

            return 1;
        }

        var monitor = MonitorController.FindMonitor(monitors, args[1]);

        if (monitor == null)
        {
            if (jsonOutput)
            {
                var error = new ErrorResponse(false, $"Monitor '{args[1]}' not found");
                Console.WriteLine(JsonSerializer.Serialize(error, JsonContext.Default.ErrorResponse));
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Monitor '{args[1]}' not found.");
                AnsiConsole.MarkupLine("Use [yellow]DDCSwitch list[/] to see available monitors.");
            }

            // Cleanup
            foreach (var m in monitors)
            {
                m.Dispose();
            }

            return 1;
        }

        // If we have a percentage value or need to validate raw value range, get the monitor's max value
        if (percentageValue.HasValue || (feature!.Code != InputSource.VcpInputSource && !percentageValue.HasValue))
        {
            if (monitor.TryGetVcpFeature(feature.Code, out uint currentValue, out uint maxValue, out int errorCode))
            {
                if (percentageValue.HasValue)
                {
                    // Convert percentage to raw value
                    setValue = FeatureResolver.ConvertPercentageToRaw(percentageValue.Value, maxValue);
                }
                else if (feature.Code != InputSource.VcpInputSource)
                {
                    // Validate raw value is within supported range
                    if (!FeatureResolver.IsValidRawVcpValue(setValue, maxValue))
                    {
                        string rangeError = VcpErrorHandler.CreateRangeValidationMessage(feature, setValue, maxValue);
                        
                        if (jsonOutput)
                        {
                            var monitorRef = new MonitorReference(monitor.Index, monitor.Name, monitor.DeviceName, monitor.IsPrimary);
                            var error = new ErrorResponse(false, rangeError, monitorRef);
                            Console.WriteLine(JsonSerializer.Serialize(error, JsonContext.Default.ErrorResponse));
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[red]Error:[/] {rangeError}");
                        }

                        // Cleanup
                        foreach (var m in monitors)
                        {
                            m.Dispose();
                        }

                        return 1;
                    }
                }
            }
            else
            {
                string readError;
                
                if (VcpErrorHandler.IsTimeoutError(errorCode))
                {
                    readError = VcpErrorHandler.CreateTimeoutMessage(monitor, feature, "read");
                }
                else if (VcpErrorHandler.IsUnsupportedFeatureError(errorCode))
                {
                    readError = VcpErrorHandler.CreateUnsupportedFeatureMessage(monitor, feature);
                }
                else if (errorCode == 0x00000006) // ERROR_INVALID_HANDLE
                {
                    readError = VcpErrorHandler.CreateCommunicationFailureMessage(monitor);
                }
                else
                {
                    readError = $"Failed to read current {feature.Name} from monitor '{monitor.Name}' to validate range. {VcpErrorHandler.CreateReadFailureMessage(monitor, feature)}";
                }
                
                if (jsonOutput)
                {
                    var monitorRef = new MonitorReference(monitor.Index, monitor.Name, monitor.DeviceName, monitor.IsPrimary);
                    var error = new ErrorResponse(false, readError, monitorRef);
                    Console.WriteLine(JsonSerializer.Serialize(error, JsonContext.Default.ErrorResponse));
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] {readError}");
                }

                // Cleanup
                foreach (var m in monitors)
                {
                    m.Dispose();
                }

                return 1;
            }
        }

        bool success = false;
        string? errorMsg = null;

        if (!jsonOutput)
        {
            string displayValue = percentageValue.HasValue ? $"{percentageValue}%" : setValue.ToString();
            AnsiConsole.Status()
                .Start($"Setting {monitor.Name} {feature.Name} to {displayValue}...", ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);

                    if (!monitor.TrySetVcpFeature(feature!.Code, setValue, out int errorCode))
                    {
                        if (VcpErrorHandler.IsTimeoutError(errorCode))
                        {
                            errorMsg = VcpErrorHandler.CreateTimeoutMessage(monitor, feature, "write");
                        }
                        else if (VcpErrorHandler.IsUnsupportedFeatureError(errorCode))
                        {
                            errorMsg = VcpErrorHandler.CreateUnsupportedFeatureMessage(monitor, feature);
                        }
                        else if (errorCode == 0x00000006) // ERROR_INVALID_HANDLE
                        {
                            errorMsg = VcpErrorHandler.CreateCommunicationFailureMessage(monitor);
                        }
                        else
                        {
                            errorMsg = VcpErrorHandler.CreateWriteFailureMessage(monitor, feature, setValue);
                        }
                    }
                    else
                    {
                        success = true;
                    }

                    if (success)
                    {
                        // Give the monitor a moment to apply the change
                        Thread.Sleep(500);
                    }
                });
        }
        else
        {
            if (!monitor.TrySetVcpFeature(feature!.Code, setValue, out int errorCode))
            {
                if (VcpErrorHandler.IsTimeoutError(errorCode))
                {
                    errorMsg = VcpErrorHandler.CreateTimeoutMessage(monitor, feature, "write");
                }
                else if (VcpErrorHandler.IsUnsupportedFeatureError(errorCode))
                {
                    errorMsg = VcpErrorHandler.CreateUnsupportedFeatureMessage(monitor, feature);
                }
                else if (errorCode == 0x00000006) // ERROR_INVALID_HANDLE
                {
                    errorMsg = VcpErrorHandler.CreateCommunicationFailureMessage(monitor);
                }
                else
                {
                    errorMsg = VcpErrorHandler.CreateWriteFailureMessage(monitor, feature, setValue);
                }
            }
            else
            {
                success = true;
                Thread.Sleep(500);
            }
        }

        if (!success)
        {
            if (jsonOutput)
            {
                var monitorRef =
                    new MonitorReference(monitor.Index, monitor.Name, monitor.DeviceName, monitor.IsPrimary);
                var error = new ErrorResponse(false, errorMsg!, monitorRef);
                Console.WriteLine(JsonSerializer.Serialize(error, JsonContext.Default.ErrorResponse));
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {errorMsg}");
            }

            // Cleanup
            foreach (var m in monitors)
            {
                m.Dispose();
            }

            return 1;
        }

        if (jsonOutput)
        {
            var monitorRef = new MonitorReference(monitor.Index, monitor.Name, monitor.DeviceName, monitor.IsPrimary);
            
            // Use generic VCP response for all features
            var result = new SetVcpResponse(true, monitorRef, feature!.Name, setValue, percentageValue);
            Console.WriteLine(JsonSerializer.Serialize(result, JsonContext.Default.SetVcpResponse));
        }
        else
        {
            if (feature!.Code == InputSource.VcpInputSource)
            {
                // Display input with name resolution
                AnsiConsole.MarkupLine($"[green]✓[/] Successfully set {monitor.Name} {feature.Name} to {InputSource.GetName(setValue)}");
            }
            else if (percentageValue.HasValue)
            {
                // Display percentage for brightness/contrast
                AnsiConsole.MarkupLine($"[green]✓[/] Successfully set {monitor.Name} {feature.Name} to {percentageValue}%");
            }
            else
            {
                // Display raw value for unknown VCP codes
                AnsiConsole.MarkupLine($"[green]✓[/] Successfully set {monitor.Name} {feature.Name} to {setValue}");
            }
        }

        // Cleanup
        foreach (var m in monitors)
        {
            m.Dispose();
        }

        return 0;
    }

    private static int HandleVcpScan(List<DDCSwitch.Monitor> monitors, bool jsonOutput)
    {
        if (jsonOutput)
        {
            // JSON output for VCP scan
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
        else
        {
            // Table output for VCP scan
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
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error scanning monitor {monitor.Index} ({monitor.Name}): {ex.Message}[/]");
                }
            }
        }

        // Cleanup
        foreach (var monitor in monitors)
        {
            monitor.Dispose();
        }

        return 0;
    }

    private static int HandleVcpScanForMonitor(string monitorIdentifier, bool jsonOutput)
    {
        List<DDCSwitch.Monitor> monitors;

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
                AnsiConsole.MarkupLine("[red]Error:[/] No DDC/CI capable monitors found.");
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
                AnsiConsole.MarkupLine($"[red]Error:[/] Monitor '{monitorIdentifier}' not found.");
                AnsiConsole.MarkupLine("Use [yellow]DDCSwitch list[/] to see available monitors.");
            }

            // Cleanup
            foreach (var m in monitors)
            {
                m.Dispose();
            }

            return 1;
        }

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
                // JSON output for VCP scan
                var scanResult = new VcpScanResponse(true, monitorRef, supportedFeatures);
                Console.WriteLine(JsonSerializer.Serialize(scanResult, JsonContext.Default.VcpScanResponse));
            }
            else
            {
                // Table output for VCP scan - consistent with verbose listing format
                AnsiConsole.MarkupLine($"[bold blue]Monitor {monitor.Index}: {monitor.Name}[/] ({monitor.DeviceName})");
                
                if (supportedFeatures.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]  No supported VCP features found[/]");
                }
                else
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
                AnsiConsole.MarkupLine($"[red]Error scanning monitor {monitor.Index} ({monitor.Name}): {ex.Message}[/]");
            }

            // Cleanup
            foreach (var m in monitors)
            {
                m.Dispose();
            }

            return 1;
        }

        // Cleanup
        foreach (var m in monitors)
        {
            m.Dispose();
        }

        return 0;
    }
}