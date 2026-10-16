$ErrorActionPreference='Stop'
& (Join-Path $PSScriptRoot 'build.ps1')
$release=Join-Path $PSScriptRoot 'releases'
New-Item -ItemType Directory -Path $release -Force | Out-Null
$files=@('PPTimizNext.exe','PPTimizCore.dll','Install-Standalone.ps1','Installer.cmd','README.md') | ForEach-Object { Join-Path (Join-Path $PSScriptRoot 'dist') $_ }
$archive=Join-Path $release 'PPTimizNext-Standalone-2.1.zip'
Compress-Archive -LiteralPath $files -DestinationPath $archive -Force
Write-Output $archive
