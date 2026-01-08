# Chocolatey Package for ddcswitch

This directory contains the files needed to create a Chocolatey package for ddcswitch.

## Structure

- `ddcswitch.nuspec` - Package metadata and configuration
- `tools/chocolateyinstall.ps1` - Installation script (downloads from GitHub releases)
- `tools/chocolateyuninstall.ps1` - Uninstallation script
- `tools/VERIFICATION.txt` - Verification instructions and checksums

## Building the Package Locally

**Note**: The version and checksum are automatically populated by the CI/CD pipeline from `CHANGELOG.md`.

- **Version**: Set in `ddcswitch.nuspec` as `__VERSION__` placeholder, then passed to PowerShell via `$env:chocolateyPackageVersion`
- **Checksum**: Set in `chocolateyinstall.ps1` as `__CHECKSUM__` placeholder

1. Install Chocolatey if you haven't already:
   ```powershell
   Set-ExecutionPolicy Bypass -Scope Process -Force; [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
   ```

2. If building manually (for testing), replace the placeholders:
   - Replace `__VERSION__` in `ddcswitch.nuspec` (e.g., `1.0.2`)
   - Replace `__CHECKSUM__` in `tools/chocolateyinstall.ps1` with the SHA256 checksum

3. Build the package:
   ```powershell
   cd chocolatey
   choco pack
   ```

4. Test the package locally:
   ```powershell
   choco install ddcswitch -s . --force
   ddcswitch --version
   choco uninstall ddcswitch
   ```

## Getting the Checksum

After a GitHub release is created, calculate the SHA256 checksum:

```powershell
$version = "1.0.2"  # Update this
$url = "https://github.com/markdwags/DDCSwitch/releases/download/v$version/ddcswitch-$version-win-x64.zip"
$tempFile = "$env:TEMP\ddcswitch.zip"
Invoke-WebRequest -Uri $url -OutFile $tempFile
$hash = Get-FileHash $tempFile -Algorithm SHA256
$checksum = $hash.Hash
Write-Host "SHA256: $checksum"

# Create the CHECKSUM file
Set-Content chocolatey\tools\CHECKSUM $checksum -NoNewline

Remove-Item $tempFile
```

## Automated Package Creation

The GitHub Actions workflow (`.github/workflows/ci-cd.yml`) automatically:
1. Detects the version from `CHANGELOG.md` (e.g., `[1.0.2]`)
2. Calculates the SHA256 checksum of the release ZIP
3. Replaces `__VERSION__` in `ddcswitch.nuspec` (Chocolatey passes this to scripts via `$env:chocolateyPackageVersion`)
4. Replaces `__CHECKSUM__` in `chocolateyinstall.ps1`
5. Creates the `.nupkg` file
6. Uploads it as an artifact and to the GitHub release

**No manual version updates needed!** Just update `CHANGELOG.md` with the new version.

The version flows naturally: `CHANGELOG.md` → `nuspec` → `$env:chocolateyPackageVersion` → PowerShell script.
3. Creates the `.nupkg` file
4. Uploads it as an artifact

## Submitting to Chocolatey

### First-Time Submission (Manual)

1. Create a Chocolatey account at https://community.chocolatey.org/
2. Download the `.nupkg` artifact from GitHub Actions
3. Submit via https://community.chocolatey.org/packages/submit
4. Wait for moderation and approval (typically 1-7 days)
5. Address any feedback from moderators

### Optional: Automated Submission (After Establishing Trust)

After 2-3 successful manual submissions, you can enable automatic pushing:

1. Get your API key from https://community.chocolatey.org/account
2. Add it as a GitHub Actions secret: `CHOCO_API_KEY`
3. Add a step to the workflow to automatically push packages:
   ```yaml
   - name: Push to Chocolatey
     shell: pwsh
     run: |
       choco apikey --key $env:CHOCO_API_KEY --source https://push.chocolatey.org/
       choco push chocolatey/*.nupkg --source https://push.chocolatey.org/
     env:
       CHOCO_API_KEY: ${{ secrets.CHOCO_API_KEY }}
   ```

**Note**: Automatic pushing is not recommended for initial submissions as they require human moderation.

## Package Maintenance

For each new release:

1. **Update `CHANGELOG.md`** with the new version:
   ```markdown
   ## [1.0.3] - 2026-01-15
   
   ### Added
   - New feature
   ```

2. **Push to `main` branch** - triggers the workflow

3. **Wait for GitHub Actions** (~5-10 minutes):
   - Builds ddcswitch.exe
   - Creates GitHub release with ZIP
   - Downloads ZIP and calculates SHA256 checksum
   - Creates Chocolatey package (`.nupkg`)
   - Uploads to GitHub release assets and artifacts

4. **Download the `.nupkg` file** from GitHub release assets

5. **Manually submit** to https://community.chocolatey.org/packages/submit

6. **Wait for approval** (faster after first approval)

**That's it!** The version is automatically sourced from `CHANGELOG.md` - no manual file editing needed.

**The version is automatically detected from CHANGELOG.md - no manual file updates needed!**

## Resources

- [Chocolatey Package Creation Guide](https://docs.chocolatey.org/en-us/create/create-packages)
- [Package Guidelines](https://docs.chocolatey.org/en-us/community-repository/moderation/package-validator)
- [Submitting Packages](https://docs.chocolatey.org/en-us/community-repository/maintainers/package-submission)

