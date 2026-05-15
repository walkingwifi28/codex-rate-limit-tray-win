#define MyAppId "WalkingWiFi.CodexRateLimitTray"
#define MyAppName "Codex レート制限"
#define MyAppExeName "CodexRateLimitTray.exe"
#define MyAppVersion GetEnv("APP_VERSION")

#if MyAppVersion == ""
  #define MyAppVersion "0.1.0"
#endif

[Setup]
AppId={{6A646903-84B8-4E53-A9A1-96F49E1427EC}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=WalkingWiFi
DefaultDirName={autopf}\CodexRateLimitTray
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=Output
OutputBaseFilename=CodexRateLimitTray-{#MyAppVersion}-win-x64-setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayName={#MyAppName}
CloseApplications=yes

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "startup"; Description: "Windows 起動時に実行"; GroupDescription: "追加設定:"; Flags: unchecked

[Files]
Source: "..\artifacts\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{#MyAppName} を起動"; Flags: nowait postinstall skipifsilent

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppId}"; ValueData: """{app}\{#MyAppExeName}"""; Tasks: startup

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
