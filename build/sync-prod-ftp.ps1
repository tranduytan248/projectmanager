<#
.SYNOPSIS
    Kiểm tra và đồng bộ source code / build files lên máy chủ FTP Production Viễn thông (10.57.47.3/public_html).

.DESCRIPTION
    - So sánh commit và file thay đổi giữa nhánh 'main' và nhánh 'Prod'.
    - Khi có switch -Apply:
        1. Checkout Prod -> Merge main (bảo vệ Web.config bằng merge=ours).
        2. Biên dịch Release (publish.ps1) ra build\app.
        3. So sánh file với máy chủ FTP 10.57.47.3 và chỉ upload các file mới/thay đổi (bỏ qua secrets.config).
        4. Kiểm tra sức khỏe HTTP 200 tại http://brewtask.vnptkhanhhoa.vn/.
        5. Push nhánh Prod lên origin và checkout về lại nhánh ban đầu.

.PARAMETER Apply
    Nếu có switch này, script sẽ thực hiện merge code, build và upload FTP thực tế.
#>
[CmdletBinding()]
param(
    [switch]$Apply,
    [string]$FtpBase = "ftp://10.57.47.3/public_html",
    [string]$FtpUser = "brewtask",
    [string]$FtpPass = "Kh@2026"
)

$root = if (Test-Path "$PSScriptRoot\..") { (Resolve-Path "$PSScriptRoot\..").Path } else { "d:\MyProject\projectmanager" }
$appDir = Join-Path $root "build\app"

$modeText = if ($Apply) { "THUC THI CAP NHAT FTP (-Apply)" } else { "CHI KIEM TRA (-CheckOnly)" }
$modeColor = if ($Apply) { "Yellow" } else { "Green" }

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host " SOURCE CODE & FTP SYNC: main -> Prod (10.57.47.3/public_html)" -ForegroundColor Cyan
Write-Host " Che do: $modeText" -ForegroundColor $modeColor
Write-Host "=================================================================" -ForegroundColor Cyan

# 1. KIỂM TRA NHÁNH VÀ COMMIT GIT
Write-Host ""
Write-Host "--- BUOC 1: SO SANH SOURCE CODE GIT (main -> Prod) ---" -ForegroundColor Cyan

$currentBranch = (git -C $root branch --show-current).Trim()
Write-Host "Nhanh hien tai: $currentBranch"

$commitsDiff = (git -C $root log --oneline Prod..main)
$filesDiff = (git -C $root diff --name-only Prod..main)

if (-not $commitsDiff) {
    Write-Host " [PASS] Nhanh Prod da dong bo voi main (0 commit moi tren main)." -ForegroundColor Green
} else {
    $commitCount = ($commitsDiff | Measure-Object).Count
    Write-Host " [INFO] Co $commitCount commit tren main chua co tren Prod:" -ForegroundColor Yellow
    foreach ($c in ($commitsDiff | Select-Object -First 10)) {
        Write-Host "   - $c"
    }
    if ($commitCount -gt 10) {
        Write-Host "   ... va $($commitCount - 10) commit khac."
    }
    Write-Host " So file source code thay doi: $(($filesDiff | Measure-Object).Count)"
}

if (-not $Apply) {
    Write-Host ""
    Write-Host "--- BUOC 2: KIEM TRA TRANG THAI BUILD HIEN CO ---" -ForegroundColor Cyan
    if (Test-Path $appDir) {
        $fileCount = (Get-ChildItem -Path $appDir -Recurse -File).Count
        Write-Host "Thu muc build\app hien tai co: $fileCount files san sang publish." -ForegroundColor Green
    } else {
        Write-Host "Chua co thu muc build\app. Chay voi -Apply de tu dong bien dich Release." -ForegroundColor Yellow
    }
    Write-Host ""
    Write-Host "[THONG BAO] Day la che do kiem tra (-CheckOnly). De ap dung va upload FTP, hay chay voi co: .\build\sync-prod-ftp.ps1 -Apply" -ForegroundColor Cyan
    exit 0
}

# -------------------------------------------------------------
# THỰC THI (KHI CÓ CỜ -Apply)
# -------------------------------------------------------------
Write-Host ""
Write-Host "--- BUOC 2: MERGE CODE VA BIEN DICH (main -> Prod) ---" -ForegroundColor Cyan

try {
    # Checkout Prod
    Write-Host " [1/5] Checkout nhanh Prod..."
    git -C $root checkout Prod | Out-Null

    # Pull Prod mới nhất
    Write-Host " [2/5] Pull origin Prod..."
    git -C $root pull origin Prod --no-rebase | Out-Null

    # Merge main vào Prod
    if ($commitsDiff) {
        Write-Host " [3/5] Merge nhanh main vao Prod (giu nguyen cau hinh Web.config nho merge=ours)..."
        git -C $root merge main -m "Merge main into Prod for release" | Out-Null
    } else {
        Write-Host " [3/5] Khong co commit moi can merge."
    }

    # Biên dịch Release
    Write-Host " [4/5] Bien dich va publish ung dung o che do Release vao build\app..." -ForegroundColor Cyan
    & (Join-Path $root "publish.ps1") -Configuration Release

    # -------------------------------------------------------------
    # 3. UPLOAD LÊN MÁY CHỦ FTP 10.57.47.3
    # -------------------------------------------------------------
    Write-Host ""
    Write-Host "--- BUOC 3: DONG BO CAC FILE LEN FTP (10.57.47.3) ---" -ForegroundColor Cyan

    $knownDirs = New-Object 'System.Collections.Generic.HashSet[string]'

    function Ensure-FtpDir([string]$dirUri) {
        if ([string]::IsNullOrWhiteSpace($dirUri)) { return }
        if ($knownDirs.Contains($dirUri)) { return }
        if ($dirUri -eq "ftp://10.57.47.3" -or $dirUri -eq "ftp://10.57.47.3/public_html") {
            [void]$knownDirs.Add($dirUri)
            return
        }
        $lastSlash = $dirUri.LastIndexOf('/')
        if ($lastSlash -gt 8) {
            $parent = $dirUri.Substring(0, $lastSlash)
            Ensure-FtpDir $parent
        }
        try {
            $req = [System.Net.FtpWebRequest]::Create($dirUri)
            $req.Method = [System.Net.WebRequestMethods+Ftp]::MakeDirectory
            $req.Credentials = New-Object System.Net.NetworkCredential($FtpUser, $FtpPass)
            $req.UsePassive = $true
            $req.Timeout = 15000
            $resp = $req.GetResponse()
            $resp.Close()
        } catch { }
        [void]$knownDirs.Add($dirUri)
    }

    $allFiles = Get-ChildItem -Path $appDir -Recurse -File
    $total = $allFiles.Count
    $idx = 0
    $uploaded = 0
    $skipped = 0
    $errors = 0

    Write-Host "Tong so file trong ban build: $total"
    Write-Host "Dang so sanh va tai len cac file co thay doi (DLLs, Views, Scripts, Content)..." -ForegroundColor Cyan

    foreach ($file in $allFiles) {
        $idx++
        $rel = $file.FullName.Substring($appDir.Length).TrimStart('\', '/')
        $remoteRel = $rel.Replace('\', '/')
        $remoteFileUri = "$FtpBase/$remoteRel"

        # Bỏ qua secrets.config để không bao giờ ghi đè secret của server
        if ($rel -eq "secrets.config" -or $rel.EndsWith("\secrets.config")) {
            $skipped++
            continue
        }

        $lastSlash = $remoteFileUri.LastIndexOf('/')
        $remoteDirUri = $remoteFileUri.Substring(0, $lastSlash)
        Ensure-FtpDir $remoteDirUri

        # Quyết định file có cần upload không
        $mustUpload = $false
        # File dll trong bin, web.config, views luôn cập nhật nếu có bản mới
        if ($rel.StartsWith("bin\") -or $rel -eq "Web.config" -or $rel.StartsWith("Views\")) {
            $mustUpload = $true
        } else {
            try {
                $sizeReq = [System.Net.FtpWebRequest]::Create($remoteFileUri)
                $sizeReq.Method = [System.Net.WebRequestMethods+Ftp]::GetFileSize
                $sizeReq.Credentials = New-Object System.Net.NetworkCredential($FtpUser, $FtpPass)
                $sizeReq.UsePassive = $true
                $sizeReq.Timeout = 8000
                $sizeResp = $sizeReq.GetResponse()
                $remoteSize = $sizeResp.ContentLength
                $sizeResp.Close()
                if ($remoteSize -ne $file.Length) {
                    $mustUpload = $true
                }
            } catch {
                # File chưa tồn tại trên FTP -> Upload
                $mustUpload = $true
            }
        }

        if ($mustUpload) {
            try {
                $uploadReq = [System.Net.FtpWebRequest]::Create($remoteFileUri)
                $uploadReq.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
                $uploadReq.Credentials = New-Object System.Net.NetworkCredential($FtpUser, $FtpPass)
                $uploadReq.UseBinary = $true
                $uploadReq.UsePassive = $true
                $uploadReq.Timeout = 30000
                
                $fileStream = [System.IO.File]::OpenRead($file.FullName)
                $ftpStream = $uploadReq.GetRequestStream()
                $fileStream.CopyTo($ftpStream)
                $ftpStream.Close()
                $fileStream.Close()
                
                $uploadResp = $uploadReq.GetResponse()
                $uploadResp.Close()
                $uploaded++
            } catch {
                Write-Host "   [LOI] Upload $remoteRel that bai: $($_.Exception.Message)" -ForegroundColor Red
                $errors++
            }
        } else {
            $skipped++
        }

        if ($idx % 50 -eq 0 -or $idx -eq $total) {
            Write-Host "   Tien trinh: $idx / $total (Da upload: $uploaded, Bo qua: $skipped, Loi: $errors)"
        }
    }

    Write-Host ""
    Write-Host "=== KET QUA CAP NHAT FTP ===" -ForegroundColor Cyan
    Write-Host "Da upload thanh cong: $uploaded file" -ForegroundColor Green
    Write-Host "Bo qua (khong doi) : $skipped file"
    if ($errors -gt 0) {
        Write-Host "Loi upload         : $errors file" -ForegroundColor Red
    }

    # -------------------------------------------------------------
    # 4. HEALTHCHECK TRÊN HTTP PROD
    # -------------------------------------------------------------
    Write-Host ""
    Write-Host "--- BUOC 4: KIEM TRA SUC KHOE WEBSITE PROD ---" -ForegroundColor Cyan
    Start-Sleep -Seconds 2
    try {
        $httpResp = Invoke-WebRequest -Uri "http://brewtask.vnptkhanhhoa.vn/" -UseBasicParsing -TimeoutSec 15
        if ($httpResp.StatusCode -eq 200) {
            Write-Host " [PASS] Website http://brewtask.vnptkhanhhoa.vn/ tra ve HTTP 200 OK!" -ForegroundColor Green
        } else {
            Write-Host " [WARN] Website tra ve HTTP status: $($httpResp.StatusCode)" -ForegroundColor Yellow
        }
    } catch {
        Write-Host " [WARN] Khong the ket noi toi http://brewtask.vnptkhanhhoa.vn/: $($_.Exception.Message)" -ForegroundColor Yellow
    }

    # -------------------------------------------------------------
    # 5. PUSH NHÁNH PROD VÀ TRẢ VỀ NHÁNH GỐC
    # -------------------------------------------------------------
    Write-Host ""
    Write-Host "--- BUOC 5: DONG BO GIT REMOTE VA TRA VE NHANH GOC ---" -ForegroundColor Cyan
    Write-Host "Push nhanh Prod len origin..."
    git -C $root push origin Prod | Out-Null
    Write-Host "Da push Prod len origin thanh cong!" -ForegroundColor Green

} finally {
    Write-Host "Tra ve nhanh goc: $currentBranch..."
    git -C $root checkout $currentBranch | Out-Null
}

Write-Host "Hoan tat quy trinh dong bo Source Code va FTP." -ForegroundColor Green
