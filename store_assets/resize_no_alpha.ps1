Add-Type -AssemblyName System.Drawing

$srcDir = "C:\Users\K\.gemini\antigravity-ide\brain\68498c00-f405-4998-bff8-2fe8c7b5dfa6"
$dstDir = "d:\MyProject\projectmanager\store_assets\ios"

# Remove all existing files in target
Get-ChildItem -Path $dstDir | Remove-Item -Force -ErrorAction SilentlyContinue

$files = @(
    @{ Src = "iphone_screenshot_dashboard_1788339550390.jpg"; Name = "01_dashboard.jpg" },
    @{ Src = "iphone_screenshot_discussion_1788339570541.jpg"; Name = "02_discussion.jpg" },
    @{ Src = "iphone_screenshot_kpi_analytics_1788339591257.jpg"; Name = "03_kpi_analytics.jpg" },
    @{ Src = "iphone_screenshot_leave_management_1788339895284.jpg"; Name = "04_leave_management.jpg" },
    @{ Src = "iphone_screenshot_projects_milestones_1788339930865.jpg"; Name = "05_projects_milestones.jpg" },
    @{ Src = "iphone_screenshot_notifications_profile_1788339956248.jpg"; Name = "06_notifications.jpg" }
)

$jpegCodec = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where-Object { $_.MimeType -eq 'image/jpeg' }
$encoderParams = New-Object System.Drawing.Imaging.EncoderParameters(1)
$encoderParams.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter([System.Drawing.Imaging.Encoder]::Quality, [long]95)

foreach ($item in $files) {
    $srcPath = Join-Path $srcDir $item.Src
    $dstPath = Join-Path $dstDir $item.Name
    if (Test-Path $srcPath) {
        $sourceImg = [System.Drawing.Image]::FromFile($srcPath)
        # PixelFormat 24bppRgb strictly contains NO alpha channel
        $targetBmp = New-Object System.Drawing.Bitmap 1242, 2688, ([System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
        $graphics = [System.Drawing.Graphics]::FromImage($targetBmp)
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.Clear([System.Drawing.Color]::Black)
        $graphics.DrawImage($sourceImg, 0, 0, 1242, 2688)
        
        $targetBmp.Save($dstPath, $jpegCodec, $encoderParams)
        $graphics.Dispose()
        $targetBmp.Dispose()
        $sourceImg.Dispose()
        Write-Host "Success: $($item.Name) -> 1242x2688 JPG (No Alpha)"
    }
}
