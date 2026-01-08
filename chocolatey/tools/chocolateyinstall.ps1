$ErrorActionPreference = 'Stop'

$packageName = 'ddcswitch'
$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"

# Version is automatically provided by Chocolatey from the nuspec
$version = $env:chocolateyPackageVersion
$url64 = "https://github.com/markdwags/ddcswitch/releases/download/v$version/ddcswitch-$version-win-x64.zip"

# Checksum is stored in a separate file created during package build
$checksumFile = Join-Path $toolsDir "CHECKSUM"
$checksum64 = Get-Content $checksumFile -Raw
$checksum64 = $checksum64.Trim()
$checksumType64 = 'sha256'

$packageArgs = @{
  packageName    = $packageName
  unzipLocation  = $toolsDir
  url64bit       = $url64
  checksum64     = $checksum64
  checksumType64 = $checksumType64
}

Install-ChocolateyZipPackage @packageArgs

# The ZIP contains the executable at the root
# Chocolatey will automatically create a shim for ddcswitch.exe

