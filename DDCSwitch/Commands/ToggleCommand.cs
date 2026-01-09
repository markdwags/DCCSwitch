using Spectre.Console;
using System.Text.Json;

namespace DDCSwitch.Commands;

internal static class ToggleCommand
{
    public static int Execute(string[] args, bool jsonOutput)
    {
        // Require exactly 4 arguments: toggle <monitor> <input1> <input2>
        if (args.Length < 4)
        {
            return HandleMissingArguments(args.Length, jsonOutput);
        }

        // Extract arguments
        string monitorArg = args[1];
        string input1Arg = args[2];
        string input2Arg = args[3];

        // Validate input sources and get their values
        if (!InputSource.TryParse(input1Arg, out uint input1Value))
        {
            return HandleInvalidInput(input1Arg, "first", jsonOutput);
        }

        if (!InputSource.TryParse(input2Arg, out uint input2Value))
        {
            return HandleInvalidInput(input2Arg, "second", jsonOutput);
        }

        // Validate that inputs are different
        if (input1Value == input2Value)
        {
            return HandleIdenticalInputs(input1Arg, input2Arg, jsonOutput);
        }

        // Enumerate monitors
        var monitors = MonitorController.EnumerateMonitors();
        
        if (monitors.Count == 0)
        {
            return HandleNoMonitors(jsonOutput);
        }

        // Find the specified monitor
        var monitor = MonitorController.FindMonitor(monitors, monitorArg);
        
        if (monitor == null)
        {
            return HandleMonitorNotFound(monitors, monitorArg, jsonOutput);
        }

        // Execute the toggle operation
        int result = ExecuteToggle(monitor, input1Value, input2Value, input1Arg, input2Arg, jsonOutput);

        // Cleanup resources
        foreach (var m in monitors)
        {
            m.Dispose();
        }

        return result;
    }

    private static int HandleMissingArguments(int actualCount, bool jsonOutput)
    {
        string errorMessage = actualCount switch
        {
            0 => "Command 'toggle' requires monitor and two input sources",
            1 => "Monitor and two input sources required",
            2 => "Two input sources required",
            3 => "Second input source required",
            _ => "Invalid number of arguments"
        };

        if (jsonOutput)
        {
            var error = new ErrorResponse(false, errorMessage);
            Console.WriteLine(JsonSerializer.Serialize(error, JsonContext.Default.ErrorResponse));
        }
        else
        {
            ConsoleOutputFormatter.WriteError(errorMessage);
            AnsiConsole.MarkupLine("Usage: [yellow]ddcswitch toggle <monitor> <input1> <input2>[/]");
        }

        return 1;
    }

    private static int HandleInvalidInput(string invalidInput, string position, bool jsonOutput)
    {
        string errorMessage = $"Invalid {position} input source: '{invalidInput}'";
        string suggestion = "Valid input sources include: HDMI1, HDMI2, DisplayPort1, DisplayPort2, DVI1, DVI2, VGA1, VGA2, or numeric codes (e.g., 0x11, 17)";

        if (jsonOutput)
        {
            var error = new ErrorResponse(false, $"{errorMessage}. {suggestion}");
            Console.WriteLine(JsonSerializer.Serialize(error, JsonContext.Default.ErrorResponse));
        }
        else
        {
            ConsoleOutputFormatter.WriteError(errorMessage);
            AnsiConsole.MarkupLine($"[dim]{suggestion}[/]");
        }

        return 1;
    }

    private static int HandleIdenticalInputs(string input1, string input2, bool jsonOutput)
    {
        string errorMessage = $"Input sources must be different. Both inputs resolve to the same source: '{input1}' and '{input2}'";
        string suggestion = "Please specify two different input sources to toggle between";

        if (jsonOutput)
        {
            var error = new ErrorResponse(false, $"{errorMessage}. {suggestion}");
            Console.WriteLine(JsonSerializer.Serialize(error, JsonContext.Default.ErrorResponse));
        }
        else
        {
            ConsoleOutputFormatter.WriteError(errorMessage);
            AnsiConsole.MarkupLine($"[dim]{suggestion}[/]");
        }

        return 1;
    }

    /// <summary>
    /// Detects the current input source from the monitor using VCP code 0x60
    /// </summary>
    /// <param name="monitor">Monitor to read current input from</param>
    /// <param name="jsonOutput">Whether to format output as JSON</param>
    /// <param name="currentInput">The detected current input value</param>
    /// <returns>0 on success, 1 on failure</returns>
    private static int DetectCurrentInput(Monitor monitor, bool jsonOutput, out uint currentInput)
    {
        currentInput = 0;

        // Try to read current input source using VCP code 0x60
        if (monitor.TryGetVcpFeature(InputSource.VcpInputSource, out currentInput, out uint maxValue, out int errorCode))
        {
            // Successfully detected current input
            return 0;
        }

        // Handle detection failure
        return HandleInputDetectionFailure(monitor, errorCode, jsonOutput);
    }

    private static int HandleInputDetectionFailure(Monitor monitor, int errorCode, bool jsonOutput)
    {
        string errorMessage = errorCode switch
        {
            0x00000006 => "Monitor handle is invalid", // ERROR_INVALID_HANDLE
            0x00000057 => "Monitor does not support input source reading", // ERROR_INVALID_PARAMETER
            0x00000102 => "Monitor communication timeout", // WAIT_TIMEOUT
            _ => $"Failed to read current input source (Error: 0x{errorCode:X8})"
        };

        string fallbackMessage = "Will use fallback behavior and switch to first input source";
        string fullMessage = $"{errorMessage}. {fallbackMessage}";

        if (jsonOutput)
        {
            var monitorRef = new MonitorReference(monitor.Index, monitor.Name, monitor.DeviceName, monitor.IsPrimary);
            var error = new ErrorResponse(false, fullMessage, monitorRef);
            Console.WriteLine(JsonSerializer.Serialize(error, JsonContext.Default.ErrorResponse));
        }
        else
        {
            string escapedMonitorName = monitor.Name.Replace("[", "[[").Replace("]", "]]");
            ConsoleOutputFormatter.WriteError($"Monitor [[{monitor.Index}]] {escapedMonitorName}: {errorMessage}");
            AnsiConsole.MarkupLine($"[yellow]Warning:[/] {fallbackMessage}");
        }

        // Return 2 to indicate a warning condition that allows fallback behavior
        // This is different from return 1 which indicates a hard error
        return 2;
    }

    /// <summary>
    /// Determines which input to switch to based on current input and the two specified inputs
    /// </summary>
    /// <param name="currentInput">The currently active input source</param>
    /// <param name="input1Value">First input source value</param>
    /// <param name="input2Value">Second input source value</param>
    /// <returns>The target input value to switch to</returns>
    private static uint DetermineTargetInput(uint currentInput, uint input1Value, uint input2Value)
    {
        // If current input matches input1, switch to input2
        if (currentInput == input1Value)
        {
            return input2Value;
        }
        
        // If current input matches input2, switch to input1
        if (currentInput == input2Value)
        {
            return input1Value;
        }
        
        // If current input is neither specified input, default to input1
        // This handles the edge case where the current input is something else entirely
        return input1Value;
    }

    private static int HandleNoMonitors(bool jsonOutput)
    {
        string errorMessage = "No DDC/CI capable monitors found";

        if (jsonOutput)
        {
            var error = new ErrorResponse(false, errorMessage);
            Console.WriteLine(JsonSerializer.Serialize(error, JsonContext.Default.ErrorResponse));
        }
        else
        {
            ConsoleOutputFormatter.WriteError(errorMessage);
        }

        return 1;
    }

    private static int HandleMonitorNotFound(List<Monitor> monitors, string monitorId, bool jsonOutput)
    {
        string errorMessage = $"Monitor '{monitorId}' not found";

        if (jsonOutput)
        {
            var error = new ErrorResponse(false, errorMessage);
            Console.WriteLine(JsonSerializer.Serialize(error, JsonContext.Default.ErrorResponse));
        }
        else
        {
            ConsoleOutputFormatter.WriteError(errorMessage);
            AnsiConsole.MarkupLine("Use [yellow]ddcswitch list[/] to see available monitors.");
        }

        // Cleanup
        foreach (var m in monitors)
        {
            m.Dispose();
        }

        return 1;
    }

    private static int ExecuteToggle(Monitor monitor, uint input1Value, uint input2Value, string input1Arg, string input2Arg, bool jsonOutput)
    {
        // Detect current input
        int detectionResult = DetectCurrentInput(monitor, jsonOutput, out uint currentInput);
        bool hasWarning = false;
        string? warningMessage = null;

        // Handle detection failure with fallback behavior
        if (detectionResult == 2) // Warning condition - detection failed but we can continue
        {
            hasWarning = true;
            warningMessage = "Could not detect current input source, switching to first input source";
            currentInput = 0; // Will cause DetermineTargetInput to default to input1
        }
        else if (detectionResult == 1) // Hard error
        {
            return 1;
        }

        // Determine target input
        uint targetInput = DetermineTargetInput(currentInput, input1Value, input2Value);

        // Check if current input is neither of the specified inputs
        if (detectionResult == 0 && currentInput != input1Value && currentInput != input2Value)
        {
            hasWarning = true;
            warningMessage = $"Current input '{InputSource.GetName(currentInput)}' is not one of the specified inputs, switching to '{InputSource.GetName(targetInput)}'";
        }

        // Perform the input switch
        return PerformInputSwitch(monitor, currentInput, targetInput, input1Arg, input2Arg, hasWarning, warningMessage, jsonOutput);
    }

    private static int PerformInputSwitch(Monitor monitor, uint currentInput, uint targetInput, string input1Arg, string input2Arg, bool hasWarning, string? warningMessage, bool jsonOutput)
    {
        bool success = false;
        string? errorMessage = null;

        if (!jsonOutput)
        {
            string targetName = InputSource.GetName(targetInput);
            AnsiConsole.Status()
                .Start($"Switching {monitor.Name} to {targetName}...", ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    ctx.SpinnerStyle(Style.Parse("cyan"));

                    if (!monitor.TrySetVcpFeature(InputSource.VcpInputSource, targetInput, out int errorCode))
                    {
                        errorMessage = GetInputSwitchErrorMessage(monitor, targetInput, errorCode);
                    }
                    else
                    {
                        success = true;
                    }

                    if (success)
                    {
                        // Give the monitor a moment to switch inputs
                        Thread.Sleep(1000);
                    }
                });
        }
        else
        {
            if (!monitor.TrySetVcpFeature(InputSource.VcpInputSource, targetInput, out int errorCode))
            {
                errorMessage = GetInputSwitchErrorMessage(monitor, targetInput, errorCode);
            }
            else
            {
                success = true;
                Thread.Sleep(1000);
            }
        }

        if (!success)
        {
            if (jsonOutput)
            {
                var monitorRef = new MonitorReference(monitor.Index, monitor.Name, monitor.DeviceName, monitor.IsPrimary);
                var error = new ErrorResponse(false, errorMessage!, monitorRef);
                Console.WriteLine(JsonSerializer.Serialize(error, JsonContext.Default.ErrorResponse));
            }
            else
            {
                ConsoleOutputFormatter.WriteError(errorMessage!);
            }

            return 1;
        }

        // Output success
        OutputToggleSuccess(monitor, currentInput, targetInput, hasWarning, warningMessage, jsonOutput);
        return 0;
    }

    private static string GetInputSwitchErrorMessage(Monitor monitor, uint targetInput, int errorCode)
    {
        string targetName = InputSource.GetName(targetInput);
        string escapedMonitorName = monitor.Name.Replace("[", "[[").Replace("]", "]]");
        
        return errorCode switch
        {
            0x00000006 => $"Monitor '{escapedMonitorName}' handle is invalid - DDC/CI communication failed",
            0x00000057 => $"Monitor '{escapedMonitorName}' does not support input source switching",
            0x00000102 => $"Timeout switching monitor '{escapedMonitorName}' to {targetName} - monitor may be unresponsive",
            _ => $"Failed to switch monitor '{escapedMonitorName}' to {targetName} (Error: 0x{errorCode:X8})"
        };
    }

    private static void OutputToggleSuccess(Monitor monitor, uint currentInput, uint targetInput, bool hasWarning, string? warningMessage, bool jsonOutput)
    {
        string fromInputName = InputSource.GetName(currentInput);
        string toInputName = InputSource.GetName(targetInput);

        if (jsonOutput)
        {
            var monitorRef = new MonitorReference(monitor.Index, monitor.Name, monitor.DeviceName, monitor.IsPrimary);
            var result = new ToggleInputResponse(
                Success: true,
                Monitor: monitorRef,
                FromInput: fromInputName,
                ToInput: toInputName,
                FromInputCode: currentInput,
                ToInputCode: targetInput,
                Warning: hasWarning ? warningMessage : null
            );
            Console.WriteLine(JsonSerializer.Serialize(result, JsonContext.Default.ToggleInputResponse));
        }
        else
        {
            var successPanel = new Panel(
                $"[bold cyan]Monitor:[/] {monitor.Name}\n" +
                $"[bold yellow]From:[/] {fromInputName}\n" +
                $"[bold green]To:[/] {toInputName}")
            {
                Header = new PanelHeader("[bold green]>> Input Toggled Successfully[/]", Justify.Left),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Green)
            };
            
            AnsiConsole.Write(successPanel);

            if (hasWarning && warningMessage != null)
            {
                AnsiConsole.MarkupLine($"[yellow]Warning:[/] {warningMessage}");
            }
        }
    }
}