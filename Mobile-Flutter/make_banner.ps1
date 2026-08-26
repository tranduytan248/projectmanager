Add-Type -AssemblyName System.Drawing
$assetsDir = "D:\MyProject\projectmanager\Mobile-Flutter\google_play_assets"
$srcIconPath = "D:\MyProject\projectmanager\Mobile-Flutter\icon_app.png"

$featBmp = New-Object System.Drawing.Bitmap 1024, 500
$fg = [System.Drawing.Graphics]::FromImage($featBmp)
$fg.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$fg.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$fg.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

$rect = New-Object System.Drawing.Rectangle 0, 0, 1024, 500
$brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, ([System.Drawing.Color]::FromArgb(30, 41, 59)), ([System.Drawing.Color]::FromArgb(15, 23, 42)), ([System.Drawing.Drawing2D.LinearGradientMode]::Horizontal)
$fg.FillRectangle($brush, $rect)
$brush.Dispose()

# Icon
if (Test-Path $srcIconPath) {
    $iconImg = [System.Drawing.Image]::FromFile($srcIconPath)
    $fg.DrawImage($iconImg, 80, 100, 300, 300)
    $iconImg.Dispose()
}

$titleFont = New-Object System.Drawing.Font "Segoe UI", [single]46.0, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel
$sloganFont = New-Object System.Drawing.Font "Segoe UI", [single]24.0, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel
$descFont = New-Object System.Drawing.Font "Segoe UI", [single]18.0, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel

$whiteBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
$blueBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(59, 130, 246))
$grayBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(203, 213, 225))

$fg.DrawString("BrewTask", $titleFont, $whiteBrush, 410.0, 130.0)
$fg.DrawString("Quản Lý Dự Án & Công Việc", $sloganFont, $blueBrush, 415.0, 210.0)
$fg.DrawString("Tổ NCPT - VNPT Khánh Hòa", $descFont, $grayBrush, 415.0, 265.0)
$fg.DrawString("Thời gian thực • Phân công • Bảng điểm KPI", $descFont, $grayBrush, 415.0, 305.0)

$titleFont.Dispose()
$sloganFont.Dispose()
$descFont.Dispose()
$whiteBrush.Dispose()
$blueBrush.Dispose()
$grayBrush.Dispose()
$fg.Dispose()

$featBmp.Save("$assetsDir\feature_graphic_1024x500.png", [System.Drawing.Imaging.ImageFormat]::Png)
$featBmp.Dispose()
Write-Host "Da tao xong feature_graphic_1024x500.png"