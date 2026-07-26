; Inno Setup script for NcPasswords.
;
; Expects a self-contained win-x64 `dotnet publish` output. Pass its location and the
; app version via command-line defines, e.g.:
;
;   iscc installer\NcPasswords.iss /DPublishDir=..\publish /DAppVersion=1.0.0
;
; Both defines have local-testing defaults so the script also compiles standalone.

#ifndef PublishDir
  #define PublishDir "..\publish"
#endif
#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif

#define AppName "NcPasswords"
#define AppPublisher "Kaliumhexacyanoferrat"
#define AppURL "https://github.com/Kaliumhexacyanoferrat/nc-passwords-windows"
#define AppExeName "NcPasswords.exe"

[Setup]
; Fixed GUID identifying this application across versions - do not change.
AppId={{6F5D9C2E-6F2C-4B84-9F9E-6E3E4C6F0B77}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
VersionInfoVersion={#AppVersion}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
UninstallDisplayIcon={app}\{#AppExeName}
DisableProgramGroupPage=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=commandline
Compression=lzma2
SolidCompression=yes
OutputDir=..\artifacts
OutputBaseFilename=NcPasswords-Setup-{#AppVersion}
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent
