Add-Type -AssemblyName System.Drawing
$adb = "C:\Users\K\AppData\Local\Android\Sdk\platform-tools\adb.exe"
$assetsDir = "D:\MyProject\projectmanager\Mobile-Flutter\google_play_assets"
if (-not (Test-Path $assetsDir)) { New-Item -ItemType Directory -Path $assetsDir -Force | Out-Null }

Write-Host "1. Tao App Icon 512x512..."
$srcIconPath = "D:\MyProject\projectmanager\Mobile-Flutter\icon_app.png"
if (Test-Path $srcIconPath) {
    $srcImg = [System.Drawing.Image]::FromFile($srcIconPath)
    $icon512 = New-Object System.Drawing.Bitmap 512, 512
    $g = [System.Drawing.Graphics]::FromImage($icon512)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::FromArgb(26, 86, 168)) # #1A56A8
    $g.DrawImage($srcImg, 0, 0, 512, 512)
    $g.Dispose()
    $srcImg.Dispose()
    $icon512.Save("$assetsDir\app_icon_512x512.png", [System.Drawing.Imaging.ImageFormat]::Png)
    $icon512.Dispose()
    Write-Host "Da tao app_icon_512x512.png"
}

Write-Host "2. Tao Feature Graphic 1024x500..."
$featBmp = New-Object System.Drawing.Bitmap 1024, 500
$fg = [System.Drawing.Graphics]::FromImage($featBmp)
$fg.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$fg.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$fg.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

# Gradient background
$rect = New-Object System.Drawing.Rectangle 0, 0, 1024, 500
$brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, ([System.Drawing.Color]::FromArgb(26, 86, 168)), ([System.Drawing.Color]::FromArgb(15, 45, 95)), ([System.Drawing.Drawing2D.LinearGradientMode]::Horizontal)
$fg.FillRectangle($brush, $rect)
$brush.Dispose()

# Ve icon giua ben trai
if (Test-Path $srcIconPath) {
    $iconImg = [System.Drawing.Image]::FromFile($srcIconPath)
    $fg.DrawImage($iconImg, 80, 110, 280, 280)
    $iconImg.Dispose()
}

# Ve text BrewTask va Slogan
$titleFont = New-Object System.Drawing.Font "Segoe UI", 48, [System.Drawing.FontStyle]::Bold
$sloganFont = New-Object System.Drawing.Font "Segoe UI", 22, [System.Drawing.FontStyle]::Regular
$descFont = New-Object System.Drawing.Font "Segoe UI", 16, [System.Drawing.FontStyle]::Regular
$whiteBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
$cyanBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(147, 197, 253)) # #93C5FD
$goldBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(254, 240, 138)) # #FEF08A

$fg.DrawString("BrewTask", $titleFont, $whiteBrush, 400, 130)
$fg.DrawString("Quản Lý Dự Án & Công Việc", $sloganFont, $goldBrush, 405, 220)
$fg.DrawString("Tổ NCPT - VNPT Khánh Hòa", $descFont, $cyanBrush, 405, 275)

$titleFont.Dispose()
$sloganFont.Dispose()
$descFont.Dispose()
$whiteBrush.Dispose()
$cyanBrush.Dispose()
$goldBrush.Dispose()
$fg.Dispose()
$featBmp.Save("$assetsDir\feature_graphic_1024x500.png", [System.Drawing.Imaging.ImageFormat]::Png)
$featBmp.Dispose()
Write-Host "Da tao feature_graphic_1024x500.png"

Write-Host "3. Chup anh man hinh tu emulator..."
& $adb exec-out screencap -p > "$assetsDir\screenshot_1_dang_nhap.png"
Write-Host "Da chup screenshot_1_dang_nhap.png"