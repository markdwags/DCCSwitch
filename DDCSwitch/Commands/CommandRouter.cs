using System.Text.Json;

namespace DDCSwitch.Commands;

internal static class CommandRouter
{
    public static int Route(string[] args)
    {
        if (args.Length == 0)
        {
            HelpCommand.ShowUsage();
            return 1;
        }

        // Check for --json flag
        bool jsonOutput = args.Contains("--json", StringComparer.OrdinalIgnoreCase);
        var filteredArgs = args.Where(a => !a.Equals("--json", StringComparison.OrdinalIgnoreCase)).ToArray();

        // Check for --verbose flag
        bool verboseOutput = filteredArgs.Contains("--verbose", StringComparer.OrdinalIgnoreCase);
        filteredArgs = filteredArgs.Where(a => !a.Equals("--verbose", StringComparison.OrdinalIgnoreCase)).ToArray();



        if (filteredArgs.Length == 0)
        {
            HelpCommand.ShowUsage();
            return 1;
        }

        var command = filteredArgs[0].ToLowerInvariant();

        try
        {
            return command switch
            {
                "list" or "ls" => ListCommand.Execute(jsonOutput, verboseOutput),
                "get" => GetCommand.Execute(filteredArgs, jsonOutput),
                "set" => SetCommand.Execute(filteredArgs, jsonOutput),
                "version" or "-v" or "--version" => HelpCommand.ShowVersion(jsonOutput),
                "help" or "-h" or "--help" or "/?" => HelpCommand.ShowUsage(),
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
                ConsoleOutputFormatter.WriteError(ex.Message);
            }

            return 1;
        }
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
            ConsoleOutputFormatter.WriteError($"Unknown command: {command}");
            ConsoleOutputFormatter.WriteInfo("Run DDCSwitch help for usage information.");
        }

        return 1;
    }
}

