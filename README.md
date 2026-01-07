# DDCSwitch

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

**Requirements:**
- .NET 10.0 SDK or later
- Windows (x64)
- Visual Studio 2022 with "Desktop development with C++" workload (or [C++ Build Tools](https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2022))

**Build:**

```powershell
git clone https://github.com/yourusername/DDCSwitch.git
cd DDCSwitch
dotnet publish -c Release
```

The project is pre-configured with NativeAOT (`<PublishAot>true</PublishAot>`), which produces a ~3-5 MB native executable with instant startup and no .NET runtime dependency.

Executable location: `DDCSwitch/bin/Release/net10.0/win-x64/publish/DDCSwitch.exe`

> **Note:** First build may take 2-5 minutes for AOT compilation. Subsequent builds are faster.

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
╰───────┴─────────────────────┴──────────────┴───────────────────────────┴────────╯
```

Add `--json` for machine-readable output (see [EXAMPLES.md](EXAMPLES.md) for automation examples).

### Get Current Input

Get the current input source for a specific monitor:

```powershell
DDCSwitch get 0
```

Output: `Monitor: Generic PnP Monitor (\\.\DISPLAY2)` / `Current Input: HDMI1 (0x11)`

### Set Input Source

Switch a monitor to a different input:

```powershell
# By monitor index
DDCSwitch set 0 HDMI1

# By monitor name (partial match)
DDCSwitch set "LG ULTRAGEAR" HDMI2
```

Output: `✓ Successfully switched Generic PnP Monitor to HDMI1`

### Supported Input Names

- **HDMI**: `HDMI1`, `HDMI2`
- **DisplayPort**: `DP1`, `DP2`, `DisplayPort1`, `DisplayPort2`
- **DVI**: `DVI1`, `DVI2`
- **VGA/Analog**: `VGA1`, `VGA2`, `Analog1`, `Analog2`
- **Other**: `SVideo1`, `SVideo2`, `Tuner1`, `ComponentVideo1`, etc.
- **Custom codes**: Use hex values like `0x11` for manufacturer-specific inputs

## Use Cases

### Quick Examples

**Switch multiple monitors:**
```powershell
DDCSwitch set 0 HDMI1
DDCSwitch set 1 DP1
```

**Desktop shortcut:**
Create a shortcut with target: `C:\Path\To\DDCSwitch.exe set 0 HDMI1`

**AutoHotkey:**
```autohotkey
^!h::Run, DDCSwitch.exe set 0 HDMI1  ; Ctrl+Alt+H for HDMI1
^!d::Run, DDCSwitch.exe set 0 DP1    ; Ctrl+Alt+D for DisplayPort
```

### JSON Output for Automation

All commands support `--json` for machine-readable output:

```powershell
# PowerShell: Conditional switching
$result = DDCSwitch get 0 --json | ConvertFrom-Json
if ($result.currentInputCode -ne "0x11") {
    DDCSwitch set 0 HDMI1
}
```

```python
# Python: Switch all monitors
import subprocess, json
data = json.loads(subprocess.run(['DDCSwitch', 'list', '--json'], 
                                 capture_output=True, text=True).stdout)
for m in data['monitors']:
    if m['status'] == 'ok':
        subprocess.run(['DDCSwitch', 'set', str(m['index']), 'HDMI1'])
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

DDCSwitch uses the Windows DXVA2 API to communicate with monitors via DDC/CI protocol. It reads/writes VCP (Virtual Control Panel) feature 0x60 (Input Source) following the MCCS specification.

**Common VCP Input Codes:**
- `0x01` VGA, `0x03` DVI, `0x0F` DisplayPort 1, `0x10` DisplayPort 2, `0x11` HDMI 1, `0x12` HDMI 2

**NativeAOT Compatible:** Uses source generators for JSON, `DllImport` for P/Invoke, and zero reflection for reliable AOT compilation.

## Why Windows Only?

Linux has excellent DDC/CI support through `ddcutil`, which is more mature and feature-rich. This tool focuses on Windows where native CLI options are limited.

## Contributing

Contributions welcome! Please open issues for bugs or feature requests.

## License

MIT License - see LICENSE file for details

## Acknowledgments

- Inspired by `ddcutil` for Linux
- Uses Spectre.Console for beautiful terminal output

