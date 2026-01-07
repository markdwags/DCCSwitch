# GitHub Actions Workflows

This directory contains GitHub Actions workflows for automated building and releasing of DDCSwitch.

## Workflows

### 🚀 ci-cd.yml - Continuous Integration and Deployment

**Triggers:**
- Pull requests to `main` branch
- Pushes to any branch
- Push of version tags (e.g., `v1.0.0`)
- Manual workflow dispatch

**What it does:**

#### Build Job (runs on all triggers)
1. Builds the project in Debug configuration
2. Builds the project in Release configuration with NativeAOT
3. Verifies the executable was created successfully
4. Shows the executable size
5. Uploads the build artifact for 7 days

#### Release Job (only runs on pushes to `main`)
1. Builds the NativeAOT executable for Windows x64
2. Extracts version number from `CHANGELOG.md`
3. Creates a release package with:
   - `DDCSwitch.exe` (NativeAOT compiled)
   - `README.md`
   - `LICENSE`
   - `EXAMPLES.md`
   - `CHANGELOG.md`
4. Creates a ZIP archive: `DDCSwitch-{version}-win-x64.zip`
5. Generates release notes from the CHANGELOG
6. Creates and pushes a version tag (if it doesn't exist)
7. Creates a GitHub Release with the artifacts
8. Uploads build artifacts for 30 days

**Requirements:**
- The first version in `CHANGELOG.md` must follow the format: `## [X.Y.Z] - YYYY-MM-DD`
- Repository must have `contents: write` permissions (already configured)

**Benefits of consolidation:**
- Single workflow file to maintain
- Build always runs before release (ensures quality)
- Automatic release creation on every push to `main`
- PR and feature branch testing still works
- No duplicate build steps

## Usage

### Automatic Release on Merge to Main

1. Update `CHANGELOG.md` with the new version:
   ```markdown
   ## [1.1.0] - 2026-01-15
   
   ### Added
   - New feature description
   ```

2. Commit and push to a feature branch

3. Create a pull request (this triggers `build.yml`)

4. Once merged to `main`, `release.yml` automatically:
   - Builds the release
   - Creates tag `v1.1.0`
   - Creates GitHub Release with artifacts

### Manual Release Trigger

You can also manually trigger a release from the GitHub Actions tab:
1. Go to Actions → Build and Release
2. Click "Run workflow"
3. Select the `main` branch
4. Click "Run workflow"

## Version Management

The version is automatically extracted from `CHANGELOG.md`. Ensure the latest version entry follows this format:

```markdown
## [1.2.3] - 2026-01-15
```

The workflow will:
- Extract version `1.2.3`
- Create tag `v1.2.3`
- Create release named "DDCSwitch 1.2.3"

## Build Artifacts

### Release Artifacts (30 days retention)
- Full release package with docs
- Available in the workflow run summary

### Build Artifacts (7 days retention)
- Executable only from PR/branch builds
- Useful for testing before merge

## Troubleshooting

### "Could not find version in CHANGELOG.md"
- Ensure `CHANGELOG.md` has a version entry matching `[X.Y.Z]`
- The version should be the first one in the file

### "Tag already exists"
- The workflow checks if the tag exists and skips tag creation
- It will still create/update the release

### NativeAOT Build Fails
- Ensure the project has C++ build tools available
- GitHub's `windows-latest` runners include these by default

### Release Not Created
- Check the workflow logs in Actions tab
- Ensure `GITHUB_TOKEN` has write permissions
- Verify the version format in CHANGELOG.md

## Local Testing

To test the build process locally:

```powershell
# Restore dependencies
dotnet restore DDCSwitch/DDCSwitch.csproj

# Build release (same as workflow)
dotnet publish DDCSwitch/DDCSwitch.csproj -c Release -r win-x64

# Check the output
ls DDCSwitch/bin/Release/net10.0/win-x64/publish/
```

## Permissions

The workflows use:
- `contents: write` - To create releases and push tags
- `GITHUB_TOKEN` - Automatically provided by GitHub Actions

No additional secrets or configuration needed!

