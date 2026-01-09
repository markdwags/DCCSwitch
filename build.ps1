Write-Host "========================================"
Write-Host "Building ddcswitch with NativeAOT"
Write-Host "========================================"
Write-Host ""

# Clean previous build
Write-Host "Cleaning previous build..."
dotnet clean DDCSwitch\DDCSwitch.csproj -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Clean failed" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Build with NativeAOT
Write-Host "Building with NativeAOT..."
dotnet publish DDCSwitch\DDCSwitch.csproj -c Release -r win-x64 --self-contained
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Create dist folder
Write-Host "Creating dist folder..."
if (-not (Test-Path "dist")) {
    New-Item -ItemType Directory -Path "dist" | Out-Null
}

# Copy the NativeAOT executable
Write-Host "Copying executable to dist folder..."
Copy-Item -Path "DDCSwitch\bin\Release\net10.0\win-x64\publish\ddcswitch.exe" -Destination "dist\ddcswitch.exe" -Force
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to copy executable" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "========================================"
Write-Host "Build completed successfully!"
Write-Host "Output: dist\ddcswitch.exe"
Write-Host "========================================"

# Display file size
$fileInfo = Get-Item "dist\ddcswitch.exe"
$sizeMB = [math]::Round($fileInfo.Length / 1MB, 2)
Write-Host "File size: $sizeMB MB"
