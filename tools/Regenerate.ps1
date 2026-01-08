# Regenerate VCP Feature Code
# Run this after editing VcpFeatureData.json

Write-Host "=" * 60
Write-Host "VCP Feature Code Generator"
Write-Host "=" * 60

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptPath

Push-Location $projectRoot

try {
    Write-Host "`nGenerating VcpFeature.Generated.cs from VcpFeatureData.json..."
    python "$scriptPath\GenerateVcpFeatures.py"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n✓ Generation successful!" -ForegroundColor Green
        Write-Host "`nNext steps:"
        Write-Host "  1. Review the changes in VcpFeature.Generated.cs"
        Write-Host "  2. Build the project: dotnet build"
        Write-Host "  3. Test the changes"
    } else {
        Write-Host "`n✗ Generation failed!" -ForegroundColor Red
        exit 1
    }
} finally {
    Pop-Location
}

