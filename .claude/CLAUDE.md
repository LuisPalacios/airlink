# CLAUDE.md

This file provides guidance to Claude Code when working with code in this repository.

## What this repo is

Windows .NET 10 C# Windows Forms (WinForms) system tray application that simplifies Bluetooth connectivity with Apple AirPods. No visible window — runs exclusively as a `NotifyIcon` via `ApplicationContext`.

## Build Commands

```shell
# Build
dotnet build

# Run
dotnet run --project src/AirLink

# Test
dotnet test

# Regenerate icons from SVGs (requires ImageMagick)
pwsh scripts/convert-assets.ps1

# Publish single-file EXE
dotnet publish src/AirLink/AirLink.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

## Architecture

- **Solution file:** `AirLink.slnx` (.NET 10 XML solution format)
- **TFM:** `net10.0-windows10.0.19041.0` (WinRT API access for Bluetooth)
- **Entry point:** `src/AirLink/Program.cs` → `TrayApplicationContext`
- **Services (src/AirLink/Services/):**
  - `BluetoothService` — WinRT device discovery, connection, monitoring
  - `HotkeyService` — Global hotkey via Win32 RegisterHotKey + NativeWindow + registry config
  - `RegistryService` — HKCU startup registry management
  - `ThemeService` — Light/dark theme detection via registry + SystemEvents
  - `IconService` — Embedded .ico resource loading by state/theme
- **Tests:** `tests/AirLink.Tests/` (xUnit, tests business logic only)
- **Assets:** `assets/` — SVG sources + generated .ico and banner.png
- **Docs:** `docs/user-guide.md` (consumers), `docs/developer-guide.md` (onboarding)

## Workflow Orchestration

**Priority order when rules conflict**: Correctness > Simplicity > Elegance.

### 1. Plan First

- Enter plan mode for ANY non-trivial task (3+ steps or architectural decisions)
- If something goes sideways, STOP and re-plan immediately — don't keep pushing
- Write detailed specs upfront to reduce ambiguity

### 2. Subagent Strategy

- Use subagents liberally to keep main context window clean
- Offload research, exploration, and parallel analysis to subagents
- One task per subagent for focused execution

### 3. Verify Before Done

- Never mark a task complete without proving it works
- Test scripts with `bash -n` (syntax check) and `shellcheck` when available
- Validate config templates render correctly before committing
- Diff behavior between main and your changes when relevant

### 4. Autonomous Bug Fixing

- When given a bug report: just fix it. Don't ask for hand-holding
- Point at logs, errors, failing tests — then resolve them
- Zero context switching required from the user

### 5. Learn from Corrections

- After corrections from the user: save a `feedback` memory via the memory system
- Do NOT maintain a separate lessons file — use `.claude/projects/.../memory/` exclusively

## Task Management

1. **Plan First**: Write a plan with checkable items before starting
2. **Verify Plan**: Check in before starting implementation
3. **Track Progress**: Mark items complete as you go
4. **Explain Changes**: High-level summary at each step

## Core Principles

- **Simplicity First**: Make every change as simple as possible. Impact minimal code.
- **No Laziness**: Find root causes. No temporary fixes. Senior developer standards.
- **Minimal Impact**: Changes should only touch what's necessary. Avoid introducing bugs.
- **Skills first**: Check if a skill exists before doing the work manually
- **Self-improve**: When a skill fails or has gaps, update its `SKILL.md` with the fix
- **Zero entropy**: Never create files outside defined structure
