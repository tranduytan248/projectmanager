Add-Type -AssemblyName System.Drawing
$adb = "C:\Users\K\AppData\Local\Android\Sdk\platform-tools\adb.exe"
$assetsDir = "D:\MyProject\projectmanager\Mobile-Flutter\google_play_assets"

function CaptureScreen($fileName) {
    & $adb shell screencap -p /sdcard/temp_screen.png
    & $adb pull /sdcard/temp_screen.png "$assetsDir\$fileName"
    Write-Host "Da chup $fileName"
}

# 1. Chup Dashboard
Write-Host "1. Chup Dashboard..."
CaptureScreen "screenshot_1_dashboard.png"

# 2. Chuyen sang Tab Du an
Write-Host "2. Chuyen sang Tab Du an..."
& $adb shell input tap 405 2280
Start-Sleep -Seconds 2
CaptureScreen "screenshot_2_danh_sach_du_an.png"

# 3. Bam vao Du an dau tien (Chi tiet du an)
Write-Host "3. Mo Chi tiet du an..."
& $adb shell input tap 400 450
Start-Sleep -Seconds 2
CaptureScreen "screenshot_3_chi_tiet_du_an.png"

# 4. Mo Phong trao doi / Thao luan trong du an hoac back ve dashboard mo Trao doi
& $adb shell input keyevent 4 # Back
Start-Sleep -Seconds 1
& $adb shell input tap 135 2280 # Tab Dashboard
Start-Sleep -Seconds 1
& $adb shell input tap 760 235 # Icon Trao doi
Start-Sleep -Seconds 2
CaptureScreen "screenshot_4_trung_tam_trao_doi.png"

# 5. Back ve Dashboard va sang Tab Cong viec
& $adb shell input keyevent 4 # Back
Start-Sleep -Seconds 1
Write-Host "5. Chuyen sang Tab Cong viec..."
& $adb shell input tap 675 2280 # Tab Cong viec
Start-Sleep -Seconds 2
CaptureScreen "screenshot_5_danh_sach_cong_viec.png"

# 6. Chuyen sang Tab Cai dat
Write-Host "6. Chuyen sang Tab Cai dat..."
& $adb shell input tap 945 2280 # Tab Cai dat
Start-Sleep -Seconds 2
CaptureScreen "screenshot_6_cai_dat_ho_so.png"

# 7. Hoan thien Feature Graphic 1024x500
Write-Host "7. Tao Feature Graphic 1024x500 chuan..."
$featBmp = New-Object System.Drawing.Bitmap 1024, 500
$fg = [System.Drawing.Graphics]::FromImage($featBmp)
$fg.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$fg.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$fg.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

$rect = New-Object System.Drawing.Rectangle 0, 0, 1024, 500
$brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, ([System.Drawing.Color]::FromArgb(24, 30, 42)), ([System.Drawing.Color]::FromArgb(10, 14, 22)), ([System.Drawing.Drawing2D.LinearGradientMode]::ForwardDiagonal)
$fg.FillRectangle($brush, $rect)
$brush.Dispose()

# Icon
$srcIconPath = "D:\MyProject\projectmanager\Mobile-Flutter\icon_app.png"
if (Test-Path $srcIconPath) {
    $iconImg = [System.Drawing.Image]::FromFile($srcIconPath)
    $fg.DrawImage($iconImg, 70, 100, 300, 300)
    $iconImg.Dispose()
}

$titleFont = New-Object System.Drawing.Font ([System.Drawing.FontFamily]::GenericSansSerif), [float]44.0, [System.Drawing.FontStyle]::Bold
$sloganFont = New-Object System.Drawing.Font ([System.Drawing.FontFamily]::GenericSansSerif), [float]20.0, [System.Drawing.FontStyle]::Bold
$descFont = New-Object System.Drawing.Font ([System.Drawing.FontFamily]::GenericSansSerif), [float]15.0, [System.Drawing.FontStyle]::Regular

$whiteBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
$blueBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(55, 148, 255))
$grayBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(156, 163, 175))

$fg.DrawString("BrewTask", $titleFont, $whiteBrush, 400, 120)
$fg.DrawString("Quản Lý Dự Án & Nhân Sự - Tổ NCPT", $sloganFont, $blueBrush, 405, 205)
$fg.DrawString("Phân công công việc • Trao đổi thời gian thực • Chấm điểm KPI", $descFont, $grayBrush, 405, 260)
$fg.DrawString("VNPT Khánh Hòa", $descFont, $whiteBrush, 405, 310)

$titleFont.Dispose()
$sloganFont.Dispose()
$descFont.Dispose()
$whiteBrush.Dispose()
$blueBrush.Dispose()
$grayBrush.Dispose()
$fg.Dispose()

$featBmp.Save("$assetsDir\feature_graphic_1024x500.png", [System.Drawing.Imaging.ImageFormat]::Png)
$featBmp.Dispose()
Write-Host "Da tao feature_graphic_1024x500.png"