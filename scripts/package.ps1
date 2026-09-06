$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.Drawing
$root = Split-Path $PSScriptRoot -Parent
$manifest = Get-Content (Join-Path $root 'manifest.json') -Raw | ConvertFrom-Json
if ($manifest.name -notmatch '^[A-Za-z0-9_]{1,128}$' -or $manifest.version_number -notmatch '^\d+\.\d+\.\d+$') { throw 'Invalid package name/version.' }
if ($manifest.description.Length -gt 250) { throw 'Description exceeds 250 characters.' }
$dll = Join-Path $root 'bin/Release/netstandard2.1/FoodUnlock.dll'
$version = [Reflection.AssemblyName]::GetAssemblyName($dll).Version
if ($version.ToString(3) -ne $manifest.version_number) { throw 'DLL and manifest versions differ.' }
$icon = [Drawing.Image]::FromFile((Join-Path $root 'icon.png'))
try { if ($icon.Width -ne 256 -or $icon.Height -ne 256) { throw 'Icon must be 256 by 256.' } } finally { $icon.Dispose() }
$dist = Join-Path $root 'dist'
New-Item -ItemType Directory -Path $dist -Force | Out-Null
$output = Join-Path $dist ($manifest.name + '-' + $manifest.version_number + '.zip')
$stream = [IO.File]::Open($output, [IO.FileMode]::Create)
$zip = [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($name in 'manifest.json','README.md','CHANGELOG.md','LICENSE','icon.png') {
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, (Join-Path $root $name), $name) | Out-Null
    }
    [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $dll, 'plugins/FoodUnlock/FoodUnlock.dll') | Out-Null
} finally { $zip.Dispose(); $stream.Dispose() }
$check = [IO.Compression.ZipFile]::OpenRead($output)
try {
    if ($check.Entries.Count -ne 6) { throw 'Unexpected archive contents.' }
    $check.Entries | Select-Object FullName, Length
} finally { $check.Dispose() }
Get-FileHash -LiteralPath $output | Select-Object Hash
Write-Output "Ready: $output"
