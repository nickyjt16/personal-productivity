# Creates a Desktop shortcut to the published WPF desktop app.
# Publish first:
#   dotnet publish src\ProductivityHub.Desktop -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
# Then run:
#   powershell -ExecutionPolicy Bypass -File .\create-desktop-app-shortcut.ps1

$ErrorActionPreference = 'Stop'
$repo = $PSScriptRoot
$exe = Join-Path $repo 'src\ProductivityHub.Desktop\bin\Release\net9.0-windows\win-x64\publish\ProductivityHub.Desktop.exe'

if (-not (Test-Path $exe)) {
    Write-Host "Published exe not found. Publish it first (see the comment at the top of this script)."
    exit 1
}

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
