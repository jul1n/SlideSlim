param([string]$OutputDirectory = "dist")
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$out = Join-Path $root $OutputDirectory
& (Join-Path $root 'build.ps1') -OutputDirectory $OutputDirectory
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $compiler /nologo /target:exe "/out:$out\Tests.exe" "/r:$out\PPTimizCore.dll" /r:System.Drawing.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /r:System.Core.dll (Join-Path $root 'tests\Tests.cs') (Join-Path $root 'tests\SmartTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
$testDir = Join-Path $root ('tests\run-' + [Guid]::NewGuid().ToString('N'))
& (Join-Path $out 'Tests.exe') $testDir
if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
