# Chocolatey Distribution Guide for ddcswitch

This guide explains how ddcswitch is set up for Chocolatey distribution and how to maintain the package.

## 📦 What Has Been Set Up

### 1. Chocolatey Package Files

All files are located in the `chocolatey/` directory:

- **`ddcswitch.nuspec`** - Package metadata (name, version, description, dependencies)
- **`tools/chocolateyinstall.ps1`** - Installation script that downloads the ZIP from GitHub releases
- **`tools/chocolateyuninstall.ps1`** - Cleanup script for uninstallation
- **`tools/VERIFICATION.txt`** - Verification instructions with checksums for moderators
- **`README.md`** - Documentation for building and submitting packages

### 2. Automated CI/CD Integration

The GitHub Actions workflow (`.github/workflows/ci-cd.yml`) now includes a `chocolatey-package` job that:

1. ✅ Runs automatically after each release to `main` branch
2. ✅ Downloads the release ZIP from GitHub
3. ✅ Calculates SHA256 checksum
4. ✅ Updates all Chocolatey files with the new version and checksum
5. ✅ Creates the `.nupkg` package file
6. ✅ Uploads to both:
   - GitHub Actions artifacts (90-day retention)
   - GitHub Release assets (permanent)

### 3. Packaging Strategy

**Automatic Packaging** (recommended by Chocolatey community):
- Package downloads the ZIP from GitHub releases during installation
- Verifies the download using SHA256 checksum (of the ZIP file, not the binary)
- Keeps package size small (~5KB instead of ~3MB)
- Faster approval process
- Better security (GitHub's virus scanning + checksum verification)

## 🚀 Getting Started: First-Time Setup

### Step 1: Create Chocolatey Account

1. Go to https://community.chocolatey.org/
2. Click "Sign In" and create an account
3. Verify your email address

### Step 2: Test Package Locally (Optional but Recommended)

Before submitting, test the package on your local machine:

```powershell
# Install Chocolatey if not already installed
Set-ExecutionPolicy Bypass -Scope Process -Force
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))

# Navigate to the chocolatey directory
cd D:\Programming\DDCSwitch\chocolatey

# Create the CHECKSUM file (required for installation)
# Replace version with your actual version
$version = "1.0.2"
$url = "https://github.com/markdwags/ddcswitch/releases/download/v$version/ddcswitch-$version-win-x64.zip"
$tempFile = "$env:TEMP\ddcswitch-test.zip"
Invoke-WebRequest -Uri $url -OutFile $tempFile
$checksum = (Get-FileHash $tempFile -Algorithm SHA256).Hash
Set-Content -Path "tools\CHECKSUM" -Value $checksum
Write-Host "Created CHECKSUM file with: $checksum"
Remove-Item $tempFile

# Build the package manually (if you want to test before the CI creates it)
choco pack

# Test install
choco install ddcswitch -s . --force --debug --verbose

# Verify it works
ddcswitch --version
ddcswitch list

# Test uninstall
choco uninstall ddcswitch --debug --verbose
```

### Step 3: Submit First Package

After your next release to `main`:

1. **Wait for GitHub Actions to complete** (~5-10 minutes)
   - Go to https://github.com/markdwags/ddcswitch/actions
   - Check that the `chocolatey-package` job completed successfully

2. **Download the package**:
   - Go to the latest workflow run
   - Download the `chocolatey-package-X.X.X` artifact
   - Extract the `.nupkg` file

3. **Submit to Chocolatey**:
   - Go to https://community.chocolatey.org/packages/submit
   - Upload the `.nupkg` file
   - Fill in any additional information requested
   - Click "Submit"

4. **Wait for moderation**:
   - Initial review typically takes 1-7 days
   - Moderators will check:
     - Package follows guidelines
     - Installation/uninstallation works correctly
     - Checksums are correct
     - No malware/security issues
   - You'll receive email notifications about the status

5. **Address feedback** (if needed):
   - If moderators request changes, update the files locally
   - Create a new release or manually rebuild the package
   - Resubmit

## 🔄 Ongoing Maintenance: Publishing Updates

For each new version of ddcswitch:

### The Process (Manual Submission Required)

1. **Update CHANGELOG.md** with the new version in the format:
   ```markdown
   ## [1.0.3] - 2026-01-15
   
   ### Added
   - New feature
   ```

2. **Commit and push to `main`** branch

3. **GitHub Actions automatically**:
   - Extracts version from CHANGELOG.md
   - Builds the release and creates GitHub release with ZIP
   - Downloads the ZIP and calculates its SHA256 checksum
   - Replaces `__VERSION__` in nuspec
   - Creates `tools/CHECKSUM` file with the hash
   - Updates `VERIFICATION.txt` for moderators
   - Builds the `.nupkg` package
   - Uploads to GitHub release assets (permanent) and artifacts (90-day)

4. **You manually submit**:
   - Download `.nupkg` from GitHub Release assets
   - Submit at https://community.chocolatey.org/packages/submit
   - Wait for approval (usually faster after first approval)

**That's it!** No manual file editing - just update CHANGELOG.md and let CI/CD handle the rest.

### Optional: API Key Automation (After Establishing Trust)

After 2-3 successful submissions, you can automate submission by adding your Chocolatey API key:

1. **Get your API key**:
   - Go to https://community.chocolatey.org/account
   - Copy your API key

2. **Add GitHub Secret**:
   - Go to https://github.com/markdwags/ddcswitch/settings/secrets/actions
   - Add a new secret: `CHOCO_API_KEY` with your API key value

3. **Add workflow step**:
   
   Add this step to the end of the `chocolatey-package` job in `.github/workflows/ci-cd.yml`:

   ```yaml
   - name: Push to Chocolatey (if API key available)
     if: env.CHOCO_API_KEY != ''
     shell: pwsh
     run: |
       choco apikey --key $env:CHOCO_API_KEY --source https://push.chocolatey.org/
       choco push chocolatey/*.nupkg --source https://push.chocolatey.org/
     env:
       CHOCO_API_KEY: ${{ secrets.CHOCO_API_KEY }}
   ```

4. **Now releases are fully automatic**! 🎉
   - Just update CHANGELOG.md and push
   - Everything else happens automatically

## 📋 Checklist for Each Release

- [ ] Update version in `CHANGELOG.md`
- [ ] Push to `main` branch
- [ ] Wait for GitHub Actions to complete
- [ ] Download `.nupkg` from release assets or artifacts
- [ ] Submit to Chocolatey (if not using API automation)
- [ ] Monitor for approval/feedback
- [ ] Announce on social media/Discord/etc. (optional)

## 🔍 Troubleshooting

### Package Build Fails in CI

Check the GitHub Actions logs. Common issues:
- Version number format in CHANGELOG.md
- GitHub release not available yet (the job waits/retries)

### Local Testing Fails

```powershell
# Clean up previous attempts
choco uninstall ddcswitch --force
Remove-Item "$env:ChocolateyInstall\lib\ddcswitch" -Recurse -Force -ErrorAction SilentlyContinue

# Try again with verbose output
choco install ddcswitch -s . --force --debug --verbose
```

### Checksum Mismatch

The CI automatically calculates checksums, but if you need to manually verify:

```powershell
$version = "1.0.2"  # Update this
$url = "https://github.com/markdwags/ddcswitch/releases/download/v$version/ddcswitch-$version-win-x64.zip"
$tempFile = "$env:TEMP\ddcswitch.zip"
Invoke-WebRequest -Uri $url -OutFile $tempFile
(Get-FileHash $tempFile -Algorithm SHA256).Hash
```

### Moderator Rejection

Common reasons and fixes:
- **Download URL not accessible**: Wait longer or manually verify the release
- **Checksum incorrect**: Recalculate and update (CI should do this automatically)
- **Icon missing**: The nuspec references an icon, ensure it exists or remove the iconUrl
- **Description too short**: Expand the description in ddcswitch.nuspec

## 📚 Resources

- **Chocolatey Package Guidelines**: https://docs.chocolatey.org/en-us/community-repository/moderation/package-validator
- **Package Creation**: https://docs.chocolatey.org/en-us/create/create-packages
- **Automatic Packages**: https://docs.chocolatey.org/en-us/guides/create/create-packages-quick-start#automatic-packages
- **Moderator Review Process**: https://docs.chocolatey.org/en-us/community-repository/moderation/package-validator-rules

## 🎯 Quick Reference

### User Installation (Once Published)

```powershell
choco install ddcswitch
```

### User Update

```powershell
choco upgrade ddcswitch
```

### User Uninstall

```powershell
choco uninstall ddcswitch
```

### Package Repository

Once approved, your package will be available at:
https://community.chocolatey.org/packages/ddcswitch

## ✅ Next Steps

1. **Wait for your next release** (when you update CHANGELOG.md to a new version and push to main)
2. **Download the `.nupkg` file** from GitHub Release assets (permanent) or GitHub Actions artifacts (90-day)
3. **Submit to Chocolatey** at https://community.chocolatey.org/packages/submit for initial approval
4. **Consider setting up API key automation** after 2-3 successful submissions (see Optional section above)

That's it! The infrastructure is ready. You just need to go through the initial submission process with Chocolatey moderators.

---

## 🎓 Technical Details

### How Version and Checksum Are Passed

**Version** (no PowerShell file modification):
1. CI/CD reads `[1.0.3]` from CHANGELOG.md
2. Replaces `__VERSION__` in `ddcswitch.nuspec`
3. When package installs, Chocolatey automatically sets `$env:chocolateyPackageVersion = "1.0.3"`
4. Install script reads: `$version = $env:chocolateyPackageVersion`

**Checksum** (no PowerShell file modification):
1. CI/CD downloads the release ZIP
2. Calculates SHA256: `ABC123DEF456...`
3. Creates `tools/CHECKSUM` file containing the hash
4. Install script reads: `$checksum64 = Get-Content $checksumFile`

**Result**: PowerShell files are 100% static code with zero placeholders!

