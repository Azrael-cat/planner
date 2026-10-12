param([switch]$SkipPublishSync)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$output = Join-Path $root 'DeskTaskAssistant.exe'
$sources = @(Get-ChildItem -LiteralPath (Join-Path $root 'src'), (Join-Path $root 'tests') -Filter '*.cs' -File | Sort-Object FullName | ForEach-Object { $_.FullName })
if ($sources.Count -eq 0) { throw 'Source files not found.' }
& $compiler /nologo /target:winexe /optimize+ /codepage:65001 /utf8output ("/win32icon:" + (Join-Path $root 'App.ico')) "/out:$output" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll /reference:Microsoft.CSharp.dll @sources
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
Write-Output "Built: $output"
$publish = Join-Path $root 'publish-export\upload-ready'
if (-not $SkipPublishSync -and (Test-Path -LiteralPath $publish)) { & (Join-Path $root 'Sync-Publish.ps1') }