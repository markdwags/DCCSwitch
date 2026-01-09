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
    /// Attempts to retrieve EDID data for a monitor from Windows Registry.
    /// </summary>
    /// <param name="deviceName">Device name from MONITORINFOEX (e.g., \\.\DISPLAY1)</param>
    /// <returns>EDID byte array or null if not found</returns>
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
