param([string]$OutputDirectory = "dist")
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$out = Join-Path $root $OutputDirectory
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { throw '.NET Framework 4.x est requis.' }
New-Item -ItemType Directory -Force $out | Out-Null
$references = @('/r:System.Drawing.dll', '/r:System.Windows.Forms.dll', '/r:System.IO.Compression.dll', '/r:System.IO.Compression.FileSystem.dll', '/r:System.Core.dll', '/r:Microsoft.CSharp.dll')
$sources = @((Join-Path $root 'src\Engine.cs'), (Join-Path $root 'src\AddIn.cs'), (Join-Path $root 'src\LayoutCleaner.cs'),(Join-Path $root 'src\ImageGeometry.cs'))
& $compiler /nologo /target:library /platform:anycpu /optimize+ "/out:$out\PPTimizCore.dll" @references @sources
if ($LASTEXITCODE -ne 0) { throw 'Compilation du complément échouée.' }
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ "/out:$out\PPTimizNext.exe" "/r:$out\PPTimizCore.dll" /r:System.Windows.Forms.dll /r:System.Drawing.dll (Join-Path $root 'src\Program.cs') (Join-Path $root 'src\Launcher.cs')
if ($LASTEXITCODE -ne 0) { throw 'Compilation de l application échouée.' }
Copy-Item -LiteralPath (Join-Path $root 'Install-Standalone.ps1'), (Join-Path $root 'Installer.cmd'), (Join-Path $root 'README.md') -Destination $out -Force
Write-Host "Compilation réussie : $out"
