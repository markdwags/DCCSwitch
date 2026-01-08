namespace DDCSwitch;

/// <summary>
/// Defines the access type for a VCP feature
/// </summary>
public enum VcpFeatureType
{
    /// <summary>
    /// Feature can only be read
    /// </summary>
    ReadOnly,
    
    /// <summary>
    /// Feature can only be written
    /// </summary>
    WriteOnly,
    
    /// <summary>
    /// Feature can be both read and written
    /// </summary>
    ReadWrite
}

/// <summary>
/// Defines the functional category of a VCP feature
/// </summary>
public enum VcpFeatureCategory
{
    /// <summary>
    /// Image adjustment features (brightness, contrast, sharpness, etc.)
    /// </summary>
    ImageAdjustment,
    
    /// <summary>
    /// Color control features (RGB gains, color temperature, etc.)
    /// </summary>
    ColorControl,
    
    /// <summary>
    /// Geometry features (position, size, pincushion - mainly CRT)
    /// </summary>
    Geometry,
    
    /// <summary>
    /// Audio features (volume, mute, balance, etc.)
    /// </summary>
    Audio,
    
    /// <summary>
    /// Preset and factory features (restore defaults, degauss, etc.)
    /// </summary>
    Preset,
    
    /// <summary>
    /// Miscellaneous features that don't fit other categories
    /// </summary>
    Miscellaneous
}

/// <summary>
/// Represents a VCP (Virtual Control Panel) feature with its properties.
/// This is a partial class - feature definitions are in VcpFeature.Generated.cs
/// which is auto-generated from VcpFeatureData.json.
/// </summary>
public partial class VcpFeature
{
    public byte Code { get; }
    public string Name { get; }
    public string Description { get; }
    public VcpFeatureType Type { get; }
    public VcpFeatureCategory Category { get; }
    public bool SupportsPercentage { get; }
    public string[] Aliases { get; }

    public VcpFeature(byte code, string name, string description, VcpFeatureType type, VcpFeatureCategory category, bool supportsPercentage, params string[] aliases)
    {
        Code = code;
        Name = name;
        Description = description;
        Type = type;
        Category = category;
        SupportsPercentage = supportsPercentage;
        Aliases = aliases;
    }

    // Legacy constructor for backward compatibility
    public VcpFeature(byte code, string name, VcpFeatureType type, bool supportsPercentage)
        : this(code, name, $"VCP feature {name} (0x{code:X2})", type, VcpFeatureCategory.Miscellaneous, supportsPercentage)
    {
    }

    public override string ToString()
    {
        return $"{Name} (0x{Code:X2})";
    }

    public override bool Equals(object? obj)
    {
        return obj is VcpFeature other && Code == other.Code;
    }

    public override int GetHashCode()
    {
        return Code.GetHashCode();
    }
}

/// <summary>
/// Information about a VCP feature discovered during scanning
/// </summary>
public record VcpFeatureInfo(
    byte Code,
    string Name,
    string Description,
    VcpFeatureType Type,
    VcpFeatureCategory Category,
    uint CurrentValue,
    uint MaxValue,
    bool IsSupported
);

