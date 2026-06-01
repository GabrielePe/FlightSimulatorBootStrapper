; ─────────────────────────────────────────────────────────────────────────────
;  FsBootStrapper – Inno Setup Script
; ─────────────────────────────────────────────────────────────────────────────

#define AppName      "FsBootStrapper"
#define AppVersion   "0.0.4"
#define AppPublisher "Gabriele"
#define AppExeName   "FsBootStrapper.exe"
#define PublishDir   "publish"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL=https://github.com/
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
AllowNoIcons=yes
OutputDir=output
OutputBaseFilename={#AppName}-Setup-{#AppVersion}
SetupIconFile=..\src\FsBootStrapper\Resources\Icons\FSBSIcon.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible
MinVersion=10.0.19041
DisableProgramGroupPage=yes

[Languages]
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{commondesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Crea un'icona sul Desktop"; GroupDescription: "Icone aggiuntive:"

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Avvia {#AppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; rimuove la cartella logs creata a runtime
Type: filesandordirs; Name: "{app}\logs"

; ─────────────────────────────────────────────────────────────────────────────
;  Codice Pascal: verifica .NET 9 Desktop Runtime e propone installazione
; ─────────────────────────────────────────────────────────────────────────────
[Code]

{ 
  Cerca il runtime Windows Desktop .NET 9.x nel registro.
  Microsoft lo registra sotto:
  HKLM\SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App
  come valori DWORD con nome = versione (es. "9.0.5") e valore = 1.
}
function IsDotNet9DesktopRuntimeInstalled(): Boolean;
var
  Names: TArrayOfString;
  I: Integer;
  RegKey: string;
begin
  Result := False;
  RegKey := 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App';
  if not RegGetValueNames(HKLM, RegKey, Names) then
    Exit;
  for I := 0 to GetArrayLength(Names) - 1 do
  begin
    // Basta che inizi con "9." per considerarlo compatibile
    if Copy(Names[I], 1, 2) = '9.' then
    begin
      Result := True;
      Exit;
    end;
  end;
end;

function InitializeSetup(): Boolean;
var
  Answer: Integer;
  DownloadUrl: string;
  ResultCode: Integer;
begin
  Result := True;

  if IsDotNet9DesktopRuntimeInstalled() then
    Exit;

  DownloadUrl := 'https://aka.ms/dotnet/9.0/windowsdesktop-runtime-win-x64.exe';

  Answer := MsgBox(
    '.NET 9 Desktop Runtime non è installato su questo computer.' + #13#10 + #13#10 +
    'FsBootStrapper richiede .NET 9 Desktop Runtime per funzionare.' + #13#10 +
    'Vuoi aprire la pagina di download Microsoft adesso?' + #13#10 + #13#10 +
    'Dopo averlo installato, riavvia questo installer.',
    mbConfirmation, MB_YESNO);

  if Answer = IDYES then
    ShellExec('open', DownloadUrl, '', '', SW_SHOWNORMAL, ewNoWait, ResultCode);

  // Blocca l'installazione: l'utente deve prima installare il runtime
  Result := False;
end;