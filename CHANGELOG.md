# Changelog

All notable changes to ddcswitch will be documented in this file.

## [1.0.3] - 2026-01-08

### Added
- Added the toggle command to flip a monitor between two input sources
- `info` command to display detailed EDID (Extended Display Identification Data) information
  - Full JSON output support for programmatic access

## [1.0.2] - 2026-01-07

### Added
- Full support for all DDC/CI compliant monitors
- Improved error handling for unsupported monitors
- Enhanced documentation with additional examples
- Optimized performance for input switching operations
- Better clarity in CLI output messages

## [1.0.1] - 2026-01-07

### Added

- `version` command to display current version information

## [1.0.0] - 2026-01-07

### Initial Release

#### Added
- Monitor enumeration via Windows DDC/CI APIs
- Current input source detection (VCP feature 0x60)
- Input source switching for DDC/CI capable monitors
- Support for standard input types:
  - HDMI (HDMI1, HDMI2)
  - DisplayPort (DP1, DP2)
  - DVI (DVI1, DVI2)
  - VGA/Analog (VGA1, VGA2)
  - Custom hex codes (0x11, etc.)
- CLI commands:
  - `list` - Show all monitors with current inputs
  - `get <monitor>` - Get current input for specific monitor
  - `set <monitor> <input>` - Switch monitor input
  - `help` - Display usage information
- Monitor selection by index or name (partial match)
- Rich terminal output using Spectre.Console
- Comprehensive error handling and user feedback
- Single-file executable build
- Complete documentation:
  - README with installation and usage
  - EXAMPLES with integration scenarios
  - QUICK-REFERENCE for common tasks
  - IMPLEMENTATION-SUMMARY for developers

#### Technical Details
- Built with .NET 10.0
- Windows x64 only
- P/Invoke wrappers for dxva2.dll and user32.dll
- MCCS VCP code compliance
- Native AOT JSON serialization using System.Text.Json source generators
- Optimized release build (~19 MB)

#### Known Limitations
- Requires DDC/CI support on monitors
- Some monitors may need administrator privileges
- Input switching speed depends on monitor hardware
- No USB peripheral switching (KVM functionality)
- Some monitors have non-standard DDC/CI implementations and may report incorrect current input values (switching still works correctly)

### Future Considerations
- [ ] Add verbose logging mode
- [ ] Configuration file for monitor presets
- [ ] Brightness/contrast control (VCP codes 0x10/0x12)
- [ ] Power management (VCP code 0xD6)
- [ ] Profile system for quick setup switching
- [ ] GUI version (optional)
- [ ] Installer package

