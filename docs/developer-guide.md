# Developer Guide

This guide walks you through setting up a development environment for AirLink from scratch on a fresh Windows machine.

## Prerequisites

### Git for Windows

Download and install [Git for Windows](https://gitforwindows.org/). This provides `git`, Git Bash, and other Unix utilities.

During installation, the defaults are fine. Make sure "Git from the command line and also from 3rd-party software" is selected so `git` is available in your terminal.

### Windows Terminal (Recommended)

Install [Windows Terminal](https://aka.ms/terminal) from the Microsoft Store or [GitHub](https://github.com/microsoft/terminal). It provides a modern terminal experience with tabs, profiles for PowerShell, Command Prompt, and Git Bash.

### IDE and SDK

Install [Visual Studio Community 2026](https://visualstudio.microsoft.com/downloads/) with the **".NET desktop development"** workload selected in the Visual Studio Installer.

This workload includes:

- .NET 10 SDK
- WinForms and WPF designers
- Debugging tools

**Recommended optional components** (in the workload's "Optional" tab):

- Development tools for .NET (required)
- Just-In-Time debugger

You can uncheck everything else (Entity Framework tools, ML.NET, Blend, JavaScript diagnostics) — they are not needed for this project.

**Verify the SDK is installed:**

```shell
dotnet --version
# Should output 10.x.xxx or later
```

### ImageMagick (Optional)

Only needed if you want to regenerate the icon assets from the SVG source files.

Download from [imagemagick.org](https://imagemagick.org/script/download.php#windows) and ensure `magick` is in your PATH.

## Clone and Build

```shell
git clone https://github.com/LuisPalacios/airlink.git
cd AirLink
```

### Generate Assets (Optional)

The `.ico` and `banner.png` files are already committed. To regenerate them from the SVG sources:

```shell
pwsh scripts/convert-assets.ps1
```

### Build

```shell
# From repo's root folder
dotnet build
```

Or open `AirLink.slnx` in Visual Studio 2026 and build from the IDE.

### Run

```shell
# From repo's root folder, will build if not done already
dotnet run --project src/AirLink
```

AirLink will appear in the system tray. Right-click > Exit to close.

## Project Architecture

```text
src/AirLink/
├── Program.cs                    Entry point, single-instance mutex
├── TrayApplicationContext.cs     Main orchestrator (NotifyIcon, context menu)
└── Services/
    ├── BluetoothService.cs       Device discovery, connection, monitoring (WinRT APIs)
    ├── HotkeyService.cs          Global hotkey registration (Win32 P/Invoke + registry config)
    ├── RegistryService.cs        Windows startup registry management
    ├── ThemeService.cs           Light/dark theme detection and change events
    └── IconService.cs            Embedded icon loading based on state + theme
```

**Key design decisions:**

- **No window.** The app uses `ApplicationContext` instead of a `Form`. `Application.Run(new TrayApplicationContext())` keeps the message loop alive without showing any UI.
- **Service layer.** Business logic is separated from the WinForms `NotifyIcon` code into testable service classes.
- **Embedded resources.** The 4 `.ico` files are embedded in the assembly so the single-file EXE is fully self-contained.
- **Global hotkey.** `HotkeyService` uses a hidden `NativeWindow` to receive `WM_HOTKEY` messages via Win32 `RegisterHotKey` P/Invoke. Configuration (modifier flags + key code) is stored in `HKCU\SOFTWARE\AirLink`.
- **WinRT APIs.** The project targets `net10.0-windows10.0.19041.0` to access `Windows.Devices.Bluetooth` and `Windows.Devices.Enumeration` directly without extra NuGet packages.

## Testing

```shell
dotnet test
```

Tests cover:

- **BluetoothService** — AirPods device name matching logic
- **RegistryService** — Startup registry read/write round-trip (uses a test-only registry subkey)
- **ThemeService** — Registry theme value reading
- **IconService** — Resource name generation for all state/theme combinations

## Creating a Release Build

Build in Release configuration (without single-file packaging):

```shell
dotnet build -c Release
```

Build the final single portable executable for distribution:

```shell
dotnet publish src/AirLink/AirLink.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

The output EXE will be at:

```text
src/AirLink/bin/Release/net10.0-windows10.0.19041.0/win-x64/publish/AirLink.exe
```

## Contributing

1. Fork the repository on GitHub.
2. Create a feature branch: `git checkout -b my-feature`.
3. Make your changes and ensure tests pass: `dotnet test`.
4. Push and open a Pull Request against `main`.
