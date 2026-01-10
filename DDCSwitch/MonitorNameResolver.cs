namespace DDCSwitch;

/// <summary>
/// Resolves monitor names with DDC/CI priority over EDID registry data
/// </summary>
public static class MonitorNameResolver
{
    /// <summary>
    /// Resolves the best available monitor name using DDC/CI first, then EDID, then Windows fallback
    /// </summary>
    /// <param name="monitor">Monitor instance</param>
    /// <param name="ddcCiIdentity">DDC/CI identity information (if available)</param>
    /// <param name="edidInfo">EDID information (if available)</param>
    /// <returns>Resolved monitor name</returns>
    public static string ResolveMonitorName(Monitor monitor, MonitorIdentityInfo? ddcCiIdentity, ParsedEdidInfo? edidInfo)
    {
        // Priority 1: DDC/CI Controller Manufacturer + Capabilities
        if (HasNameFromDdcCi(ddcCiIdentity))
        {
            var ddcCiName = BuildDdcCiName(ddcCiIdentity!);
            if (!string.IsNullOrEmpty(ddcCiName))
            {
                return ddcCiName;
            }
        }

        // Priority 2: EDID Manufacturer + Model
        if (HasNameFromEdid(edidInfo))
        {
            var edidName = BuildEdidName(edidInfo!);
            if (!string.IsNullOrEmpty(edidName))
            {
                return edidName;
            }
        }

        // Priority 3: Windows Physical Monitor Description (fallback)
        return GetFallbackName(monitor);
    }

    /// <summary>
    /// Checks if DDC/CI provides sufficient information for naming
    /// </summary>
    /// <param name="ddcCiIdentity">DDC/CI identity information</param>
    /// <returns>True if DDC/CI can provide a name</returns>
    public static bool HasNameFromDdcCi(MonitorIdentityInfo? ddcCiIdentity)
    {
        return ddcCiIdentity != null && 
               ddcCiIdentity.IsFromDdcCi && 
               (!string.IsNullOrEmpty(ddcCiIdentity.ControllerManufacturer) || 
                !string.IsNullOrEmpty(ddcCiIdentity.CapabilitiesString));
    }

    /// <summary>
    /// Checks if EDID provides sufficient information for naming
    /// </summary>
    /// <param name="edidInfo">EDID information</param>
    /// <returns>True if EDID can provide a name</returns>
    public static bool HasNameFromEdid(ParsedEdidInfo? edidInfo)
    {
        return edidInfo != null && 
               (!string.IsNullOrEmpty(edidInfo.ManufacturerName) || 
                !string.IsNullOrEmpty(edidInfo.ManufacturerCode));
    }

    /// <summary>
    /// Gets fallback name from Windows monitor description
    /// </summary>
    /// <param name="monitor">Monitor instance</param>
    /// <returns>Fallback name</returns>
    public static string GetFallbackName(Monitor monitor)
    {
        if (!string.IsNullOrEmpty(monitor.Name) && monitor.Name != "Generic PnP Monitor")
        {
            return monitor.Name;
        }

        return $"Monitor {monitor.Index}";
    }

    /// <summary>
    /// Builds a monitor name from DDC/CI information
    /// </summary>
    /// <param name="ddcCiIdentity">DDC/CI identity information</param>
    /// <returns>DDC/CI-derived monitor name</returns>
    private static string BuildDdcCiName(MonitorIdentityInfo ddcCiIdentity)
    {
        var nameParts = new List<string>();

        // Add controller manufacturer if available
        if (!string.IsNullOrEmpty(ddcCiIdentity.ControllerManufacturer))
        {
            nameParts.Add(ddcCiIdentity.ControllerManufacturer);
        }

        // Try to extract model information from capabilities string
        var modelFromCaps = ExtractModelFromCapabilities(ddcCiIdentity.CapabilitiesString);
        if (!string.IsNullOrEmpty(modelFromCaps))
        {
            nameParts.Add(modelFromCaps);
        }

        // Add firmware/version info if available and no model found
        if (nameParts.Count == 1) // Only manufacturer so far
        {
            if (!string.IsNullOrEmpty(ddcCiIdentity.FirmwareLevel))
            {
                nameParts.Add($"FW{ddcCiIdentity.FirmwareLevel}");
            }
            else if (!string.IsNullOrEmpty(ddcCiIdentity.ControllerVersion))
            {
                nameParts.Add($"v{ddcCiIdentity.ControllerVersion}");
            }
        }

        return nameParts.Count > 0 ? string.Join(" ", nameParts) : string.Empty;
    }

    /// <summary>
    /// Builds a monitor name from EDID information
    /// </summary>
    /// <param name="edidInfo">EDID information</param>
    /// <returns>EDID-derived monitor name</returns>
    private static string BuildEdidName(ParsedEdidInfo edidInfo)
    {
        var nameParts = new List<string>();

        // Add manufacturer name (prefer full name over code)
        if (!string.IsNullOrEmpty(edidInfo.ManufacturerName) && 
            edidInfo.ManufacturerName != edidInfo.ManufacturerCode)
        {
            nameParts.Add(edidInfo.ManufacturerName);
        }
        else if (!string.IsNullOrEmpty(edidInfo.ManufacturerCode))
        {
            nameParts.Add(edidInfo.ManufacturerCode);
        }

        // Try to get model name from raw EDID data
        var modelName = EdidParser.ParseModelName(edidInfo.RawData);
        if (!string.IsNullOrEmpty(modelName))
        {
            nameParts.Add(modelName);
        }
        else if (edidInfo.ProductCode != 0)
        {
            // Fallback to product code if no model name
            nameParts.Add($"0x{edidInfo.ProductCode:X4}");
        }

        return nameParts.Count > 0 ? string.Join(" ", nameParts) : string.Empty;
    }

    /// <summary>
    /// Attempts to extract model information from DDC/CI capabilities string
    /// </summary>
    /// <param name="capabilitiesString">DDC/CI capabilities string</param>
    /// <returns>Model name if found, null otherwise</returns>
    private static string? ExtractModelFromCapabilities(string? capabilitiesString)
    {
        if (string.IsNullOrEmpty(capabilitiesString))
            return null;

        try
        {
            // DDC/CI capabilities strings often contain model information
            // Format varies but may include model names or codes
            // Example: "(prot(monitor)type(LCD)model(VG248QE)cmds(01 02 03 07 0C E3 F3)vcp(10 12 14(05 08 0B) 16 18 1A 52 60(11 12) AC AE B2 B6 C6 C8 C9 D6(01 04) DF)mswhql(1))"
            
            // Look for model() tag
            var modelMatch = System.Text.RegularExpressions.Regex.Match(
                capabilitiesString, 
                @"model\(([^)]+)\)", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            if (modelMatch.Success)
            {
                return modelMatch.Groups[1].Value.Trim();
            }

            // Look for type() tag as fallback
            var typeMatch = System.Text.RegularExpressions.Regex.Match(
                capabilitiesString, 
                @"type\(([^)]+)\)", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            if (typeMatch.Success)
            {
                var typeValue = typeMatch.Groups[1].Value.Trim();
                if (typeValue != "LCD" && typeValue != "CRT") // Skip generic types
                {
                    return typeValue;
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}