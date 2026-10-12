# Explicit allowlist: never mirror the workspace or include user data.
$ErrorActionPreference = 'Stop'
$sourceRoot = [IO.Path]::GetFullPath($PSScriptRoot)
$destination = [IO.Path]::GetFullPath((Join-Path $sourceRoot 'publish-export\upload-ready'))
$prefix = $sourceRoot.TrimEnd('\') + '\publish-export\'
if (-not $destination.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected publish destination.' }
if (-not (Test-Path -LiteralPath $destination)) { New-Item -ItemType Directory -Path $destination -Force | Out-Null }
$files = @('AGENTS.md', '.gitignore', 'README.md', 'LICENSE', 'App.ico', 'Build.ps1', 'Install.ps1', 'Uninstall.ps1', 'UpdateInstalled.ps1', 'Sync-Publish.ps1', 'VerifyUI.ps1', '安装小助手.cmd', '使用说明.txt')
foreach ($folder in @('src', 'tests')) {
    $files += @(Get-ChildItem -LiteralPath (Join-Path $sourceRoot $folder) -Filter '*.cs' -File | ForEach-Object { $folder + '\' + $_.Name })
}
$files += @('docs\images\today-card.png', 'docs\images\task-center.png', 'docs\images\progress.png', 'docs\images\daily-summary.png')
$legacy = @('TaskAssistant.cs', 'Card.cs', 'Theme.cs', 'TaskGroups.cs', 'ManagerViews.cs', 'RolloverTests.cs')
$manifestPath = Join-Path $destination '.publish-manifest.json'
$previous = @{}
if (Test-Path -LiteralPath $manifestPath) {
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    foreach ($property in $manifest.PSObject.Properties) { $previous[$property.Name] = $property.Value }
}
$hasGit = (Test-Path -LiteralPath (Join-Path $destination '.git')) -and (Get-Command git -ErrorAction SilentlyContinue)
function Assert-NoConflictingEdit([string]$relative, [string]$sourceHash) {
    $target = Join-Path $destination $relative
    if (-not (Test-Path -LiteralPath $target)) { return }
    $current = (Get-FileHash -LiteralPath $target).Hash
    if ($sourceHash -eq $current -or $previous[$relative] -eq $current) { return }
    if ($hasGit) {
        $status = & git -C $destination status --porcelain -- $relative
        if ($LASTEXITCODE -ne 0) { throw "Cannot inspect checkout: $relative" }
        if ($status) { throw "Publish checkout has a conflicting local edit: $relative. Review it before syncing." }
    } else { throw "Unrecognized existing publish file differs: $relative" }
}
$hashes = [ordered]@{}
foreach ($relative in $files) {
    $source = Join-Path $sourceRoot $relative
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Missing publish source: $relative" }
    $hashes[$relative] = (Get-FileHash -LiteralPath $source).Hash
    Assert-NoConflictingEdit $relative $hashes[$relative]
}
foreach ($relative in $legacy) { Assert-NoConflictingEdit $relative '' }
foreach ($relative in $files) {
    $target = Join-Path $destination $relative
    $parent = Split-Path -Parent $target
    if (-not (Test-Path -LiteralPath $parent)) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
    Copy-Item -LiteralPath (Join-Path $sourceRoot $relative) -Destination $target -Force
}
foreach ($relative in $legacy) {
    $target = [IO.Path]::GetFullPath((Join-Path $destination $relative))
    if ([IO.Path]::GetDirectoryName($target) -ne $destination) { throw 'Unexpected legacy file path.' }
    if (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target -Force }
}
$hashes | ConvertTo-Json | Set-Content -LiteralPath $manifestPath -Encoding UTF8
$exe = Join-Path $sourceRoot 'DeskTaskAssistant.exe'
if (Test-Path -LiteralPath $exe) {
    $package = Join-Path $sourceRoot '桌面任务小助手-安装包.zip'
    $packageFiles = @('DeskTaskAssistant.exe', 'Install.ps1', 'Uninstall.ps1', '安装小助手.cmd', '使用说明.txt') | ForEach-Object { Join-Path $sourceRoot $_ }
    Compress-Archive -LiteralPath $packageFiles -DestinationPath $package -Force
    Copy-Item -LiteralPath $exe, $package -Destination $destination -Force
}
foreach ($relative in $files) {
    if ((Get-FileHash -LiteralPath (Join-Path $destination $relative)).Hash -ne $hashes[$relative]) { throw "Publish verification failed: $relative" }
}
Write-Output "Synced $($files.Count) reviewed source/document files and installation artifacts to: $destination"
Write-Output 'Git metadata and unrelated files preserved. No commit or push performed.'
