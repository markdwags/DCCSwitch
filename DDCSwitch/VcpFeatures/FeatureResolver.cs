using System.Globalization;

namespace DDCSwitch;

/// <summary>
/// Resolves feature names to VCP codes and handles value conversions
/// </summary>
public static class FeatureResolver
{
    private static readonly Dictionary<string, VcpFeature> FeatureMap = BuildFeatureMap();
    private static readonly Dictionary<byte, VcpFeature> CodeMap = BuildCodeMap();

    /// <summary>
    /// Builds the feature name to VcpFeature mapping including aliases
    /// </summary>
    private static Dictionary<string, VcpFeature> BuildFeatureMap()
    {
        var map = new Dictionary<string, VcpFeature>(capacity: 200, StringComparer.OrdinalIgnoreCase);
        
        foreach (var feature in VcpFeature.AllFeatures)
        {
            // Add primary name
            map[feature.Name] = feature;
            
            // Add aliases
            foreach (var alias in feature.Aliases)
            {
                map[alias] = feature;
            }
        }
        
        return map;
    }

    /// <summary>
    /// Builds the VCP code to VcpFeature mapping
    /// </summary>
    private static Dictionary<byte, VcpFeature> BuildCodeMap()
    {
        var map = new Dictionary<byte, VcpFeature>(capacity: 150);
        
        foreach (var feature in VcpFeature.AllFeatures)
        {
            map[feature.Code] = feature;
        }
        
        return map;
    }

    /// <summary>
    /// Attempts to resolve a feature name or VCP code to a VcpFeature
    /// </summary>
    /// <param name="input">Feature name (brightness, contrast, input) or VCP code (0x10, 0x12, etc.)</param>
    /// <param name="feature">The resolved VcpFeature if successful</param>
    /// <returns>True if the feature was resolved successfully</returns>
    public static bool TryResolveFeature(string input, out VcpFeature? feature)
    {
        feature = null;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        // First try to resolve as a known feature name or alias
        if (FeatureMap.TryGetValue(input.Trim(), out feature))
        {
            return true;
        }

        // Try to parse as a VCP code
        if (TryParseVcpCode(input, out byte vcpCode))
        {
            // Check if we have a predefined feature for this code
            if (CodeMap.TryGetValue(vcpCode, out feature))
            {
                return true;
            }
            
            // Create a generic VCP feature for unknown codes
            feature = new VcpFeature(vcpCode, $"VCP_{vcpCode:X2}", $"Unknown VCP feature 0x{vcpCode:X2}", VcpFeatureType.ReadWrite, VcpFeatureCategory.Miscellaneous, false);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets VCP features by category
    /// </summary>
    /// <param name="category">The category to filter by</param>
    /// <returns>Array of VCP features in the specified category</returns>
    public static VcpFeature[] GetFeaturesByCategory(VcpFeatureCategory category)
    {
        return VcpFeature.AllFeatures.Where(f => f.Category == category).ToArray();
    }

    /// <summary>
    /// Searches for VCP features by partial name matching
    /// </summary>
    /// <param name="partialName">Partial name to search for</param>
    /// <returns>Array of VCP features matching the partial name</returns>
    public static VcpFeature[] SearchFeatures(string partialName)
    {
        if (string.IsNullOrWhiteSpace(partialName))
        {
            return Array.Empty<VcpFeature>();
        }

        var searchTerm = partialName.Trim();
        return VcpFeature.AllFeatures
            .Where(f => f.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                       f.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                       f.Aliases.Any(alias => alias.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
    }

    /// <summary>
    /// Gets a VCP feature by its code
    /// </summary>
    /// <param name="code">VCP code</param>
    /// <returns>VCP feature if found, otherwise a generic feature for the code</returns>
    public static VcpFeature GetFeatureByCode(byte code)
    {
        if (CodeMap.TryGetValue(code, out var feature))
        {
            return feature;
        }
        
        // Return a generic feature for unknown codes
        return new VcpFeature(code, $"VCP_{code:X2}", $"Unknown VCP feature 0x{code:X2}", VcpFeatureType.ReadWrite, VcpFeatureCategory.Miscellaneous, false);
    }

    /// <summary>
    /// Gets all available VCP feature categories
    /// </summary>
    /// <returns>Array of category names</returns>
    public static string[] GetAllCategories()
    {
        return Enum.GetNames<VcpFeatureCategory>();
    }

    /// <summary>
    /// Converts a percentage value (0-100) to raw VCP value based on the maximum value
    /// </summary>
    /// <param name="percentage">Percentage value (0-100)</param>
    /// <param name="maxValue">Maximum raw value supported by the monitor</param>
    /// <returns>Raw VCP value</returns>
    public static uint ConvertPercentageToRaw(uint percentage, uint maxValue)
    {
        if (percentage > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percentage), "Percentage must be between 0 and 100");
        }

        if (maxValue == 0)
        {
            return 0;
        }

        // Convert percentage to raw value with proper rounding
        return (uint)Math.Round((double)percentage * maxValue / 100.0);
    }

    /// <summary>
    /// Converts a raw VCP value to percentage based on the maximum value
    /// </summary>
    /// <param name="rawValue">Raw VCP value</param>
    /// <param name="maxValue">Maximum raw value supported by the monitor</param>
    /// <returns>Percentage value (0-100)</returns>
    public static uint ConvertRawToPercentage(uint rawValue, uint maxValue)
    {
        if (maxValue == 0)
        {
            return 0;
        }

        if (rawValue > maxValue)
        {
            rawValue = maxValue;
        }

        // Convert raw value to percentage with proper rounding
        return (uint)Math.Round((double)rawValue * 100.0 / maxValue);
    }

    /// <summary>
    /// Attempts to parse a VCP code from a string (supports hex format like 0x10 or decimal)
    /// </summary>
    /// <param name="input">Input string containing VCP code</param>
    /// <param name="vcpCode">Parsed VCP code if successful</param>
    /// <returns>True if parsing was successful and VCP code is in valid range (0x00-0xFF)</returns>
    public static bool TryParseVcpCode(string input, out byte vcpCode)
    {
        vcpCode = 0;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        input = input.Trim();

        // Try hex format (0x10, 0X10)
        ReadOnlySpan<char> inputSpan = input.AsSpan();
        if (inputSpan.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ||
            inputSpan.StartsWith("0X", StringComparison.OrdinalIgnoreCase))
        {
            ReadOnlySpan<char> hexPart = inputSpan.Slice(2);
            if (byte.TryParse(hexPart, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out vcpCode))
            {
                // VCP codes are inherently valid for byte range (0x00-0xFF)
                return true;
            }
            return false;
        }

        // Try decimal format - validate range for decimal input
        if (uint.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint decimalValue))
        {
            // Validate VCP code is in valid range (0x00-0xFF)
            if (decimalValue <= 255)
            {
                vcpCode = (byte)decimalValue;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to parse a percentage value from a string (supports % suffix)
    /// </summary>
    /// <param name="input">Input string containing percentage value</param>
    /// <param name="percentage">Parsed percentage value if successful</param>
    /// <returns>True if parsing was successful and value is in valid range (0-100)</returns>
    public static bool TryParsePercentage(string input, out uint percentage)
    {
        percentage = 0;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        input = input.Trim();

        // Remove % suffix if present
        ReadOnlySpan<char> inputSpan = input.AsSpan();
        if (inputSpan.EndsWith("%"))
        {
            inputSpan = inputSpan.Slice(0, inputSpan.Length - 1);
            // Trim trailing whitespace after removing %
            while (inputSpan.Length > 0 && char.IsWhiteSpace(inputSpan[inputSpan.Length - 1]))
            {
                inputSpan = inputSpan.Slice(0, inputSpan.Length - 1);
            }
        }

        if (!uint.TryParse(inputSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out percentage))
        {
            return false;
        }

        // Validate range (0-100%)
        return percentage <= 100;
    }

    /// <summary>
    /// Validates that a raw VCP value is within the monitor's supported range
    /// </summary>
    /// <param name="value">Raw VCP value to validate</param>
    /// <param name="maxValue">Maximum value supported by the monitor for this VCP code</param>
    /// <returns>True if the value is within the valid range (0 to maxValue)</returns>
    public static bool IsValidRawVcpValue(uint value, uint maxValue)
    {
        return value <= maxValue;
    }

    /// <summary>
    /// Validates that a percentage value is in the valid range
    /// </summary>
    /// <param name="percentage">Percentage value to validate</param>
    /// <returns>True if the percentage is between 0 and 100 inclusive</returns>
    public static bool IsValidPercentage(uint percentage)
    {
        return percentage <= 100;
    }

    /// <summary>
    /// Validates that a VCP code is in the valid range
    /// </summary>
    /// <param name="vcpCode">VCP code to validate</param>
    /// <returns>True if the VCP code is in the valid range (0x00-0xFF)</returns>
    public static bool IsValidVcpCode(byte vcpCode)
    {
        // All byte values are valid VCP codes (0x00-0xFF)
        return true;
    }

    /// <summary>
    /// Gets a descriptive error message for invalid percentage values
    /// </summary>
    /// <param name="input">The invalid input that was provided</param>
    /// <returns>Error message describing the validation failure</returns>
    public static string GetPercentageValidationError(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "Percentage value cannot be empty";
        }

        var cleanInput = input.Trim();
        if (cleanInput.EndsWith("%"))
        {
            cleanInput = cleanInput.Substring(0, cleanInput.Length - 1).Trim();
        }

        if (!uint.TryParse(cleanInput, out uint value))
        {
            return $"'{input}' is not a valid percentage value. Expected format: 0-100 or 0%-100%";
        }

        if (value > 100)
        {
            return $"Percentage value {value}% is out of range. Valid range: 0-100%";
        }

        return $"'{input}' is not a valid percentage value";
    }

    /// <summary>
    /// Gets a descriptive error message for invalid VCP codes
    /// </summary>
    /// <param name="input">The invalid input that was provided</param>
    /// <returns>Error message describing the validation failure</returns>
    public static string GetVcpCodeValidationError(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "VCP code cannot be empty";
        }

        var cleanInput = input.Trim();

        if (cleanInput.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return $"'{input}' is not a valid VCP code. Expected hex format: 0x00-0xFF";
        }

        if (uint.TryParse(cleanInput, out uint value) && value > 255)
        {
            return $"VCP code {value} is out of range. Valid range: 0-255 (0x00-0xFF)";
        }

        return $"'{input}' is not a valid VCP code. Expected format: 0-255 or 0x00-0xFF";
    }

    /// <summary>
    /// Gets a descriptive error message for invalid raw VCP values
    /// </summary>
    /// <param name="value">The invalid value that was provided</param>
    /// <param name="maxValue">The maximum value supported by the monitor</param>
    /// <param name="featureName">The name of the VCP feature</param>
    /// <returns>Error message describing the validation failure</returns>
    public static string GetRawValueValidationError(uint value, uint maxValue, string featureName)
    {
        return $"Value {value} is out of range for {featureName}. Valid range: 0-{maxValue}";
    }

    /// <summary>
    /// Gets all known feature names including aliases
    /// </summary>
    /// <returns>Collection of known feature names</returns>
    public static IEnumerable<string> GetKnownFeatureNames()
    {
        return FeatureMap.Keys;
    }

    /// <summary>
    /// Gets all predefined VCP features
    /// </summary>
    /// <returns>Collection of predefined VCP features</returns>
    public static IEnumerable<VcpFeature> GetPredefinedFeatures()
    {
        return VcpFeature.AllFeatures;
    }
}