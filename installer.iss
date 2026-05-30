[Setup]
AppName=Real Estate Manager
AppVersion=1.0
AppPublisher=Furqan, Eisha, Rasba
DefaultDirName={localappdata}\RealEstateManager
DefaultGroupName=RealEstateManager
OutputDir=Output
OutputBaseFilename=RealEstateManagerSetup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
SetupIconFile=office-block.ico

[Files]
Source: "C:\Users\arsha\Documents\University Work Semester 3\VP\VP\bin\Release\net8.0-windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Real Estate Manager"; Filename: "{app}\Project.exe"
Name: "{commondesktop}\Real Estate Manager"; Filename: "{app}\Project.exe"

[Run]
Filename: "{app}\Project.exe"; Description: "Launch Real Estate Manager"; Flags: nowait postinstall skipifsilent