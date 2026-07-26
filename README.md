# NcPasswords

NcPasswords is a lightweight, read-only Windows desktop client for the Nextcloud
[Passwords](https://apps.nextcloud.com/apps/passwords) app. It lets you browse, search and copy your existing
Nextcloud passwords without opening a browser.

> **Disclaimer:** This project — including portions of its source code, tooling and documentation — was built
> with the assistance of Claude (Anthropic's AI coding assistant). It has not undergone third-party security
> auditing. As with any tool that handles credentials, please review the source and use it at your own
> discretion, particularly in sensitive environments.

## Features

- Sign in with your Nextcloud server URL, username and password (use an **app password** if your account has
  two-factor authentication enabled).
- Your connection details are encrypted at rest for your Windows user account (DPAPI) — nothing is stored in
  plain text.
- Entries are cached locally under `%LOCALAPPDATA%\NcPasswords` and refreshed on launch or on demand.
- Folders and entries live in a single combined tree — a navigation style borrowed from
  [Password Safe](https://github.com/pwsafe/pwsafe), which NcPasswords' tree view is deliberately modeled after.
  Entries show only their name and username (`Name [username]`); everything else stays out of view until you
  need it.
- Double-click an entry to copy its password, or select it and press `Ctrl+C` to copy its username — the
  clipboard clears itself automatically after 30 seconds. The right-click menu offers both, plus "Details...".
- Search filters the tree by name, username, URL or notes, keeping only the matching entries and the folders
  that contain them.
- Open an entry's details (via the right-click menu) to see all fields, custom fields and tags, each with its
  own copy button.
- Runs from the system tray: closing the main window minimizes it to the tray instead of quitting; use the
  tray icon's menu (or double-click it) to reopen the window, and "Exit" to close the app fully.

**Limitation:** NcPasswords only supports accounts using Nextcloud's default server-side encryption (SSE).
Accounts with client-side encryption (CSE) enabled are detected on sign-in and rejected with a clear message.

## Installation

- **winget:** `winget install Kaliumhexacyanoferrat.NcPasswords` *(pending submission to the winget-pkgs
  repository — see `winget/README.md`)*.
- **Installer:** download `NcPasswords-Setup-<version>.exe` from the
  [latest release](https://github.com/Kaliumhexacyanoferrat/nc-passwords-windows/releases/latest) and run it.
  It installs for your user only (no administrator rights required) and adds Start Menu / optional desktop
  shortcuts.
- **Portable:** download `NcPasswords-<version>-win-x64.zip` from the same page and run `NcPasswords.exe`
  directly — no installation needed.

The installer and executable are currently **unsigned**, so Windows SmartScreen may show an "unknown publisher"
warning the first time you run them. This is expected; choose "More info" → "Run anyway" to proceed.

## Project layout

- `src/NcPasswords.Core` — API client, models, local cache and credential storage (no WPF dependency).
- `src/NcPasswords.App` — WPF UI (MVVM via CommunityToolkit.Mvvm).
- `tests/NcPasswords.Core.Tests` — unit tests for the API client, cache, folder tree and search.
- `installer/` — Inno Setup script used to build the installer.
- `winget/` — winget package manifest, kept in sync with published releases.

## Building & running from source

```
dotnet build NcPasswords.slnx
dotnet test tests/NcPasswords.Core.Tests/NcPasswords.Core.Tests.csproj
dotnet run --project src/NcPasswords.App/NcPasswords.App.csproj
```

## Releasing

Releases are built manually via the **Release** GitHub Actions workflow (`workflow_dispatch`), which publishes a
self-contained `win-x64` build and attaches two assets to a new GitHub release:

- `NcPasswords-<version>-win-x64.zip` — the raw published files (portable).
- `NcPasswords-Setup-<version>.exe` — the Inno Setup installer (`installer/NcPasswords.iss`).

No code signing certificate is required to submit an unsigned installer to winget. See `winget/README.md` for
updating the winget manifest after cutting a release.
