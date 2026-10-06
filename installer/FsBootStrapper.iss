; ─────────────────────────────────────────────────────────────────────────────
;  FsBootStrapper – Inno Setup Script
; ─────────────────────────────────────────────────────────────────────────────

#define AppName      "FsBootStrapper"
#define AppVersion   "0.0.7"
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

[CustomMessages]
italian.DotNetMissing=.NET 9 Desktop Runtime non è installato su questo computer.%n%nFsBootStrapper richiede .NET 9 Desktop Runtime per funzionare.%nVuoi aprire la pagina di download Microsoft adesso?%n%nDopo averlo installato, riavvia questo installer.
english.DotNetMissing=.NET 9 Desktop Runtime is not installed on this computer.%n%nFsBootStrapper requires .NET 9 Desktop Runtime to run.%nDo you want to open the Microsoft download page now?%n%nAfter installing it, restart this installer.

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
  Cerca .NET 9 Windows Desktop Runtime controllando la cartella
  C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App\9.*
  che è sempre presente indipendentemente dalla struttura del registro.
}
function IsDotNet9DesktopRuntimeInstalled(): Boolean;
var
  DotnetPath: string;
  FindRec: TFindRec;
begin
  Result := False;
  DotnetPath := ExpandConstant('{pf}\dotnet\shared\Microsoft.WindowsDesktop.App\');
  if FindFirst(DotnetPath + '9.*', FindRec) then
  begin
    try
      repeat
        if FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY <> 0 then
        begin
          Result := True;
          Exit;
        end;
      until not FindNext(FindRec);
    finally
      FindClose(FindRec);
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

  Answer := MsgBox(CustomMessage('DotNetMissing'), mbConfirmation, MB_YESNO);

  if Answer = IDYES then
    ShellExec('open', DownloadUrl, '', '', SW_SHOWNORMAL, ewNoWait, ResultCode);

  // Blocca l'installazione: l'utente deve prima installare il runtime
  Result := False;
end;