# FsBootStrapper

FsBootStrapper (FSBS) is a lightweight Windows launcher for **Microsoft Flight Simulator 2024** that automates starting your add-ons, tools, and utilities in the right order — before and after MSFS — and cleans them up again when you're done flying.

## Features

### ✈️ MSFS Detection & Launch
- Automatic detection of MSFS 2024 installations via **Steam** and **Xbox / Microsoft Store**, with a manual "Detect" option and a **Custom executable path** fallback
- One-click launch sequence from the Dashboard: runs your *before-MSFS* items, starts MSFS, waits for it, then runs your *after-MSFS* items — all with a live progress log (waiting / launching / launched / failed)

### 📋 Launch Profiles
- Create, rename, duplicate, and delete named profiles, each holding an ordered list of add-ons to launch alongside MSFS
- Per add-on: display name, executable path, launch arguments, launch phase (before/after MSFS), manual reordering, pre-/post-launch delays, and an optional "wait for process" condition before launching
- **Import from MSFS `exe.xml`**: pull your existing add-on entries straight from MSFS's own config, with an option to back up and strip them from `exe.xml` so they aren't launched twice
- Remembers your last-used profile between sessions

### 🛑 Smart Shutdown
- When MSFS exits, FSBS automatically closes tracked add-ons — either a graceful window-close (with a configurable timeout before falling back to a force-kill) or an immediate kill, configurable per item
- **Shutdown process targeting**: for launcher/updater-style add-ons whose original process has already exited, configure a separate process name to close (exact or keyword match)
- Mark add-ons that already close themselves on MSFS exit as "auto-close supported" so FSBS leaves them alone
- Manual **Stop All** command to shut everything down on demand
- Optional auto-close of FSBS itself once MSFS and all add-ons have shut down cleanly

### 🔐 UAC Elevation Handling
- Add-ons can be flagged as requiring administrator privileges; when FSBS runs elevated, they inherit the elevated token
- Non-elevated add-ons are automatically de-elevated to medium integrity when FSBS itself is elevated, so they can still talk to non-elevated MSFS/Xbox processes

### 🔔 Tray & Notifications
- Minimize to system tray, with an optional "close to tray after launch" behavior
- Balloon-tip notifications (with a message-box fallback) for add-on crashes and MSFS exit, independently toggleable

### ⚙️ Settings
- Central settings page to configure MSFS detection/installation type, tray behavior, notifications, and auto-close — plus the current app version at a glance

## Requirements

- Windows 10 (build 19041) or Windows 11, 64-bit
- [.NET 9 Desktop Runtime](https://aka.ms/dotnet/9.0/windowsdesktop-runtime-win-x64.exe) (the installer detects and prompts you if it's missing)

## Installation

Download the latest `FsBootStrapper-Setup-x.x.x.exe` from the [Releases](../../releases) page and run it. The installer checks for the .NET 9 Desktop Runtime and guides you through installing it if needed.

## Getting Started

1. Open **Settings** and let FSBS auto-detect your MSFS 2024 installation (or set a custom executable path).
2. Go to the **Profile Editor** and create a profile, adding the add-ons/tools you want to launch alongside MSFS — or import them directly from your existing `exe.xml`.
3. For each add-on, configure its launch phase (before/after MSFS), any delays, and how FSBS should close it when MSFS exits.
4. From the **Dashboard**, pick your profile and installation, then launch — FSBS takes care of the rest.

## Building from Source

```
git clone https://github.com/GabrielePe/FlightSimulatorBootStrapper.git
cd FlightSimulatorBootStrapper
dotnet build FsBootStrapper.sln
```

The solution is split into:
- `src/FsBootStrapper` — the WPF UI application
- `src/FsBootStrapper.Core` — core business logic (profiles, launch orchestration, process monitoring, detection)
- `tests/FsBootStrapper.Tests` — unit tests

To build the installer, use [Inno Setup](https://jrsoftware.org/isinfo.php) with `installer/FsBootStrapper.iss` after publishing the app to the `publish` folder.

## License

This project is licensed under the [MIT License](LICENSE).
