namespace DDCSwitch;

/// <summary>
/// Common VCP input source codes based on MCCS specification
/// </summary>
public static class InputSource
{
    // VCP Code for Input Source Select
    public const byte VcpInputSource = 0x60;

    // Common input source values
    private const uint Analog1 = 0x01;
    private const uint Analog2 = 0x02;
    private const uint DVI1 = 0x03;
    private const uint DVI2 = 0x04;
    private const uint CompositeVideo1 = 0x05;
    private const uint CompositeVideo2 = 0x06;
    private const uint SVideo1 = 0x07;
    private const uint SVideo2 = 0x08;
    private const uint Tuner1 = 0x09;
    private const uint Tuner2 = 0x0A;
    private const uint Tuner3 = 0x0B;
    private const uint ComponentVideo1 = 0x0C;
    private const uint ComponentVideo2 = 0x0D;
    private const uint ComponentVideo3 = 0x0E;
    private const uint DisplayPort1 = 0x0F;
    private const uint DisplayPort2 = 0x10;
    private const uint HDMI1 = 0x11;
    private const uint HDMI2 = 0x12;

    /// <summary>
    /// Get a friendly name for an input source code
    /// </summary>
    public static string GetName(uint code)
    {
        return code switch
        {
            Analog1 => "Analog1 (VGA1)",
            Analog2 => "Analog2 (VGA2)",
            DVI1 => "DVI1",
            DVI2 => "DVI2",
            CompositeVideo1 => "CompositeVideo1",
            CompositeVideo2 => "CompositeVideo2",
            SVideo1 => "SVideo1",
            SVideo2 => "SVideo2",
            Tuner1 => "Tuner1",
            Tuner2 => "Tuner2",
            Tuner3 => "Tuner3",
            ComponentVideo1 => "ComponentVideo1",
            ComponentVideo2 => "ComponentVideo2",
            ComponentVideo3 => "ComponentVideo3",
            DisplayPort1 => "DisplayPort1 (DP1)",
            DisplayPort2 => "DisplayPort2 (DP2)",
            HDMI1 => "HDMI1",
            HDMI2 => "HDMI2",
            _ => $"Unknown (0x{code:X2})"
        };
    }

    /// <summary>
    /// Try to parse an input source name or code to a VCP value
    /// </summary>
    public static bool TryParse(string input, out uint value)
    {
        value = 0;
        // Try direct hex or numeric parsing first
        if (input.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return uint.TryParse(input[2..], System.Globalization.NumberStyles.HexNumber, null, out value);
        }

        if (uint.TryParse(input, out value))
        {
            return true;
        }

        // Try friendly name matching (case-insensitive)
        var normalized = input.ToLowerInvariant().Replace(" ", "").Replace("-", "").Replace("_", "");
        value = normalized switch
        {
            "analog1" or "vga1" or "vga" => Analog1,
            "analog2" or "vga2" => Analog2,
            "dvi1" or "dvi" => DVI1,
            "dvi2" => DVI2,
            "compositevideo1" or "composite1" => CompositeVideo1,
            "compositevideo2" or "composite2" => CompositeVideo2,
            "svideo1" or "svideo" => SVideo1,
            "svideo2" => SVideo2,
            "tuner1" or "tuner" => Tuner1,
            "tuner2" => Tuner2,
            "tuner3" => Tuner3,
            "componentvideo1" or "component1" => ComponentVideo1,
            "componentvideo2" or "component2" => ComponentVideo2,
            "componentvideo3" or "component3" => ComponentVideo3,
            "displayport1" or "dp1" or "dp" => DisplayPort1,
            "displayport2" or "dp2" => DisplayPort2,
            "hdmi1" or "hdmi" => HDMI1,
            "hdmi2" => HDMI2,
            _ => 0
        };
        return value != 0;
    }
}