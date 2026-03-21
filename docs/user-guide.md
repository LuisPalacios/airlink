# User Guide

## What is AirLink?

AirLink is a lightweight Windows utility that makes connecting your Apple AirPods effortless. Windows does not provide a quick way to connect already-paired Bluetooth audio devices — AirLink solves this with a single click from the system tray.

## System Requirements

- Windows 10 version 2004 (May 2020 Update) or later
- Bluetooth adapter
- Apple AirPods already paired in Windows Bluetooth settings

## Installation

AirLink is a portable application — no installer required.

1. Download `AirLink.exe` from the [GitHub Releases](https://github.com/LuisPalacios/airlink/releases) page.
2. Place it anywhere you like (e.g., `C:\bin\AirLink.exe`).
3. Double-click to run.

That's it. AirLink will appear in your system tray (the small icon area near the clock).

## How to Use

### Connecting Your Device

**Left-click** the AirLink tray icon. On the first click, a device picker dialog will show all your paired Bluetooth devices — select yours and click OK. AirLink will remember your choice and connect automatically on subsequent clicks.

To switch to a different device later, right-click and select **Select Device**.

### Context Menu (Right-Click)

Right-click the tray icon to access these options:

| Option | Description |
| --- | --- |
| **Help** | Shows app information, author, and GitHub link |
| **Select Device** | Choose which paired Bluetooth device to connect |
| **Shortcut** | Configure a global keyboard shortcut to toggle connection |
| **Notifications** | Toggle balloon tip notifications on connect/disconnect |
| **Run at Startup** | Toggle whether AirLink starts automatically with Windows |
| **Exit** | Closes AirLink completely |

### Keyboard Shortcut

You can assign a global keyboard shortcut to toggle your AirPods connection from anywhere in Windows — no need to click the tray icon.

1. Right-click the tray icon and select **Shortcut**.
2. In the dialog, press your desired key combination (e.g., `Ctrl+Win+Shift+A`). You need at least one modifier key (Ctrl, Alt, Shift, or Win) plus a letter or number.
3. Check **Enable shortcut** and click **OK**.

The shortcut works system-wide, even when other applications are focused. The menu item will show the active shortcut (e.g., "Shortcut (Ctrl+Win+Shift+A)").

To disable the shortcut, open the dialog again and uncheck **Enable shortcut**.

### Icons

The tray icon changes based on two factors:

- **Connection state** — blue icon when connected, gray when disconnected
- **Windows theme** — adapts to your taskbar's light or dark mode setting

### First Launch

On first launch, AirLink automatically registers itself to start with Windows. You can disable this via the "Run at Startup" option in the context menu.

## Troubleshooting

### Notifications stay too long

AirLink shows brief notifications when connecting or disconnecting. The duration is controlled by Windows, not AirLink. To change it, go to **Settings > Accessibility > Visual effects** and adjust "Dismiss notifications after this amount of time."

You can also disable AirLink notifications entirely via the **Notifications** option in the right-click context menu.

### AirPods not found

- Make sure your AirPods are paired in **Settings > Bluetooth & devices**.
- Open the AirPods case near your PC before clicking the tray icon.
- Ensure Bluetooth is enabled on your PC.

### Connection fails

- Close and reopen the AirPods case, then try again.
- Remove and re-pair the AirPods in Windows Bluetooth settings.
- Restart the Bluetooth adapter: toggle Bluetooth off and on in Windows Settings.

### Icon not visible in the system tray

Windows may hide tray icons by default. Click the **^** arrow in the taskbar to find AirLink, then drag it to the visible area.

## Uninstall

1. Right-click the tray icon and select **Exit**.
2. If "Run at Startup" was enabled, it was stored in the Windows Registry at `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`. Exiting the app and deleting the EXE is sufficient — or disable "Run at Startup" before exiting.
3. Delete `AirLink.exe`.
