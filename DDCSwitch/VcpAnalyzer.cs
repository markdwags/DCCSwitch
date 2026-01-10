namespace DDCSwitch;

/// <summary>
/// DDC/CI communication status levels
/// </summary>
public enum DdcCiStatus
{
    /// <summary>
    /// Monitor responds to all tested VCP codes
    /// </summary>
    FullyResponsive,
    
    /// <summary>
    /// Monitor responds to some but not all VCP codes
    /// </summary>
    PartiallyResponsive,
    
    /// <summary>
    /// Monitor does not respond to any VCP codes
    /// </summary>
    NonResponsive,
    
    /// <summary>
    /// Communication error occurred during testing
    /// </summary>
    CommunicationError,
    
    /// <summary>
    /// Status could not be determined
    /// </summary>
    Unknown
}

/// <summary>
/// VCP version information
/// </summary>
public record VcpVersionInfo(
    byte MajorVersion,
    byte MinorVersion,
    bool IsValid
)
{
    public override string ToString() => IsValid ? $"{MajorVersion}.{MinorVersion}" : "Unknown";
}

/// <summary>
/// Result of testing a specific VCP code
/// </summary>
public record VcpTestResult(
    byte VcpCode,
    string FeatureName,
    bool Success,
    uint CurrentValue,
    uint MaxValue,
    string? ErrorMessage
);

/// <summary>
/// Comprehensive VCP capability information
/// </summary>
public record VcpCapabilityInfo(
    VcpVersionInfo Version,
    string? ControllerManufacturer,
    string? FirmwareVersion,
    Dictionary<byte, VcpFeatureInfo> SupportedFeatures,
    bool SupportsNullResponse,
    DdcCiStatus DdcCiStatus,
    List<VcpTestResult> TestResults
);

/// <summary>
/// Provides comprehensive VCP analysis and DDC/CI capability testing
/// </summary>
public static class VcpAnalyzer
{
    // VCP codes for comprehensive testing
    private static readonly byte[] TestVcpCodes = new byte[]
    {
        0x10, // Brightness
        0x12, // Contrast
        0x60, // Input Source
        0x62, // Audio Volume
        0x8D, // Audio Mute
        0xDF, // VCP Version
        0xC2, // Firmware Level
        0xC4, // Controller Manufacturer
        0xC8  // Controller Version
    };

    // Retry configuration
    private const int MaxRetries = 3;
    private const int RetryDelayMs = 50;

    /// <summary>
    /// Performs comprehensive analysis of monitor VCP capabilities
    /// </summary>
    /// <param name="monitor">Monitor to analyze</param>
    /// <returns>Comprehensive VCP capability information</returns>
    public static VcpCapabilityInfo AnalyzeCapabilities(Monitor monitor)
    {
        if (monitor == null)
        {
            return new VcpCapabilityInfo(
                new VcpVersionInfo(0, 0, false),
                null,
                null,
                new Dictionary<byte, VcpFeatureInfo>(),
                false,
                DdcCiStatus.Unknown,
                new List<VcpTestResult>()
            );
        }

        // Get VCP version information
        var vcpVersion = GetVcpVersion(monitor);

        // Get controller information via DDC/CI
        var ddcCiIdentity = DdcCiMonitorIdentifier.GetIdentityViaDdcCi(monitor);
        string? controllerManufacturer = ddcCiIdentity?.ControllerManufacturer;
        string? firmwareVersion = ddcCiIdentity?.FirmwareLevel;

        // Perform comprehensive DDC/CI testing
        var ddcCiStatus = TestDdcCiComprehensive(monitor);

        // Test multiple VCP codes for responsiveness
        var testResults = new List<VcpTestResult>();
        bool multipleVcpSuccess = TestMultipleVcpCodes(monitor, TestVcpCodes, testResults);

        // Test null response support
        bool supportsNullResponse = TestNullResponseSupport(monitor);

        // Get supported features using existing scan functionality
        var supportedFeatures = monitor.ScanVcpFeatures();

        return new VcpCapabilityInfo(
            vcpVersion,
            controllerManufacturer,
            firmwareVersion,
            supportedFeatures,
            supportsNullResponse,
            ddcCiStatus,
            testResults
        );
    }

    /// <summary>
    /// Determines VCP version supported by the monitor
    /// </summary>
    /// <param name="monitor">Monitor to query</param>
    /// <returns>VCP version information</returns>
    public static VcpVersionInfo GetVcpVersion(Monitor monitor)
    {
        if (monitor == null)
            return new VcpVersionInfo(0, 0, false);

        try
        {
            // VCP Version is at code 0xDF
            if (monitor.TryGetVcpFeature(0xDF, out uint value, out _))
            {
                // VCP version is typically encoded as major.minor in BCD or binary
                byte major = (byte)((value >> 8) & 0xFF);
                byte minor = (byte)(value & 0xFF);

                // Validate reasonable version numbers
                if (major >= 1 && major <= 10 && minor <= 99)
                {
                    return new VcpVersionInfo(major, minor, true);
                }

                // Try BCD decoding
                if ((major & 0xF0) <= 0x90 && (major & 0x0F) <= 0x09 &&
                    (minor & 0xF0) <= 0x90 && (minor & 0x0F) <= 0x09)
                {
                    byte majorBcd = (byte)(((major >> 4) * 10) + (major & 0x0F));
                    byte minorBcd = (byte)(((minor >> 4) * 10) + (minor & 0x0F));
                    
                    if (majorBcd >= 1 && majorBcd <= 10 && minorBcd <= 99)
                    {
                        return new VcpVersionInfo(majorBcd, minorBcd, true);
                    }
                }
            }

            return new VcpVersionInfo(0, 0, false);
        }
        catch
        {
            return new VcpVersionInfo(0, 0, false);
        }
    }

    /// <summary>
    /// Performs comprehensive DDC/CI testing to determine communication status
    /// </summary>
    /// <param name="monitor">Monitor to test</param>
    /// <returns>DDC/CI communication status</returns>
    public static DdcCiStatus TestDdcCiComprehensive(Monitor monitor)
    {
        if (monitor == null)
            return DdcCiStatus.Unknown;

        try
        {
            var testResults = new List<VcpTestResult>();
            bool anySuccess = TestMultipleVcpCodes(monitor, TestVcpCodes, testResults);

            if (!anySuccess)
            {
                return DdcCiStatus.NonResponsive;
            }

            // Count successful tests
            int successCount = testResults.Count(r => r.Success);
            int totalTests = testResults.Count;

            // Determine status based on success ratio
            if (successCount == totalTests)
            {
                return DdcCiStatus.FullyResponsive;
            }
            else if (successCount > 0)
            {
                return DdcCiStatus.PartiallyResponsive;
            }
            else
            {
                return DdcCiStatus.NonResponsive;
            }
        }
        catch
        {
            return DdcCiStatus.CommunicationError;
        }
    }

    /// <summary>
    /// Tests multiple VCP codes to determine monitor responsiveness
    /// </summary>
    /// <param name="monitor">Monitor to test</param>
    /// <param name="testCodes">Array of VCP codes to test</param>
    /// <param name="results">List to store test results (optional)</param>
    /// <returns>True if any VCP code responded successfully</returns>
    public static bool TestMultipleVcpCodes(Monitor monitor, byte[] testCodes, List<VcpTestResult>? results = null)
    {
        if (monitor == null || testCodes == null)
            return false;

        bool anySuccess = false;

        foreach (byte vcpCode in testCodes)
        {
            var testResult = TestSingleVcpCode(monitor, vcpCode);
            results?.Add(testResult);

            if (testResult.Success)
            {
                anySuccess = true;
            }
        }

        return anySuccess;
    }

    /// <summary>
    /// Tests a single VCP code with retry logic for timing-sensitive monitors
    /// </summary>
    /// <param name="monitor">Monitor to test</param>
    /// <param name="vcpCode">VCP code to test</param>
    /// <returns>Test result</returns>
    private static VcpTestResult TestSingleVcpCode(Monitor monitor, byte vcpCode)
    {
        string featureName = FeatureResolver.GetFeatureByCode(vcpCode).Name;

        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                if (monitor.TryGetVcpFeature(vcpCode, out uint currentValue, out uint maxValue, out int errorCode))
                {
                    return new VcpTestResult(
                        vcpCode,
                        featureName,
                        true,
                        currentValue,
                        maxValue,
                        null
                    );
                }

                // If this is not the last attempt, wait before retrying
                if (attempt < MaxRetries - 1)
                {
                    Thread.Sleep(RetryDelayMs);
                }
            }
            catch (Exception ex)
            {
                if (attempt == MaxRetries - 1)
                {
                    return new VcpTestResult(
                        vcpCode,
                        featureName,
                        false,
                        0,
                        0,
                        ex.Message
                    );
                }

                Thread.Sleep(RetryDelayMs);
            }
        }

        return new VcpTestResult(
            vcpCode,
            featureName,
            false,
            0,
            0,
            "Failed after multiple retries"
        );
    }

    /// <summary>
    /// Tests DDC null response behavior for unsupported features
    /// </summary>
    /// <param name="monitor">Monitor to test</param>
    /// <returns>True if monitor supports null responses for unsupported features</returns>
    private static bool TestNullResponseSupport(Monitor monitor)
    {
        if (monitor == null)
            return false;

        try
        {
            // Test with a VCP code that's unlikely to be supported (0xFF)
            // A monitor that supports null responses should return false without error
            // A monitor that doesn't support null responses might hang or return an error
            
            var startTime = DateTime.UtcNow;
            bool result = monitor.TryGetVcpFeature(0xFF, out _, out _);
            var elapsed = DateTime.UtcNow - startTime;

            // If the call completed quickly (< 100ms), the monitor likely supports null responses
            // If it took longer, the monitor might not handle unsupported codes gracefully
            return elapsed.TotalMilliseconds < 100;
        }
        catch
        {
            // If an exception occurred, the monitor doesn't handle null responses well
            return false;
        }
    }

    /// <summary>
    /// Gets a human-readable description of the DDC/CI status
    /// </summary>
    /// <param name="status">DDC/CI status</param>
    /// <returns>Human-readable description</returns>
    public static string GetStatusDescription(DdcCiStatus status)
    {
        return status switch
        {
            DdcCiStatus.FullyResponsive => "Fully responsive to DDC/CI commands",
            DdcCiStatus.PartiallyResponsive => "Partially responsive to DDC/CI commands",
            DdcCiStatus.NonResponsive => "Does not respond to DDC/CI commands",
            DdcCiStatus.CommunicationError => "Communication error during DDC/CI testing",
            DdcCiStatus.Unknown => "DDC/CI status unknown",
            _ => "Unknown status"
        };
    }

    /// <summary>
    /// Gets a summary of VCP test results
    /// </summary>
    /// <param name="testResults">List of test results</param>
    /// <returns>Summary string</returns>
    public static string GetTestResultsSummary(List<VcpTestResult> testResults)
    {
        if (testResults == null || testResults.Count == 0)
            return "No tests performed";

        int successCount = testResults.Count(r => r.Success);
        int totalCount = testResults.Count;

        return $"{successCount}/{totalCount} VCP codes responded successfully";
    }
}