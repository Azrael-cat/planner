$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$output = Join-Path $root 'DeskTaskAssistant.exe'
& $compiler /nologo /target:winexe /optimize+ /codepage:65001 /utf8output ("/win32icon:" + (Join-Path $root "App.ico")) "/out:$output" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Windows.Forms.DataVisualization.dll /reference:System.Web.Extensions.dll /reference:Microsoft.CSharp.dll (Join-Path $root 'TaskAssistant.cs') (Join-Path $root 'Card.cs') (Join-Path $root 'Theme.cs') (Join-Path $root 'TaskGroups.cs') (Join-Path $root 'RolloverTests.cs') (Join-Path $root 'ManagerViews.cs')
if ($LASTEXITCODE -ne 0) { throw '编译失败' }
Write-Output "已生成：$output"
