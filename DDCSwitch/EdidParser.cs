using System.Text;

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
/// Parses EDID (Extended Display Identification Data) blocks to extract monitor information.
/// </summary>
public static class EdidParser
{
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
