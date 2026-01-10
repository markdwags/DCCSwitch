using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace DDCSwitch;

internal static class NativeMethods
{
    // Monitor enumeration
    [DllImport("user32.dll")]
    public static extern bool EnumDisplayMonitors(
        IntPtr hdc,
        IntPtr lprcClip,
        MonitorEnumProc lpfnEnum,
        IntPtr dwData);

    public delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    // Physical monitor structures
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct PHYSICAL_MONITOR
    {
        public IntPtr hPhysicalMonitor;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szPhysicalMonitorDescription;
    }

    // DDC/CI functions from dxva2.dll
    [DllImport("dxva2.dll", SetLastError = true)]
    public static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(
        IntPtr hMonitor,
        out uint pdwNumberOfPhysicalMonitors);

    [DllImport("dxva2.dll", SetLastError = true)]
    public static extern bool GetPhysicalMonitorsFromHMONITOR(
        IntPtr hMonitor,
        uint dwPhysicalMonitorArraySize,
        [Out] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

    [DllImport("dxva2.dll", SetLastError = true)]
    public static extern bool GetVCPFeatureAndVCPFeatureReply(
        IntPtr hMonitor,
        byte bVCPCode,
        out uint pvct,
        out uint pdwCurrentValue,
        out uint pdwMaximumValue);

    [DllImport("dxva2.dll", SetLastError = true)]
    public static extern bool SetVCPFeature(
        IntPtr hMonitor,
        byte bVCPCode,
        uint dwNewValue);

    [DllImport("dxva2.dll", SetLastError = true)]
    public static extern bool DestroyPhysicalMonitor(IntPtr hMonitor);

    [DllImport("dxva2.dll", SetLastError = true)]
    public static extern bool DestroyPhysicalMonitors(
        uint dwPhysicalMonitorArraySize,
        PHYSICAL_MONITOR[] pPhysicalMonitorArray);

    // Additional Windows API for getting EDID directly from monitor handle
    [DllImport("gdi32.dll", SetLastError = true)]
    public static extern IntPtr CreateDC(
        string? lpszDriver,
        string lpszDevice,
        string? lpszOutput,
        IntPtr lpInitData);

    [DllImport("gdi32.dll", SetLastError = true)]
    public static extern bool DeleteDC(IntPtr hdc);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr MonitorFromPoint(
        POINT pt,
        uint dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr MonitorFromWindow(
        IntPtr hwnd,
        uint dwFlags);

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    public const uint MONITOR_DEFAULTTONULL = 0x00000000;
    public const uint MONITOR_DEFAULTTOPRIMARY = 0x00000001;
    public const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

    // Monitor info structures
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct MONITORINFOEX
    {
        public uint cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    public const uint MONITORINFOF_PRIMARY = 0x00000001;

    /// <summary>
    /// Attempts to retrieve EDID data for a specific physical monitor using enhanced registry mapping.
    /// This method properly maps physical monitors to their corresponding registry entries by matching monitor descriptions.
    /// </summary>
    /// <param name="deviceName">Device name from MONITORINFOEX (e.g., \\.\DISPLAY1)</param>
    /// <param name="physicalMonitorHandle">Physical monitor handle for precise mapping</param>
    /// <param name="physicalMonitorDescription">Physical monitor description from Windows</param>
    /// <returns>EDID byte array or null if not found</returns>
    public static byte[]? GetEdidFromRegistryEnhanced(string deviceName, IntPtr physicalMonitorHandle, string? physicalMonitorDescription = null)
    {
        try
        {
            // Get all available EDID entries from registry
            var allEdidEntries = GetAllRegistryEdidEntries();
            if (allEdidEntries.Count == 0)
                return null;

            // Try to match by monitor description if available
            if (!string.IsNullOrEmpty(physicalMonitorDescription))
            {
                var matchingEntry = FindEdidByDescription(allEdidEntries, physicalMonitorDescription);
                if (matchingEntry.HasValue)
                {
                    return matchingEntry.Value.edidData;
                }
            }

            // For "Generic PnP Monitor", use process of elimination
            if (physicalMonitorDescription == "Generic PnP Monitor")
            {
                var remainingEntry = FindRemainingEdidEntry(allEdidEntries);
                if (remainingEntry.HasValue)
                {
                    return remainingEntry.Value.edidData;
                }
            }

            // Fallback: Use device name based mapping with better heuristics
            return GetEdidByDeviceNameHeuristic(deviceName, allEdidEntries);
        }
        catch
        {
            // Fallback to original method if enhanced method fails
            return GetEdidFromRegistry(deviceName);
        }
    }

    /// <summary>
    /// Finds the remaining EDID entry by process of elimination.
    /// This is used for "Generic PnP Monitor" which doesn't have a descriptive name.
    /// </summary>
    /// <param name="edidEntries">Available EDID entries</param>
    /// <returns>The remaining EDID entry that hasn't been matched yet</returns>
    private static (byte[] edidData, DateTime lastWriteTime, string registryPath)? FindRemainingEdidEntry(
        List<(byte[] edidData, DateTime lastWriteTime, string registryPath)> edidEntries)
    {
        // Get all currently active monitors to see which ones have been matched
        var activeMonitors = GetCurrentPhysicalMonitors();
        
        // Filter out entries that would be matched by other monitors
        var unmatchedEntries = new List<(byte[] edidData, DateTime lastWriteTime, string registryPath)>();
        
        foreach (var entry in edidEntries)
        {
            var modelName = EdidParser.ParseModelName(entry.edidData);
            var manufacturerName = EdidParser.GetManufacturerName(EdidParser.ParseManufacturerId(entry.edidData));
            
            // Skip entries that would be matched by descriptive monitor names
            bool wouldBeMatched = false;
            
            foreach (var (handle, description) in activeMonitors)
            {
                if (description != "Generic PnP Monitor" && !string.IsNullOrEmpty(modelName))
                {
                    if (description.Contains(modelName, StringComparison.OrdinalIgnoreCase) ||
                        (!string.IsNullOrEmpty(manufacturerName) && description.Contains(manufacturerName, StringComparison.OrdinalIgnoreCase)))
                    {
                        wouldBeMatched = true;
                        break;
                    }
                }
            }
            
            if (!wouldBeMatched)
            {
                unmatchedEntries.Add(entry);
            }
        }
        
        // Return the most recent unmatched entry
        return unmatchedEntries
            .OrderByDescending(e => e.lastWriteTime)
            .FirstOrDefault();
    }

    /// <summary>
    /// Finds EDID entry that matches a physical monitor description.
    /// </summary>
    /// <param name="edidEntries">Available EDID entries</param>
    /// <param name="description">Physical monitor description from Windows</param>
    /// <returns>Matching EDID entry or null</returns>
    private static (byte[] edidData, DateTime lastWriteTime, string registryPath)? FindEdidByDescription(
        List<(byte[] edidData, DateTime lastWriteTime, string registryPath)> edidEntries,
        string description)
    {
        // Try exact model name matches first
        foreach (var entry in edidEntries)
        {
            var modelName = EdidParser.ParseModelName(entry.edidData);
            var manufacturerId = EdidParser.ParseManufacturerId(entry.edidData);
            var manufacturerName = EdidParser.GetManufacturerName(manufacturerId);
            
            if (!string.IsNullOrEmpty(modelName))
            {
                // Check if description contains the model name
                if (description.Contains(modelName, StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }
        }

        // Try manufacturer name matches
        foreach (var entry in edidEntries)
        {
            var manufacturerId = EdidParser.ParseManufacturerId(entry.edidData);
            var manufacturerName = EdidParser.GetManufacturerName(manufacturerId);
            
            if (!string.IsNullOrEmpty(manufacturerName))
            {
                // Check if description contains manufacturer name
                if (description.Contains(manufacturerName, StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }
        }

        // Try manufacturer ID matches
        foreach (var entry in edidEntries)
        {
            var manufacturerId = EdidParser.ParseManufacturerId(entry.edidData);
            if (!string.IsNullOrEmpty(manufacturerId))
            {
                // Check if description contains manufacturer ID
                if (description.Contains(manufacturerId, StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Gets EDID data using device name with improved heuristics.
    /// </summary>
    /// <param name="deviceName">Device name</param>
    /// <param name="edidEntries">Available EDID entries</param>
    /// <returns>EDID data or null</returns>
    private static byte[]? GetEdidByDeviceNameHeuristic(string deviceName, 
        List<(byte[] edidData, DateTime lastWriteTime, string registryPath)> edidEntries)
    {
        // Extract display index from device name
        string displayNum = deviceName.Replace(@"\\.\DISPLAY", "");
        if (!int.TryParse(displayNum, out int displayIndex))
            return null;

        // Get currently active monitors to determine proper mapping
        var activeMonitors = GetCurrentPhysicalMonitors();
        int monitorIndex = displayIndex - 1; // Convert to 0-based

        if (monitorIndex < 0 || monitorIndex >= activeMonitors.Count)
            return null;

        // Sort EDID entries by recency and uniqueness
        var uniqueEdids = FilterUniqueEdidEntries(edidEntries);
        var sortedEdids = uniqueEdids
            .OrderByDescending(e => e.lastWriteTime)
            .Take(activeMonitors.Count)
            .ToList();

        // Map by index among the most recent unique entries
        if (monitorIndex < sortedEdids.Count)
        {
            return sortedEdids[monitorIndex].edidData;
        }

        return null;
    }

    /// <summary>
    /// Filters EDID entries to remove duplicates based on manufacturer and product signatures.
    /// </summary>
    /// <param name="edidEntries">All EDID entries</param>
    /// <returns>Filtered unique EDID entries</returns>
    private static List<(byte[] edidData, DateTime lastWriteTime, string registryPath)> FilterUniqueEdidEntries(
        List<(byte[] edidData, DateTime lastWriteTime, string registryPath)> edidEntries)
    {
        var uniqueEntries = new List<(byte[] edidData, DateTime lastWriteTime, string registryPath)>();
        var seenSignatures = new HashSet<string>();

        foreach (var entry in edidEntries.OrderByDescending(e => e.lastWriteTime))
        {
            var manufacturerId = EdidParser.ParseManufacturerId(entry.edidData);
            var productCode = EdidParser.ParseProductCode(entry.edidData);
            var serialNumber = EdidParser.ParseNumericSerialNumber(entry.edidData);
            
            // Create a unique signature including serial number for better uniqueness
            var signature = $"{manufacturerId}_{productCode:X4}_{serialNumber}";
            
            if (!seenSignatures.Contains(signature))
            {
                seenSignatures.Add(signature);
                uniqueEntries.Add(entry);
            }
        }

        return uniqueEntries;
    }

    /// <summary>
    /// Gets all EDID entries from the registry with metadata.
    /// </summary>
    /// <returns>List of EDID entries with timestamps</returns>
    private static List<(byte[] edidData, DateTime lastWriteTime, string registryPath)> GetAllRegistryEdidEntries()
    {
        var entries = new List<(byte[] edidData, DateTime lastWriteTime, string registryPath)>();

        try
        {
            const string displayKey = @"SYSTEM\CurrentControlSet\Enum\DISPLAY";
            using var displayRoot = Registry.LocalMachine.OpenSubKey(displayKey);
            if (displayRoot == null) return entries;

            foreach (string mfgKey in displayRoot.GetSubKeyNames())
            {
                using var mfgSubKey = displayRoot.OpenSubKey(mfgKey);
                if (mfgSubKey == null) continue;

                foreach (string instanceKey in mfgSubKey.GetSubKeyNames())
                {
                    using var instanceSubKey = mfgSubKey.OpenSubKey(instanceKey);
                    if (instanceSubKey == null) continue;

                    using var deviceParams = instanceSubKey.OpenSubKey("Device Parameters");
                    if (deviceParams == null) continue;

                    var edidData = deviceParams.GetValue("EDID") as byte[];
                    if (edidData == null || edidData.Length < 128)
                        continue;

                    if (!EdidParser.ValidateHeader(edidData))
                        continue;

                    // Use a heuristic for last write time based on registry structure
                    var lastWriteTime = EstimateRegistryEntryAge(instanceSubKey);
                    var registryPath = $@"{displayKey}\{mfgKey}\{instanceKey}";

                    entries.Add((edidData, lastWriteTime, registryPath));
                }
            }
        }
        catch
        {
            // Return partial results
        }

        return entries;
    }

    /// <summary>
    /// Gets information about currently active physical monitors.
    /// </summary>
    /// <returns>List of active monitor information</returns>
    private static List<(IntPtr handle, string description)> GetCurrentPhysicalMonitors()
    {
        var monitors = new List<(IntPtr handle, string description)>();

        try
        {
            NativeMethods.MonitorEnumProc callback = (IntPtr hMonitor, IntPtr hdcMonitor, ref NativeMethods.RECT lprcMonitor, IntPtr dwData) =>
            {
                if (NativeMethods.GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out uint count) && count > 0)
                {
                    var physicalMonitors = new NativeMethods.PHYSICAL_MONITOR[count];
                    if (NativeMethods.GetPhysicalMonitorsFromHMONITOR(hMonitor, count, physicalMonitors))
                    {
                        foreach (var pm in physicalMonitors)
                        {
                            monitors.Add((pm.hPhysicalMonitor, pm.szPhysicalMonitorDescription));
                        }
                    }
                }
                return true;
            };

            NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);
        }
        catch
        {
            // Return partial results
        }

        return monitors;
    }

    /// <summary>
    /// Estimates the age of a registry entry using heuristics.
    /// </summary>
    /// <param name="key">Registry key to analyze</param>
    /// <returns>Estimated last modification time</returns>
    private static DateTime EstimateRegistryEntryAge(RegistryKey key)
    {
        try
        {
            // Heuristic: entries with more subkeys/values are likely more recent
            var subKeyCount = key.GetSubKeyNames().Length;
            var valueCount = key.GetValueNames().Length;
            
            if (subKeyCount > 5 || valueCount > 10)
            {
                return DateTime.Now.AddDays(-1); // Very recent
            }
            else if (subKeyCount > 0 || valueCount > 5)
            {
                return DateTime.Now.AddDays(-7); // Recent
            }
            else
            {
                return DateTime.Now.AddDays(-30); // Older
            }
        }
        catch
        {
            return DateTime.Now.AddDays(-30);
        }
    }
    public static byte[]? GetEdidFromRegistry(string deviceName)
    {
        try
        {
            // Enumerate all DISPLAY devices in registry
            const string displayKey = @"SYSTEM\CurrentControlSet\Enum\DISPLAY";
            using var displayRoot = Registry.LocalMachine.OpenSubKey(displayKey);
            if (displayRoot == null) return null;

            // Collect all EDIDs from active monitors
            var edidList = new List<byte[]>();

            // Try each monitor manufacturer key
            foreach (string mfgKey in displayRoot.GetSubKeyNames())
            {
                using var mfgSubKey = displayRoot.OpenSubKey(mfgKey);
                if (mfgSubKey == null) continue;

                // Try each instance under this manufacturer
                foreach (string instanceKey in mfgSubKey.GetSubKeyNames())
                {
                    using var instanceSubKey = mfgSubKey.OpenSubKey(instanceKey);
                    if (instanceSubKey == null) continue;

                    // Check if this is an active device
                    using var deviceParams = instanceSubKey.OpenSubKey("Device Parameters");
                    if (deviceParams == null) continue;

                    // Read EDID data
                    var edidData = deviceParams.GetValue("EDID") as byte[];
                    if (edidData != null && edidData.Length >= 128)
                    {
                        // Validate EDID header
                        if (EdidParser.ValidateHeader(edidData))
                        {
                            edidList.Add(edidData);
                        }
                    }
                }
            }

            // Extract display index from device name (e.g., \\.\DISPLAY1 -> 0-based index 0)
            string displayNum = deviceName.Replace(@"\\.\DISPLAY", "");
            if (!int.TryParse(displayNum, out int displayIndex))
                return null;
            
            // Convert 1-based display number to 0-based index
            int listIndex = displayIndex - 1;
            
            // Return EDID at the corresponding index if available
            if (listIndex >= 0 && listIndex < edidList.Count)
            {
                return edidList[listIndex];
            }

            // No reliable EDID mapping found for this display index
            return null;
        }
        catch (System.Security.SecurityException)
        {
            // No access to registry
            return null;
        }
        catch (System.UnauthorizedAccessException)
        {
            // Access denied
            return null;
        }
        catch (System.IO.IOException)
        {
            // Registry I/O error
            return null;
        }
        catch (Exception)
        {
            // Unexpected error
            return null;
        }
    }
}
