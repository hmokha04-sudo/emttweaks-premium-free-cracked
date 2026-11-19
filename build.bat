@echo off
setlocal
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set B=%~dp0
"%CSC%" /nologo /target:winexe /out:"%B%uninstall.exe" /r:System.dll /r:System.Core.dll "%B%Uninstaller.cs"
if errorlevel 1 exit /b 1
"%CSC%" /nologo /target:winexe /out:"%B%setup_raw.exe" /r:System.dll /r:System.Core.dll /r:System.Windows.Forms.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /resource:"%B%uninstall.exe",emtuninst "%B%Installer.cs"
if errorlevel 1 exit /b 1
echo compiled
