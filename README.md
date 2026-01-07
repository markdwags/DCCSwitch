﻿# DDCSwitch

A Windows command-line utility to control monitor input sources via DDC/CI (Display Data Channel Command Interface). Switch between HDMI, DisplayPort, DVI, and VGA inputs without touching physical buttons.

📚 **[Examples](EXAMPLES.md)** | 📝 **[Changelog](CHANGELOG.md)**

## Features

- 🖥️ **List all DDC/CI capable monitors** with their current input sources
- 🔄 **Switch monitor inputs** programmatically (HDMI, DisplayPort, DVI, VGA, etc.)
- 🎯 **Simple CLI interface** perfect for scripts, shortcuts, and hotkeys
- ⚡ **Fast and lightweight** - NativeAOT compiled for instant startup
- 📦 **True native executable** - No .NET runtime dependency required
- 🪟 **Windows-only** - uses native Windows DDC/CI APIs (use ddcutil on Linux)

## Installation

### Pre-built Binary

Download the latest release from the [Releases](../../releases) page and extract `DDCSwitch.exe` to a folder in your PATH.

### Build from Source

Requirements:
- .NET 10.0 SDK or later
- Windows (x64)
- **For NativeAOT**: Visual Studio 2022 with "Desktop development with C++" workload (or equivalent C++ build tools)

#### Standard Build

```powershell
git clone https://github.com/yourusername/DDCSwitch.git
cd DDCSwitch
dotnet publish -c Release
```

The compiled executable will be in `DDCSwitch/bin/Release/net10.0/win-x64/publish/DDCSwitch.exe`.

#### NativeAOT Build (Recommended)

This project is configured for **NativeAOT compilation**, which produces a native executable with:
- ⚡ **Faster startup time** - No JIT compilation required
- 💾 **Lower memory usage** - Smaller runtime footprint
- 📦 **Smaller deployment** - Fully self-contained native binary
- 🚀 **No .NET runtime dependency** - True native executable

**Prerequisites for NativeAOT:**
- Visual Studio 2022 with "Desktop development with C++" workload
- Or install [C++ Build Tools](https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2022) standalone

**Build command:**

```powershell
git clone https://github.com/yourusername/DDCSwitch.git
cd DDCSwitch
dotnet publish -c Release
```

The project is pre-configured with `<PublishAot>true</PublishAot>`, so the standard publish command will automatically use NativeAOT compilation.

The native executable will be in `DDCSwitch/bin/Release/net10.0/win-x64/publish/DDCSwitch.exe`.

**Build output:**
- **Standard build**: ~8-12 MB (with .NET runtime)
- **NativeAOT build**: ~3-5 MB (native, trimmed, optimized)

> **Note:** The first NativeAOT build may take longer (2-5 minutes) as it performs ahead-of-time compilation and optimization. Subsequent builds are faster.

## Usage

### List Monitors

Display all DDC/CI capable monitors with their current input sources:

```powershell
DDCSwitch list
```

Example output:
```
╭───────┬─────────────────────┬──────────────┬───────────────────────────┬────────╮
│ Index │ Monitor Name        │ Device       │ Current Input             │ Status │
├───────┼─────────────────────┼──────────────┼───────────────────────────┼────────┤
│ 0     │ Generic PnP Monitor │ \\.\DISPLAY2 │ HDMI1 (0x11)              │ OK     │
│ 1*    │ VG270U P            │ \\.\DISPLAY1 │ DisplayPort1 (DP1) (0x0F) │ OK     │
│ 2     │ LG ULTRAGEAR(HDMI)  │ \\.\DISPLAY3 │ DisplayPort1 (DP1) (0x0F) │ OK     │
╰───────┴─────────────────────┴──────────────┴───────────────────────────┴────────╯
```

**JSON Output:**

Add `--json` flag for machine-readable output:

```powershell
DDCSwitch list --json
```

Example JSON output:
```json
{
  "success": true,
  "monitors": [
    {
      "index": 0,
      "name": "Generic PnP Monitor",
      "deviceName": "\\\\.\\DISPLAY2",
      "isPrimary": false,
      "currentInput": "HDMI1",
      "currentInputCode": "0x11",
      "status": "ok"
    },
    {
      "index": 1,
      "name": "VG270U P",
      "deviceName": "\\\\.\\DISPLAY1",
      "isPrimary": true,
      "currentInput": "DisplayPort1 (DP1)",
      "currentInputCode": "0x0F",
      "status": "ok"
    }
  ]
}
```

### Get Current Input

Get the current input source for a specific monitor:

```powershell
DDCSwitch get 0
```

Example output:
```
Monitor: Generic PnP Monitor (\\.\DISPLAY2)
Current Input: HDMI1 (0x11)
```

**JSON Output:**

```powershell
DDCSwitch get 0 --json
```

Example JSON output:
```json
{
  "success": true,
  "monitor": {
    "index": 0,
    "name": "Generic PnP Monitor",
    "deviceName": "\\\\.\\DISPLAY2",
    "isPrimary": false
  },
  "currentInput": "HDMI1",
  "currentInputCode": "0x11",
  "maxValue": 255
}
```

### Set Input Source

Switch a monitor to a different input:

```powershell
# By monitor index
DDCSwitch set 0 HDMI1
DDCSwitch set 1 DP1
DDCSwitch set 2 DVI1

# By monitor name (partial match)
DDCSwitch set "LG ULTRAGEAR" HDMI2
```

Example output:
```
✓ Successfully switched Generic PnP Monitor to HDMI1
```

**JSON Output:**

```powershell
DDCSwitch set 0 HDMI1 --json
```

Example JSON output:
```json
{
  "success": true,
  "monitor": {
    "index": 0,
    "name": "Generic PnP Monitor",
    "deviceName": "\\\\.\\DISPLAY2",
    "isPrimary": false
  },
  "newInput": "HDMI1",
  "newInputCode": "0x11"
}
```

**Error Responses:**

When using `--json`, errors are also returned in JSON format:

```json
{
  "success": false,
  "error": "Monitor '5' not found"
}
```

### Supported Input Names

- **HDMI**: `HDMI1`, `HDMI2`
- **DisplayPort**: `DP1`, `DP2`, `DisplayPort1`, `DisplayPort2`
- **DVI**: `DVI1`, `DVI2`
- **VGA/Analog**: `VGA1`, `VGA2`, `Analog1`, `Analog2`
- **Other**: `SVideo1`, `SVideo2`, `Tuner1`, `ComponentVideo1`, etc.
- **Custom codes**: Use hex values like `0x11` for manufacturer-specific inputs

## Use Cases

### JSON Output for Automation

All commands support `--json` flag for machine-readable output, perfect for scripting and integration:

**PowerShell Script Example:**
```powershell
# Check if primary monitor is on HDMI1, switch if not
$result = DDCSwitch get 0 --json | ConvertFrom-Json
if ($result.currentInputCode -ne "0x11") {
    DDCSwitch set 0 HDMI1 --json | Out-Null
    Write-Host "Switched to HDMI1"
}
```

**Python Script Example:**
```python
import subprocess
import json

# Get monitor information
result = subprocess.run(['DDCSwitch', 'list', '--json'], capture_output=True, text=True)
data = json.loads(result.stdout)

# Switch all monitors to HDMI1
for monitor in data['monitors']:
    if monitor['status'] == 'ok':
        subprocess.run(['DDCSwitch', 'set', str(monitor['index']), 'HDMI1'])
```

**Node.js Script Example:**
```javascript
const { execSync } = require('child_process');

// Get current input
const output = execSync('DDCSwitch get 0 --json', { encoding: 'utf-8' });
const data = JSON.parse(output);

console.log(`Monitor ${data.monitor.name} is on ${data.currentInput}`);
```

### Windows Shortcuts

Create desktop shortcuts to quickly switch inputs:

1. Right-click on desktop → New → Shortcut
2. Enter: `C:\Path\To\DDCSwitch.exe set 0 HDMI1`
3. Name it "Switch to HDMI1"

### AutoHotkey Script

```autohotkey
; Press Ctrl+Alt+H to switch to HDMI1
^!h::
Run, DDCSwitch.exe set 0 HDMI1
return

; Press Ctrl+Alt+D to switch to DisplayPort1
^!d::
Run, DDCSwitch.exe set 0 DP1
return
```

### PowerShell Script

```powershell
# Switch multiple monitors at once
DDCSwitch set 0 HDMI1
DDCSwitch set 1 DP1
```

### Batch Script

```batch
@echo off
echo Switching to gaming setup...
DDCSwitch.exe set 0 HDMI1
DDCSwitch.exe set 1 HDMI2
echo Done!
```

## Troubleshooting

### "No DDC/CI capable monitors found"

- Ensure your monitor supports DDC/CI (most modern monitors do)
- Check that DDC/CI is enabled in your monitor's OSD settings
- Try running as Administrator

### "Failed to set input source"

- The input may not exist on your monitor
- Try running as Administrator
- Some monitors have quirks - try different input codes or use `list` to see what works

### Monitor doesn't respond

- DDC/CI can be slow - wait a few seconds between commands
- Some monitors need to be on the target input at least once before DDC/CI can switch to it
- Check monitor OSD settings for DDC/CI enable/disable options

### Current input displays incorrectly

Some monitors have non-standard DDC/CI implementations and may report incorrect current input values, even though input switching still works correctly. This is a monitor firmware limitation, not a tool issue.

If you need to verify DDC/CI values or troubleshoot monitor-specific issues, try [ControlMyMonitor](https://www.nirsoft.net/utils/control_my_monitor.html) by NirSoft - a comprehensive GUI tool for DDC/CI debugging.

## Technical Details

### How It Works

DDCSwitch uses the Windows DXVA2 API to communicate with monitors via DDC/CI protocol:

1. Enumerates physical monitors using `EnumDisplayMonitors`
2. Gets physical monitor handles via `GetPhysicalMonitorsFromHMONITOR`
3. Reads/writes VCP (Virtual Control Panel) feature 0x60 (Input Source) using `GetVCPFeatureAndVCPFeatureReply` and `SetVCPFeature`

### NativeAOT Compatibility

This project is fully compatible with .NET NativeAOT compilation:

- **JSON Serialization**: Uses source generators (`JsonSerializerContext`) instead of reflection
- **P/Invoke**: All native Windows API calls use `DllImport` (fully supported in NativeAOT)
- **No Dynamic Code**: Zero reflection or dynamic code generation
- **Trimming-Safe**: All code is statically analyzable and trim-friendly

The codebase follows NativeAOT best practices, ensuring reliable ahead-of-time compilation without runtime surprises.

### VCP Codes

The tool uses VCP code 0x60 for input source selection, following the MCCS (Monitor Control Command Set) specification:

- `0x01` - VGA 1
- `0x03` - DVI 1
- `0x0F` - DisplayPort 1
- `0x10` - DisplayPort 2
- `0x11` - HDMI 1
- `0x12` - HDMI 2

## Why Windows Only?

Linux has excellent DDC/CI support through `ddcutil`, which is more mature and feature-rich. This tool focuses on Windows where native CLI options are limited.

## Contributing

Contributions welcome! Please open issues for bugs or feature requests.

## License

MIT License - see LICENSE file for details

## Acknowledgments

- Inspired by `ddcutil` for Linux
- Uses Spectre.Console for beautiful terminal output

