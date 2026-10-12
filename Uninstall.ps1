$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
$answer = [System.Windows.Forms.MessageBox]::Show('是否卸载桌面任务小助手？', '确认卸载', 'YesNo', 'Question')
if ($answer -ne 'Yes') { exit }
$erase = [System.Windows.Forms.MessageBox]::Show('是否同时删除所有任务、总结与本地备份？选“否”会保留数据，重新安装后可继续使用。', '是否清除数据', 'YesNo', 'Question')
try {
    $root = [IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'DeskTaskAssistant'))
    $app = [IO.Path]::GetFullPath((Join-Path $root 'app'))
    $expected = [IO.Path]::GetFullPath((Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'DeskTaskAssistant'))
    if ($root -ne $expected -or $app -ne (Join-Path $expected 'app')) { throw '卸载路径验证失败' }
    Get-Process -Name DeskTaskAssistant -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq (Join-Path $app 'DeskTaskAssistant.exe') } | Stop-Process -Force
    foreach ($folder in @([Environment]::GetFolderPath('Desktop'), [Environment]::GetFolderPath('Programs'), [Environment]::GetFolderPath('Startup'))) {
        $shortcut = Join-Path $folder 'DeskTaskAssistant.lnk'
        if (Test-Path -LiteralPath $shortcut) { Remove-Item -LiteralPath $shortcut -Force }
    }
    $key = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\DeskTaskAssistant'
    if (Test-Path -LiteralPath $key) { Remove-Item -LiteralPath $key -Recurse -Force }
    if (Test-Path -LiteralPath $app) { Remove-Item -LiteralPath $app -Recurse -Force }
    if ($erase -eq 'Yes' -and (Test-Path -LiteralPath $root)) { Remove-Item -LiteralPath $root -Recurse -Force }
    [System.Windows.Forms.MessageBox]::Show('已卸载。项目源文件与您自行导出的备份不会被删除。', '桌面任务小助手') | Out-Null
} catch {
    [System.Windows.Forms.MessageBox]::Show(('卸载未完成：' + $_.Exception.Message), '桌面任务小助手') | Out-Null
    exit 1
}
