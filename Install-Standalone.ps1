$ErrorActionPreference='Stop'
$source=if(Test-Path -LiteralPath (Join-Path $PSScriptRoot 'PPTimizNext.exe')) {$PSScriptRoot} else {Join-Path $PSScriptRoot 'dist'}
$target=Join-Path $env:USERPROFILE 'Applications\PPTimizNext\Standalone-2.1'
New-Item -ItemType Directory -Path $target -Force | Out-Null
foreach($name in @('PPTimizNext.exe','PPTimizCore.dll')) { Copy-Item -LiteralPath (Join-Path $source $name) -Destination (Join-Path $target $name) -Force }
# Disable only our own previous add-ins. No Office application needs to be stopped.
foreach($id in @('PPTimizNext.Native','PPTimizNext.AddIn','PPTimizNext.Connect')) {
 $key="HKCU:\Software\Microsoft\Office\PowerPoint\Addins\$id"
 if(Test-Path -LiteralPath $key) { Set-ItemProperty -LiteralPath $key -Name LoadBehavior -Value 0 -Type DWord }
}
$shell=New-Object -ComObject WScript.Shell
foreach($folder in @([Environment]::GetFolderPath('Desktop'),[Environment]::GetFolderPath('Programs'))) {
 $shortcut=$shell.CreateShortcut((Join-Path $folder 'PPTimiz Next.lnk'))
 $shortcut.TargetPath=Join-Path $target 'PPTimizNext.exe'; $shortcut.WorkingDirectory=$target
 $shortcut.Description='Compresser une copie PowerPoint — application indépendante'
 $shortcut.Save()
}
Write-Output "Application indépendante installée : $target"

