<#
.SYNOPSIS
    Script tổng hợp quy trình UPCODE PROD (Cập nhật Database & Source Code FTP cho môi trường Viễn thông brewtask.vnptkhanhhoa.vn).

.DESCRIPTION
    1. Kiểm tra / Đồng bộ CSDL từ pmncpt.cenit.vn (10.57.30.10) sang brewtask.vnptkhanhhoa.vn (10.57.47.2\MSSQL2012).
    2. Kiểm tra / Đồng bộ Source Code Git (main -> Prod) và upload file thay đổi lên FTP (10.57.47.3/public_html).
    3. Kiểm tra sức khỏe HTTP 200 cho cả 2 hệ thống.

.PARAMETER Apply
    Nếu có switch này, script sẽ thực hiện đồng bộ thực tế vào CSDL và đẩy lên FTP.
    Nếu không có, script chỉ chạy ở chế độ kiểm tra và báo cáo chênh lệch.
#>
[CmdletBinding()]
param(
    [switch]$Apply
)

$ErrorActionPreference = "Stop"
$root = "d:\SVN\projectmanager"

$modeTitle = if ($Apply) { "THUC THI CAP NHAT PROD TOAN DIEN (-Apply)" } else { "KIEM TRA TONG THE (-CheckOnly)" }
$modeColor = if ($Apply) { "Yellow" } else { "Green" }

Write-Host "==========================================================================" -ForegroundColor Cyan
Write-Host "                QUY TRINH UPCODE PRODUCTION (BREWTASK)                    " -ForegroundColor Cyan
Write-Host "               Che do: $modeTitle" -ForegroundColor $modeColor
Write-Host "==========================================================================" -ForegroundColor Cyan
Write-Host ""

# -------------------------------------------------------------
# PHẦN 1: ĐỒNG BỘ DATABASE
# -------------------------------------------------------------
Write-Host ">>> PHAN 1: KIEM TRA & DONG BO CSDL (10.57.30.10 -> 10.57.47.2) <<<" -ForegroundColor Cyan
$dbArgs = @{
    FilePath = "powershell.exe"
    ArgumentList = @("-ExecutionPolicy", "Bypass", "-File", (Join-Path $root "build\sync-prod-db.ps1"))
}
if ($Apply) {
    $dbArgs.ArgumentList += "-Apply"
}
& $dbArgs.FilePath $dbArgs.ArgumentList
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] Tien trinh dong bo CSDL gap loi!" -ForegroundColor Red
    exit $LASTEXITCODE
}

# -------------------------------------------------------------
# PHẦN 2: ĐỒNG BỘ SOURCE CODE & FTP
# -------------------------------------------------------------
Write-Host ""
Write-Host ">>> PHAN 2: KIEM TRA & DONG BO SOURCE CODE / FTP (10.57.47.3) <<<" -ForegroundColor Cyan
$ftpArgs = @{
    FilePath = "powershell.exe"
    ArgumentList = @("-ExecutionPolicy", "Bypass", "-File", (Join-Path $root "build\sync-prod-ftp.ps1"))
}
if ($Apply) {
    $ftpArgs.ArgumentList += "-Apply"
}
& $ftpArgs.FilePath $ftpArgs.ArgumentList
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] Tien trinh dong bo FTP gap loi!" -ForegroundColor Red
    exit $LASTEXITCODE
}

# -------------------------------------------------------------
# PHẦN 3: XÁC MINH HAI SITE HOẠT ĐỘNG BÌNH THƯỜNG
# -------------------------------------------------------------
Write-Host ""
Write-Host ">>> PHAN 3: XAC MINH TRANG THAI CA HAI HE THONG <<<" -ForegroundColor Cyan

function Check-Site([string]$name, [string]$url) {
    try {
        $resp = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 10
        if ($resp.StatusCode -eq 200) {
            Write-Host " [PASS] $name ($url) => HTTP 200 OK" -ForegroundColor Green
        } else {
            Write-Host " [WARN] $name ($url) => HTTP Status $($resp.StatusCode)" -ForegroundColor Yellow
        }
    } catch {
        Write-Host " [FAIL] $name ($url) => Khong ket noi duoc: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Check-Site "Website Noi bo Trung tam" "http://pmncpt.cenit.vn/"
Check-Site "Website Vien thong (Prod)" "http://brewtask.vnptkhanhhoa.vn/"

Write-Host ""
Write-Host "==========================================================================" -ForegroundColor Cyan
Write-Host "        HOAN TAT QUY TRINH UPCODE PRODUCTION CHO BREWTASK!                " -ForegroundColor Green
Write-Host "==========================================================================" -ForegroundColor Cyan
