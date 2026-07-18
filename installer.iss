; installer.iss
; Script do Inno Setup para compilação do instalador AbleToDJ

[Setup]
AppId={{D9B2CE74-9C78-4395-88B1-3E5A4476D1F9}
AppName=AbleToDJ
AppVersion=1.0.0
AppPublisher=Marentropico
DefaultDirName={localappdata}\AbleToDJ
DisableDirPage=yes
DisableProgramGroupPage=yes
OutputDir=.
OutputBaseFilename=AbleToDJ_Installer
SetupIconFile=icon.ico
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\AbleToDJ.exe
PrivilegesRequired=lowest

[Files]
Source: "src\LiveBridge.App\bin\Publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "src\AbletonScript\DDJ_LiveBridge\*"; DestDir: "{userdocs}\Ableton\User Library\Remote Scripts\DDJ_LiveBridge"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\AbleToDJ"; Filename: "{app}\AbleToDJ.exe"
Name: "{autodesktop}\AbleToDJ"; Filename: "{app}\AbleToDJ.exe"

[Run]
Filename: "{app}\AbleToDJ.exe"; Description: "Iniciar AbleToDJ agora"; Flags: nowait postinstall skipifsilent
