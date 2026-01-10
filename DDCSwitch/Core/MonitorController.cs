using System.Runtime.InteropServices;

namespace DDCSwitch;

/// <summary>
/// Controls DDC/CI enabled monitors
/// </summary>
public static class MonitorController
{
    /// <summary>
    /// Enumerate all physical monitors with DDC/CI support
    /// </summary>
    public static List<Monitor> EnumerateMonitors()
    {
        var monitors = new List<Monitor>();
        int index = 0;
        
        NativeMethods.MonitorEnumProc callback =
            (IntPtr hMonitor, IntPtr hdcMonitor, ref NativeMethods.RECT lprcMonitor, IntPtr dwData) =>
            {
                try
                {
                    // Get monitor info
                    var monitorInfo = new NativeMethods.MONITORINFOEX();
                    monitorInfo.cbSize = (uint) Marshal.SizeOf(monitorInfo);
                    bool gotInfo = NativeMethods.GetMonitorInfo(hMonitor, ref monitorInfo);
                    string deviceName = gotInfo ? monitorInfo.szDevice : "Unknown";
                    bool isPrimary = gotInfo && (monitorInfo.dwFlags & NativeMethods.MONITORINFOF_PRIMARY) != 0;
                    // Get physical monitors
                    if (!NativeMethods.GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out uint physicalMonitorCount))
                    {
                        return true; // Continue enumeration
                    }

                    if (physicalMonitorCount == 0)
                    {
                        return true; // Continue enumeration
                    }

                    var physicalMonitors = new NativeMethods.PHYSICAL_MONITOR[physicalMonitorCount];
                    if (!NativeMethods.GetPhysicalMonitorsFromHMONITOR(hMonitor, physicalMonitorCount,
                            physicalMonitors))
                    {
                        return true; // Continue enumeration
                    }

                    // Add each physical monitor
                    foreach (var physicalMonitor in physicalMonitors)
                    {
                        var monitor = new Monitor(
                            index++,
                            physicalMonitor.szPhysicalMonitorDescription,
                            deviceName,
                            isPrimary,
                            physicalMonitor.hPhysicalMonitor
                        );
                        
                        // Load EDID data for monitor identification
                        monitor.LoadEdidData();
                        
                        monitors.Add(monitor);
                    }
                }
                catch
                {
                    // Continue enumeration even if one monitor fails
                }

                return true; // Continue enumeration
            };
        NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);
        return monitors;
    }

    /// <summary>
    /// Find a monitor by index or name/device pattern
    /// </summary>
    public static Monitor? FindMonitor(List<Monitor> monitors, string identifier)
    {
        // Try as index first
        if (int.TryParse(identifier, out int index))
        {
            return monitors.FirstOrDefault(m => m.Index == index);
        }

        // Try as name or device name (case-insensitive contains)
        var normalized = identifier.ToLowerInvariant();
        
        return monitors.FirstOrDefault(m =>
            m.Name.Contains(normalized, StringComparison.InvariantCultureIgnoreCase) ||
            m.DeviceName.Contains(normalized, StringComparison.InvariantCultureIgnoreCase));
    }
}