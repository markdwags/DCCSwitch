$ErrorActionPreference = 'Stop'

$packageName = 'ddcswitch'
$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"

# Remove the executable (Chocolatey handles shim removal automatically)
Remove-Item "$toolsDir\ddcswitch.exe" -ErrorAction SilentlyContinue -Force

Write-Host "$packageName has been uninstalled successfully."

