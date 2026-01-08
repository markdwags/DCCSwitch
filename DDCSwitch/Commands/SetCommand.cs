using Spectre.Console;
using System.Text.Json;

namespace DDCSwitch.Commands;

internal static class SetCommand
{
    public static int Execute(string[] args, bool jsonOutput)
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
                ConsoleOutputFormatter.WriteError("Monitor, feature, and value required.");
                AnsiConsole.MarkupLine("Usage: [yellow]ddcswitch set <monitor> <feature> <value>[/]");
            }

            return 1;
        }

        string featureInput = args[2];
        string valueInput = args[3];

        if (!FeatureResolver.TryResolveFeature(featureInput, out VcpFeature? feature))
        {
            return HandleInvalidFeature(featureInput, jsonOutput);
        }

        var (setValue, percentageValue, validationError) = ParseAndValidateValue(feature!, valueInput);

        if (validationError != null)
        {
            return HandleValidationError(validationError, jsonOutput);
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

        int result = SetFeatureValue(monitor, feature!, setValue, percentageValue, jsonOutput);

        // Cleanup
        foreach (var m in monitors)
        {
            m.Dispose();
        }

        return result;
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

    private static (uint setValue, uint? percentageValue, string? validationError) ParseAndValidateValue(VcpFeature feature, string valueInput)
    {
        uint setValue = 0;
        uint? percentageValue = null;
        string? validationError = null;
        
        if (feature.Code == InputSource.VcpInputSource)
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

        return (setValue, percentageValue, validationError);
    }

    private static int HandleValidationError(string validationError, bool jsonOutput)
    {
        if (jsonOutput)
        {
            var error = new ErrorResponse(false, validationError);
            Console.WriteLine(JsonSerializer.Serialize(error, JsonContext.Default.ErrorResponse));
        }
        else
        {
            ConsoleOutputFormatter.WriteError(validationError);
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

    private static int SetFeatureValue(Monitor monitor, VcpFeature feature, uint setValue, uint? percentageValue, bool jsonOutput)
    {
        // If we have a percentage value or need to validate raw value range, get the monitor's max value
        if (percentageValue.HasValue || (feature.Code != InputSource.VcpInputSource && !percentageValue.HasValue))
        {
            if (monitor.TryGetVcpFeature(feature.Code, out _, out uint maxValue, out int errorCode))
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
                            ConsoleOutputFormatter.WriteError(rangeError);
                        }

                        return 1;
                    }
                }
            }
            else
            {
                return HandleReadErrorDuringValidation(monitor, feature, errorCode, jsonOutput);
            }
        }

        return WriteFeatureValue(monitor, feature, setValue, percentageValue, jsonOutput);
    }

    private static int HandleReadErrorDuringValidation(Monitor monitor, VcpFeature feature, int errorCode, bool jsonOutput)
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
            ConsoleOutputFormatter.WriteError(readError);
        }

        return 1;
    }

    private static int WriteFeatureValue(Monitor monitor, VcpFeature feature, uint setValue, uint? percentageValue, bool jsonOutput)
    {
        bool success = false;
        string? errorMsg = null;

        if (!jsonOutput)
        {
            string displayValue = percentageValue.HasValue ? $"{percentageValue}%" : setValue.ToString();
            AnsiConsole.Status()
                .Start($"Setting {monitor.Name} {feature.Name} to {displayValue}...", ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    ctx.SpinnerStyle(Style.Parse("cyan"));

                    if (!monitor.TrySetVcpFeature(feature.Code, setValue, out int errorCode))
                    {
                        errorMsg = GetWriteErrorMessage(monitor, feature, setValue, errorCode);
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
            if (!monitor.TrySetVcpFeature(feature.Code, setValue, out int errorCode))
            {
                errorMsg = GetWriteErrorMessage(monitor, feature, setValue, errorCode);
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
                var monitorRef = new MonitorReference(monitor.Index, monitor.Name, monitor.DeviceName, monitor.IsPrimary);
                var error = new ErrorResponse(false, errorMsg!, monitorRef);
                Console.WriteLine(JsonSerializer.Serialize(error, JsonContext.Default.ErrorResponse));
            }
            else
            {
                ConsoleOutputFormatter.WriteError(errorMsg!);
            }

            return 1;
        }

        OutputSuccess(monitor, feature, setValue, percentageValue, jsonOutput);
        return 0;
    }

    private static string GetWriteErrorMessage(Monitor monitor, VcpFeature feature, uint setValue, int errorCode)
    {
        if (VcpErrorHandler.IsTimeoutError(errorCode))
        {
            return VcpErrorHandler.CreateTimeoutMessage(monitor, feature, "write");
        }
        else if (VcpErrorHandler.IsUnsupportedFeatureError(errorCode))
        {
            return VcpErrorHandler.CreateUnsupportedFeatureMessage(monitor, feature);
        }
        else if (errorCode == 0x00000006) // ERROR_INVALID_HANDLE
        {
            return VcpErrorHandler.CreateCommunicationFailureMessage(monitor);
        }
        else
        {
            return VcpErrorHandler.CreateWriteFailureMessage(monitor, feature, setValue);
        }
    }

    private static void OutputSuccess(Monitor monitor, VcpFeature feature, uint setValue, uint? percentageValue, bool jsonOutput)
    {
        if (jsonOutput)
        {
            var monitorRef = new MonitorReference(monitor.Index, monitor.Name, monitor.DeviceName, monitor.IsPrimary);
            
            // Use generic VCP response for all features
            var result = new SetVcpResponse(true, monitorRef, feature.Name, setValue, percentageValue);
            Console.WriteLine(JsonSerializer.Serialize(result, JsonContext.Default.SetVcpResponse));
        }
        else
        {
            string displayValue;
            if (feature.Code == InputSource.VcpInputSource)
            {
                // Display input with name resolution
                displayValue = $"[cyan]{InputSource.GetName(setValue)}[/]";
            }
            else if (percentageValue.HasValue)
            {
                // Display percentage for brightness/contrast
                displayValue = $"[green]{percentageValue}%[/]";
            }
            else
            {
                // Display raw value for unknown VCP codes
                displayValue = $"[green]{setValue}[/]";
            }

            var successPanel = new Panel(
                $"[bold cyan]Monitor:[/] {monitor.Name}\n" +
                $"[bold yellow]Feature:[/] {feature.Name}\n" +
                $"[bold green]New Value:[/] {displayValue}")
            {
                Header = new PanelHeader("[bold green]>> Successfully Applied[/]", Justify.Left),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Green)
            };
            
            AnsiConsole.Write(successPanel);
        }
    }
}

