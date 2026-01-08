using System.Text.Json.Serialization;

namespace DDCSwitch;

[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(ListMonitorsResponse))]
[JsonSerializable(typeof(MonitorInfo))]
[JsonSerializable(typeof(GetVcpResponse))]
[JsonSerializable(typeof(SetVcpResponse))]
[JsonSerializable(typeof(VcpScanResponse))]
[JsonSerializable(typeof(VcpFeatureInfo))]
[JsonSerializable(typeof(VcpFeatureType))]
[JsonSerializable(typeof(MonitorReference))]
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
    string? Contrast = null);

internal record MonitorReference(int Index, string Name, string DeviceName, bool IsPrimary = false);

