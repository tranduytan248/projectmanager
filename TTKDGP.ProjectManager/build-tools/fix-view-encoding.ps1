<#
    Bảo đảm mọi file .cshtml được lưu UTF-8 CÓ BOM.

    Vì sao cần: Razor đọc file view không có BOM theo mã trang mặc định của hệ thống, nên mọi chữ
    tiếng Việt viết thẳng trong view (nhãn, tiêu đề, thông báo) hiện thành ký tự rác. Lỗi này chỉ
    lộ ra khi mở trang, không làm build đỏ, nên rất dễ lọt lên bản chạy thật.

    Chạy tự động ở bước trước khi build (xem target KiemTraMaHoaView trong .csproj). Không chỉ báo
    lỗi mà TỰ SỬA luôn, để một lần quên lưu sai mã hoá cũng không bao giờ tới được người dùng.

    Trả về 0 khi mọi thứ ổn, kể cả khi vừa sửa; chỉ trả 1 khi thật sự không ghi được file.
#>
param(
    [Parameter(Mandatory = $true)]
    [string] $Root
)

$ErrorActionPreference = 'Stop'
$bom = [byte[]](0xEF, 0xBB, 0xBF)

if (-not (Test-Path $Root)) {
    Write-Host "fix-view-encoding: khong tim thay thu muc '$Root', bo qua."
    exit 0
}

$fixed = @()
$failed = @()

Get-ChildItem -Path $Root -Recurse -Filter *.cshtml -File | ForEach-Object {
    try {
        $bytes = [System.IO.File]::ReadAllBytes($_.FullName)

        $hasBom = $bytes.Length -ge 3 -and
                  $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
        if ($hasBom) { return }

        # Chỉ thêm BOM cho file thật sự có ký tự ngoài ASCII. File thuần ASCII đọc kiểu nào cũng
        # như nhau nên không cần đụng vào, tránh làm bẩn lịch sử git.
        $nonAscii = $false
        foreach ($b in $bytes) { if ($b -gt 127) { $nonAscii = $true; break } }
        if (-not $nonAscii) { return }

        [System.IO.File]::WriteAllBytes($_.FullName, $bom + $bytes)
        $fixed += $_.FullName.Substring($Root.Length).TrimStart('\')
    }
    catch {
        $failed += ("{0}: {1}" -f $_.FullName, $_.Exception.Message)
    }
}

foreach ($f in $fixed) {
    # Định dạng "warning XX:" để MSBuild nhặt lên thành cảnh báo, hiện rõ trong log build.
    Write-Host "fix-view-encoding : warning BOM001: Da them BOM UTF-8 cho view '$f'."
}

foreach ($f in $failed) {
    Write-Host "fix-view-encoding : error BOM002: Khong sua duoc '$f'."
}

if ($failed.Count -gt 0) { exit 1 }

if ($fixed.Count -eq 0) { Write-Host "fix-view-encoding: tat ca view da dung ma hoa UTF-8 co BOM." }
exit 0
