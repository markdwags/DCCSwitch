using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace DDCSwitch;

/// <summary>
/// Connection type for monitor hardware
/// </summary>
public enum ConnectionType
{
    Unknown,
    HDMI,
    DisplayPort,
    DVI,
    VGA,
    USBC,
    eDP,
    LVDS
}

/// <summary>
/// Hardware information for a monitor
/// </summary>
public record HardwareInfo(
    string GraphicsDriver,
    ConnectionType ConnectionType,
    string HardwarePath,
    bool IsEmbeddedDisplay,
    DdcCiStatus DdcCiStatus
);

/// <summary>
/// Provides hardware inspection functionality for monitors
/// </summary>
public static class HardwareInspector
{
    /// <summary>
    /// Inspects monitor hardware and returns comprehensive hardware information
    /// </summary>
    /// <param name="monitor">Monitor to inspect</param>
    /// <param name="deviceName">Device name from Windows (e.g., \\.\DISPLAY1)</param>
    /// <returns>Hardware information for the monitor</returns>
    public static HardwareInfo InspectMonitor(Monitor monitor, string deviceName)
    {
        if (monitor == null || string.IsNullOrEmpty(deviceName))
        {
            return new HardwareInfo(
                "Unknown",
                ConnectionType.Unknown,
                "Unknown",
                false,
                DdcCiStatus.Unknown
            );
        }

        try
        {
            // Get graphics driver information
            string graphicsDriver = GetGraphicsDriver(deviceName);

            // Determine connection type
            ConnectionType connectionType = DetermineConnectionType(deviceName);

            // Get hardware path information
            string hardwarePath = GetHardwarePath(deviceName);

            // Check if this is an embedded display
            bool isEmbeddedDisplay = IsEmbeddedDisplay(connectionType, deviceName);

            // Assess DDC/CI responsiveness status
            DdcCiStatus ddcCiStatus = AssessDdcCiStatus(monitor);

            return new HardwareInfo(
                graphicsDriver,
                connectionType,
                hardwarePath,
                isEmbeddedDisplay,
                ddcCiStatus
            );
        }
        catch (Exception)
        {
            // Graceful degradation on any error
            return new HardwareInfo(
                "Unknown",
                ConnectionType.Unknown,
                deviceName,
                false,
                DdcCiStatus.Unknown
            );
        }
    }

    /// <summary>
    /// Determines the connection type for a monitor
    /// </summary>
    /// <param name="deviceName">Device name from Windows</param>
    /// <returns>Connection type</returns>
    public static ConnectionType DetermineConnectionType(string deviceName)
    {
        if (string.IsNullOrEmpty(deviceName))
            return ConnectionType.Unknown;

        try
        {
            // Try to get connection information from WMI
            var connectionType = GetConnectionTypeFromWmi(deviceName);
            if (connectionType != ConnectionType.Unknown)
                return connectionType;

            // Try to get connection information from registry
            connectionType = GetConnectionTypeFromRegistry(deviceName);
            if (connectionType != ConnectionType.Unknown)
                return connectionType;

            // Try to infer from device name patterns
            return InferConnectionTypeFromDeviceName(deviceName);
        }
        catch
        {
            return ConnectionType.Unknown;
        }
    }

    /// <summary>
    /// Gets the graphics driver information for a monitor
    /// </summary>
    /// <param name="deviceName">Device name from Windows</param>
    /// <returns>Graphics driver name and version</returns>
    public static string GetGraphicsDriver(string deviceName)
    {
        if (string.IsNullOrEmpty(deviceName))
            return "Unknown";

        try
        {
            // Try to get driver information from WMI
            var driverInfo = GetDriverInfoFromWmi(deviceName);
            if (!string.IsNullOrEmpty(driverInfo))
                return driverInfo;

            // Try to get driver information from registry
            driverInfo = GetDriverInfoFromRegistry(deviceName);
            if (!string.IsNullOrEmpty(driverInfo))
                return driverInfo;

            return "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    /// <summary>
    /// Gets Windows hardware path information for a monitor
    /// </summary>
    /// <param name="deviceName">Device name from Windows</param>
    /// <returns>Hardware path string</returns>
    private static string GetHardwarePath(string deviceName)
    {
        if (string.IsNullOrEmpty(deviceName))
            return "Unknown";

        try
        {
            // Try to get hardware path from WMI
            var hardwarePath = GetHardwarePathFromWmi(deviceName);
            if (!string.IsNullOrEmpty(hardwarePath))
                return hardwarePath;

            // Fallback to device name
            return deviceName;
        }
        catch
        {
            return deviceName;
        }
    }

    /// <summary>
    /// Determines if the display is embedded (laptop screen)
    /// </summary>
    /// <param name="connectionType">Connection type</param>
    /// <param name="deviceName">Device name</param>
    /// <returns>True if embedded display</returns>
    private static bool IsEmbeddedDisplay(ConnectionType connectionType, string deviceName)
    {
        // eDP and LVDS are typically embedded displays
        if (connectionType == ConnectionType.eDP || connectionType == ConnectionType.LVDS)
            return true;

        try
        {
            // Check for laptop indicators in WMI
            return CheckForEmbeddedDisplayInWmi(deviceName);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Assesses DDC/CI responsiveness status for the monitor
    /// </summary>
    /// <param name="monitor">Monitor to assess</param>
    /// <returns>DDC/CI status</returns>
    private static DdcCiStatus AssessDdcCiStatus(Monitor monitor)
    {
        if (monitor == null)
            return DdcCiStatus.Unknown;

        try
        {
            // Use existing VCP analyzer to determine DDC/CI status
            return VcpAnalyzer.TestDdcCiComprehensive(monitor);
        }
        catch
        {
            return DdcCiStatus.Unknown;
        }
    }

    /// <summary>
    /// Gets connection type from WMI
    /// </summary>
    private static ConnectionType GetConnectionTypeFromWmi(string deviceName)
    {
        // WMI is not compatible with NativeAOT, use registry-based approach instead
        return GetConnectionTypeFromRegistry(deviceName);
    }

    /// <summary>
    /// Gets connection type from registry
    /// </summary>
    private static ConnectionType GetConnectionTypeFromRegistry(string deviceName)
    {
        try
        {
            // Look in display registry for connection information
            const string displayKey = @"SYSTEM\CurrentControlSet\Enum\DISPLAY";
            using var displayRoot = Registry.LocalMachine.OpenSubKey(displayKey);
            if (displayRoot == null) return ConnectionType.Unknown;

            foreach (string mfgKey in displayRoot.GetSubKeyNames())
            {
                using var mfgSubKey = displayRoot.OpenSubKey(mfgKey);
                if (mfgSubKey == null) continue;

                foreach (string instanceKey in mfgSubKey.GetSubKeyNames())
                {
                    using var instanceSubKey = mfgSubKey.OpenSubKey(instanceKey);
                    if (instanceSubKey == null) continue;

                    // Check hardware ID for connection type indicators
                    var hardwareId = instanceSubKey.GetValue("HardwareID") as string[];
                    if (hardwareId != null)
                    {
                        foreach (var id in hardwareId)
                        {
                            var idUpper = id.ToUpperInvariant();
                            
                            if (idUpper.Contains("HDMI"))
                                return ConnectionType.HDMI;
                            if (idUpper.Contains("DISPLAYPORT") || idUpper.Contains("DP"))
                                return ConnectionType.DisplayPort;
                            if (idUpper.Contains("DVI"))
                                return ConnectionType.DVI;
                            if (idUpper.Contains("VGA"))
                                return ConnectionType.VGA;
                            if (idUpper.Contains("EDP"))
                                return ConnectionType.eDP;
                            if (idUpper.Contains("LVDS"))
                                return ConnectionType.LVDS;
                        }
                    }
                }
            }
        }
        catch
        {
            // Registry access error
        }

        return ConnectionType.Unknown;
    }

    /// <summary>
    /// Infers connection type from device name patterns
    /// </summary>
    private static ConnectionType InferConnectionTypeFromDeviceName(string deviceName)
    {
        if (string.IsNullOrEmpty(deviceName))
            return ConnectionType.Unknown;

        var nameUpper = deviceName.ToUpperInvariant();

        // Check for common patterns in device names
        if (nameUpper.Contains("HDMI"))
            return ConnectionType.HDMI;
        if (nameUpper.Contains("DP") || nameUpper.Contains("DISPLAYPORT"))
            return ConnectionType.DisplayPort;
        if (nameUpper.Contains("DVI"))
            return ConnectionType.DVI;
        if (nameUpper.Contains("VGA"))
            return ConnectionType.VGA;
        if (nameUpper.Contains("EDP"))
            return ConnectionType.eDP;
        if (nameUpper.Contains("LVDS"))
            return ConnectionType.LVDS;

        return ConnectionType.Unknown;
    }

    /// <summary>
    /// Gets driver information from WMI
    /// </summary>
    private static string GetDriverInfoFromWmi(string deviceName)
    {
        // WMI is not compatible with NativeAOT, use registry-based approach instead
        return GetDriverInfoFromRegistry(deviceName);
    }

    /// <summary>
    /// Gets driver information from registry
    /// </summary>
    private static string GetDriverInfoFromRegistry(string deviceName)
    {
        try
        {
            // Look for video controller information in registry
            const string videoKey = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";
            using var videoRoot = Registry.LocalMachine.OpenSubKey(videoKey);
            if (videoRoot == null) return string.Empty;

            foreach (string subKeyName in videoRoot.GetSubKeyNames())
            {
                if (subKeyName == "Properties") continue;

                using var subKey = videoRoot.OpenSubKey(subKeyName);
                if (subKey == null) continue;

                var driverDesc = subKey.GetValue("DriverDesc")?.ToString();
                var driverVersion = subKey.GetValue("DriverVersion")?.ToString();

                if (!string.IsNullOrEmpty(driverDesc))
                {
                    var result = driverDesc;
                    if (!string.IsNullOrEmpty(driverVersion))
                    {
                        result += $" (v{driverVersion})";
                    }
                    return result;
                }
            }
        }
        catch
        {
            // Registry access error
        }

        return string.Empty;
    }

    /// <summary>
    /// Gets hardware path from registry
    /// </summary>
    private static string GetHardwarePathFromRegistry(string deviceName)
    {
        try
        {
            // Look in display registry for hardware path information
            const string displayKey = @"SYSTEM\CurrentControlSet\Enum\DISPLAY";
            using var displayRoot = Registry.LocalMachine.OpenSubKey(displayKey);
            if (displayRoot == null) return string.Empty;

            foreach (string mfgKey in displayRoot.GetSubKeyNames())
            {
                using var mfgSubKey = displayRoot.OpenSubKey(mfgKey);
                if (mfgSubKey == null) continue;

                foreach (string instanceKey in mfgSubKey.GetSubKeyNames())
                {
                    using var instanceSubKey = mfgSubKey.OpenSubKey(instanceKey);
                    if (instanceSubKey == null) continue;

                    // Get hardware ID as the hardware path
                    var hardwareId = instanceSubKey.GetValue("HardwareID") as string[];
                    if (hardwareId != null && hardwareId.Length > 0)
                    {
                        return hardwareId[0];
                    }

                    // Fallback to device instance path
                    var deviceInstancePath = $@"DISPLAY\{mfgKey}\{instanceKey}";
                    return deviceInstancePath;
                }
            }
        }
        catch
        {
            // Registry access error
        }

        return string.Empty;
    }

    /// <summary>
    /// Checks for embedded display indicators in registry
    /// </summary>
    private static bool CheckForEmbeddedDisplayInRegistry(string deviceName)
    {
        try
        {
            // Check for laptop indicators in system information
            const string systemKey = @"HARDWARE\DESCRIPTION\System";
            using var systemRoot = Registry.LocalMachine.OpenSubKey(systemKey);
            if (systemRoot != null)
            {
                var systemBiosVersion = systemRoot.GetValue("SystemBiosVersion") as string[];
                if (systemBiosVersion != null)
                {
                    foreach (var version in systemBiosVersion)
                    {
                        var versionUpper = version.ToUpperInvariant();
                        if (versionUpper.Contains("LAPTOP") || versionUpper.Contains("PORTABLE") || 
                            versionUpper.Contains("NOTEBOOK") || versionUpper.Contains("MOBILE"))
                        {
                            return true;
                        }
                    }
                }
            }

            // Check for battery presence in registry (indicates laptop)
            const string batteryKey = @"SYSTEM\CurrentControlSet\Services\battery";
            using var batteryRoot = Registry.LocalMachine.OpenSubKey(batteryKey);
            if (batteryRoot != null)
            {
                return true;
            }

            // Check for ACPI battery devices
            const string acpiKey = @"SYSTEM\CurrentControlSet\Enum\ACPI";
            using var acpiRoot = Registry.LocalMachine.OpenSubKey(acpiKey);
            if (acpiRoot != null)
            {
                foreach (string deviceKey in acpiRoot.GetSubKeyNames())
                {
                    if (deviceKey.StartsWith("PNP0C0A", StringComparison.OrdinalIgnoreCase)) // Battery device
                    {
                        return true;
                    }
                }
            }
        }
        catch
        {
            // Registry access error
        }

        return false;
    }

    /// <summary>
    /// Gets hardware path from WMI
    /// </summary>
    private static string GetHardwarePathFromWmi(string deviceName)
    {
        // WMI is not compatible with NativeAOT, use registry-based approach instead
        return GetHardwarePathFromRegistry(deviceName);
    }

    /// <summary>
    /// Checks for embedded display indicators in WMI
    /// </summary>
    private static bool CheckForEmbeddedDisplayInWmi(string deviceName)
    {
        // WMI is not compatible with NativeAOT, use registry-based approach instead
        return CheckForEmbeddedDisplayInRegistry(deviceName);
    }
}