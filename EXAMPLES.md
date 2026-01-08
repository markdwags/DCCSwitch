# ddcswitch Examples

This document contains detailed examples and use cases for ddcswitch, including input switching, brightness/contrast control, comprehensive VCP feature access, and automation.

## Comprehensive VCP Feature Examples

ddcswitch now supports all MCCS (Monitor Control Command Set) standardized VCP features, organized by categories for easy discovery.

### VCP Feature Categories

List all available feature categories:

```powershell
ddcswitch list --categories
```

Output:
```
Available VCP Feature Categories:
- ImageAdjustment: Brightness, contrast, sharpness, backlight controls
- ColorControl: RGB gains, color temperature, gamma, hue, saturation
- Geometry: Position, size, pincushion controls (mainly CRT monitors)
- Audio: Volume, mute, balance, treble, bass controls
- Preset: Factory defaults, degauss, calibration features
- Miscellaneous: Power mode, OSD settings, firmware information
```

### Browse Features by Category

```powershell
# Image adjustment features
ddcswitch list --category image

# Color control features  
ddcswitch list --category color

# Audio features
ddcswitch list --category audio
```

### Color Control Examples

Control RGB gains for color calibration:

```powershell
# Set individual RGB gains (percentage values)
ddcswitch set 0 red-gain 95%
ddcswitch set 0 green-gain 90%
ddcswitch set 0 blue-gain 85%

# Get current RGB values
ddcswitch get 0 red-gain
ddcswitch get 0 green-gain  
ddcswitch get 0 blue-gain

# Color temperature control (if supported)
ddcswitch set 0 color-temp-request 6500
ddcswitch get 0 color-temp-request

# Gamma control
ddcswitch set 0 gamma 2.2
ddcswitch get 0 gamma

# Hue and saturation
ddcswitch set 0 hue 50%
ddcswitch set 0 saturation 80%
```

### Audio Control Examples

Control monitor speakers (if supported):

```powershell
# Volume control (percentage)
ddcswitch set 0 volume 75%
ddcswitch get 0 volume

# Mute/unmute
ddcswitch set 0 mute 1    # Mute
ddcswitch set 0 mute 0    # Unmute

# Audio balance (if supported)
ddcswitch set 0 audio-balance 50%  # Centered

# Treble and bass (if supported)
ddcswitch set 0 audio-treble 60%
ddcswitch set 0 audio-bass 70%
```

### Advanced Image Controls

Beyond basic brightness and contrast:

```powershell
# Sharpness control
ddcswitch set 0 sharpness 75%
ddcswitch get 0 sharpness

# Backlight control (LED monitors)
ddcswitch set 0 backlight 80%
ddcswitch get 0 backlight

# Image orientation (if supported)
ddcswitch set 0 image-orientation 0  # Normal
ddcswitch set 0 image-orientation 1  # 90� rotation

# Image mode presets (if supported)
ddcswitch set 0 image-mode 1  # Standard
ddcswitch set 0 image-mode 2  # Movie
ddcswitch set 0 image-mode 3  # Game
```

### Factory Reset and Calibration

```powershell
# Restore all factory defaults
ddcswitch set 0 restore-defaults 1

# Restore specific defaults
ddcswitch set 0 restore-brightness-contrast 1
ddcswitch set 0 restore-color 1
ddcswitch set 0 restore-geometry 1

# Degauss (CRT monitors)
ddcswitch set 0 degauss 1

# Auto calibration features (if supported)
ddcswitch set 0 auto-color-setup 1
ddcswitch set 0 auto-size-center 1
```

### Complete Monitor Profile Examples

Create comprehensive monitor profiles using all available features:

```powershell
# gaming-profile-advanced.ps1
Write-Host "Activating Advanced Gaming Profile..." -ForegroundColor Cyan

# Input and basic settings
ddcswitch set 0 input HDMI1
ddcswitch set 0 brightness 90%
ddcswitch set 0 contrast 85%

# Color optimization for gaming
ddcswitch set 0 red-gain 100%
ddcswitch set 0 green-gain 95%
ddcswitch set 0 blue-gain 90%
ddcswitch set 0 saturation 110%  # Enhanced colors
ddcswitch set 0 sharpness 80%    # Crisp details

# Audio settings
ddcswitch set 0 volume 60%
ddcswitch set 0 mute 0

Write-Host "Advanced Gaming Profile Activated!" -ForegroundColor Green
```

```powershell
# work-profile-advanced.ps1  
Write-Host "Activating Advanced Work Profile..." -ForegroundColor Cyan

# Input and basic settings
ddcswitch set 0 input DP1
ddcswitch set 0 brightness 60%
ddcswitch set 0 contrast 75%

# Color optimization for text work
ddcswitch set 0 red-gain 85%
ddcswitch set 0 green-gain 90%
ddcswitch set 0 blue-gain 95%
ddcswitch set 0 saturation 70%   # Reduced saturation for comfort
ddcswitch set 0 sharpness 60%    # Softer for long reading

# Audio settings
ddcswitch set 0 volume 40%       # Lower for office environment
ddcswitch set 0 mute 0

Write-Host "Advanced Work Profile Activated!" -ForegroundColor Green
```

```powershell
# photo-editing-profile.ps1
Write-Host "Activating Photo Editing Profile..." -ForegroundColor Cyan

# Input and basic settings
ddcswitch set 0 input DP1
ddcswitch set 0 brightness 70%
ddcswitch set 0 contrast 80%

# Accurate color reproduction
ddcswitch set 0 red-gain 90%
ddcswitch set 0 green-gain 90%
ddcswitch set 0 blue-gain 90%
ddcswitch set 0 saturation 100%  # Natural saturation
ddcswitch set 0 gamma 2.2        # Standard gamma
ddcswitch set 0 color-temp-request 6500  # D65 standard

# Disable audio to avoid distractions
ddcswitch set 0 mute 1

Write-Host "Photo Editing Profile Activated!" -ForegroundColor Green
```

## Basic Usage Examples

### Check What Monitors Support DDC/CI

```powershell
ddcswitch list
```

This will show all your monitors and indicate which ones support DDC/CI control. Monitors with "OK" status can be controlled.

### Verbose Monitor Information

Get detailed information including brightness and contrast:

```powershell
ddcswitch list --verbose
```

This shows current brightness and contrast levels for each monitor (displays "N/A" for unsupported features).

### Get Current Settings

```powershell
# Get all VCP features for primary monitor (scans all supported features)
ddcswitch get 0

# Get all features by monitor name (partial matching supported)
ddcswitch get "VG270U P"
ddcswitch get "Generic PnP"

# Get specific features by index
ddcswitch get 0 input      # Current input source
ddcswitch get 0 brightness # Current brightness
ddcswitch get 0 contrast   # Current contrast

# Get specific features by monitor name
ddcswitch get "Generic PnP" input
ddcswitch get "LG" brightness

# Get raw VCP value
ddcswitch get 0 0x10  # Brightness (raw)
```

### Set Monitor Settings

```powershell
# Switch input
ddcswitch set 0 HDMI1

# Set brightness to 75%
ddcswitch set 0 brightness 75%

# Set contrast to 80%
ddcswitch set 0 contrast 80%

# Set raw VCP value
ddcswitch set 0 0x10 120  # Brightness (raw value)
```

### Toggle Between Input Sources

The toggle command automatically switches between two specified input sources by detecting the current input and switching to the alternate one:

```powershell
# Toggle between HDMI1 and DisplayPort1
ddcswitch toggle 0 HDMI1 DP1

# Toggle between HDMI1 and HDMI2 by monitor name
ddcswitch toggle "LG ULTRAGEAR" HDMI1 HDMI2

# Toggle with JSON output for automation
ddcswitch toggle 0 HDMI1 DP1 --json
```

#### Toggle Command Behavior

- **Current input is HDMI1** → Switches to DP1
- **Current input is DP1** → Switches to HDMI1  
- **Current input is neither** → Switches to HDMI1 (first input) with warning

#### Toggle Examples

```powershell
# Create toggle shortcuts for different monitor setups
# toggle-main-monitor.ps1
ddcswitch toggle 0 HDMI1 DP1
Write-Host "Toggled main monitor input" -ForegroundColor Green

# toggle-secondary-monitor.ps1  
ddcswitch toggle 1 HDMI2 DP2
Write-Host "Toggled secondary monitor input" -ForegroundColor Green

# Smart toggle with status feedback
$result = ddcswitch toggle 0 HDMI1 DP1 --json | ConvertFrom-Json
if ($result.success) {
    Write-Host "Switched from $($result.fromInput) to $($result.toInput)" -ForegroundColor Green
} else {
    Write-Host "Toggle failed: $($result.errorMessage)" -ForegroundColor Red
}
```

#### AutoHotkey Toggle Integration

```autohotkey
; Ctrl+Alt+T: Toggle between HDMI1 and DisplayPort
^!t::
    Run, ddcswitch.exe toggle 0 HDMI1 DP1, , Hide
    TrayTip, ddcswitch, Input toggled, 1
    return

; Ctrl+Alt+Shift+T: Toggle secondary monitor
^!+t::
    Run, ddcswitch.exe toggle 1 HDMI2 DP2, , Hide
    TrayTip, ddcswitch, Secondary monitor toggled, 1
    return
```

## Brightness and Contrast Control

### Basic Brightness Control

```powershell
# Set brightness to specific percentage
ddcswitch set 0 brightness 50%
ddcswitch set 0 brightness 75%
ddcswitch set 0 brightness 100%

# Get current brightness
ddcswitch get 0 brightness
# Output: Monitor: Generic PnP Monitor / Brightness: 75% (120/160)
```

### Basic Contrast Control

```powershell
# Set contrast to specific percentage
ddcswitch set 0 contrast 60%
ddcswitch set 0 contrast 85%
ddcswitch set 0 contrast 100%

# Get current contrast
ddcswitch get 0 contrast
# Output: Monitor: Generic PnP Monitor / Contrast: 85% (136/160)
```

### Brightness Presets

Create quick brightness presets:

```powershell
# brightness-low.ps1
ddcswitch set 0 brightness 25%
Write-Host "Brightness set to 25% (Low)" -ForegroundColor Green

# brightness-medium.ps1  
ddcswitch set 0 brightness 50%
Write-Host "Brightness set to 50% (Medium)" -ForegroundColor Green

# brightness-high.ps1
ddcswitch set 0 brightness 75%
Write-Host "Brightness set to 75% (High)" -ForegroundColor Green

# brightness-max.ps1
ddcswitch set 0 brightness 100%
Write-Host "Brightness set to 100% (Maximum)" -ForegroundColor Green
```

### Time-Based Brightness Control

Automatically adjust brightness based on time of day:

```powershell
# auto-brightness.ps1
$hour = (Get-Date).Hour

if ($hour -ge 6 -and $hour -lt 9) {
    # Morning: Medium brightness
    ddcswitch set 0 brightness 60%
    Write-Host "Morning brightness: 60%" -ForegroundColor Yellow
} elseif ($hour -ge 9 -and $hour -lt 18) {
    # Daytime: High brightness
    ddcswitch set 0 brightness 85%
    Write-Host "Daytime brightness: 85%" -ForegroundColor Green
} elseif ($hour -ge 18 -and $hour -lt 22) {
    # Evening: Medium brightness
    ddcswitch set 0 brightness 50%
    Write-Host "Evening brightness: 50%" -ForegroundColor Orange
} else {
    # Night: Low brightness
    ddcswitch set 0 brightness 25%
    Write-Host "Night brightness: 25%" -ForegroundColor Blue
}
```

### Gaming vs Work Profiles

Create different brightness/contrast profiles:

```powershell
# gaming-profile.ps1
Write-Host "Activating Gaming Profile..." -ForegroundColor Cyan
ddcswitch set 0 input HDMI1        # Switch to console
ddcswitch set 0 brightness 90%     # High brightness for gaming
ddcswitch set 0 contrast 85%       # High contrast for visibility
Write-Host "Gaming profile activated!" -ForegroundColor Green

# work-profile.ps1
Write-Host "Activating Work Profile..." -ForegroundColor Cyan
ddcswitch set 0 input DP1           # Switch to PC
ddcswitch set 0 brightness 60%     # Comfortable brightness for long work
ddcswitch set 0 contrast 75%       # Balanced contrast for text
Write-Host "Work profile activated!" -ForegroundColor Green
```

## Raw VCP Access Examples

### Discover VCP Features

```powershell
# Scan all VCP features for all monitors
ddcswitch get all

# Scan all VCP features for a specific monitor
ddcswitch get 0

# Scan by monitor name
ddcswitch get "VG270U P"
```

### Common VCP Codes

```powershell
# Brightness (VCP 0x10)
ddcswitch get 0 0x10
ddcswitch set 0 0x10 120

# Contrast (VCP 0x12)  
ddcswitch get 0 0x12
ddcswitch set 0 0x12 140

# Input Source (VCP 0x60)
ddcswitch get 0 0x60
ddcswitch set 0 0x60 0x11  # HDMI1

# Color Temperature (VCP 0x14) - if supported
ddcswitch get 0 0x14
ddcswitch set 0 0x14 6500  # 6500K
```

### Test Unknown VCP Codes

```powershell
# test-vcp-codes.ps1 - Discover what VCP codes your monitor supports
Write-Host "Testing VCP codes for Monitor 0" -ForegroundColor Cyan

$commonCodes = @(0x10, 0x12, 0x14, 0x16, 0x18, 0x1A, 0x20, 0x30, 0x60, 0x62, 0x6C, 0x6E, 0x70)

foreach ($code in $commonCodes) {
    $hexCode = "0x{0:X2}" -f $code
    try {
        $result = ddcswitch get 0 $hexCode 2>$null
        if ($result -notmatch "error|failed") {
            Write-Host "? VCP $hexCode supported: $result" -ForegroundColor Green
        }
    } catch {
        Write-Host "? VCP $hexCode not supported" -ForegroundColor Red
    }
}
```

## JSON Output and Automation

All ddcswitch commands support the `--json` flag for machine-readable output. This is perfect for scripting, automation, and integration with other tools.

### PowerShell JSON Examples

#### Example 1: Conditional Input Switching with Brightness Control

Check the current input and switch only if needed, then adjust brightness:

```powershell
# Check if monitor is on HDMI1, switch to DP1 if not, then set work brightness
$result = ddcswitch get 0 --json | ConvertFrom-Json

if ($result.success -and $result.currentInputCode -ne "0x11") {
    Write-Host "Monitor is on $($result.currentInput), switching to HDMI1..."
    ddcswitch set 0 HDMI1 --json | Out-Null
    ddcswitch set 0 brightness 75% --json | Out-Null
    Write-Host "Switched to HDMI1 and set brightness to 75%" -ForegroundColor Green
} else {
    Write-Host "Monitor already on HDMI1"
}
```

#### Example 2: Complete Monitor Setup with All Features

```powershell
# Switch all available monitors with full configuration
$listResult = ddcswitch list --json | ConvertFrom-Json

if ($listResult.success) {
    foreach ($monitor in $listResult.monitors) {
        if ($monitor.status -eq "ok") {
            Write-Host "Configuring $($monitor.name)..." -ForegroundColor Cyan
            
            # Set input
            $setResult = ddcswitch set $monitor.index HDMI1 --json | ConvertFrom-Json
            if ($setResult.success) {
                Write-Host "  ? Input: HDMI1" -ForegroundColor Green
            }
            
            # Set brightness
            $brightnessResult = ddcswitch set $monitor.index brightness 75% --json | ConvertFrom-Json
            if ($brightnessResult.success) {
                Write-Host "  ? Brightness: 75%" -ForegroundColor Green
            } else {
                Write-Host "  ? Brightness not supported" -ForegroundColor Yellow
            }
            
            # Set contrast
            $contrastResult = ddcswitch set $monitor.index contrast 80% --json | ConvertFrom-Json
            if ($contrastResult.success) {
                Write-Host "  ? Contrast: 80%" -ForegroundColor Green
            } else {
                Write-Host "  ? Contrast not supported" -ForegroundColor Yellow
            }
        }
    }
} else {
    Write-Error "Failed to list monitors: $($listResult.error)"
}
```

#### Example 3: Monitor Status Dashboard with VCP Features

Create a comprehensive dashboard showing all monitor states:

```powershell
# monitor-dashboard.ps1
$result = ddcswitch list --verbose --json | ConvertFrom-Json

if ($result.success) {
    Write-Host "`n=== Monitor Status Dashboard ===" -ForegroundColor Cyan
    Write-Host "Total Monitors: $($result.monitors.Count)`n" -ForegroundColor Cyan
    
    foreach ($monitor in $result.monitors) {
        $isPrimary = if ($monitor.isPrimary) { " [PRIMARY]" } else { "" }
        Write-Host "Monitor $($monitor.index): $($monitor.name)$isPrimary" -ForegroundColor Yellow
        Write-Host "  Device: $($monitor.deviceName)"
        Write-Host "  Input: $($monitor.currentInput) ($($monitor.currentInputCode))"
        Write-Host "  Status: $($monitor.status.ToUpper())"
        
        if ($monitor.brightness) {
            Write-Host "  Brightness: $($monitor.brightness)" -ForegroundColor Green
        } else {
            Write-Host "  Brightness: N/A" -ForegroundColor Gray
        }
        
        if ($monitor.contrast) {
            Write-Host "  Contrast: $($monitor.contrast)" -ForegroundColor Green
        } else {
            Write-Host "  Contrast: N/A" -ForegroundColor Gray
        }
        Write-Host ""
    }
}
```

#### Example 4: Smart Brightness Toggle

```powershell
# smart-brightness-toggle.ps1
param([int]$MonitorIndex = 0)

$result = ddcswitch get $MonitorIndex brightness --json | ConvertFrom-Json

if ($result.success) {
    $currentPercent = $result.percentageValue
    
    # Toggle between 25%, 50%, 75%, 100%
    $newBrightness = switch ($currentPercent) {
        {$_ -le 25} { 50 }
        {$_ -le 50} { 75 }
        {$_ -le 75} { 100 }
        default { 25 }
    }
    
    $switchResult = ddcswitch set $MonitorIndex brightness "$newBrightness%" --json | ConvertFrom-Json
    
    if ($switchResult.success) {
        Write-Host "Brightness: $currentPercent% ? $newBrightness%" -ForegroundColor Green
    }
} else {
    Write-Host "Brightness control not supported on this monitor" -ForegroundColor Yellow
}
```

### Python JSON Examples

#### Example 1: Monitor Control with Brightness/Contrast

```python
#!/usr/bin/env python3
import subprocess
import json
import sys

def run_ddc(args):
    """Run ddcswitch and return JSON result"""
    result = subprocess.run(
        ['ddcswitch'] + args + ['--json'],
        capture_output=True,
        text=True
    )
    return json.loads(result.stdout)

def list_monitors():
    """List all monitors with brightness/contrast info"""
    data = run_ddc(['list', '--verbose'])
    if data['success']:
        for monitor in data['monitors']:
            primary = " [PRIMARY]" if monitor['isPrimary'] else ""
            print(f"{monitor['index']}: {monitor['name']}{primary}")
            print(f"   Input: {monitor['currentInput']} ({monitor['currentInputCode']})")
            print(f"   Brightness: {monitor.get('brightness', 'N/A')}")
            print(f"   Contrast: {monitor.get('contrast', 'N/A')}")
    else:
        print(f"Error: {data['error']}", file=sys.stderr)

def set_brightness(monitor_index, percentage):
    """Set monitor brightness"""
    data = run_ddc(['set', str(monitor_index), 'brightness', f'{percentage}%'])
    if data['success']:
        print(f"? Set brightness to {percentage}% on {data['monitor']['name']}")
    else:
        print(f"? Error: {data['error']}", file=sys.stderr)

def set_contrast(monitor_index, percentage):
    """Set monitor contrast"""
    data = run_ddc(['set', str(monitor_index), 'contrast', f'{percentage}%'])
    if data['success']:
        print(f"? Set contrast to {percentage}% on {data['monitor']['name']}")
    else:
        print(f"? Error: {data['error']}", file=sys.stderr)

def switch_input(monitor_index, input_name):
    """Switch monitor input"""
    data = run_ddc(['set', str(monitor_index), input_name])
    if data['success']:
        print(f"? Switched {data['monitor']['name']} to {data['newInput']}")
    else:
        print(f"? Error: {data['error']}", file=sys.stderr)
        sys.exit(1)

# Example usage
if __name__ == '__main__':
    list_monitors()
    # set_brightness(0, 75)
    # set_contrast(0, 80)
    # switch_input(0, 'HDMI1')
```

#### Example 2: Automated Brightness Control

```python
#!/usr/bin/env python3
"""
Automatically adjust monitor brightness based on time of day
Usage: python auto_brightness.py
"""
import subprocess
import json
import time
from datetime import datetime

def run_ddc(args):
    result = subprocess.run(['ddcswitch'] + args + ['--json'],
                          capture_output=True, text=True)
    return json.loads(result.stdout)

def set_brightness_all(percentage):
    """Set brightness on all supported monitors"""
    print(f"?? Setting brightness to {percentage}%...")
    data = run_ddc(['list'])
    
    if data['success']:
        for monitor in data['monitors']:
            if monitor['status'] == 'ok':
                result = run_ddc(['set', str(monitor['index']), 'brightness', f'{percentage}%'])
                if result['success']:
                    print(f"  ? {monitor['name']} ? {percentage}%")
                else:
                    print(f"  ? {monitor['name']} ? Brightness not supported")

def get_brightness_for_time():
    """Get appropriate brightness based on current time"""
    hour = datetime.now().hour
    
    if 6 <= hour < 9:      # Morning
        return 60
    elif 9 <= hour < 18:   # Daytime  
        return 85
    elif 18 <= hour < 22:  # Evening
        return 50
    else:                  # Night
        return 25

def gaming_mode():
    """Switch to gaming setup with high brightness"""
    print("?? Activating gaming mode...")
    data = run_ddc(['list'])
    
    if data['success']:
        for monitor in data['monitors']:
            if monitor['status'] == 'ok':
                # Switch to HDMI (console)
                input_result = run_ddc(['set', str(monitor['index']), 'HDMI1'])
                if input_result['success']:
                    print(f"  ? {monitor['name']} ? HDMI1")
                
                # Set high brightness for gaming
                brightness_result = run_ddc(['set', str(monitor['index']), 'brightness', '90%'])
                if brightness_result['success']:
                    print(f"  ? {monitor['name']} ? 90% brightness")

def work_mode():
    """Switch to work setup with comfortable brightness"""
    print("?? Activating work mode...")
    data = run_ddc(['list'])
    
    if data['success']:
        for monitor in data['monitors']:
            if monitor['status'] == 'ok':
                # Switch to DisplayPort (PC)
                input_result = run_ddc(['set', str(monitor['index']), 'DP1'])
                if input_result['success']:
                    print(f"  ? {monitor['name']} ? DP1")
                
                # Set comfortable brightness for work
                brightness_result = run_ddc(['set', str(monitor['index']), 'brightness', '60%'])
                if brightness_result['success']:
                    print(f"  ? {monitor['name']} ? 60% brightness")

# Auto-brightness based on time
if __name__ == '__main__':
    brightness = get_brightness_for_time()
    set_brightness_all(brightness)
```

### Node.js JSON Examples

#### Example 1: Monitor Control API with VCP Features

```javascript
// monitor-api.js
const { execSync } = require('child_process');

class ddcswitch {
    static exec(args) {
        const output = execSync(`ddcswitch ${args.join(' ')} --json`, {
            encoding: 'utf-8'
        });
        return JSON.parse(output);
    }

    static listMonitors(verbose = false) {
        const args = verbose ? ['list', '--verbose'] : ['list'];
        return this.exec(args);
    }

    static getCurrentInput(monitorIndex) {
        return this.exec(['get', monitorIndex]);
    }

    static getBrightness(monitorIndex) {
        return this.exec(['get', monitorIndex, 'brightness']);
    }

    static getContrast(monitorIndex) {
        return this.exec(['get', monitorIndex, 'contrast']);
    }

    static setInput(monitorIndex, input) {
        return this.exec(['set', monitorIndex, input]);
    }

    static setBrightness(monitorIndex, percentage) {
        return this.exec(['set', monitorIndex, 'brightness', `${percentage}%`]);
    }

    static setContrast(monitorIndex, percentage) {
        return this.exec(['set', monitorIndex, 'contrast', `${percentage}%`]);
    }

    static setRawVcp(monitorIndex, vcpCode, value) {
        return this.exec(['set', monitorIndex, vcpCode, value]);
    }
}

// Usage example
const monitors = ddcswitch.listMonitors(true);
console.log(`Found ${monitors.monitors.length} monitors`);

monitors.monitors.forEach(monitor => {
    console.log(`${monitor.index}: ${monitor.name}`);
    console.log(`  Input: ${monitor.currentInput}`);
    console.log(`  Brightness: ${monitor.brightness || 'N/A'}`);
    console.log(`  Contrast: ${monitor.contrast || 'N/A'}`);
});

// Set brightness and contrast
const brightnessResult = ddcswitch.setBrightness(0, 75);
if (brightnessResult.success) {
    console.log(`? Set brightness to 75%`);
}

const contrastResult = ddcswitch.setContrast(0, 80);
if (contrastResult.success) {
    console.log(`? Set contrast to 80%`);
}
```

#### Example 2: Express.js REST API with VCP Support

```javascript
// server.js - Web API for complete monitor control
const express = require('express');
const { execSync } = require('child_process');

const app = express();
app.use(express.json());

function runDDC(args) {
    try {
        const output = execSync(`ddcswitch ${args.join(' ')} --json`, {
            encoding: 'utf-8'
        });
        return JSON.parse(output);
    } catch (error) {
        return { success: false, error: error.message };
    }
}

// GET /monitors - List all monitors (with verbose option)
app.get('/monitors', (req, res) => {
    const verbose = req.query.verbose === 'true';
    const args = verbose ? ['list', '--verbose'] : ['list'];
    const result = runDDC(args);
    res.json(result);
});

// GET /monitors/:id - Get specific monitor info
app.get('/monitors/:id', (req, res) => {
    const result = runDDC(['get', req.params.id]);
    res.json(result);
});

// GET /monitors/:id/brightness - Get brightness
app.get('/monitors/:id/brightness', (req, res) => {
    const result = runDDC(['get', req.params.id, 'brightness']);
    res.json(result);
});

// GET /monitors/:id/contrast - Get contrast
app.get('/monitors/:id/contrast', (req, res) => {
    const result = runDDC(['get', req.params.id, 'contrast']);
    res.json(result);
});

// POST /monitors/:id/input - Set monitor input
app.post('/monitors/:id/input', (req, res) => {
    const { input } = req.body;
    const result = runDDC(['set', req.params.id, input]);
    res.json(result);
});

// POST /monitors/:id/brightness - Set brightness
app.post('/monitors/:id/brightness', (req, res) => {
    const { percentage } = req.body;
    const result = runDDC(['set', req.params.id, 'brightness', `${percentage}%`]);
    res.json(result);
});

// POST /monitors/:id/contrast - Set contrast
app.post('/monitors/:id/contrast', (req, res) => {
    const { percentage } = req.body;
    const result = runDDC(['set', req.params.id, 'contrast', `${percentage}%`]);
    res.json(result);
});

// POST /monitors/:id/vcp - Set raw VCP value
app.post('/monitors/:id/vcp', (req, res) => {
    const { code, value } = req.body;
    const result = runDDC(['set', req.params.id, code, value]);
    res.json(result);
});

// POST /monitors/:id/profile - Apply complete profile
app.post('/monitors/:id/profile', (req, res) => {
    const { input, brightness, contrast } = req.body;
    const results = {};
    
    if (input) {
        results.input = runDDC(['set', req.params.id, input]);
    }
    if (brightness) {
        results.brightness = runDDC(['set', req.params.id, 'brightness', `${brightness}%`]);
    }
    if (contrast) {
        results.contrast = runDDC(['set', req.params.id, 'contrast', `${contrast}%`]);
    }
    
    res.json({ success: true, results });
});

app.listen(3000, () => {
    console.log('ddcswitch API running on http://localhost:3000');
    console.log('Endpoints:');
    console.log('  GET  /monitors?verbose=true');
    console.log('  GET  /monitors/:id/brightness');
    console.log('  POST /monitors/:id/brightness {"percentage": 75}');
    console.log('  POST /monitors/:id/profile {"input": "HDMI1", "brightness": 75, "contrast": 80}');
});
```

### Batch Script JSON Example

```batch
@echo off
REM complete-setup.bat - Set input, brightness, and contrast

echo Setting up monitor configuration...

REM Switch to HDMI1
for /f "delims=" %%i in ('ddcswitch set 0 HDMI1 --json') do set INPUT_RESULT=%%i
echo %INPUT_RESULT% | find "\"success\":true" >nul
if not errorlevel 1 (
    echo ? Switched to HDMI1
) else (
    echo ? Failed to switch input
)

REM Set brightness to 75%
for /f "delims=" %%i in ('ddcswitch set 0 brightness 75%% --json') do set BRIGHTNESS_RESULT=%%i
echo %BRIGHTNESS_RESULT% | find "\"success\":true" >nul
if not errorlevel 1 (
    echo ? Set brightness to 75%%
) else (
    echo ? Brightness not supported or failed
)

REM Set contrast to 80%
for /f "delims=" %%i in ('ddcswitch set 0 contrast 80%% --json') do set CONTRAST_RESULT=%%i
echo %CONTRAST_RESULT% | find "\"success\":true" >nul
if not errorlevel 1 (
    echo ? Set contrast to 80%%
) else (
    echo ? Contrast not supported or failed
)

echo Monitor setup complete!
pause
```

### Rust JSON Example

```rust
// monitor_controller.rs
use serde::{Deserialize, Serialize};
use std::process::Command;

#[derive(Debug, Deserialize)]
struct MonitorInfo {
    index: u32,
    name: String,
    #[serde(rename = "deviceName")]
    device_name: String,
    #[serde(rename = "isPrimary")]
    is_primary: bool,
    #[serde(rename = "currentInput")]
    current_input: Option<String>,
    #[serde(rename = "currentInputCode")]
    current_input_code: Option<String>,
    brightness: Option<String>,
    contrast: Option<String>,
    status: String,
}

#[derive(Debug, Deserialize)]
struct ListResponse {
    success: bool,
    monitors: Option<Vec<MonitorInfo>>,
    error: Option<String>,
}

#[derive(Debug, Deserialize)]
struct SetResponse {
    success: bool,
    #[serde(rename = "percentageValue")]
    percentage_value: Option<u32>,
    #[serde(rename = "rawValue")]
    raw_value: Option<u32>,
    error: Option<String>,
}

fn run_ddc_command(args: &[&str]) -> Result<String, Box<dyn std::error::Error>> {
    let mut cmd_args = args.to_vec();
    cmd_args.push("--json");
    
    let output = Command::new("ddcswitch")
        .args(&cmd_args)
        .output()?;

    Ok(String::from_utf8_lossy(&output.stdout).to_string())
}

fn list_monitors(verbose: bool) -> Result<ListResponse, Box<dyn std::error::Error>> {
    let args = if verbose { 
        vec!["list", "--verbose"] 
    } else { 
        vec!["list"] 
    };
    
    let json_str = run_ddc_command(&args)?;
    let result: ListResponse = serde_json::from_str(&json_str)?;
    Ok(result)
}

fn set_brightness(monitor_index: u32, percentage: u32) -> Result<SetResponse, Box<dyn std::error::Error>> {
    let args = vec!["set", &monitor_index.to_string(), "brightness", &format!("{}%", percentage)];
    let json_str = run_ddc_command(&args)?;
    let result: SetResponse = serde_json::from_str(&json_str)?;
    Ok(result)
}

fn set_contrast(monitor_index: u32, percentage: u32) -> Result<SetResponse, Box<dyn std::error::Error>> {
    let args = vec!["set", &monitor_index.to_string(), "contrast", &format!("{}%", percentage)];
    let json_str = run_ddc_command(&args)?;
    let result: SetResponse = serde_json::from_str(&json_str)?;
    Ok(result)
}

fn main() -> Result<(), Box<dyn std::error::Error>> {
    // List monitors with verbose info
    let result = list_monitors(true)?;

    if result.success {
        if let Some(monitors) = result.monitors {
            println!("Found {} monitors:", monitors.len());
            for monitor in &monitors {
                println!("{}: {} - Input: {:?}", 
                    monitor.index, 
                    monitor.name, 
                    monitor.current_input);
                println!("   Brightness: {:?}", monitor.brightness);
                println!("   Contrast: {:?}", monitor.contrast);
            }
            
            // Set brightness and contrast on first monitor
            if !monitors.is_empty() {
                let monitor_index = monitors[0].index;
                
                match set_brightness(monitor_index, 75) {
                    Ok(response) if response.success => {
                        println!("? Set brightness to 75%");
                    }
                    Ok(response) => {
                        println!("? Failed to set brightness: {:?}", response.error);
                    }
                    Err(e) => {
                        println!("? Error setting brightness: {}", e);
                    }
                }
                
                match set_contrast(monitor_index, 80) {
                    Ok(response) if response.success => {
                        println!("? Set contrast to 80%");
                    }
                    Ok(response) => {
                        println!("? Failed to set contrast: {:?}", response.error);
                    }
                    Err(e) => {
                        println!("? Error setting contrast: {}", e);
                    }
                }
            }
        }
    } else {
        println!("Error: {:?}", result.error);
    }

    Ok(())
}
```

## Advanced Examples

### Multi-Monitor Setup Scripts

#### Scenario: Work Setup
Switch all monitors to PC inputs with comfortable brightness:

```powershell
# work-setup.ps1
Write-Host "Switching to work setup..." -ForegroundColor Cyan
ddcswitch set 0 DP1
ddcswitch set 0 brightness 60%
ddcswitch set 0 contrast 75%
ddcswitch set 1 DP2
ddcswitch set 1 brightness 60%
ddcswitch set 1 contrast 75%
Write-Host "Work setup ready!" -ForegroundColor Green
```

#### Scenario: Gaming Setup
Switch monitors to console inputs with high brightness:

```powershell
# gaming-setup.ps1
Write-Host "Switching to gaming setup..." -ForegroundColor Cyan
ddcswitch set 0 HDMI1  # Main monitor to PS5
ddcswitch set 0 brightness 90%
ddcswitch set 0 contrast 85%
ddcswitch set 1 HDMI2  # Secondary to Switch
ddcswitch set 1 brightness 85%
ddcswitch set 1 contrast 80%
Write-Host "Gaming setup ready!" -ForegroundColor Green
```

#### Scenario: Movie/Media Setup
Optimize for media consumption:

```powershell
# media-setup.ps1
Write-Host "Switching to media setup..." -ForegroundColor Cyan
ddcswitch set 0 HDMI1  # Media device
ddcswitch set 0 brightness 40%  # Lower brightness for comfortable viewing
ddcswitch set 0 contrast 90%    # High contrast for better blacks
Write-Host "Media setup ready!" -ForegroundColor Green
```

### AutoHotkey Integration

Create a comprehensive input switching and brightness control system:

```autohotkey
; ddcswitch AutoHotkey Script with VCP Support
; Place ddcswitch.exe in C:\Tools\ or update path below

; Global variables
ddcswitchPath := "C:\Tools\ddcswitch.exe"

; Function to run ddcswitch
Runddcswitch(args) {
    global ddcswitchPath
    Run, %ddcswitchPath% %args%, , Hide
}

; Input switching hotkeys
; Ctrl+Alt+1: Switch monitor 0 to HDMI1
^!1::
    Runddcswitch("set 0 HDMI1")
    TrayTip, ddcswitch, Switched to HDMI1, 1
    return

; Ctrl+Alt+2: Switch monitor 0 to HDMI2
^!2::
    Runddcswitch("set 0 HDMI2")
    TrayTip, ddcswitch, Switched to HDMI2, 1
    return

; Ctrl+Alt+D: Switch monitor 0 to DisplayPort
^!d::
    Runddcswitch("set 0 DP1")
    TrayTip, ddcswitch, Switched to DisplayPort, 1
    return

; Brightness control hotkeys
; Ctrl+Alt+Plus: Increase brightness by 10%
^!NumpadAdd::
^!=::
    Runddcswitch("set 0 brightness +10%")
    TrayTip, ddcswitch, Brightness increased, 1
    return

; Ctrl+Alt+Minus: Decrease brightness by 10%
^!NumpadSub::
^!-::
    Runddcswitch("set 0 brightness -10%")
    TrayTip, ddcswitch, Brightness decreased, 1
    return

; Brightness presets
; Ctrl+Alt+F1: 25% brightness (night mode)
^!F1::
    Runddcswitch("set 0 brightness 25%")
    TrayTip, ddcswitch, Night Mode (25%), 1
    return

; Ctrl+Alt+F2: 50% brightness (comfortable)
^!F2::
    Runddcswitch("set 0 brightness 50%")
    TrayTip, ddcswitch, Comfortable (50%), 1
    return

; Ctrl+Alt+F3: 75% brightness (bright)
^!F3::
    Runddcswitch("set 0 brightness 75%")
    TrayTip, ddcswitch, Bright (75%), 1
    return

; Ctrl+Alt+F4: 100% brightness (maximum)
^!F4::
    Runddcswitch("set 0 brightness 100%")
    TrayTip, ddcswitch, Maximum (100%), 1
    return

; Profile hotkeys
; Ctrl+Alt+W: Work setup (all monitors to PC with comfortable settings)
^!w::
    Runddcswitch("set 0 DP1")
    Sleep 500
    Runddcswitch("set 0 brightness 60%")
    Sleep 500
    Runddcswitch("set 0 contrast 75%")
    Sleep 500
    Runddcswitch("set 1 DP2")
    TrayTip, ddcswitch, Work Setup Activated, 1
    return

; Ctrl+Alt+G: Gaming setup (all monitors to console with high brightness)
^!g::
    Runddcswitch("set 0 HDMI1")
    Sleep 500
    Runddcswitch("set 0 brightness 90%")
    Sleep 500
    Runddcswitch("set 0 contrast 85%")
    Sleep 500
    Runddcswitch("set 1 HDMI2")
    TrayTip, ddcswitch, Gaming Setup Activated, 1
    return

; Ctrl+Alt+M: Media setup (HDMI with low brightness, high contrast)
^!m::
    Runddcswitch("set 0 HDMI1")
    Sleep 500
    Runddcswitch("set 0 brightness 40%")
    Sleep 500
    Runddcswitch("set 0 contrast 90%")
    TrayTip, ddcswitch, Media Setup Activated, 1
    return

; Ctrl+Alt+L: List all monitors with verbose info
^!l::
    Run, cmd /k ddcswitch.exe list --verbose
    return

; Ctrl+Alt+I: Show current monitor info
^!i::
    Run, cmd /k "ddcswitch.exe get 0 && ddcswitch.exe get 0 brightness && ddcswitch.exe get 0 contrast && pause"
    return
```

### Stream Deck Integration

If you use Elgato Stream Deck, create actions for complete monitor control:

**Button 1: PC Mode**
```
Title: PC Mode
Command: C:\Tools\ddcswitch.exe set 0 DP1
Arguments: && C:\Tools\ddcswitch.exe set 0 brightness 60%
```

**Button 2: Console Mode**
```
Title: Console Mode
Command: C:\Tools\ddcswitch.exe set 0 HDMI1
Arguments: && C:\Tools\ddcswitch.exe set 0 brightness 90%
```

**Button 3: Brightness Low**
```
Title: ?? Low
Command: C:\Tools\ddcswitch.exe set 0 brightness 25%
```

**Button 4: Brightness High**
```
Title: ?? High
Command: C:\Tools\ddcswitch.exe set 0 brightness 85%
```

**Button 5: Monitor Info**
```
Title: Monitor Info
Command: cmd /k C:\Tools\ddcswitch.exe list --verbose
```

**Button 6: Gaming Profile**
```
Title: ?? Gaming
Command: C:\Tools\ddcswitch.exe set 0 HDMI1
Arguments: && timeout /t 1 && C:\Tools\ddcswitch.exe set 0 brightness 90% && C:\Tools\ddcswitch.exe set 0 contrast 85%
```

### Task Scheduler Integration

Automatically switch inputs at specific times:

#### Morning: Switch to Work Setup (8 AM)

1. Open Task Scheduler
2. Create Basic Task ? "Morning Work Setup"
3. Trigger: Daily at 8:00 AM
4. Action: Start a program
   - Program: `C:\Tools\ddcswitch.exe`
   - Arguments: `set 0 DP1`

#### Evening: Switch to Gaming Setup (6 PM)

Same steps, but with trigger at 6:00 PM and arguments: `set 0 HDMI1`

### Windows Terminal Alias

Add to your PowerShell profile (`$PROFILE`):

```powershell
# ddcswitch aliases for complete monitor control
function ddc-list { ddcswitch list --verbose }
function ddc-work { 
    ddcswitch set 0 DP1
    ddcswitch set 0 brightness 60%
    ddcswitch set 0 contrast 75%
    ddcswitch set 1 DP2
    Write-Host "? Work setup activated" -ForegroundColor Green
}
function ddc-game { 
    ddcswitch set 0 HDMI1
    ddcswitch set 0 brightness 90%
    ddcswitch set 0 contrast 85%
    ddcswitch set 1 HDMI2
    Write-Host "? Gaming setup activated" -ForegroundColor Green
}
function ddc-media {
    ddcswitch set 0 HDMI1
    ddcswitch set 0 brightness 40%
    ddcswitch set 0 contrast 90%
    Write-Host "? Media setup activated" -ForegroundColor Green
}
function ddc-hdmi { ddcswitch set 0 HDMI1 }
function ddc-dp { ddcswitch set 0 DP1 }
function ddc-bright([int]$level) { ddcswitch set 0 brightness "$level%" }
function ddc-contrast([int]$level) { ddcswitch set 0 contrast "$level%" }

# Brightness shortcuts
function ddc-dim { ddcswitch set 0 brightness 25% }
function ddc-normal { ddcswitch set 0 brightness 60% }
function ddc-bright { ddcswitch set 0 brightness 85% }
function ddc-max { ddcswitch set 0 brightness 100% }

# Then use: ddc-work, ddc-game, ddc-media, ddc-bright 75, ddc-list
```

### Complete Monitor Control

Use ddcswitch as a comprehensive monitor management solution:

```powershell
# complete-monitor-control.ps1
param(
    [string]$Profile = "work",  # work, gaming, media, custom
    [int]$Monitor = 0,
    [string]$Input,
    [int]$Brightness,
    [int]$Contrast
)

function Apply-Profile {
    param($ProfileName, $MonitorIndex)
    
    switch ($ProfileName.ToLower()) {
        "work" {
            ddcswitch set $MonitorIndex DP1
            ddcswitch set $MonitorIndex brightness 60%
            ddcswitch set $MonitorIndex contrast 75%
            Write-Host "? Applied work profile" -ForegroundColor Green
        }
        "gaming" {
            ddcswitch set $MonitorIndex HDMI1
            ddcswitch set $MonitorIndex brightness 90%
            ddcswitch set $MonitorIndex contrast 85%
            Write-Host "? Applied gaming profile" -ForegroundColor Green
        }
        "media" {
            ddcswitch set $MonitorIndex HDMI1
            ddcswitch set $MonitorIndex brightness 40%
            ddcswitch set $MonitorIndex contrast 90%
            Write-Host "? Applied media profile" -ForegroundColor Green
        }
        "custom" {
            if ($Input) { ddcswitch set $MonitorIndex $Input }
            if ($Brightness) { ddcswitch set $MonitorIndex brightness "$Brightness%" }
            if ($Contrast) { ddcswitch set $MonitorIndex contrast "$Contrast%" }
            Write-Host "? Applied custom settings" -ForegroundColor Green
        }
    }
}

Apply-Profile -ProfileName $Profile -MonitorIndex $Monitor

# Usage examples:
# .\complete-monitor-control.ps1 -Profile work
# .\complete-monitor-control.ps1 -Profile gaming -Monitor 1
# .\complete-monitor-control.ps1 -Profile custom -Input HDMI2 -Brightness 75 -Contrast 80
```

### Testing Monitor VCP Support

Test what VCP features your monitor supports:

```powershell
# test-vcp-support.ps1 - Test brightness, contrast, and other VCP features
$monitor = 0

Write-Host "Testing VCP feature support for Monitor $monitor" -ForegroundColor Cyan
Write-Host "=" * 50

# Test brightness support
Write-Host "`nTesting Brightness (VCP 0x10)..." -ForegroundColor Yellow
try {
    $brightness = ddcswitch get $monitor brightness 2>$null
    if ($brightness -match "Brightness:") {
        Write-Host "? Brightness supported: $brightness" -ForegroundColor Green
        
        # Test setting brightness
        ddcswitch set $monitor brightness 50% | Out-Null
        Start-Sleep -Seconds 1
        $newBrightness = ddcswitch get $monitor brightness
        Write-Host "? Brightness control works: $newBrightness" -ForegroundColor Green
    } else {
        Write-Host "? Brightness not supported" -ForegroundColor Red
    }
} catch {
    Write-Host "? Brightness not supported" -ForegroundColor Red
}

# Test contrast support
Write-Host "`nTesting Contrast (VCP 0x12)..." -ForegroundColor Yellow
try {
    $contrast = ddcswitch get $monitor contrast 2>$null
    if ($contrast -match "Contrast:") {
        Write-Host "? Contrast supported: $contrast" -ForegroundColor Green
        
        # Test setting contrast
        ddcswitch set $monitor contrast 75% | Out-Null
        Start-Sleep -Seconds 1
        $newContrast = ddcswitch get $monitor contrast
        Write-Host "? Contrast control works: $newContrast" -ForegroundColor Green
    } else {
        Write-Host "? Contrast not supported" -ForegroundColor Red
    }
} catch {
    Write-Host "? Contrast not supported" -ForegroundColor Red
}

# Test raw VCP codes
Write-Host "`nTesting Raw VCP Codes..." -ForegroundColor Yellow
$vcpCodes = @{
    "0x10" = "Brightness"
    "0x12" = "Contrast"
    "0x14" = "Color Temperature"
    "0x16" = "Red Gain"
    "0x18" = "Green Gain"
    "0x1A" = "Blue Gain"
    "0x60" = "Input Source"
    "0x62" = "Audio Volume"
    "0x6C" = "Red Black Level"
    "0x6E" = "Green Black Level"
    "0x70" = "Blue Black Level"
}

foreach ($code in $vcpCodes.Keys) {
    try {
        $result = ddcswitch get $monitor $code 2>$null
        if ($result -and $result -notmatch "error|failed|not supported") {
            Write-Host "? VCP $code ($($vcpCodes[$code])): $result" -ForegroundColor Green
        } else {
            Write-Host "? VCP $code ($($vcpCodes[$code])): Not supported" -ForegroundColor Gray
        }
    } catch {
        Write-Host "? VCP $code ($($vcpCodes[$code])): Not supported" -ForegroundColor Gray
    }
}

Write-Host "`nVCP Support Test Complete!" -ForegroundColor Cyan
```

### Finding Optimal Settings

Find the best brightness and contrast settings for different scenarios:

```powershell
# find-optimal-settings.ps1
Write-Host "Monitor Calibration Helper" -ForegroundColor Cyan
Write-Host "This script will cycle through different brightness/contrast combinations"
Write-Host "Press any key after each setting to continue, or Ctrl+C to stop"
Write-Host ""

$monitor = 0
$brightnessLevels = @(25, 40, 50, 60, 75, 85, 100)
$contrastLevels = @(60, 70, 75, 80, 85, 90, 95)

foreach ($brightness in $brightnessLevels) {
    foreach ($contrast in $contrastLevels) {
        Write-Host "Setting: Brightness $brightness%, Contrast $contrast%" -ForegroundColor Yellow
        
        ddcswitch set $monitor brightness "$brightness%" | Out-Null
        Start-Sleep -Milliseconds 500
        ddcswitch set $monitor contrast "$contrast%" | Out-Null
        
        Write-Host "How does this look? (Press Enter to continue, 'q' to quit, 's' to save this setting)" -ForegroundColor Green
        $input = Read-Host
        
        if ($input -eq 'q') {
            Write-Host "Calibration stopped." -ForegroundColor Red
            break
        } elseif ($input -eq 's') {
            Write-Host "Saved setting: Brightness $brightness%, Contrast $contrast%" -ForegroundColor Cyan
            Write-Host "Command to reproduce: ddcswitch set $monitor brightness $brightness%; ddcswitch set $monitor contrast $contrast%" -ForegroundColor White
            Read-Host "Press Enter to continue or Ctrl+C to stop"
        }
    }
    if ($input -eq 'q') { break }
}
```

### Finding Non-Standard VCP Codes

Some monitors use non-standard DDC/CI codes. If ddcswitch shows the wrong current input or switching doesn't work, you can find the correct codes:

#### Method 1: Use ControlMyMonitor by NirSoft

1. Download [ControlMyMonitor](https://www.nirsoft.net/utils/control_my_monitor.html) (free DDC/CI debugging tool)
2. Run ControlMyMonitor.exe
3. Find your monitor in the list
4. Look for VCP Code `60` (Input Source)
5. Note the **Current Value** - this is the hex code your monitor actually uses
6. Manually switch your monitor's input using its physical buttons
7. Refresh ControlMyMonitor to see the new value
8. Document which physical input corresponds to which hex code

**Example:**
```
VCP Code 60 (Input Source):
- HDMI1 physical input ? Shows value 17 (0x11) ? Standard
- HDMI2 physical input ? Shows value 18 (0x12) ? Standard  
- DisplayPort physical input ? Shows value 15 (0x0F) ? Standard
- DisplayPort physical input ? Shows value 27 (0x1B) ? Non-standard!
```

Once you know the correct codes, use them with ddcswitch:
```powershell
ddcswitch set 0 0x1B  # Use the actual code your monitor responds to
```

#### Method 2: Trial and Error

If ControlMyMonitor isn't available, systematically test codes:

```powershell
# test-all-codes.ps1
Write-Host "Testing input codes for Monitor 0" -ForegroundColor Cyan
Write-Host "Watch your monitor and note which codes cause it to switch inputs" -ForegroundColor Yellow
Write-Host ""

$codes = @(0x01, 0x02, 0x03, 0x04, 0x0F, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B)

foreach ($code in $codes) {
    $hexCode = "0x{0:X2}" -f $code
    Write-Host "Testing $hexCode..." -ForegroundColor Green
    ddcswitch set 0 $hexCode
    Start-Sleep -Seconds 3
}

Write-Host ""
Write-Host "Testing complete! Document which codes worked for your monitor." -ForegroundColor Cyan
```

## Troubleshooting Examples

### Check if Monitor Responds to DDC/CI

```powershell
# Get current input (if this works, DDC/CI is functional)
ddcswitch get 0

# Get by monitor name
ddcswitch get "VG270U"

# List all monitors with all VCP values
ddcswitch get all

# Try setting to current input (should succeed instantly)
ddcswitch list  # Note the current input
ddcswitch set 0 <current-input>
```

### Dealing with Slow Monitors

Some monitors are slow to respond to DDC/CI commands. Add delays:

```powershell
# slow-switch.ps1
ddcswitch set 0 HDMI1
Start-Sleep -Seconds 2  # Wait for monitor to switch
ddcswitch set 1 HDMI2
Start-Sleep -Seconds 2
Write-Host "Done" -ForegroundColor Green
```

### Monitor by Name Instead of Index

Useful if monitor order changes:

```powershell
# Find monitor with "LG" in the name and switch it
ddcswitch list  # Note the exact name
ddcswitch set "LG ULTRAGEAR" HDMI1

# Partial name matching works
ddcswitch set "ULTRAGEAR" HDMI1

# Get settings by monitor name
ddcswitch get "VG270U" brightness
ddcswitch get "Generic PnP"  # Gets all VCP values for this monitor
```

## Integration with Other Tools

### PowerToys Run

Add ddcswitch to your PATH, then use PowerToys Run (Alt+Space):

```
> ddcswitch set 0 HDMI1
```

### Windows Run Dialog

Press Win+R and type:

```
ddcswitch set 0 DP1
```

### Batch File Shortcuts

Create `.bat` files on your desktop:

**switch-to-hdmi.bat:**
```batch
@echo off
"C:\Tools\ddcswitch.exe" set 0 HDMI1
```

**switch-to-dp.bat:**
```batch
@echo off
"C:\Tools\ddcswitch.exe" set 0 DP1
```

Make them double-clickable for quick access!

## Tips and Tricks

1. **Add to PATH**: Add ddcswitch.exe location to your Windows PATH for easier access
2. **Create shortcuts**: Right-click ddcswitch.exe ? Send to ? Desktop (create shortcut), then edit properties to add arguments
3. **Use monitoring**: Combine with other tools to detect when certain apps launch and switch inputs automatically
4. **Test first**: Always test with `list` and `get` before creating automation scripts
5. **Admin rights**: Some monitors require running as Administrator - right-click and "Run as administrator"

## Common Patterns

### Pattern 1: Toggle Between Settings

```powershell
# toggle-brightness.ps1 - Toggle between low/medium/high brightness
$current = ddcswitch get 0 brightness --json | ConvertFrom-Json

if ($current.success) {
    $currentPercent = $current.percentageValue
    
    # Cycle through 25% ? 50% ? 75% ? 100% ? 25%
    $newBrightness = switch ($currentPercent) {
        {$_ -le 25} { 50 }
        {$_ -le 50} { 75 }
        {$_ -le 75} { 100 }
        default { 25 }
    }
    
    ddcswitch set 0 brightness "$newBrightness%"
    Write-Host "Brightness: $currentPercent% ? $newBrightness%" -ForegroundColor Green
} else {
    Write-Host "Brightness control not supported" -ForegroundColor Red
}
```

### Pattern 2: Check Before Set

```powershell
# smart-profile-switch.ps1
param([string]$Profile = "work")

# Get current settings
$inputResult = ddcswitch get 0 --json | ConvertFrom-Json
$brightnessResult = ddcswitch get 0 brightness --json | ConvertFrom-Json

if (-not $inputResult.success) {
    Write-Error "Monitor not accessible"
    exit 1
}

# Define profiles
$profiles = @{
    "work" = @{ input = "DP1"; brightness = 60; contrast = 75 }
    "gaming" = @{ input = "HDMI1"; brightness = 90; contrast = 85 }
    "media" = @{ input = "HDMI1"; brightness = 40; contrast = 90 }
}

if (-not $profiles.ContainsKey($Profile)) {
    Write-Error "Unknown profile: $Profile. Available: work, gaming, media"
    exit 1
}

$targetProfile = $profiles[$Profile]

# Apply settings only if different
if ($inputResult.currentInputCode -ne $targetProfile.input) {
    ddcswitch set 0 $targetProfile.input
    Write-Host "? Input: $($targetProfile.input)" -ForegroundColor Green
}

if ($brightnessResult.success -and $brightnessResult.percentageValue -ne $targetProfile.brightness) {
    ddcswitch set 0 brightness "$($targetProfile.brightness)%"
    Write-Host "? Brightness: $($targetProfile.brightness)%" -ForegroundColor Green
}

ddcswitch set 0 contrast "$($targetProfile.contrast)%"
Write-Host "? Profile '$Profile' applied" -ForegroundColor Cyan
```

### Pattern 3: Sync All Monitors

```powershell
# sync-all-monitors.ps1 - Apply same settings to all monitors
param(
    [string]$Input = "HDMI1",
    [int]$Brightness = 75,
    [int]$Contrast = 80
)

$result = ddcswitch list --json | ConvertFrom-Json

if (-not $result.success) {
    Write-Error $result.error
    exit 1
}

$okMonitors = $result.monitors | Where-Object { $_.status -eq "ok" }

foreach ($monitor in $okMonitors) {
    Write-Host "Configuring monitor $($monitor.index) ($($monitor.name))..." -ForegroundColor Cyan
    
    # Set input
    $inputResult = ddcswitch set $monitor.index $Input --json | ConvertFrom-Json
    if ($inputResult.success) {
        Write-Host "  ? Input: $Input" -ForegroundColor Green
    } else {
        Write-Host "  ? Input failed: $($inputResult.error)" -ForegroundColor Red
    }
    
    # Set brightness
    $brightnessResult = ddcswitch set $monitor.index brightness "$Brightness%" --json | ConvertFrom-Json
    if ($brightnessResult.success) {
        Write-Host "  ? Brightness: $Brightness%" -ForegroundColor Green
    } else {
        Write-Host "  ? Brightness not supported" -ForegroundColor Yellow
    }
    
    # Set contrast
    $contrastResult = ddcswitch set $monitor.index contrast "$Contrast%" --json | ConvertFrom-Json
    if ($contrastResult.success) {
        Write-Host "  ? Contrast: $Contrast%" -ForegroundColor Green
    } else {
        Write-Host "  ? Contrast not supported" -ForegroundColor Yellow
    }
    
    Start-Sleep -Milliseconds 500  # Prevent DDC/CI overload
}

Write-Host "Synchronized $($okMonitors.Count) monitors" -ForegroundColor Cyan
```

Happy switching and brightness controlling!

