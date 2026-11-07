; Ascent Bell installer — Inno Setup 6 (https://jrsoftware.org/isdl.php)
; Build:  .\build.ps1   (or open this file in Inno Setup and press Ctrl+F9)
; Output: Installer\Output\AscentBellSetup.exe

#define AppName    "Ascent Bell"
#define AppVersion "1.0"
#define AppExe     "AscentBellTool.exe"

[Setup]
AppId={{6F1C9A52-4B7D-4E0A-9C3B-2D8E5A71F0B4}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher=Ascent Info Solutions
DefaultDirName={autopf}\Ascent Bell
DefaultGroupName=Ascent Bell
DisableProgramGroupPage=yes
OutputDir=Output
OutputBaseFilename=AscentBellSetup
SetupIconFile=..\Assets\bell.ico
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
; Windows 7 SP1 or later
MinVersion=6.1sp1
; Matches Program.MutexName — setup asks the user to close a running copy before upgrading/uninstalling.
AppMutex=AscentBellTool_SingleInstance

[Tasks]
Name: "autostart"; Description: "Start Ascent Bell automatically when Windows starts (recommended)"
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked

[Dirs]
; Schedule + logs live here; every Windows user on the PC may edit them.
; Kept on uninstall so a reinstall keeps the school's timings.
Name: "{commonappdata}\AscentBell"; Permissions: users-modify

[Files]
Source: "..\bin\Release\{#AppExe}";        DestDir: "{app}"; Flags: ignoreversion
Source: "..\bin\Release\{#AppExe}.config"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}";           Filename: "{app}\{#AppExe}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}";     Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Registry]
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "AscentBell"; \
  ValueData: """{app}\{#AppExe}"""; Flags: uninsdeletevalue; Tasks: autostart

[Run]
Filename: "{app}\{#AppExe}"; Description: "Start {#AppName} now"; Flags: nowait postinstall skipifsilent runasoriginaluser

[UninstallRun]
Filename: "{sys}\taskkill.exe"; Parameters: "/f /im {#AppExe}"; Flags: runhidden; RunOnceId: "StopAscentBell"

[Code]
// .NET Framework 4.8 = Release >= 528040
function IsDotNet48Installed(): Boolean;
var
  release: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', release)
            and (release >= 528040);
end;

function InitializeSetup(): Boolean;
var
  ErrorCode: Integer;
begin
  Result := IsDotNet48Installed();
  if not Result then
  begin
    if MsgBox('Ascent Bell needs Microsoft .NET Framework 4.8, which is not installed on this PC.' + #13#10#13#10 +
              'Click OK to open the Microsoft download page. Install it, restart the PC, then run this setup again.',
              mbError, MB_OKCANCEL) = IDOK then
      ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet-framework/net48', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
  end;
end;
