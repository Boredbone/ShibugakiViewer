
rem at powershell:  .\build.bat 2>&1 | Add-Content -Path log.log -PassThru

call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Common7\Tools\VsDevCmd.bat"

cd %~dp0

cd "..\bin\Debug\netcoreapp3.0"
CreatePackage.exe clean
if %ERRORLEVEL% neq 0 (
    echo Failed error=%ERRORLEVEL%
    exit /B
)


cd "..\..\..\Scripts"

dotnet restore "..\..\ShibugakiViewer.sln"
if %ERRORLEVEL% neq 0 (
    echo Failed error=%ERRORLEVEL%
    exit /B
)


MSBuild "..\..\ShibugakiViewer.sln" /t:clean;rebuild /p:Configuration=Release;Platform="Any CPU" /fl /v:m
if %ERRORLEVEL% neq 0 (
    echo Failed error=%ERRORLEVEL%
    exit /B
)

cd "..\bin\Debug\netcoreapp3.0"
CreatePackage.exe file
if %ERRORLEVEL% neq 0 (
    echo Failed error=%ERRORLEVEL%
    exit /B
)

CreatePackage.exe zip
if %ERRORLEVEL% neq 0 (
    echo Failed error=%ERRORLEVEL%
    exit /B
)


cd "..\..\..\Installer"
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer.iss
if %ERRORLEVEL% neq 0 (
    echo Failed error=%ERRORLEVEL%
    exit /B
)



rem dotnet publish "..\..\ShibugakiViewer\ShibugakiViewer.csproj" -c Release /p:PublishProfile="..\..\ShibugakiViewer\Properties\PublishProfiles\FolderProfile.pubxml"

