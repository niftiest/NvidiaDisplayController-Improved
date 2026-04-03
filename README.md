
# NVIDIA Display Controller - Improved

A fork of [Mario Laurianti's NvidiaDisplayController](https://github.com/therealmariolaurianti/NvidiaDisplayController) with additional features and fixes. The original project is no longer functional with current NVIDIA drivers — this fork restores compatibility and adds new capabilities.

> **Original Author:** [Mario Laurianti](https://github.com/therealmariolaurianti) — MIT License
>
> **Fork Maintainer:** [NiftiestPixel](https://github.com/niftiest)

## What's New in This Fork

### Custom Global Hotkeys
- Assign any key combination (Ctrl/Alt/Shift + key) to profiles via right-click context menu
- Hotkeys work globally — no need to have the app focused
- Toggle behavior: press hotkey to activate profile, press again to return to default
- Conflict detection prevents duplicate bindings across profiles
- Hotkey labels displayed on profile buttons

### System Tray Integration
- Runs minimized to system tray
- Tooltip shows active profile for each monitor
- Right-click context menu (Show / Exit)
- Left-click tray icon to restore window

### UI and Usability Improvements
- Profile right-click context menu (Set Hotkey, Clear Hotkey, Remove)
- Revert button to undo unsaved profile changes
- Help/About dialog with reset functionality
- "Start With Windows" option via registry integration
- Improved multi-monitor handling and profile isolation

## Features

- Adjust brightness, contrast, gamma, and digital vibrance per monitor
- Automatic detection of all connected NVIDIA displays
- Up to 5 profiles per monitor with easy switching
- Apply default profiles automatically on startup
- Lightweight (under 100 MB)

## Requirements

- NVIDIA GPU (with installed drivers)
- Windows 10 / 11

## How to Use

1. Download the latest release from the [Releases](https://github.com/niftiest/NvidiaDisplayController-Improved/releases) page
2. Extract and run `NvidiaDisplayController.exe`
3. Select a monitor from the top, create profiles with the green **+** button
4. Adjust settings (Brightness, Contrast, Gamma, Digital Vibrance) and click **Apply**
5. Right-click a profile to assign a global hotkey

Data is stored alongside the executable in `Data\Data.json`. Use the Help button to reset if needed.

## Building from Source

Requires .NET 7 SDK with Windows Desktop workload:

```
dotnet build NvidiaDisplayController.sln
```

## Credits

- **Mario Laurianti** — Original [NvidiaDisplayController](https://github.com/therealmariolaurianti/NvidiaDisplayController)
- **[MahApps.Metro](https://mahapps.com/)** — UI framework
- **[WindowsDisplayAPI](https://github.com/falahati/WindowsDisplayAPI)** — Display management library
- **[Freepik](https://www.flaticon.com/free-icons/computer)** — Application icon

## License

[MIT License](LICENSE) — Copyright (c) 2023 Mario Laurianti
