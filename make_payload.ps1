param(
  [Parameter(Mandatory=$true)][string]$AppDir,
  [string]$Out = "payload.zip"
)

# Builds the compressed payload that the installer carries.
# -AppDir must point at a folder containing EMTweaks.exe and resources\app.asar.

$sevenZip = "C:\Program Files\7-Zip\7z.exe"
if (-not (Test-Path -LiteralPath $sevenZip)) { throw "7-Zip not found at $sevenZip" }
if (-not (Test-Path -LiteralPath (Join-Path $AppDir "EMTweaks.exe"))) { throw "No EMTweaks.exe in $AppDir" }

$work = Join-Path $env:TEMP ("emt-payload-" + [guid]::NewGuid().ToString("N"))
$stage = Join-Path $work "EMTweaks"
New-Item -ItemType Directory -Path $stage -Force | Out-Null
Copy-Item -Path (Join-Path $AppDir '*') -Destination $stage -Recurse -Force

# The stock uninstaller targets registry keys that do not exist on a fresh machine.
$old = Join-Path $stage "Uninstall EMTweaks.exe"
if (Test-Path -LiteralPath $old) { Remove-Item -LiteralPath $old -Force }

$zip = Join-Path (Get-Location) $Out
& $sevenZip a -tzip -mx=9 -y $zip $stage | Out-Null
Remove-Item -LiteralPath $work -Recurse -Force

"payload written: $zip ($((Get-Item -LiteralPath $zip).Length) bytes)"
