using System.Text.Json.Serialization;

namespace DDCSwitch;

[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(ListMonitorsResponse))]
[JsonSerializable(typeof(MonitorInfo))]
[JsonSerializable(typeof(GetVcpResponse))]
[JsonSerializable(typeof(SetVcpResponse))]
[JsonSerializable(typeof(ToggleInputResponse))]
[JsonSerializable(typeof(VcpScanResponse))]
[JsonSerializable(typeof(VcpFeatureInfo))]
[JsonSerializable(typeof(VcpFeatureType))]
[JsonSerializable(typeof(MonitorReference))]
[JsonSerializable(typeof(MonitorInfoResponse))]
[JsonSerializable(typeof(EdidInfo))]
[JsonSerializable(typeof(ParsedEdidInfo))]
[JsonSerializable(typeof(RegistryEdidEntry))]
[JsonSerializable(typeof(EdidColorInfo))]
[JsonSerializable(typeof(FeaturesInfo))]
[JsonSerializable(typeof(ChromaticityInfo))]
[JsonSerializable(typeof(ColorPointInfo))]
[JsonSerializable(typeof(MonitorIdentityInfo))]
[JsonSerializable(typeof(VcpVersionInfo))]
[JsonSerializable(typeof(VcpTestResult))]
[JsonSerializable(typeof(VcpCapabilityInfo))]
[JsonSerializable(typeof(DdcCiStatus))]
[JsonSerializable(typeof(HardwareInfo))]
[JsonSerializable(typeof(ConnectionType))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]

internal partial class JsonContext : JsonSerializerContext
{
}

// Response models
internal record ErrorResponse(bool Success, string Error, MonitorReference? Monitor = null);
internal record ListMonitorsResponse(bool Success, List<MonitorInfo>? Monitors = null, string? Error = null);
internal record GetVcpResponse(bool Success, MonitorReference Monitor, string FeatureName, uint RawValue, uint MaxValue, uint? PercentageValue = null, string? ErrorMessage = null);
internal record SetVcpResponse(bool Success, MonitorReference Monitor, string FeatureName, uint SetValue, uint? PercentageValue = null, string? ErrorMessage = null);
internal record ToggleInputResponse(
    bool Success, 
    MonitorReference Monitor, 
    string FromInput, 
    string ToInput, 
    uint FromInputCode, 
    uint ToInputCode,
    string? Warning = null,
    string? ErrorMessage = null);
internal record VcpScanResponse(bool Success, MonitorReference Monitor, List<VcpFeatureInfo> Features, string? ErrorMessage = null);

// Data models
internal record MonitorInfo(
    int Index,
    string Name,
    string DeviceName,
    bool IsPrimary,
    string? CurrentInput,
    string? CurrentInputCode,
    string Status,
    string? Brightness = null,
    string? Contrast = null,
    string? ManufacturerId = null,
    string? ManufacturerName = null,
    string? ModelName = null,
    string? SerialNumber = null,
    int? ProductCode = null,
    int? ManufactureYear = null,
    int? ManufactureWeek = null);

internal record MonitorReference(int Index, string Name, string DeviceName, bool IsPrimary = false);

internal record MonitorInfoResponse(
    bool Success,
    MonitorReference Monitor,
    string Status,
    string? CurrentInput = null,
    string? CurrentInputCode = null,
    EdidInfo? Edid = null,
    string? ErrorMessage = null);

internal record EdidInfo(
    byte? VersionMajor,
    byte? VersionMinor,
    string? ManufacturerId,
    string? ManufacturerName,
    string? ModelName,
    string? SerialNumber,
    string? ProductCode,
    int? ManufactureYear,
    int? ManufactureWeek,
    bool? IsDigitalInput,
    string? VideoInputType,
    FeaturesInfo? Features,
    ChromaticityInfo? Chromaticity);

internal record FeaturesInfo(
    string DisplayType,
    bool DpmsStandby,
    bool DpmsSuspend,
    bool DpmsActiveOff,
    bool DefaultColorSpace,
    bool PreferredTimingMode,
    bool ContinuousFrequency);

internal record ChromaticityInfo(
    ColorPointInfo Red,
    ColorPointInfo Green,
    ColorPointInfo Blue,
    ColorPointInfo White);

internal record ColorPointInfo(double X, double Y);

