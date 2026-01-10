# ddcswitch

[![GitHub Release](https://img.shields.io/github/v/release/markdwags/ddcswitch)](https://github.com/markdwags/ddcswitch/releases)
[![License](https://img.shields.io/github/license/markdwags/ddcswitch)](https://github.com/markdwags/ddcswitch/blob/main/LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows-blue)](https://github.com/markdwags/ddcswitch)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![JSON Output](https://img.shields.io/badge/JSON-Output%20Support-green)](https://github.com/markdwags/ddcswitch#json-output-for-automation)

A Windows command-line utility to control monitor settings via DDC/CI (Display Data Channel Command Interface). Control input sources, brightness, contrast, and other VCP features without touching physical buttons.

The project is pre-configured with NativeAOT, which produces a native executable with instant startup and no .NET runtime dependency.

📚 **[Examples](EXAMPLES.md)** | 📝 **[Changelog](CHANGELOG.md)**

## Features

- 🖥️ **List all DDC/CI capable monitors** with their current input sources
- 🔍 **EDID information** - View monitor specifications, capabilities, and color characteristics
- 🔄 **Switch monitor inputs** programmatically (HDMI, DisplayPort, DVI, VGA, etc.)
- 🔆 **Control brightness and contrast** with percentage values (0-100%)
- 🎛️ **Comprehensive VCP feature support** - Access all MCCS standardized monitor controls
- 🔍 **VCP scanning** to discover all supported monitor features
- 🎯 **Simple CLI interface** perfect for scripts, shortcuts, and hotkeys
- 📊 **JSON output support** - Machine-readable output for automation and integration
- ⚡ **Fast and lightweight** - NativeAOT compiled for instant startup
- 📦 **True native executable** - No .NET runtime dependency required
- 🪟 **Windows-only** - uses native Windows DDC/CI APIs (use [ddcutil](https://www.ddcutil.com/) on Linux)

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

Download the latest release from the [Releases](../../releases) page and extract `ddcswitch.exe` to a folder in your `PATH`.

#### How to add to PATH:
1. Copy `ddcswitch.exe` to a folder (e.g., `C:\Tools\ddcswitch\`).
2. Open Start Menu, search "Environment Variables", and select "Edit the system environment variables"
3. Click "Environment Variables..."
4. Under "System variables", select "Path" and click "Edit..."
5. Click "New" and add the folder path (e.g., `C:\Tools\ddcswitch\`)
6. Click OK on all dialogs to apply changes.
7. Restart any open command prompts or PowerShell windows.

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

Executable location: `ddcswitch/bin/Release/net10.0/win-x64/publish/ddcswitch.exe`

## Usage

[!NOTE]
JSON output is supported with `--json` flag all data and commands.

### List Monitors

Display all DDC/CI capable monitors with their current input sources:

```powershell
ddcswitch list
```

### Monitor Information (EDID)

View detailed EDID (Extended Display Identification Data) information for a specific monitor:

```powershell
ddcswitch info 0
```

### Get Current Settings

Get a specific feature by monitor index or name:

```powershell
# Get brightness by monitor index
ddcswitch get 0 brightness

# Get input source by monitor name (partial match supported)
ddcswitch get "VG270U" input
```

### Set Monitor Settings

Set brightness, contrast, or switch inputs by monitor index or name:

```powershell
# Set brightness by index
ddcswitch set 0 brightness 75%

# Switch input by monitor name (partial match supported)
ddcswitch set "LG ULTRAGEAR" HDMI2
```

### Toggle Between Input Sources

Automatically switch between two input sources:

```powershell
# Toggle between HDMI1 and DisplayPort1
ddcswitch toggle 0 HDMI1 DP1

# Toggle by monitor name
ddcswitch toggle "LG ULTRAGEAR" HDMI1 HDMI2
```

The toggle command detects the current input and switches to the alternate one - perfect for hotkeys and automation.

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


#### Raw VCP Codes
- Any VCP code from `0x00` to `0xFF`
- Values must be within the monitor's supported range
- Use hex format: `0x10`, `0x12`, etc.

## Quick Start

### Basic Usage Examples

```powershell
# List monitors
ddcswitch list

# Switch monitor input
ddcswitch set 0 HDMI1

# Adjust brightness
ddcswitch set 0 brightness 75%
```

### JSON Output

All commands support `--json` for machine-readable output, perfect for automation:

```powershell
# Get monitor list as JSON
ddcswitch list --json

# Get specific monitor info as JSON
ddcswitch info 0 --json
```

### Plain Text Output

To disable colors and icons (for logging or automation), set the `NO_COLOR` environment variable:

```powershell
$env:NO_COLOR = "1"
ddcswitch list
```

### Windows Shortcuts

Create a desktop shortcut to quickly adjust settings:

**Target:** `C:\Path\To\ddcswitch.exe set 0 HDMI1`

📚 **For more examples** including hotkeys, automation scripts, Stream Deck integration, and advanced usage, see **[EXAMPLES.md](EXAMPLES.md)**.

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
- Power cycle the monitor and/or remove and reconnect the video cable

### Current input displays incorrectly

Some monitors have non-standard DDC/CI implementations and may report incorrect current input values, even though input switching still works correctly. This is a monitor firmware limitation, not a tool issue.

If you prefer a graphical interface over the command-line, try [ControlMyMonitor](https://www.nirsoft.net/utils/control_my_monitor.html) by NirSoft - a comprehensive GUI tool for DDC/CI control and debugging.

## Why Windows Only?

Linux has excellent DDC/CI support through `ddcutil`, which is more mature and feature-rich. Windows needed a similar command-line tool - while `winddcutil` exists, it requires Python dependencies. This project provides a standalone native executable with no runtime requirements, though it's not trying to be a direct clone of the Linux ddcutil.

## Contributing

Contributions welcome! Please open issues for bugs or feature requests.

## License

MIT License - see LICENSE file for details

## Disclaimer and Warranty

**NO WARRANTY**: This software is provided "AS IS" without warranty of any kind, either express or implied, including but not limited to the implied warranties of merchantability, fitness for a particular purpose, or non-infringement. The entire risk as to the quality and performance of the software is with you.

**LIMITATION OF LIABILITY**: In no event shall the authors, copyright holders, or contributors be liable for any direct, indirect, incidental, special, exemplary, or consequential damages (including but not limited to procurement of substitute goods or services; loss of use, data, or profits; or business interruption) however caused and on any theory of liability, whether in contract, strict liability, or tort (including negligence or otherwise) arising in any way out of the use of this software, even if advised of the possibility of such damage.

**MONITOR DAMAGE**: The authors and contributors of this software are not responsible for any damage to monitors, display devices, or other hardware that may result from the use of this software. DDC/CI commands can potentially affect monitor settings in ways that may cause temporary or permanent changes to display behavior. Users assume all risk when using this software to control monitor settings.

**USE AT YOUR OWN RISK**: By using this software, you acknowledge that you understand the risks involved in sending DDC/CI commands to your monitors and that you use this software entirely at your own risk. It is recommended to test commands carefully and ensure you can restore your monitor settings manually if needed.

## Acknowledgments

- Inspired by [ddcutil](https://www.ddcutil.com) for Linux
- Uses Spectre.Console for beautiful terminal output

