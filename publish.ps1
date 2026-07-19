<#
.SYNOPSIS
    Đóng gói ứng dụng vào thư mục build và ghi lại danh sách file thay đổi so với lần publish trước.

.DESCRIPTION
    Mỗi lần chạy sẽ:
      1. Biên dịch và publish web app vào  build\app
      2. Băm SHA256 toàn bộ file kết quả
      3. So với lần publish trước (build\manifest.json) để tìm file thêm / sửa / xoá
      4. Ghi báo cáo vào  build\changes\changes-<ngày giờ>.md
      5. Cập nhật lại manifest cho lần sau

.PARAMETER Configuration
    Debug hoặc Release. Mặc định Release.

.EXAMPLE
    .\publish.ps1
    .\publish.ps1 -Configuration Debug
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

$root       = $PSScriptRoot
$project    = Join-Path $root 'TTKDGP.ProjectManager\TTKDGP.ProjectManager.csproj'
$buildDir   = Join-Path $root 'build'
$appDir     = Join-Path $buildDir 'app'
$changesDir = Join-Path $buildDir 'changes'
$manifest   = Join-Path $buildDir 'manifest.json'

foreach ($d in @($buildDir, $changesDir)) {
    if (-not (Test-Path $d)) { New-Item -ItemType Directory -Path $d -Force | Out-Null }
}

# ---------- Tìm MSBuild ----------
function Find-MSBuild {
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswhere) {
        $vsPath = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
        if ($vsPath) {
            $candidate = Join-Path $vsPath 'MSBuild\Current\Bin\MSBuild.exe'
            if (Test-Path $candidate) { return $candidate }
        }
    }
    $onPath = Get-Command msbuild -ErrorAction SilentlyContinue
    if ($onPath) { return $onPath.Source }
    throw "Không tìm thấy MSBuild. Cần cài Visual Studio hoặc Build Tools."
}

$msbuild = Find-MSBuild
Write-Host "MSBuild : $msbuild"
Write-Host "Cấu hình: $Configuration"
Write-Host ""

# ---------- Đọc manifest của lần publish trước ----------
$previous = @{}
$previousTime = $null
if (Test-Path $manifest) {
    $saved = Get-Content $manifest -Raw -Encoding UTF8 | ConvertFrom-Json
    $previousTime = $saved.PublishedAt
    foreach ($entry in $saved.Files) { $previous[$entry.Path] = $entry.Hash }
}

# ---------- Publish ----------
Write-Host "Đang publish vào $appDir ..."
# Không có PublishProfile thì MSBuild mặc định đóng gói thành .zip chứ không
# chép ra thư mục, nên bắt buộc phải trỏ tới FolderProfile.
$msbuildArgs = @(
    $project
    "/p:Configuration=$Configuration"
    '/p:DeployOnBuild=true'
    '/p:PublishProfile=FolderProfile'
    "/p:publishUrl=$appDir\"
    '/v:minimal'
    '/nologo'
)
& $msbuild @msbuildArgs
if ($LASTEXITCODE -ne 0) { throw "Publish thất bại (mã lỗi $LASTEXITCODE)." }

Write-Host ""

# ---------- Băm toàn bộ file vừa publish ----------
$current = @{}
Get-ChildItem $appDir -Recurse -File | ForEach-Object {
    $relative = $_.FullName.Substring($appDir.Length).TrimStart('\', '/')
    $current[$relative] = (Get-FileHash $_.FullName -Algorithm SHA256).Hash
}

# ---------- So sánh với lần trước ----------
$added    = @()
$modified = @()
$removed  = @()

foreach ($path in $current.Keys) {
    if (-not $previous.ContainsKey($path)) { $added += $path }
    elseif ($previous[$path] -ne $current[$path]) { $modified += $path }
}
foreach ($path in $previous.Keys) {
    if (-not $current.ContainsKey($path)) { $removed += $path }
}

$added    = @($added    | Sort-Object)
$modified = @($modified | Sort-Object)
$removed  = @($removed  | Sort-Object)

# ---------- Ghi báo cáo ----------
$now      = Get-Date
$stamp    = $now.ToString('yyyyMMdd-HHmmss')
$isFirst  = ($previous.Count -eq 0)
$report   = Join-Path $changesDir "changes-$stamp.md"

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("# Danh sách file thay đổi")
$lines.Add("")
$lines.Add("| | |")
$lines.Add("|---|---|")
$lines.Add("| Thời điểm publish | $($now.ToString('dd/MM/yyyy HH:mm:ss')) |")
$lines.Add("| Cấu hình | $Configuration |")
$lines.Add("| Lần publish trước | $(if ($previousTime) { $previousTime } else { '(chưa có — đây là lần đầu)' }) |")
$lines.Add("| Tổng số file | $($current.Count) |")
$lines.Add("")

if ($isFirst) {
    $lines.Add("> Lần publish đầu tiên nên toàn bộ $($current.Count) file đều được tính là mới.")
    $lines.Add("")
}

$lines.Add("## Tổng hợp")
$lines.Add("")
$lines.Add("- Thêm mới: **$($added.Count)**")
$lines.Add("- Sửa đổi: **$($modified.Count)**")
$lines.Add("- Xoá bỏ: **$($removed.Count)**")
$lines.Add("")

function Add-Section([string]$title, [string[]]$items) {
    if ($items.Count -eq 0) { return }
    $script:lines.Add("## $title ($($items.Count))")
    $script:lines.Add("")
    foreach ($i in $items) { $script:lines.Add("- ``$i``") }
    $script:lines.Add("")
}

Add-Section "Thêm mới" $added
Add-Section "Sửa đổi"  $modified
Add-Section "Xoá bỏ"   $removed

if ($added.Count -eq 0 -and $modified.Count -eq 0 -and $removed.Count -eq 0) {
    $lines.Add("## Không có thay đổi")
    $lines.Add("")
    $lines.Add("Kết quả publish lần này giống hệt lần trước.")
    $lines.Add("")
}

[System.IO.File]::WriteAllLines($report, $lines, (New-Object System.Text.UTF8Encoding($true)))

# ---------- Lưu manifest cho lần sau ----------
$entries = foreach ($path in ($current.Keys | Sort-Object)) {
    [pscustomobject]@{ Path = $path; Hash = $current[$path] }
}
$manifestData = [pscustomobject]@{
    PublishedAt   = $now.ToString('dd/MM/yyyy HH:mm:ss')
    Configuration = $Configuration
    FileCount     = $current.Count
    Files         = @($entries)
}
[System.IO.File]::WriteAllText(
    $manifest,
    ($manifestData | ConvertTo-Json -Depth 5),
    (New-Object System.Text.UTF8Encoding($false)))

# ---------- Kết quả ----------
Write-Host "Publish xong."
Write-Host ("  Thư mục   : {0}" -f $appDir)
Write-Host ("  Số file   : {0}" -f $current.Count)
Write-Host ("  Thêm mới  : {0}" -f $added.Count)
Write-Host ("  Sửa đổi   : {0}" -f $modified.Count)
Write-Host ("  Xoá bỏ    : {0}" -f $removed.Count)
Write-Host ("  Báo cáo   : {0}" -f $report)
