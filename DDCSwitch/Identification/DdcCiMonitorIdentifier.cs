using System.Text;

namespace DDCSwitch;

/// <summary>
/// Information about monitor identity obtained via DDC/CI
/// </summary>
public record MonitorIdentityInfo(
    string? ControllerManufacturer,
    string? FirmwareLevel,
    string? ControllerVersion,
    string? CapabilitiesString,
    Dictionary<byte, uint> IdentificationVcpValues,
    bool IsFromDdcCi
);

/// <summary>
/// Provides DDC/CI-based monitor identification functionality
/// </summary>
public static class DdcCiMonitorIdentifier
{
    // VCP codes for monitor identification
    private const byte VCP_CONTROLLER_MANUFACTURER = 0xC4;
    private const byte VCP_FIRMWARE_LEVEL = 0xC2;
    private const byte VCP_CONTROLLER_VERSION = 0xC8;
    private const byte VCP_CAPABILITIES_REQUEST = 0xF3;

    /// <summary>
    /// Attempts to get monitor identity information via DDC/CI
    /// </summary>
    /// <param name="monitor">Monitor to query</param>
    /// <returns>Monitor identity information or null if DDC/CI is not available</returns>
    public static MonitorIdentityInfo? GetIdentityViaDdcCi(Monitor monitor)
    {
        if (monitor == null)
        {
            return null;
        }

        var identificationValues = new Dictionary<byte, uint>();
        
        // Try to get controller manufacturer
        string? controllerManufacturer = GetControllerManufacturer(monitor);
        if (controllerManufacturer != null && monitor.TryGetVcpFeature(VCP_CONTROLLER_MANUFACTURER, out uint mfgValue, out _))
        {
            identificationValues[VCP_CONTROLLER_MANUFACTURER] = mfgValue;
        }

        // Try to get firmware level
        string? firmwareLevel = GetFirmwareLevel(monitor);
        if (firmwareLevel != null && monitor.TryGetVcpFeature(VCP_FIRMWARE_LEVEL, out uint fwValue, out _))
        {
            identificationValues[VCP_FIRMWARE_LEVEL] = fwValue;
        }

        // Try to get controller version
        string? controllerVersion = GetControllerVersion(monitor);
        if (controllerVersion != null && monitor.TryGetVcpFeature(VCP_CONTROLLER_VERSION, out uint verValue, out _))
        {
            identificationValues[VCP_CONTROLLER_VERSION] = verValue;
        }

        // Try to get capabilities string
        string? capabilitiesString = GetCapabilitiesString(monitor);

        // If we got any DDC/CI data, return the identity info
        if (controllerManufacturer != null || firmwareLevel != null || 
            controllerVersion != null || capabilitiesString != null)
        {
            return new MonitorIdentityInfo(
                controllerManufacturer,
                firmwareLevel,
                controllerVersion,
                capabilitiesString,
                identificationValues,
                true
            );
        }

        return null;
    }

    /// <summary>
    /// Attempts to retrieve DDC/CI capabilities string from the monitor
    /// </summary>
    /// <param name="monitor">Monitor to query</param>
    /// <returns>Capabilities string or null if not available</returns>
    public static string? GetCapabilitiesString(Monitor monitor)
    {
        if (monitor == null)
            return null;

        try
        {
            // DDC/CI capabilities string retrieval is complex and requires special handling
            // For now, we'll attempt to read the capabilities request VCP code
            // In a full implementation, this would use GetCapabilitiesString DDC/CI command
            
            // Try to read capabilities request VCP code as a fallback
            if (monitor.TryGetVcpFeature(VCP_CAPABILITIES_REQUEST, out uint capValue, out uint maxValue))
            {
                // This is a simplified approach - real capabilities string retrieval
                // would require implementing the full DDC/CI capabilities protocol
                return $"(caps,mccs_ver(2.1),vcp({capValue:X2}))";
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to get controller manufacturer information via VCP code 0xC4
    /// </summary>
    /// <param name="monitor">Monitor to query</param>
    /// <returns>Controller manufacturer string or null if not available</returns>
    public static string? GetControllerManufacturer(Monitor monitor)
    {
        if (monitor == null)
            return null;

        try
        {
            if (monitor.TryGetVcpFeature(VCP_CONTROLLER_MANUFACTURER, out uint value, out _))
            {
                // Convert the value to a manufacturer string
                // The format varies by manufacturer, but often uses ASCII encoding
                return DecodeManufacturerValue(value);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to get firmware level information via VCP code 0xC2
    /// </summary>
    /// <param name="monitor">Monitor to query</param>
    /// <returns>Firmware level string or null if not available</returns>
    public static string? GetFirmwareLevel(Monitor monitor)
    {
        if (monitor == null)
            return null;

        try
        {
            if (monitor.TryGetVcpFeature(VCP_FIRMWARE_LEVEL, out uint value, out _))
            {
                // Firmware level is typically encoded as a version number
                // Format varies by manufacturer but often uses BCD or simple numeric encoding
                return DecodeFirmwareLevel(value);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to get controller version information via VCP code 0xC8
    /// </summary>
    /// <param name="monitor">Monitor to query</param>
    /// <returns>Controller version string or null if not available</returns>
    public static string? GetControllerVersion(Monitor monitor)
    {
        if (monitor == null)
            return null;

        try
        {
            if (monitor.TryGetVcpFeature(VCP_CONTROLLER_VERSION, out uint value, out _))
            {
                // Controller version is typically encoded as a version number
                return DecodeControllerVersion(value);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Decodes manufacturer value from VCP code 0xC4
    /// </summary>
    private static string? DecodeManufacturerValue(uint value)
    {
        if (value == 0)
            return null;

        try
        {
            // Try to decode as ASCII characters (common format)
            var bytes = new List<byte>();
            
            // Extract bytes from the 32-bit value
            for (int i = 0; i < 4; i++)
            {
                byte b = (byte)((value >> (i * 8)) & 0xFF);
                if (b != 0 && b >= 0x20 && b <= 0x7E) // Printable ASCII
                {
                    bytes.Add(b);
                }
            }

            if (bytes.Count > 0)
            {
                bytes.Reverse(); // Most significant byte first
                return Encoding.ASCII.GetString(bytes.ToArray()).Trim();
            }

            // If ASCII decoding fails, return as hex value
            return $"0x{value:X8}";
        }
        catch
        {
            return $"0x{value:X8}";
        }
    }

    /// <summary>
    /// Decodes firmware level from VCP code 0xC2
    /// </summary>
    private static string? DecodeFirmwareLevel(uint value)
    {
        if (value == 0)
            return null;

        try
        {
            // Common formats:
            // - BCD encoding: 0x0123 = version 1.23
            // - Simple numeric: 0x001A = version 26
            // - ASCII encoding: similar to manufacturer

            // Try BCD decoding first (most common)
            if (value <= 0xFFFF)
            {
                uint major = (value >> 8) & 0xFF;
                uint minor = value & 0xFF;
                
                // Check if it looks like BCD
                if ((major & 0xF0) <= 0x90 && (major & 0x0F) <= 0x09 &&
                    (minor & 0xF0) <= 0x90 && (minor & 0x0F) <= 0x09)
                {
                    uint majorBcd = ((major >> 4) * 10) + (major & 0x0F);
                    uint minorBcd = ((minor >> 4) * 10) + (minor & 0x0F);
                    return $"{majorBcd}.{minorBcd:D2}";
                }
                
                // Try simple major.minor format
                if (major > 0 && major <= 99 && minor <= 99)
                {
                    return $"{major}.{minor:D2}";
                }
            }

            // Try ASCII decoding
            var asciiResult = DecodeManufacturerValue(value);
            if (asciiResult != null && !asciiResult.StartsWith("0x"))
            {
                return asciiResult;
            }

            // Fallback to hex representation
            return $"0x{value:X4}";
        }
        catch
        {
            return $"0x{value:X4}";
        }
    }

    /// <summary>
    /// Decodes controller version from VCP code 0xC8
    /// </summary>
    private static string? DecodeControllerVersion(uint value)
    {
        if (value == 0)
            return null;

        try
        {
            // Similar to firmware level but may have different encoding
            // Some monitors use this for hardware revision
            
            if (value <= 0xFFFF)
            {
                uint high = (value >> 8) & 0xFF;
                uint low = value & 0xFF;
                
                // Try simple version format
                if (high > 0 && high <= 99)
                {
                    return $"{high}.{low}";
                }
            }

            // Try ASCII decoding
            var asciiResult = DecodeManufacturerValue(value);
            if (asciiResult != null && !asciiResult.StartsWith("0x"))
            {
                return asciiResult;
            }

            // Fallback to hex representation
            return $"0x{value:X4}";
        }
        catch
        {
            return $"0x{value:X4}";
        }
    }
}