# winget manifest

This mirrors what gets submitted to [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs) at
`manifests/k/Kaliumhexacyanoferrat/NcPasswords/<version>/`. It is **not** installable from here — winget only
resolves packages from that community repo (or a private source), so these files exist purely so the manifest
is versioned alongside the app it describes.

Package identifier: `Kaliumhexacyanoferrat.NcPasswords`.

## After cutting a release

1. Run the **Release** workflow for the new version (produces the zip and `NcPasswords-Setup-<version>.exe`).
2. Update (or create) `manifests/k/Kaliumhexacyanoferrat/NcPasswords/<version>/` — easiest via
   [`wingetcreate`](https://github.com/microsoft/winget-create):
   ```
   winget install wingetcreate
   wingetcreate update Kaliumhexacyanoferrat.NcPasswords `
     --version <version> `
     --urls https://github.com/Kaliumhexacyanoferrat/nc-passwords-windows/releases/download/v<version>/NcPasswords-Setup-<version>.exe `
     --submit
   ```
   `wingetcreate` downloads the installer, computes `InstallerSha256`, and opens the PR against
   `winget-pkgs` directly — copy the resulting files back into this folder afterwards so it stays in sync.
3. If doing it by hand instead: copy the previous version's folder, bump `PackageVersion` in all three files,
   update `InstallerUrl`, and replace `InstallerSha256` with the real hash
   (`Get-FileHash NcPasswords-Setup-<version>.exe -Algorithm SHA256`).

## First submission

The `0.1.0` manifest in this folder is a **template**, not a submitted package — `InstallerSha256` is a
placeholder and no release has been cut yet. Before submitting for real: cut an actual release, fill in the
real hash, then run `winget validate <folder>` and `wingetcreate submit <folder>` (or open the PR by hand).

Unsigned installers are accepted by winget-pkgs; expect a SmartScreen "unknown publisher" warning until/unless
a code signing certificate is added later.
