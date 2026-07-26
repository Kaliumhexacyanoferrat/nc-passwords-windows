# NcPasswords

A read-only Windows desktop client (WPF, .NET 10) for the Nextcloud [Passwords](https://apps.nextcloud.com/apps/passwords) app.

## Scope

- Login with server URL, username and password (use a Nextcloud **app password** if your account has 2FA enabled).
- Connection details are stored encrypted for the current Windows user (DPAPI).
- Entries are cached locally under `%LOCALAPPDATA%\NcPasswords` and refreshed on launch or on demand.
- Folder tree on the left (with an "All Passwords" view) plus search across name/username/URL/notes.
- Right-click an entry for a context menu (copy username/password, details), or select it and press Ctrl+C to copy the password (clipboard clears automatically after 30s).
- A details window (`...` button or context menu) shows all fields, custom fields and tags with per-field copy.
- **Only supports accounts using the default server-side encryption (SSE).** Accounts with client-side encryption (CSE) enabled are detected and rejected with a clear message.

## Project layout

- `src/NcPasswords.Core` — API client, models, local cache/credential storage (no WPF dependency).
- `src/NcPasswords.App` — WPF UI (MVVM via CommunityToolkit.Mvvm).
- `tests/NcPasswords.Core.Tests` — unit tests for the API client, cache, folder tree and search.

## Building & running

```
dotnet build NcPasswords.slnx
dotnet test tests/NcPasswords.Core.Tests/NcPasswords.Core.Tests.csproj
dotnet run --project src/NcPasswords.App/NcPasswords.App.csproj
```

## Releasing

Releases are built manually via the **Release** GitHub Actions workflow (`workflow_dispatch`), which publishes a `win-x64` build and attaches it as a ZIP to a new GitHub release.
