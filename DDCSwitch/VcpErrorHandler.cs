using System.Runtime.InteropServices;

namespace DDCSwitch;

/// <summary>
/// Provides enhanced error handling for VCP operations with specific error messages and suggestions
/// </summary>
public static class VcpErrorHandler
{
    /// <summary>
    /// Creates a detailed error message for VCP read failures
    /// </summary>
    /// <param name="monitor">The monitor that failed</param>
    /// <param name="feature">The VCP feature that failed</param>
    /// <returns>Detailed error message with suggestions</returns>
    public static string CreateReadFailureMessage(Monitor monitor, VcpFeature feature)
    {
        var baseMessage = $"Failed to read {feature.Name} from monitor '{monitor.Name}'";
        var suggestions = GetReadFailureSuggestions(feature);
        
        return $"{baseMessage}. {suggestions}";
    }

    /// <summary>
    /// Creates a detailed error message for VCP write failures
    /// </summary>
    /// <param name="monitor">The monitor that failed</param>
    /// <param name="feature">The VCP feature that failed</param>
    /// <param name="attemptedValue">The value that was attempted to be set</param>
    /// <returns>Detailed error message with suggestions</returns>
    public static string CreateWriteFailureMessage(Monitor monitor, VcpFeature feature, uint attemptedValue)
    {
        var baseMessage = $"Failed to set {feature.Name} to {attemptedValue} on monitor '{monitor.Name}'";
        var suggestions = GetWriteFailureSuggestions(feature, attemptedValue);
        
        return $"{baseMessage}. {suggestions}";
    }

    /// <summary>
    /// Creates a detailed error message for unsupported VCP features
    /// </summary>
    /// <param name="monitor">The monitor that doesn't support the feature</param>
    /// <param name="feature">The unsupported VCP feature</param>
    /// <returns>Detailed error message with alternatives</returns>
    public static string CreateUnsupportedFeatureMessage(Monitor monitor, VcpFeature feature)
    {
        var baseMessage = $"Monitor '{monitor.Name}' doesn't support {feature.Name} control (VCP 0x{feature.Code:X2})";
        var alternatives = GetFeatureAlternatives(feature);
        
        return $"{baseMessage}. {alternatives}";
    }

    /// <summary>
    /// Creates a detailed error message for VCP communication timeouts
    /// </summary>
    /// <param name="monitor">The monitor that timed out</param>
    /// <param name="feature">The VCP feature that timed out</param>
    /// <param name="operation">The operation that timed out (read/write)</param>
    /// <returns>Detailed error message with troubleshooting steps</returns>
    public static string CreateTimeoutMessage(Monitor monitor, VcpFeature feature, string operation)
    {
        var baseMessage = $"Timeout while trying to {operation} {feature.Name} on monitor '{monitor.Name}'";
        var troubleshooting = GetTimeoutTroubleshooting();
        
        return $"{baseMessage}. {troubleshooting}";
    }

    /// <summary>
    /// Creates a detailed error message for DDC/CI communication failures
    /// </summary>
    /// <param name="monitor">The monitor with communication issues</param>
    /// <returns>Detailed error message with troubleshooting steps</returns>
    public static string CreateCommunicationFailureMessage(Monitor monitor)
    {
        var baseMessage = $"DDC/CI communication failed with monitor '{monitor.Name}'";
        var troubleshooting = GetCommunicationTroubleshooting();
        
        return $"{baseMessage}. {troubleshooting}";
    }

    /// <summary>
    /// Creates a detailed error message for value range validation failures
    /// </summary>
    /// <param name="feature">The VCP feature</param>
    /// <param name="attemptedValue">The invalid value</param>
    /// <param name="maxValue">The maximum allowed value</param>
    /// <param name="isPercentage">Whether the value is a percentage</param>
    /// <returns>Detailed error message with valid range information</returns>
    public static string CreateRangeValidationMessage(VcpFeature feature, uint attemptedValue, uint maxValue, bool isPercentage = false)
    {
        if (isPercentage)
        {
            return $"{feature.Name} value {attemptedValue}% is out of range. Valid range: 0-100%";
        }
        
        var baseMessage = $"{feature.Name} value {attemptedValue} is out of range for this monitor";
        var validRange = $"Valid range: 0-{maxValue}";
        var suggestion = GetRangeValidationSuggestion(feature, maxValue);
        
        return $"{baseMessage}. {validRange}. {suggestion}";
    }

    /// <summary>
    /// Determines if a VCP operation failure is likely due to a timeout
    /// </summary>
    /// <param name="lastError">The last Win32 error code</param>
    /// <returns>True if the error indicates a timeout</returns>
    public static bool IsTimeoutError(int lastError)
    {
        // Common timeout-related error codes
        return lastError switch
        {
            0x00000102 => true, // ERROR_TIMEOUT
            0x00000121 => true, // ERROR_SEM_TIMEOUT  
            0x000005B4 => true, // ERROR_TIMEOUT (alternative)
            0x00000079 => true, // ERROR_SEM_TIMEOUT (alternative)
            _ => false
        };
    }

    /// <summary>
    /// Determines if a VCP operation failure is likely due to unsupported feature
    /// </summary>
    /// <param name="lastError">The last Win32 error code</param>
    /// <returns>True if the error indicates unsupported feature</returns>
    public static bool IsUnsupportedFeatureError(int lastError)
    {
        // Common unsupported feature error codes
        return lastError switch
        {
            0x00000001 => true, // ERROR_INVALID_FUNCTION
            0x00000057 => true, // ERROR_INVALID_PARAMETER
            0x0000007A => true, // ERROR_INSUFFICIENT_BUFFER
            0x00000032 => true, // ERROR_NOT_SUPPORTED
            _ => false
        };
    }

    /// <summary>
    /// Gets suggestions for VCP read failures
    /// </summary>
    private static string GetReadFailureSuggestions(VcpFeature feature)
    {
        var suggestions = new List<string>();
        
        if (feature.Code == VcpFeature.Brightness.Code || feature.Code == VcpFeature.Contrast.Code)
        {
            suggestions.Add("Some monitors require administrator privileges for brightness/contrast control");
            suggestions.Add("Try running as administrator or check if the monitor supports DDC/CI for this feature");
        }
        else if (feature.Code == VcpFeature.InputSource.Code)
        {
            suggestions.Add("Ensure the monitor supports DDC/CI input switching");
            suggestions.Add("Some monitors only support input switching when not in use");
        }
        else
        {
            suggestions.Add($"VCP code 0x{feature.Code:X2} may not be supported by this monitor");
            suggestions.Add("Use 'ddcswitch get <monitor>' to see all supported VCP codes");
        }
        
        suggestions.Add("Check that the monitor is properly connected and powered on");
        
        return string.Join(". ", suggestions);
    }

    /// <summary>
    /// Gets suggestions for VCP write failures
    /// </summary>
    private static string GetWriteFailureSuggestions(VcpFeature feature, uint attemptedValue)
    {
        var suggestions = new List<string>();
        
        if (feature.Code == VcpFeature.Brightness.Code || feature.Code == VcpFeature.Contrast.Code)
        {
            suggestions.Add("Some monitors require administrator privileges for brightness/contrast control");
            suggestions.Add("Ensure the value is within the monitor's supported range (use 'get' command to check current range)");
        }
        else if (feature.Code == VcpFeature.InputSource.Code)
        {
            suggestions.Add("Verify the input source is available on this monitor");
            suggestions.Add("Some monitors only allow input switching when the input is not active");
        }
        else
        {
            suggestions.Add($"VCP code 0x{feature.Code:X2} may not support write operations on this monitor");
            suggestions.Add("Use 'ddcswitch get <monitor>' to see all supported VCP codes");
        }
        
        suggestions.Add("Try running as administrator if permission issues persist");
        
        return string.Join(". ", suggestions);
    }

    /// <summary>
    /// Gets alternative features when a feature is unsupported
    /// </summary>
    private static string GetFeatureAlternatives(VcpFeature feature)
    {
        return feature.Code switch
        {
            0x10 => "Try using your monitor's physical buttons or on-screen display (OSD) to adjust brightness",
            0x12 => "Try using your monitor's physical buttons or on-screen display (OSD) to adjust contrast", 
            0x60 => "Try using your monitor's physical input selection button or check if the monitor supports other input switching methods",
            _ => "Use 'ddcswitch get <monitor>' to see all supported VCP codes"
        };
    }

    /// <summary>
    /// Gets troubleshooting steps for timeout errors
    /// </summary>
    private static string GetTimeoutTroubleshooting()
    {
        var steps = new List<string>
        {
            "The monitor may be busy or slow to respond",
            "Try waiting a moment and running the command again",
            "Check that no other DDC/CI applications are accessing the monitor",
            "Ensure the monitor cable supports DDC/CI communication (some cheap cables don't)",
            "Try power cycling the monitor if the issue persists"
        };
        
        return string.Join(". ", steps);
    }

    /// <summary>
    /// Gets troubleshooting steps for general communication failures
    /// </summary>
    private static string GetCommunicationTroubleshooting()
    {
        var steps = new List<string>
        {
            "Ensure the monitor supports DDC/CI (check monitor documentation)",
            "Verify the video cable supports DDC/CI (HDMI, DisplayPort, DVI-D, or VGA with DDC support)",
            "Try running as administrator - some monitors require elevated privileges",
            "Check if DDC/CI is enabled in the monitor's on-screen display (OSD) settings",
            "Power cycle the monitor and try again"
        };
        
        return string.Join(". ", steps);
    }

    /// <summary>
    /// Gets suggestions for range validation failures
    /// </summary>
    private static string GetRangeValidationSuggestion(VcpFeature feature, uint maxValue)
    {
        if (feature.SupportsPercentage)
        {
            return "For percentage values, use 0-100% format (e.g., '75%')";
        }
        
        return feature.Code switch
        {
            0x60 => "For input sources, use names like 'HDMI1', 'DP1', or hex codes like '0x11'",
            _ => $"Use 'ddcswitch get {feature.Name}' to see the current value and valid range"
        };
    }
}