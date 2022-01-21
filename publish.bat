@echo off

cd %~dp0
del .\XDConsumptionNavigator.zip
dotnet publish --runtime win-x64 --self-contained /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true --configuration Release
powershell Compress-Archive -Path .\bin\Release\net6.0\win-x64\publish\* -DestinationPath .\XDConsumptionNavigator.zip -Force