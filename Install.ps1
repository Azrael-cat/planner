$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
try {
    $target = Join-Path $env:LOCALAPPDATA 'DeskTaskAssistant\app'
    New-Item -ItemType Directory -Path $target -Force | Out-Null
    foreach ($name in @('DeskTaskAssistant.exe', 'Uninstall.ps1', '使用说明.txt')) {
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination (Join-Path $target $name) -Force
    }
    $shell = New-Object -ComObject WScript.Shell
    $locations = @([Environment]::GetFolderPath('Desktop'), [Environment]::GetFolderPath('Programs'), [Environment]::GetFolderPath('Startup'))
    foreach ($folder in $locations) {
        $link = $shell.CreateShortcut((Join-Path $folder 'DeskTaskAssistant.lnk'))
        $link.TargetPath = Join-Path $target 'DeskTaskAssistant.exe'
        $link.WorkingDirectory = $target
        $link.Description = '桌面任务小助手'
        $link.Save()
    }
    $key = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\DeskTaskAssistant'
    New-Item -Path $key -Force | Out-Null
    New-ItemProperty -Path $key -Name DisplayName -Value '桌面任务小助手' -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $key -Name DisplayVersion -Value '1.0.0' -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $key -Name InstallLocation -Value $target -PropertyType String -Force | Out-Null
    $uninstall = 'powershell.exe -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File "' + (Join-Path $target 'Uninstall.ps1') + '"'
    New-ItemProperty -Path $key -Name UninstallString -Value $uninstall -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $key -Name NoModify -Value 1 -PropertyType DWord -Force | Out-Null
    New-ItemProperty -Path $key -Name NoRepair -Value 1 -PropertyType DWord -Force | Out-Null
    Start-Process -FilePath (Join-Path $target 'DeskTaskAssistant.exe') -WindowStyle Normal
    [System.Windows.Forms.MessageBox]::Show('安装完成，已开启登录 Windows 后自动启动。可在任务中心关闭自启动，也可在 Windows 已安装的应用中卸载。', '桌面任务小助手') | Out-Null
} catch {
    [System.Windows.Forms.MessageBox]::Show(('安装失败：' + $_.Exception.Message), '桌面任务小助手') | Out-Null
    exit 1
}
