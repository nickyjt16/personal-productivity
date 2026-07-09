# Creates a Desktop shortcut to the published WPF desktop app.
#
# Publish as a normal MULTI-FILE build (NOT single-file). Single-file bundles
# extract native executables to a temp folder at runtime, which corporate
# Microsoft Defender ASR rules (e.g. "block untrusted/low-prevalence
# executables") flag repeatedly — causing "blocked by your IT administrator"
# prompts every few minutes. A multi-file build loads its DLLs in place, so
# nothing executable is written to temp:
#   dotnet publish src\ProductivityHub.Desktop -c Release -r win-x64 --self-contained false
# (needs the .NET Desktop Runtime 9 installed; add --self-contained true for a
#  runtime-free build — still multi-file, just larger.)
# Then run:
#   powershell -ExecutionPolicy Bypass -File .\create-desktop-app-shortcut.ps1

$ErrorActionPreference = 'Stop'
$repo = $PSScriptRoot
$exe = Join-Path $repo 'src\ProductivityHub.Desktop\bin\Release\net9.0-windows\win-x64\publish\ProductivityHub.Desktop.exe'

if (-not (Test-Path $exe)) {
    Write-Host "Published exe not found. Publish it first (see the comment at the top of this script)."
    exit 1
}

# If the exe was downloaded (e.g. from a GitHub Release), Windows tags it with a
# "Mark of the Web", which makes SmartScreen show a "Windows protected your PC"
# warning. Unblock it so the app launches without that prompt.
Unblock-File -Path $exe -ErrorAction SilentlyContinue
Write-Host "Unblocked the app (clears the 'downloaded from the internet' flag)."

$desktop = [Environment]::GetFolderPath('Desktop')
$shortcut = Join-Path $desktop 'Productivity Hub.lnk'

$shell = New-Object -ComObject WScript.Shell
$lnk = $shell.CreateShortcut($shortcut)
$lnk.TargetPath = $exe
$lnk.WorkingDirectory = Split-Path $exe
$lnk.IconLocation = 'shell32.dll,43'
$lnk.Description = 'Productivity Hub (desktop)'
$lnk.Save()

Write-Host "Created shortcut: $shortcut"
