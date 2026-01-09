# ddcswitch

[![GitHub Release](https://img.shields.io/github/v/release/markdwags/ddcswitch)](https://github.com/markdwags/ddcswitch/releases)
[![License](https://img.shields.io/github/license/markdwags/ddcswitch)](https://github.com/markdwags/ddcswitch/blob/main/LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows-blue)](https://github.com/markdwags/ddcswitch)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![JSON Output](https://img.shields.io/badge/JSON-Output%20Support-green)](https://github.com/markdwags/ddcswitch#json-output-for-automation)

A Windows command-line utility to control monitor settings via DDC/CI (Display Data Channel Command Interface). Control input sources, brightness, contrast, and other VCP features without touching physical buttons.

📚 **[Examples](EXAMPLES.md)** | 📝 **[Changelog](CHANGELOG.md)**

## Features

- 🖥️ **List all DDC/CI capable monitors** with their current input sources
- **Detailed EDID information** - View monitor specifications, capabilities, and color characteristics
- 🔄 **Switch monitor inputs** programmatically (HDMI, DisplayPort, DVI, VGA, etc.)
- 🔆 **Control brightness and contrast** with percentage values (0-100%)
- 🎛️ **Comprehensive VCP feature support** - Access all MCCS standardized monitor controls
- 🏷️ **Feature categories and discovery** - Browse VCP features by category (Image, Color, Geometry, Audio, etc.)
- 🔍 **VCP scanning** to discover all supported monitor features
- 🎯 **Simple CLI interface** perfect for scripts, shortcuts, and hotkeys
- 📊 **JSON output support** - Machine-readable output for automation and integration
- ⚡ **Fast and lightweight** - NativeAOT compiled for instant startup
- 📦 **True native executable** - No .NET runtime dependency required
- 🪟 **Windows-only** - uses native Windows DDC/CI APIs (use ddcutil on Linux)

## Installation

### Chocolatey (Recommended)

Install via [Chocolatey](https://chocolatey.org/) package manager:

```powershell
choco install ddcswitch
```

To upgrade to the latest version:

```powershell
choco upgrade ddcswitch
```

### Pre-built Binary

Download the latest release from the [Releases](../../releases) page and extract `ddcswitch.exe` to a folder in your PATH.

### Build from Source

**Requirements:**
- .NET 10.0 SDK or later
- Windows (x64)
- Visual Studio 2022 with "Desktop development with C++" workload (or [C++ Build Tools](https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2022))

**Build:**

```powershell
git clone https://github.com/markdwags/ddcswitch.git
cd ddcswitch
dotnet publish -c Release
```

The project is pre-configured with NativeAOT (`<PublishAot>true</PublishAot>`), which produces a ~3-5 MB native executable with instant startup and no .NET runtime dependency.

Executable location: `ddcswitch/bin/Release/net10.0/win-x64/publish/ddcswitch.exe`

## Usage

### List Monitors

Display all DDC/CI capable monitors with their current input sources:

```powershell
ddcswitch list
```

Example output:
```
╭───────┬─────────────────────┬──────────────┬───────────────────────────┬────────╮
│ Index │ Monitor Name        │ Device       │ Current Input             │ Status │
├───────┼─────────────────────┼──────────────┼───────────────────────────┼────────┤
│ 0     │ Generic PnP Monitor │ \\.\DISPLAY2 │ HDMI1 (0x11)              │ OK     │
│ 1*    │ VG270U P            │ \\.\DISPLAY1 │ DisplayPort1 (DP1) (0x0F) │ OK     │
╰───────┴─────────────────────┴──────────────┴───────────────────────────┴────────╯
```

#### Verbose Listing

Add `--verbose` to include brightness and contrast information:

```powershell
ddcswitch list --verbose
```

Example output:
```
╭───────┬─────────────────────┬──────────────┬───────────────────────────┬────────┬────────────┬──────────╮
│ Index │ Monitor Name        │ Device       │ Current Input             │ Status │ Brightness │ Contrast │
├───────┼─────────────────────┼──────────────┼───────────────────────────┼────────┼────────────┼──────────┤
│ 0     │ Generic PnP Monitor │ \\.\DISPLAY2 │ HDMI1 (0x11)              │ OK     │ 75%        │ 80%      │
│ 1*    │ VG270U P            │ \\.\DISPLAY1 │ DisplayPort1 (DP1) (0x0F) │ OK     │ N/A        │ N/A      │
╰───────┴─────────────────────┴──────────────┴───────────────────────────┴────────┴────────────┴──────────╯
```

Add `--json` for machine-readable output (see [EXAMPLES.md](EXAMPLES.md) for automation examples).

### Monitor Information (EDID)

View detailed EDID (Extended Display Identification Data) information for a specific monitor:

```powershell
ddcswitch info 0
```

The info command provides comprehensive monitor details including:
- **EDID version** and manufacturer information
- **Model name**, serial number, and manufacture date
- **Video input type** (Digital/Analog)
- **Supported features** (DPMS power modes, display type, color space)
- **Chromaticity coordinates** for color calibration (red, green, blue, white points in CIE 1931 color space)
- **Current input source** status

JSON output is supported with `--json` flag for programmatic access to all EDID data:

```powershell
ddcswitch info 0 --json
```

### Get Current Settings

Get all VCP features for a specific monitor:

```powershell
ddcswitch get 0
```

This will scan and display all supported VCP features for monitor 0, showing their names, access types, current values, and maximum values.

You can also use the monitor name instead of the index (partial name matching supported):

```powershell
# Get all settings by monitor name
ddcswitch get "VG270U P"
ddcswitch get "Generic PnP"
```

Get a specific feature:

```powershell
# Get current input source
ddcswitch get 0 input

# Get brightness as percentage
ddcswitch get 0 brightness

# Get contrast as percentage  
ddcswitch get 0 contrast

# Works with monitor names too
ddcswitch get "VG270U P" brightness
ddcswitch get "Generic PnP" input
```

Output: `Monitor: Generic PnP Monitor` / `Brightness: 75% (120/160)`

### Set Monitor Settings

Switch a monitor to a different input:

```powershell
# By monitor index
ddcswitch set 0 HDMI1

# By monitor name (partial match)
ddcswitch set "LG ULTRAGEAR" HDMI2
```

### Toggle Between Input Sources

Automatically switch between two input sources without specifying which one:

```powershell
# Toggle between HDMI1 and DisplayPort1
ddcswitch toggle 0 HDMI1 DP1

# Toggle by monitor name
ddcswitch toggle "LG ULTRAGEAR" HDMI1 HDMI2
```

The toggle command detects the current input and switches to the alternate one:
- If current input is HDMI1 → switches to DP1
- If current input is DP1 → switches to HDMI1  
- If current input is neither → switches to HDMI1 (with warning)

Perfect for hotkeys and automation where you want to switch between two specific inputs without knowing which one is currently active.

### Raw VCP Access

For advanced users, access any VCP feature by code:

```powershell
# Get raw VCP value (e.g., VCP code 0x10 for brightness)
ddcswitch get 0 0x10

# Set raw VCP value
ddcswitch set 0 0x10 120
```

### VCP Feature Scanning

Discover all supported VCP features on all monitors:

```powershell
ddcswitch get all
```

This scans all VCP codes (0x00-0xFF) for every monitor and displays supported features with their current values, maximum values, and access types (read-only, write-only, read-write).

To scan a specific monitor:

```powershell
# Scan specific monitor by index
ddcswitch get 0

# Scan specific monitor by name
ddcswitch get "VG270U"
```

### VCP Feature Categories and Discovery

Discover and browse VCP features by category:

```powershell
# List all available categories
ddcswitch list --categories

# List features in a specific category
ddcswitch list --category image
ddcswitch list --category color
ddcswitch list --category audio
```

Example output:
```
Image Adjustment Features:
- brightness (0x10): Brightness control
- contrast (0x12): Contrast control  
- sharpness (0x87): Sharpness control
- backlight (0x13): Backlight control

Color Control Features:
- red-gain (0x16): Video gain: Red
- green-gain (0x18): Video gain: Green
- blue-gain (0x1A): Video gain: Blue
```

### Supported Features

#### Input Sources
- **HDMI**: `HDMI1`, `HDMI2`
- **DisplayPort**: `DP1`, `DP2`, `DisplayPort1`, `DisplayPort2`
- **DVI**: `DVI1`, `DVI2`
- **VGA/Analog**: `VGA1`, `VGA2`, `Analog1`, `Analog2`
- **Other**: `SVideo1`, `SVideo2`, `Tuner1`, `ComponentVideo1`, etc.
- **Custom codes**: Use hex values like `0x11` for manufacturer-specific inputs

#### Common VCP Features
- **Brightness**: `brightness` (VCP 0x10) - accepts percentage values (0-100%)
- **Contrast**: `contrast` (VCP 0x12) - accepts percentage values (0-100%)
- **Input Source**: `input` (VCP 0x60) - existing functionality maintained
- **Color Controls**: `red-gain`, `green-gain`, `blue-gain` (VCP 0x16, 0x18, 0x1A)
- **Audio**: `volume`, `mute` (VCP 0x62, 0x8D) - volume accepts percentage values
- **Geometry**: `h-position`, `v-position`, `clock`, `phase` (mainly for CRT monitors)
- **Presets**: `restore-defaults`, `degauss` (VCP 0x04, 0x01)

#### VCP Feature Categories
- **Image Adjustment**: brightness, contrast, sharpness, backlight, etc.
- **Color Control**: RGB gains, color temperature, gamma, hue, saturation
- **Geometry**: position, size, pincushion controls (mainly CRT)
- **Audio**: volume, mute, balance, treble, bass
- **Preset**: factory defaults, degauss, calibration
- **Miscellaneous**: power mode, OSD settings, firmware info

#### Raw VCP Codes
- Any VCP code from `0x00` to `0xFF`
- Values must be within the monitor's supported range
- Use hex format: `0x10`, `0x12`, etc.

## Use Cases

### Quick Examples

**Switch multiple monitors:**
```powershell
ddcswitch set 0 HDMI1
ddcswitch set 1 DP1
```

**Toggle between input sources:**
```powershell
# Toggle main monitor between HDMI1 and DisplayPort
ddcswitch toggle 0 HDMI1 DP1

# Toggle secondary monitor between HDMI inputs
ddcswitch toggle 1 HDMI1 HDMI2
```

**Control comprehensive VCP features:**
```powershell
ddcswitch set 0 brightness 75%
ddcswitch set 0 contrast 80%
ddcswitch get 0 brightness

# Color controls
ddcswitch set 0 red-gain 90%
ddcswitch set 0 green-gain 85%
ddcswitch set 0 blue-gain 95%

# Audio controls (if supported)
ddcswitch set 0 volume 50%
ddcswitch set 0 mute 1
```

**VCP feature discovery:**
```powershell
# List all available VCP feature categories
ddcswitch list --categories

# List features in a specific category
ddcswitch list --category color

# Search for features by name
ddcswitch get 0 bright  # Matches "brightness"

# Or by monitor name
ddcswitch get "VG270U" bright
```

**Desktop shortcut:**
Create a shortcut with target: `C:\Path\To\ddcswitch.exe set 0 brightness 50%`

**AutoHotkey:**
```autohotkey
^!h::Run, ddcswitch.exe set 0 HDMI1        ; Ctrl+Alt+H for HDMI1
^!d::Run, ddcswitch.exe set 0 DP1          ; Ctrl+Alt+D for DisplayPort
^!b::Run, ddcswitch.exe set 0 brightness 75%  ; Ctrl+Alt+B for 75% brightness
```

### JSON Output for Automation

All commands support `--json` for machine-readable output:

```powershell
# PowerShell: Conditional switching
$result = ddcswitch get 0 --json | ConvertFrom-Json
if ($result.currentInputCode -ne "0x11") {
    ddcswitch set 0 HDMI1
}
```

```python
# Python: Switch all monitors
import subprocess, json
data = json.loads(subprocess.run(['ddcswitch', 'list', '--json'], 
                                 capture_output=True, text=True).stdout)
for m in data['monitors']:
    if m['status'] == 'ok':
        subprocess.run(['ddcswitch', 'set', str(m['index']), 'HDMI1'])
```

📚 **See [EXAMPLES.md](EXAMPLES.md) for comprehensive automation examples** including Stream Deck, Task Scheduler, Python, Node.js, Rust, and more.

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

ddcswitch uses the Windows DXVA2 API to communicate with monitors via DDC/CI protocol. It reads/writes VCP (Virtual Control Panel) features following the MCCS specification.

**Common VCP Codes:**
- `0x10` Brightness, `0x12` Contrast, `0x60` Input Source
- `0x01` VGA, `0x03` DVI, `0x0F` DisplayPort 1, `0x10` DisplayPort 2, `0x11` HDMI 1, `0x12` HDMI 2

**VCP Feature Types:**
- **Read-Write**: Can get and set values (brightness, contrast, input)
- **Read-Only**: Can only read current value (some monitor info)
- **Write-Only**: Can only set values (some calibration features)

**NativeAOT Compatible:** Uses source generators for JSON, `DllImport` for P/Invoke, and zero reflection for reliable AOT compilation.

## Why Windows Only?

Linux has excellent DDC/CI support through `ddcutil`, which is more mature and feature-rich. This tool focuses on Windows where native CLI options are limited.

## Contributing

Contributions welcome! Please open issues for bugs or feature requests.

## License

MIT License - see LICENSE file for details

## Acknowledgments

- Inspired by [ddcutil](https://www.ddcutil.com) for Linux
- Uses Spectre.Console for beautiful terminal output

