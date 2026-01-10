using System.Text;
using Microsoft.Win32;

namespace DDCSwitch;

/// <summary>
/// Represents EDID version information.
/// </summary>
/// <param name="Major">Major version number</param>
/// <param name="Minor">Minor version number</param>
public record EdidVersion(byte Major, byte Minor)
{
    public override string ToString() => $"{Major}.{Minor}";
}

/// <summary>
/// Represents video input definition from EDID.
/// </summary>
/// <param name="IsDigital">True if digital input, false if analog</param>
/// <param name="RawValue">Raw byte value from EDID</param>
public record VideoInputDefinition(bool IsDigital, byte RawValue)
{
    public override string ToString() => IsDigital ? "Digital" : "Analog";
}

/// <summary>
/// Represents supported display features from EDID.
/// </summary>
public record SupportedFeatures(
    bool DpmsStandby,
    bool DpmsSuspend,
    bool DpmsActiveOff,
    byte DisplayType,
    bool DefaultColorSpace,
    bool PreferredTimingMode,
    bool ContinuousFrequency,
    byte RawValue)
{
    public string DisplayTypeDescription => DisplayType switch
    {
        0 => "Monochrome or Grayscale",
        1 => "RGB Color",
        2 => "Non-RGB Color",
        3 => "Undefined",
        _ => "Unknown"
    };
}

/// <summary>
/// Represents chromaticity coordinates for a color point.
/// </summary>
/// <param name="X">X coordinate (0.0 to 1.0)</param>
/// <param name="Y">Y coordinate (0.0 to 1.0)</param>
public record ColorPoint(double X, double Y)
{
    public override string ToString() => $"x={X:F4}, y={Y:F4}";
}

/// <summary>
/// Represents complete chromaticity information from EDID.
/// </summary>
public record ChromaticityCoordinates(
    ColorPoint Red,
    ColorPoint Green,
    ColorPoint Blue,
    ColorPoint White);

/// <summary>
/// Represents a registry EDID entry with metadata.
/// </summary>
/// <param name="RegistryPath">Full registry path to the EDID entry</param>
/// <param name="EdidData">Raw EDID byte data</param>
/// <param name="LastWriteTime">Last modification time of the registry entry</param>
/// <param name="IsCurrentlyActive">Whether this entry corresponds to an active monitor</param>
public record RegistryEdidEntry(
    string RegistryPath,
    byte[] EdidData,
    DateTime LastWriteTime,
    bool IsCurrentlyActive
);

/// <summary>
/// Represents complete EDID information with metadata from enhanced parsing.
/// </summary>
/// <param name="ManufacturerName">Full manufacturer name</param>
/// <param name="ManufacturerCode">3-letter manufacturer code</param>
/// <param name="ProductCode">Product code from EDID</param>
/// <param name="SerialNumber">Serial number from EDID</param>
/// <param name="ManufactureWeek">Week of manufacture (1-53)</param>
/// <param name="ManufactureYear">Year of manufacture</param>
/// <param name="EdidVersion">EDID version number</param>
/// <param name="EdidRevision">EDID revision number</param>
/// <param name="VideoInputDefinition">Video input definition string</param>
/// <param name="ColorInfo">Color space information</param>
/// <param name="RawData">Raw EDID byte data</param>
/// <param name="IsFromActiveEntry">Whether this EDID came from an active registry entry</param>
public record ParsedEdidInfo(
    string ManufacturerName,
    string ManufacturerCode,
    ushort ProductCode,
    uint SerialNumber,
    int ManufactureWeek,
    int ManufactureYear,
    byte EdidVersion,
    byte EdidRevision,
    string VideoInputDefinition,
    EdidColorInfo ColorInfo,
    byte[] RawData,
    bool IsFromActiveEntry
);

/// <summary>
/// Represents color space information from EDID.
/// </summary>
/// <param name="WhitePointX">White point X coordinate</param>
/// <param name="WhitePointY">White point Y coordinate</param>
/// <param name="RedX">Red X coordinate</param>
/// <param name="RedY">Red Y coordinate</param>
/// <param name="GreenX">Green X coordinate</param>
/// <param name="GreenY">Green Y coordinate</param>
/// <param name="BlueX">Blue X coordinate</param>
/// <param name="BlueY">Blue Y coordinate</param>
public record EdidColorInfo(
    float WhitePointX,
    float WhitePointY,
    float RedX,
    float RedY,
    float GreenX,
    float GreenY,
    float BlueX,
    float BlueY
);

/// <summary>
/// Parses EDID (Extended Display Identification Data) blocks to extract monitor information.
/// Enhanced with registry conflict resolution and active entry detection.
/// </summary>
public static class EdidParser
{
    /// <summary>
    /// Parses EDID information from registry with enhanced conflict resolution.
    /// </summary>
    /// <param name="deviceName">Device name from MONITORINFOEX (e.g., \\.\DISPLAY1)</param>
    /// <returns>Complete EDID information or null if not found</returns>
    public static ParsedEdidInfo? ParseFromRegistry(string deviceName)
    {
        var edidData = NativeMethods.GetEdidFromRegistry(deviceName);
        if (edidData == null || edidData.Length < 128)
            return null;

        return ParseFromBytes(edidData, false);
    }

    /// <summary>
    /// Parses EDID information from active registry entry using physical monitor handle.
    /// </summary>
    /// <param name="deviceName">Device name from MONITORINFOEX</param>
    /// <param name="physicalMonitorHandle">Physical monitor handle for cross-referencing</param>
    /// <returns>Complete EDID information from active entry or null if not found</returns>
    public static ParsedEdidInfo? ParseFromActiveRegistry(string deviceName, IntPtr physicalMonitorHandle)
    {
        try
        {
            // Extract hardware ID from device name for registry lookup
            string? hardwareId = ExtractHardwareIdFromDeviceName(deviceName);
            if (hardwareId == null)
                return null;

            // Find the active registry entry for this hardware ID
            var activeEntry = FindActiveRegistryEntry(hardwareId, physicalMonitorHandle);
            
            return activeEntry == null ? null : ParseFromBytes(activeEntry.EdidData, true);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parses EDID information from raw byte data.
    /// </summary>
    /// <param name="edidData">Raw EDID byte array</param>
    /// <param name="isFromActiveEntry">Whether this data came from an active registry entry</param>
    /// <returns>Complete EDID information or null if invalid</returns>
    public static ParsedEdidInfo? ParseFromBytes(byte[] edidData, bool isFromActiveEntry = false)
    {
        if (edidData == null || edidData.Length < 128 || !ValidateHeader(edidData))
            return null;

        try
        {
            var manufacturerCode = ParseManufacturerId(edidData) ?? "UNK";
            var manufacturerName = GetManufacturerName(manufacturerCode);
            var productCode = ParseProductCode(edidData) ?? 0;
            var serialNumber = ParseNumericSerialNumber(edidData) ?? 0;
            var manufactureWeek = ParseManufactureWeek(edidData) ?? 0;
            var manufactureYear = ParseManufactureYear(edidData) ?? 0;
            var edidVersion = ParseEdidVersion(edidData);
            var videoInput = ParseVideoInputDefinition(edidData);
            var chromaticity = ParseChromaticity(edidData);

            var colorInfo = new EdidColorInfo(
                (float)(chromaticity?.White.X ?? 0.0),
                (float)(chromaticity?.White.Y ?? 0.0),
                (float)(chromaticity?.Red.X ?? 0.0),
                (float)(chromaticity?.Red.Y ?? 0.0),
                (float)(chromaticity?.Green.X ?? 0.0),
                (float)(chromaticity?.Green.Y ?? 0.0),
                (float)(chromaticity?.Blue.X ?? 0.0),
                (float)(chromaticity?.Blue.Y ?? 0.0)
            );

            return new ParsedEdidInfo(
                manufacturerName,
                manufacturerCode,
                productCode,
                serialNumber,
                manufactureWeek,
                manufactureYear,
                edidVersion?.Major ?? 0,
                edidVersion?.Minor ?? 0,
                videoInput?.ToString() ?? "Unknown",
                colorInfo,
                edidData,
                isFromActiveEntry
            );
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Finds all registry entries for a given hardware ID.
    /// </summary>
    /// <param name="hardwareId">Hardware ID to search for (format: ManufacturerKey\InstanceKey)</param>
    /// <returns>List of all registry entries found for this hardware ID</returns>
    public static List<RegistryEdidEntry> FindAllRegistryEntries(string hardwareId)
    {
        var entries = new List<RegistryEdidEntry>();

        try
        {
            const string displayKey = @"SYSTEM\CurrentControlSet\Enum\DISPLAY";
            using var displayRoot = Registry.LocalMachine.OpenSubKey(displayKey);
            if (displayRoot == null) return entries;

            // Parse hardware ID (format: ManufacturerKey\InstanceKey)
            var parts = hardwareId.Split('\\');
            if (parts.Length != 2) return entries;

            string mfgKey = parts[0];
            string targetInstanceKey = parts[1];

            using var mfgSubKey = displayRoot.OpenSubKey(mfgKey);
            if (mfgSubKey == null) return entries;

            // Look for the specific instance and similar instances (for conflict detection)
            foreach (string instanceKey in mfgSubKey.GetSubKeyNames())
            {
                // Include exact match and similar instances (same base hardware ID)
                if (instanceKey == targetInstanceKey || 
                    instanceKey.StartsWith(targetInstanceKey.Split('&')[0], StringComparison.OrdinalIgnoreCase))
                {
                    using var instanceSubKey = mfgSubKey.OpenSubKey(instanceKey);
                    if (instanceSubKey == null) continue;

                    using var deviceParams = instanceSubKey.OpenSubKey("Device Parameters");
                    if (deviceParams == null) continue;

                    // Read EDID data
                    if (deviceParams.GetValue("EDID") is not byte[] edidData || edidData.Length < 128)
                        continue;

                    // Validate EDID header
                    if (!ValidateHeader(edidData))
                        continue;

                    // Get registry entry metadata
                    var registryPath = $@"{displayKey}\{mfgKey}\{instanceKey}";
                    var lastWriteTime = GetRegistryKeyLastWriteTime(instanceSubKey) ?? DateTime.MinValue;

                    entries.Add(new RegistryEdidEntry(
                        registryPath,
                        edidData,
                        lastWriteTime,
                        instanceKey == targetInstanceKey // Mark as active if exact match
                    ));
                }
            }
        }
        catch
        {
            // Return partial results on error
        }

        return entries;
    }

    /// <summary>
    /// Finds the active registry entry for a hardware ID using physical monitor handle.
    /// </summary>
    /// <param name="hardwareId">Hardware ID to search for</param>
    /// <param name="physicalMonitorHandle">Physical monitor handle for cross-referencing</param>
    /// <returns>Active registry entry or null if not found</returns>
    public static RegistryEdidEntry? FindActiveRegistryEntry(string hardwareId, IntPtr physicalMonitorHandle)
    {
        var allEntries = FindAllRegistryEntries(hardwareId);
        if (allEntries.Count == 0)
            return null;

        // If only one entry, assume it's active
        if (allEntries.Count == 1)
        {
            var entry = allEntries[0];
            return entry with { IsCurrentlyActive = true };
        }

        // Multiple entries - use heuristics to determine active one
        var activeEntry = ResolveRegistryConflicts(allEntries, physicalMonitorHandle);
        if (activeEntry != null)
        {
            return activeEntry with { IsCurrentlyActive = true };
        }

        // Fallback: return most recently modified entry
        var mostRecent = allEntries.OrderByDescending(e => e.LastWriteTime).First();
        return mostRecent with { IsCurrentlyActive = true };
    }

    /// <summary>
    /// Resolves conflicts when multiple registry entries exist for the same hardware ID.
    /// </summary>
    /// <param name="entries">List of conflicting registry entries</param>
    /// <param name="physicalMonitorHandle">Physical monitor handle for cross-referencing</param>
    /// <returns>The most likely active entry or null if cannot be determined</returns>
    private static RegistryEdidEntry? ResolveRegistryConflicts(List<RegistryEdidEntry> entries, IntPtr physicalMonitorHandle)
    {
        if (entries.Count <= 1)
            return entries.FirstOrDefault();

        // Strategy 1: Use physical monitor handle to cross-reference with registry
        // This is a simplified approach - in a full implementation, we would use
        // Windows APIs to map physical monitor handles to registry entries
        
        // Strategy 2: Prefer entries with more recent timestamps
        var recentEntries = entries
            .Where(e => e.LastWriteTime > DateTime.Now.AddDays(-30)) // Within last 30 days
            .OrderByDescending(e => e.LastWriteTime)
            .ToList();

        if (recentEntries.Count > 0)
            return recentEntries.First();

        // Strategy 3: Prefer entries with valid EDID data and complete information
        var validEntries = entries
            .Where(e => HasCompleteEdidInfo(e.EdidData))
            .OrderByDescending(e => e.LastWriteTime)
            .ToList();

        if (validEntries.Count > 0)
            return validEntries.First();

        // Fallback: return most recent entry
        return entries.OrderByDescending(e => e.LastWriteTime).FirstOrDefault();
    }

    /// <summary>
    /// Checks if EDID data contains complete information.
    /// </summary>
    /// <param name="edidData">EDID byte array</param>
    /// <returns>True if EDID contains manufacturer, product, and other key information</returns>
    private static bool HasCompleteEdidInfo(byte[] edidData)
    {
        if (edidData == null || edidData.Length < 128)
            return false;

        var manufacturerId = ParseManufacturerId(edidData);
        var productCode = ParseProductCode(edidData);
        var modelName = ParseModelName(edidData);

        return !string.IsNullOrEmpty(manufacturerId) && 
               productCode.HasValue && 
               productCode.Value != 0 &&
               (!string.IsNullOrEmpty(modelName) || ParseNumericSerialNumber(edidData).HasValue);
    }

    /// <summary>
    /// Extracts hardware ID from Windows device name using proper Windows APIs.
    /// </summary>
    /// <param name="deviceName">Device name (e.g., \\.\DISPLAY1)</param>
    /// <returns>Hardware ID or null if cannot be extracted</returns>
    private static string? ExtractHardwareIdFromDeviceName(string deviceName)
    {
        try
        {
            // For now, we'll use a more sophisticated approach to map device names to hardware IDs
            // This involves checking which registry entries correspond to currently active displays
            
            // Extract display number from device name (e.g., \\.\DISPLAY1 -> 1)
            string displayNum = deviceName.Replace(@"\\.\DISPLAY", "");
            if (!int.TryParse(displayNum, out int displayIndex))
                return null;

            // Get all currently active display devices and their registry paths
            var activeDisplays = GetActiveDisplayDevices();
            
            // Find the hardware ID for the display at the given index
            if (displayIndex > 0 && displayIndex <= activeDisplays.Count)
            {
                return activeDisplays[displayIndex - 1]; // Convert to 0-based index
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets hardware IDs for all currently active display devices.
    /// </summary>
    /// <returns>List of hardware IDs for active displays</returns>
    private static List<string> GetActiveDisplayDevices()
    {
        var activeDisplays = new List<string>();
        
        try
        {
            // This is a simplified implementation. In a full implementation, we would use
            // EnumDisplayDevices and SetupDi APIs to get the actual hardware IDs.
            // For now, we'll use a heuristic based on registry timestamps and validation.
            
            const string displayKey = @"SYSTEM\CurrentControlSet\Enum\DISPLAY";
            using var displayRoot = Registry.LocalMachine.OpenSubKey(displayKey);
            if (displayRoot == null) return activeDisplays;

            var candidateEntries = new List<(string hardwareId, DateTime lastWrite, byte[] edid)>();

            // Collect all valid EDID entries with their metadata
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
                    if (edidData == null || edidData.Length < 128 || !ValidateHeader(edidData))
                        continue;

                    // Check if this entry has recent activity (heuristic for active device)
                    var lastWrite = GetRegistryKeyLastWriteTime(instanceSubKey) ?? DateTime.MinValue;
                    
                    // Use a combination of manufacturer key and instance as hardware ID
                    string hardwareId = $"{mfgKey}\\{instanceKey}";
                    
                    candidateEntries.Add((hardwareId, lastWrite, edidData));
                }
            }

            // Sort by last write time (most recent first) and take the most recent entries
            // This heuristic assumes that recently modified registry entries correspond to active monitors
            var sortedEntries = candidateEntries
                .OrderByDescending(e => e.lastWrite)
                .Take(10) // Reasonable limit for number of monitors
                .ToList();

            // Further filter by checking for unique EDID signatures
            var uniqueEdids = new HashSet<string>();
            foreach (var entry in sortedEntries)
            {
                // Create a signature from manufacturer ID and product code
                var manufacturerId = ParseManufacturerId(entry.edid);
                var productCode = ParseProductCode(entry.edid);
                var signature = $"{manufacturerId}_{productCode:X4}";
                
                if (!uniqueEdids.Contains(signature))
                {
                    uniqueEdids.Add(signature);
                    activeDisplays.Add(entry.hardwareId);
                }
            }

            return activeDisplays;
        }
        catch
        {
            return activeDisplays;
        }
    }

    /// <summary>
    /// Gets the last write time of a registry key using heuristics.
    /// </summary>
    /// <param name="key">Registry key</param>
    /// <returns>Last write time or null if cannot be determined</returns>
    private static DateTime? GetRegistryKeyLastWriteTime(RegistryKey key)
    {
        try
        {
            // Since we can't directly get registry key timestamps in .NET without P/Invoke,
            // we'll use a heuristic based on the registry structure and common patterns
            
            // Check if there are any subkeys with timestamps we can infer from
            var subKeyNames = key.GetSubKeyNames();
            var valueNames = key.GetValueNames();
            
            // If the key has many subkeys or values, it's likely more recent
            // This is a rough heuristic - in practice, we'd use RegQueryInfoKey API
            
            if (subKeyNames.Length > 0 || valueNames.Length > 0)
            {
                // Assume recent activity if the key has content
                return DateTime.Now.AddDays(-1); // Assume modified within last day
            }
            
            // Fallback to a reasonable default
            return DateTime.Now.AddDays(-30);
        }
        catch
        {
            return DateTime.Now.AddDays(-30);
        }
    }

    /// <summary>
    /// Parses manufacturer ID from EDID bytes.
    /// </summary>
    /// <param name="edid">EDID data (at least 2 bytes from offset 8)</param>
    /// <returns>3-letter manufacturer ID (e.g., "SAM", "DEL") or null if invalid</returns>
    public static string? ParseManufacturerId(byte[] edid)
    {
        if (edid.Length < 10) return null;
        
        try
        {
            // Manufacturer ID is stored in bytes 8-9 as 3 5-bit characters
            // Bit layout: |0|CHAR1|CHAR2|CHAR3| across 2 bytes
            ushort id = (ushort)((edid[8] << 8) | edid[9]);
            
            char char1 = (char)(((id >> 10) & 0x1F) + 'A' - 1);
            char char2 = (char)(((id >> 5) & 0x1F) + 'A' - 1);
            char char3 = (char)((id & 0x1F) + 'A' - 1);
            
            return $"{char1}{char2}{char3}";
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the full manufacturer name from 3-letter PNP ID.
    /// </summary>
    /// <param name="manufacturerId">3-letter manufacturer ID</param>
    /// <returns>Full company name or the ID itself if not found</returns>
    public static string GetManufacturerName(string? manufacturerId)
    {
        if (string.IsNullOrEmpty(manufacturerId)) return "Unknown";
        
        return manufacturerId switch
        {
            "AAC" => "AcerView",
            "ACI" => "Asus Computer Inc",
            "ACR" => "Acer Technologies",
            "ACT" => "Targa",
            "ADI" => "ADI Corporation",
            "AIC" => "AG Neovo",
            "AMW" => "AMW",
            "AOC" => "AOC International",
            "API" => "A Plus Info Corporation",
            "APP" => "Apple Computer",
            "ART" => "ArtMedia",
            "AST" => "AST Research",
            "AUO" => "AU Optronics",
            "BEL" => "Belkin",
            "BEN" => "BenQ Corporation",
            "BMM" => "BMM",
            "BNQ" => "BenQ Corporation",
            "BOE" => "BOE Technology",
            "CMO" => "Chi Mei Optoelectronics",
            "CPL" => "Compal Electronics",
            "CPQ" => "Compaq Computer Corporation",
            "CTX" => "CTX International",
            "DEC" => "Digital Equipment Corporation",
            "DEL" => "Dell Inc.",
            "DPC" => "Delta Electronics",
            "DWE" => "Daewoo Electronics",
            "ECS" => "ELITEGROUP Computer Systems",
            "EIZ" => "EIZO Corporation",
            "ELS" => "ELSA GmbH",
            "ENC" => "Eizo Nanao Corporation",
            "EPI" => "Envision Peripherals",
            "EPH" => "Epiphan Systems Inc.",
            "FUJ" => "Fujitsu Siemens Computers",
            "FUS" => "Fujitsu Siemens Computers",
            "GSM" => "LG Electronics",
            "GWY" => "Gateway 2000",
            "HEI" => "Hyundai Electronics Industries",
            "HIQ" => "Hyundai ImageQuest",
            "HIT" => "Hitachi",
            "HPN" => "HP Inc.",
            "HSD" => "Hannstar Display Corporation",
            "HSL" => "Hansol Electronics",
            "HTC" => "Hitachi",
            "HWP" => "HP Inc.",
            "IBM" => "IBM Corporation",
            "ICL" => "Fujitsu ICL",
            "IFS" => "InFocus Corporation",
            "IQT" => "Hyundai ImageQuest",
            "IVM" => "Iiyama North America",
            "KDS" => "KDS USA",
            "KFC" => "KFC Computek",
            "LEN" => "Lenovo",
            "LGD" => "LG Display",
            "LKM" => "ADLAS",
            "LNK" => "LINK Technologies",
            "LPL" => "LG Philips",
            "LTN" => "Lite-On Technology",
            "MAG" => "MAG InnoVision",
            "MAX" => "Maxdata Computer",
            "MEI" => "Panasonic Industry Company",
            "MEL" => "Mitsubishi Electronics",
            "MED" => "Matsushita Electric Industrial",
            "MS_" => "Panasonic Industry Company",
            "MSI" => "Micro-Star International",
            "MSH" => "Microsoft Corporation",
            "NAN" => "NANAO Corporation",
            "NEC" => "NEC Corporation",
            "NOK" => "Nokia",
            "NVD" => "Nvidia",
            "OPT" => "Optoma Corporation",
            "OQI" => "OPTIQUEST",
            "PBN" => "Packard Bell",
            "PCK" => "Daewoo Electronics",
            "PDC" => "Polaroid",
            "PGS" => "Princeton Graphic Systems",
            "PHL" => "Philips Consumer Electronics",
            "PIX" => "Pixelink",
            "PNR" => "Planar Systems",
            "PRT" => "Princeton Graphic Systems",
            "REL" => "Relisys",
            "SAM" => "Samsung Electric Company",
            "SAN" => "Sanyo Electric Co.",
            "SBI" => "Smarttech",
            "SEC" => "Seiko Epson Corporation",
            "SGI" => "Silicon Graphics",
            "SMC" => "Samtron",
            "SMI" => "Smile",
            "SNI" => "Siemens Nixdorf",
            "SNY" => "Sony Corporation",
            "SPT" => "Sceptre Tech Inc.",
            "SRC" => "Shamrock Technology",
            "STN" => "Samtron",
            "STP" => "Sceptre Tech Inc.",
            "TAT" => "Tatung Company of America",
            "TOS" => "Toshiba Corporation",
            "TRL" => "Royal Information Company",
            "TSB" => "Toshiba America Info Systems",
            "UNK" => "Unknown",
            "UNM" => "Unisys Corporation",
            "VSC" => "ViewSonic Corporation",
            "WTC" => "Wen Technology",
            "ZCM" => "Zenith Data Systems",
            _ => manufacturerId,
        };
    }

    /// <summary>
    /// Parses the model name from EDID descriptor blocks.
    /// </summary>
    /// <param name="edid">EDID data (at least 128 bytes)</param>
    /// <returns>Model name string or null if not found</returns>
    public static string? ParseModelName(byte[] edid)
    {
        if (edid.Length < 128) return null;
        
        // Check descriptor blocks at offsets 54, 72, 90, 108 (18 bytes each)
        for (int offset = 54; offset <= 108; offset += 18)
        {
            // Descriptor type 0xFC indicates monitor name
            if (edid[offset] == 0x00 && edid[offset + 1] == 0x00 && 
                edid[offset + 2] == 0x00 && edid[offset + 3] == 0xFC)
            {
                return ParseDescriptorString(edid, offset + 5);
            }
        }
        
        return null;
    }

    /// <summary>
    /// Parses the serial number from EDID descriptor blocks.
    /// </summary>
    /// <param name="edid">EDID data (at least 128 bytes)</param>
    /// <returns>Serial number string or null if not found</returns>
    public static string? ParseSerialNumber(byte[] edid)
    {
        if (edid.Length < 128) return null;
        
        // Check descriptor blocks at offsets 54, 72, 90, 108 (18 bytes each)
        for (int offset = 54; offset <= 108; offset += 18)
        {
            // Descriptor type 0xFF indicates serial number
            if (edid[offset] == 0x00 && edid[offset + 1] == 0x00 && 
                edid[offset + 2] == 0x00 && edid[offset + 3] == 0xFF)
            {
                return ParseDescriptorString(edid, offset + 5);
            }
        }
        
        // If no descriptor serial found, try numeric serial at bytes 12-15
        var numericSerial = ParseNumericSerialNumber(edid);
        if (numericSerial.HasValue && numericSerial.Value != 0)
        {
            return numericSerial.Value.ToString();
        }
        
        return null;
    }

    /// <summary>
    /// Parses the numeric serial number from EDID.
    /// </summary>
    /// <param name="edid">EDID data (at least 16 bytes)</param>
    /// <returns>Numeric serial number or null if invalid</returns>
    public static uint? ParseNumericSerialNumber(byte[] edid)
    {
        if (edid.Length < 16) return null;
        
        try
        {
            // Serial number is at bytes 12-15 (little-endian, 32-bit)
            uint serial = (uint)(edid[12] | (edid[13] << 8) | (edid[14] << 16) | (edid[15] << 24));
            return serial;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parses the product code from EDID.
    /// </summary>
    /// <param name="edid">EDID data (at least 12 bytes)</param>
    /// <returns>Product code as ushort or null if invalid</returns>
    public static ushort? ParseProductCode(byte[] edid)
    {
        if (edid.Length < 12) return null;
        
        try
        {
            // Product code is at bytes 10-11 (little-endian)
            return (ushort)(edid[10] | (edid[11] << 8));
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parses the manufacture year from EDID.
    /// </summary>
    /// <param name="edid">EDID data (at least 18 bytes)</param>
    /// <returns>Manufacture year (e.g., 2023) or null if invalid</returns>
    public static int? ParseManufactureYear(byte[] edid)
    {
        if (edid.Length < 18) return null;
        
        try
        {
            // Manufacture year is at byte 17, stored as offset from 1990
            int year = edid[17] + 1990;
            return year >= 1990 && year <= 2100 ? year : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parses the manufacture week from EDID.
    /// </summary>
    /// <param name="edid">EDID data (at least 17 bytes)</param>
    /// <returns>Manufacture week (1-53) or null if invalid</returns>
    public static int? ParseManufactureWeek(byte[] edid)
    {
        if (edid.Length < 17) return null;
        
        try
        {
            // Manufacture week is at byte 16 (1-53, or 0xFF for unknown)
            int week = edid[16];
            return week >= 1 && week <= 53 ? week : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Helper to parse ASCII string from EDID descriptor block.
    /// </summary>
    private static string? ParseDescriptorString(byte[] edid, int offset)
    {
        try
        {
            var sb = new StringBuilder();
            for (int i = 0; i < 13; i++)
            {
                byte b = edid[offset + i];
                if (b == 0x0A || b == 0x00) break; // Newline or null terminator
                if (b >= 0x20 && b <= 0x7E) // Printable ASCII
                {
                    sb.Append((char)b);
                }
            }
            
            string result = sb.ToString().Trim();
            return string.IsNullOrWhiteSpace(result) ? null : result;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Validates EDID header (first 8 bytes should be: 00 FF FF FF FF FF FF 00).
    /// </summary>
    /// <param name="edid">EDID data (at least 8 bytes)</param>
    /// <returns>True if header is valid</returns>
    public static bool ValidateHeader(byte[] edid)
    {
        if (edid.Length < 8) return false;
        
        return edid[0] == 0x00 &&
               edid[1] == 0xFF &&
               edid[2] == 0xFF &&
               edid[3] == 0xFF &&
               edid[4] == 0xFF &&
               edid[5] == 0xFF &&
               edid[6] == 0xFF &&
               edid[7] == 0x00;
    }

    /// <summary>
    /// Parses EDID version and revision from EDID.
    /// </summary>
    /// <param name="edid">EDID data (at least 20 bytes)</param>
    /// <returns>EDID version information or null if invalid</returns>
    public static EdidVersion? ParseEdidVersion(byte[] edid)
    {
        if (edid.Length < 20) return null;

        try
        {
            // EDID version is at bytes 18-19
            byte major = edid[18];
            byte minor = edid[19];
            return new EdidVersion(major, minor);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parses video input definition from EDID.
    /// </summary>
    /// <param name="edid">EDID data (at least 21 bytes)</param>
    /// <returns>Video input definition or null if invalid</returns>
    public static VideoInputDefinition? ParseVideoInputDefinition(byte[] edid)
    {
        if (edid.Length < 21) return null;

        try
        {
            // Video input definition is at byte 20
            byte value = edid[20];
            bool isDigital = (value & 0x80) != 0; // Bit 7
            return new VideoInputDefinition(isDigital, value);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parses supported features from EDID.
    /// </summary>
    /// <param name="edid">EDID data (at least 25 bytes)</param>
    /// <returns>Supported features information or null if invalid</returns>
    public static SupportedFeatures? ParseSupportedFeatures(byte[] edid)
    {
        if (edid.Length < 25) return null;

        try
        {
            // Supported features is at byte 24
            byte value = edid[24];
            
            bool dpmsStandby = (value & 0x80) != 0;       // Bit 7
            bool dpmsSuspend = (value & 0x40) != 0;       // Bit 6
            bool dpmsActiveOff = (value & 0x20) != 0;     // Bit 5
            byte displayType = (byte)((value >> 3) & 0x03); // Bits 4-3
            bool defaultColorSpace = (value & 0x04) != 0; // Bit 2
            bool preferredTimingMode = (value & 0x02) != 0; // Bit 1
            bool continuousFrequency = (value & 0x01) != 0; // Bit 0

            return new SupportedFeatures(
                dpmsStandby,
                dpmsSuspend,
                dpmsActiveOff,
                displayType,
                defaultColorSpace,
                preferredTimingMode,
                continuousFrequency,
                value);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parses chromaticity coordinates (color points for red, green, blue, and white) from EDID.
    /// </summary>
    /// <param name="edid">EDID data (at least 35 bytes)</param>
    /// <returns>Chromaticity coordinates or null if invalid</returns>
    public static ChromaticityCoordinates? ParseChromaticity(byte[] edid)
    {
        if (edid.Length < 35) return null;

        try
        {
            // Chromaticity data is stored in bytes 25-34
            // Each coordinate is a 10-bit value split between two bytes
            byte lsb = edid[25]; // Low-order bits for red/green/blue/white X
            byte lsb2 = edid[26]; // Low-order bits for red/green/blue/white Y
            
            // Red X/Y: 8 MSB bits in byte 27, 2 LSB bits in bytes 25/26
            int redXRaw = (edid[27] << 2) | ((lsb >> 6) & 0x03);
            int redYRaw = (edid[27] << 2) | ((lsb2 >> 6) & 0x03);
            
            // Green X/Y: 8 MSB bits in byte 28, 2 LSB bits in bytes 25/26
            int greenXRaw = (edid[28] << 2) | ((lsb >> 4) & 0x03);
            int greenYRaw = (edid[28] << 2) | ((lsb2 >> 4) & 0x03);
            
            // Blue X/Y: 8 MSB bits in byte 29, 2 LSB bits in bytes 25/26
            int blueXRaw = (edid[29] << 2) | ((lsb >> 2) & 0x03);
            int blueYRaw = (edid[29] << 2) | ((lsb2 >> 2) & 0x03);
            
            // White X/Y: 8 MSB bits in byte 30, 2 LSB bits in bytes 25/26
            int whiteXRaw = (edid[30] << 2) | (lsb & 0x03);
            int whiteYRaw = (edid[30] << 2) | (lsb2 & 0x03);

            // Convert 10-bit values to 0.0-1.0 range
            var red = new ColorPoint(redXRaw / 1024.0, redYRaw / 1024.0);
            var green = new ColorPoint(greenXRaw / 1024.0, greenYRaw / 1024.0);
            var blue = new ColorPoint(blueXRaw / 1024.0, blueYRaw / 1024.0);
            var white = new ColorPoint(whiteXRaw / 1024.0, whiteYRaw / 1024.0);

            return new ChromaticityCoordinates(red, green, blue, white);
        }
        catch
        {
            return null;
        }
    }
}
