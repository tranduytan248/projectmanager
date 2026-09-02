Add-Type -AssemblyName System.Drawing

$srcDir = "C:\Users\K\.gemini\antigravity-ide\brain\68498c00-f405-4998-bff8-2fe8c7b5dfa6"
$dstDir = "d:\MyProject\projectmanager\store_assets\ios"

$files = @(
    @{ Src = "iphone_screenshot_dashboard_1788339550390.jpg"; Name = "01_dashboard_1242x2688.png" },
    @{ Src = "iphone_screenshot_discussion_1788339570541.jpg"; Name = "02_discussion_1242x2688.png" },
    @{ Src = "iphone_screenshot_kpi_analytics_1788339591257.jpg"; Name = "03_kpi_analytics_1242x2688.png" },
    @{ Src = "iphone_screenshot_leave_management_1788339895284.jpg"; Name = "04_leave_management_1242x2688.png" },
    @{ Src = "iphone_screenshot_projects_milestones_1788339930865.jpg"; Name = "05_projects_milestones_1242x2688.png" },
    @{ Src = "iphone_screenshot_notifications_profile_1788339956248.jpg"; Name = "06_notifications_1242x2688.png" }
)

foreach ($item in $files) {
    $srcPath = Join-Path $srcDir $item.Src
    $dstPath = Join-Path $dstDir $item.Name
    if (Test-Path $srcPath) {
        $sourceImg = [System.Drawing.Image]::FromFile($srcPath)
        $targetBmp = New-Object System.Drawing.Bitmap 1242, 2688
        $graphics = [System.Drawing.Graphics]::FromImage($targetBmp)
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.DrawImage($sourceImg, 0, 0, 1242, 2688)
        $targetBmp.Save($dstPath, [System.Drawing.Imaging.ImageFormat]::Png)
        $graphics.Dispose()
        $targetBmp.Dispose()
        $sourceImg.Dispose()
        Write-Host "Success: $($item.Name) -> 1242x2688"
    }
}
