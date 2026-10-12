param([switch]$NoLaunch)
$ErrorActionPreference = 'Stop'
$target = Join-Path $env:LOCALAPPDATA 'DeskTaskAssistant\app'
$exe = Join-Path $target 'DeskTaskAssistant.exe'
if (-not (Test-Path -LiteralPath $exe)) { throw 'Installed application not found.' }
$expected = [IO.Path]::GetFullPath((Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'DeskTaskAssistant\app\DeskTaskAssistant.exe'))
if ([IO.Path]::GetFullPath($exe) -ne $expected) { throw 'Unexpected installation path.' }
$installedHash = (Get-FileHash -LiteralPath $exe).Hash
$running = @(Get-Process -Name DeskTaskAssistant -ErrorAction SilentlyContinue | Where-Object {
    $_.Path -eq $exe -or ($_.Path -and $_.Path.EndsWith('\DeskTaskAssistant\app\DeskTaskAssistant.exe', [StringComparison]::OrdinalIgnoreCase) -and (Get-FileHash -LiteralPath $_.Path).Hash -eq $installedHash)
})
foreach ($process in $running) {
    Stop-Process -Id $process.Id -Force
    if (-not $process.WaitForExit(5000)) { throw 'The running application has not exited yet.' }
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'DeskTaskAssistant.exe') -Destination $exe -Force
if (-not $NoLaunch) { Start-Process -FilePath $exe -WindowStyle Normal }
Write-Output ('Updated and launched: ' + $exe)
Write-Output ('Startup shortcut exists: ' + (Test-Path -LiteralPath (Join-Path ([Environment]::GetFolderPath('Startup')) 'DeskTaskAssistant.lnk')))
