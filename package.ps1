param([string]$OutputDirectory = "dist")
$ErrorActionPreference='Stop'
& (Join-Path $PSScriptRoot 'build.ps1') -OutputDirectory $OutputDirectory
$release=Join-Path $PSScriptRoot 'releases'
New-Item -ItemType Directory -Path $release -Force | Out-Null
$files=@('PPTimizNext.exe','PPTimizCore.dll','Install-Standalone.ps1','Installer.cmd','README.md') | ForEach-Object { Join-Path (Join-Path $PSScriptRoot $OutputDirectory) $_ }
$archive=Join-Path $release 'SlideSlim-2.2-Windows.zip'
Compress-Archive -LiteralPath $files -DestinationPath $archive -Force
Write-Output $archive
