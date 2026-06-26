# Creates a "Productivity Hub" shortcut on your Desktop that launches the app.
# Run once:  powershell -ExecutionPolicy Bypass -File .\create-shortcut.ps1

$ErrorActionPreference = 'Stop'

$repoRoot   = $PSScriptRoot
$launcher   = Join-Path $repoRoot 'launch.cmd'
$desktop    = [Environment]::GetFolderPath('Desktop')
$shortcut   = Join-Path $desktop 'Productivity Hub.lnk'

$shell = New-Object -ComObject WScript.Shell
$lnk = $shell.CreateShortcut($shortcut)
$lnk.TargetPath       = $launcher
$lnk.WorkingDirectory = $repoRoot
$lnk.Description       = 'Launch Productivity Hub'
$lnk.IconLocation      = 'shell32.dll,43'   # generic checklist-style icon
$lnk.Save()

Write-Host "Created shortcut: $shortcut"
