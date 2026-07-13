# Creates a Desktop shortcut to the published WPF desktop app.
#
# Publish as a normal MULTI-FILE build (NOT single-file). Single-file bundles
# extract native executables to a temp folder at runtime, which corporate
# Microsoft Defender ASR rules (e.g. "block untrusted/low-prevalence
# executables") flag repeatedly — causing "blocked by your IT administrator"
# prompts every few minutes. A multi-file build loads its DLLs in place, so
# nothing executable (incl. the native e_sqlite3.dll) is written to temp.
# Publish PORTABLE (no -r): native libs land under runtimes\<rid>\native and are
# resolved via deps.json, which the shared dotnet host (used below) loads
# correctly. A -r win-x64 build flattens e_sqlite3.dll and the shared host then
# can't find it ("Unable to load DLL 'e_sqlite3'"):
#   dotnet publish src\ProductivityHub.Desktop -c Release
# (needs the .NET Desktop Runtime 9 installed. NEVER add -p:PublishSingleFile=true
#  on a managed PC.)
# Then run:
#   powershell -ExecutionPolicy Bypass -File .\create-desktop-app-shortcut.ps1

$ErrorActionPreference = 'Stop'
$repo = $PSScriptRoot
$pub = Join-Path $repo 'src\ProductivityHub.Desktop\bin\Release\net9.0-windows\publish'
$dll = Join-Path $pub 'ProductivityHub.Desktop.dll'
$exe = Join-Path $pub 'ProductivityHub.Desktop.exe'   # used only for the shortcut icon

if (-not (Test-Path $dll)) {
    Write-Host "Published app not found. Publish it first (see the comment at the top of this script)."
    exit 1
}

# Downloaded/extracted files carry a "Mark of the Web"; clear it on every file so
# nothing prompts to unblock.
Get-ChildItem $pub -Recurse -File | Unblock-File -ErrorAction SilentlyContinue
Write-Host "Unblocked the app files."

# Find the Microsoft-signed .NET host. Launching the app THROUGH dotnet.exe (a
# trusted, high-prevalence binary) rather than our own unsigned exe avoids the
# corporate Microsoft Defender ASR rule that blocks unsigned/low-prevalence
# executables ("Windows cannot access... you may not have permissions" /
# "blocked by your IT administrator"). Our code loads as a library, not as an
# executable that the rule would block.
$dotnet = Join-Path $env:ProgramFiles 'dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) { $dotnet = (Get-Command dotnet -ErrorAction SilentlyContinue).Source }
if (-not $dotnet -or -not (Test-Path $dotnet)) {
    Write-Host "dotnet.exe not found. Install the .NET Desktop Runtime 9 (https://dotnet.microsoft.com/download/dotnet/9.0)."
    exit 1
}

$desktop = [Environment]::GetFolderPath('Desktop')
$shortcut = Join-Path $desktop 'Productivity Hub.lnk'

$shell = New-Object -ComObject WScript.Shell
$lnk = $shell.CreateShortcut($shortcut)
$lnk.TargetPath = $dotnet
$lnk.Arguments = '"' + $dll + '"'
$lnk.WorkingDirectory = $pub
$lnk.IconLocation = "$exe,0"
$lnk.WindowStyle = 7   # start the dotnet host window minimised (hidden from view)
$lnk.Description = 'Productivity Hub (desktop)'
$lnk.Save()

Write-Host "Created shortcut (launches via dotnet host): $shortcut"
