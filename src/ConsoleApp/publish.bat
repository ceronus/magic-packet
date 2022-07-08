@echo off
:: *********************************** SETTINGS ***********************************

:: Set the project name to build / publish
set "ProjectName=ConsoleApp.csproj"

:: Set the relative path of the publish profiles
set "PublishProfilePath=ConsoleApp\Properties\PublishProfiles"

:: Set the output directory as defined in each publish profile
:: Make sure that the publish profiles all go to the same place.
set "OutputDirectory=.\bin\Publish\net6.0"

:: Set to true, if you want to delete all the publish files and only ouput the
:: archive (.zip) files only. Otherwise, set to false to keep both.
set "OnlyOutputArchiveFiles=true"

:: Set the list of publish profiles to excuted (comma-separated)
:: Do not use spaces.
set "PublishProfiles=linux-arm,linux-x64,osx-x64,win-arm,win-arm64,win-x64,win-x86"

:: ********************************************************************************


:: Clean the output directory
:: i.e. Delete everything, including subfolders.
rd /s  /q %OutputDirectory%

:: Iterate over each defined publish profile

for %%p in (%PublishProfiles%) do (
  echo %%p
  :: Execute dotnet publish
  dotnet publish %ProjectName% /p:PublishProfile=%PublishProfilePath%\%%p.pubxml /p:DebugType=None /p:DebugSymbols=false /p:Configuration=Release
  :: Create the archive (.zip) file
  powershell.exe -nologo -noprofile -command "Compress-Archive -Path %OutputDirectory%\%%p -DestinationPath %OutputDirectory%\%%p"
  :: Delete the directory and files from dotnet publish (only keep the archive file)
  if %OnlyOutputArchiveFiles% equ true rd /s /q %OutputDirectory%\%%p
)