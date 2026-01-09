using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace DDCSwitch;

/// <summary>
/// Represents a physical monitor with DDC/CI capabilities
/// </summary>
public class Monitor(int index, string name, string deviceName, bool isPrimary, IntPtr handle)
    : IDisposable
{
    public int Index { get; } = index;
    public string Name { get; } = name;
    public string DeviceName { get; } = deviceName;
    public bool IsPrimary { get; } = isPrimary;

    // EDID properties
    public string? ManufacturerId { get; private set; }
    public string? ManufacturerName { get; private set; }
    public string? ModelName { get; private set; }
    public string? SerialNumber { get; private set; }
    public ushort? ProductCode { get; private set; }
    public int? ManufactureYear { get; private set; }
    public int? ManufactureWeek { get; private set; }
    public EdidVersion? EdidVersion { get; private set; }
    public VideoInputDefinition? VideoInputDefinition { get; private set; }
    public SupportedFeatures? SupportedFeatures { get; private set; }
    public ChromaticityCoordinates? Chromaticity { get; private set; }

    private IntPtr Handle { get; } = handle;
    private bool _disposed;

    /// <summary>
    /// Loads EDID data from registry and populates EDID properties.
    /// </summary>
    public void LoadEdidData()
    {
        try
        {
            var edid = NativeMethods.GetEdidFromRegistry(DeviceName);
            if (edid == null || edid.Length < 128) return;

            ManufacturerId = EdidParser.ParseManufacturerId(edid);
            if (ManufacturerId != null)
            {
                ManufacturerName = EdidParser.GetManufacturerName(ManufacturerId);
            }
            ModelName = EdidParser.ParseModelName(edid);
            SerialNumber = EdidParser.ParseSerialNumber(edid);
            ProductCode = EdidParser.ParseProductCode(edid);
            ManufactureYear = EdidParser.ParseManufactureYear(edid);
            ManufactureWeek = EdidParser.ParseManufactureWeek(edid);
            EdidVersion = EdidParser.ParseEdidVersion(edid);
            VideoInputDefinition = EdidParser.ParseVideoInputDefinition(edid);
            SupportedFeatures = EdidParser.ParseSupportedFeatures(edid);
            Chromaticity = EdidParser.ParseChromaticity(edid);
        }
        catch
        {
            // Graceful degradation - EDID properties remain null
        }
    }

    public bool TryGetInputSource(out uint currentValue, out uint maxValue)
    {
        currentValue = 0;
        maxValue = 0;

        if (_disposed || Handle == IntPtr.Zero)
        {
            return false;
        }

        return NativeMethods.GetVCPFeatureAndVCPFeatureReply(
            Handle,
            InputSource.VcpInputSource,
            out _,
            out currentValue,
            out maxValue);
    }

    public bool TrySetInputSource(uint value)
    {
        if (_disposed || Handle == IntPtr.Zero)
        {
            return false;
        }

        return NativeMethods.SetVCPFeature(Handle, InputSource.VcpInputSource, value);
    }

    /// <summary>
    /// Attempts to read a VCP feature value from the monitor with enhanced error detection
    /// </summary>
    /// <param name="vcpCode">VCP code to read (0x00-0xFF)</param>
    /// <param name="currentValue">Current value of the VCP feature</param>
    /// <param name="maxValue">Maximum value supported by the VCP feature</param>
    /// <param name="errorCode">Win32 error code if operation fails</param>
    /// <returns>True if the operation was successful</returns>
    public bool TryGetVcpFeature(byte vcpCode, out uint currentValue, out uint maxValue, out int errorCode)
    {
        currentValue = 0;
        maxValue = 0;
        errorCode = 0;

        if (_disposed || Handle == IntPtr.Zero)
        {
            errorCode = 0x00000006; // ERROR_INVALID_HANDLE
            return false;
        }

        bool success = NativeMethods.GetVCPFeatureAndVCPFeatureReply(
            Handle,
            vcpCode,
            out _,
            out currentValue,
            out maxValue);

        if (!success)
        {
            errorCode = Marshal.GetLastWin32Error();
        }

        return success;
    }

    /// <summary>
    /// Attempts to read a VCP feature value from the monitor (legacy method for backward compatibility)
    /// </summary>
    /// <param name="vcpCode">VCP code to read (0x00-0xFF)</param>
    /// <param name="currentValue">Current value of the VCP feature</param>
    /// <param name="maxValue">Maximum value supported by the VCP feature</param>
    /// <returns>True if the operation was successful</returns>
    public bool TryGetVcpFeature(byte vcpCode, out uint currentValue, out uint maxValue)
    {
        return TryGetVcpFeature(vcpCode, out currentValue, out maxValue, out _);
    }

    /// <summary>
    /// Attempts to write a VCP feature value to the monitor with enhanced error detection
    /// </summary>
    /// <param name="vcpCode">VCP code to write (0x00-0xFF)</param>
    /// <param name="value">Value to set for the VCP feature</param>
    /// <param name="errorCode">Win32 error code if operation fails</param>
    /// <returns>True if the operation was successful</returns>
    public bool TrySetVcpFeature(byte vcpCode, uint value, out int errorCode)
    {
        errorCode = 0;

        if (_disposed || Handle == IntPtr.Zero)
        {
            errorCode = 0x00000006; // ERROR_INVALID_HANDLE
            return false;
        }

        bool success = NativeMethods.SetVCPFeature(Handle, vcpCode, value);

        if (!success)
        {
            errorCode = Marshal.GetLastWin32Error();
        }

        return success;
    }

    /// <summary>
    /// Attempts to write a VCP feature value to the monitor (legacy method for backward compatibility)
    /// </summary>
    /// <param name="vcpCode">VCP code to write (0x00-0xFF)</param>
    /// <param name="value">Value to set for the VCP feature</param>
    /// <returns>True if the operation was successful</returns>
    public bool TrySetVcpFeature(byte vcpCode, uint value)
    {
        return TrySetVcpFeature(vcpCode, value, out _);
    }

    /// <summary>
    /// Scans all VCP codes (0x00-0xFF) to discover supported features
    /// </summary>
    /// <returns>Dictionary mapping VCP codes to their feature information</returns>
    public Dictionary<byte, VcpFeatureInfo> ScanVcpFeatures()
    {
        var features = new Dictionary<byte, VcpFeatureInfo>();

        if (_disposed || Handle == IntPtr.Zero)
        {
            return features;
        }

        // Get predefined features for name lookup
        var predefinedFeatures = FeatureResolver.GetPredefinedFeatures()
            .ToDictionary(f => f.Code, f => f);

        // Scan all possible VCP codes (0x00 to 0xFF)
        for (int code = 0; code <= 255; code++)
        {
            byte vcpCode = (byte)code;
            
            if (TryGetVcpFeature(vcpCode, out uint currentValue, out uint maxValue))
            {
                // Feature is supported - determine name and type
                string name;
                VcpFeatureType type;

                if (predefinedFeatures.TryGetValue(vcpCode, out VcpFeature? predefined))
                {
                    name = predefined.Name;
                    type = predefined.Type;
                }
                else
                {
                    name = $"VCP_{vcpCode:X2}";
                    type = VcpFeatureType.ReadWrite; // Assume read-write for unknown codes
                }

                features[vcpCode] = new VcpFeatureInfo(
                    vcpCode,
                    name,
                    predefined?.Description ?? $"VCP feature {name}",
                    type,
                    predefined?.Category ?? VcpFeatureCategory.Miscellaneous,
                    currentValue,
                    maxValue,
                    true
                );
            }
            else
            {
                // Feature is not supported - still add entry for completeness
                string name = predefinedFeatures.TryGetValue(vcpCode, out VcpFeature? predefined) 
                    ? predefined.Name 
                    : $"VCP_{vcpCode:X2}";

                features[vcpCode] = new VcpFeatureInfo(
                    vcpCode,
                    name,
                    predefined?.Description ?? $"VCP feature {name}",
                    VcpFeatureType.ReadWrite,
                    predefined?.Category ?? VcpFeatureCategory.Miscellaneous,
                    0,
                    0,
                    false
                );
            }
        }

        return features;
    }

    public void Dispose()
    {
        if (!_disposed && Handle != IntPtr.Zero)
        {
            NativeMethods.DestroyPhysicalMonitor(Handle);
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }

    ~Monitor()
    {
        Dispose();
    }

    public override string ToString()
    {
        return $"[{Index}] {Name} ({DeviceName}){(IsPrimary ? " *PRIMARY*" : "")}";
    }
}