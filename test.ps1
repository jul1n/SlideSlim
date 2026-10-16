$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
& (Join-Path $root 'build.ps1')
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $compiler /nologo /target:exe "/out:$root\dist\Tests.exe" "/r:$root\dist\PPTimizCore.dll" /r:System.Drawing.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /r:System.Core.dll (Join-Path $root 'tests\Tests.cs') (Join-Path $root 'tests\SmartTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
$testDir = Join-Path $root ('tests\run-' + [Guid]::NewGuid().ToString('N'))
& (Join-Path $root 'dist\Tests.exe') $testDir
if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
