$adb = "C:\Users\K\AppData\Local\Android\Sdk\platform-tools\adb.exe"
$assetsDir = "D:\MyProject\projectmanager\Mobile-Flutter\google_play_assets"

function Capture($name) {
    Start-Sleep -Milliseconds 800
    & $adb shell screencap -p /sdcard/s.png
    & $adb pull /sdcard/s.png "$assetsDir\$name"
    Write-Host "Captured $name"
}

# An ban phim neu dang mo
& $adb shell input keyevent 111
Start-Sleep -Milliseconds 500

# 1. Chup Dashboard
& $adb shell input tap 135 2280
Start-Sleep -Seconds 1
Capture "screenshot_1_dashboard.png"

# 2. Vao Tab Du an -> Bam Card "Du an demo"
& $adb shell input tap 405 2280
Start-Sleep -Seconds 1
& $adb shell input keyevent 111 # ensure no keyboard
& $adb shell input tap 400 700 # Tap vao card Du an demo
Start-Sleep -Seconds 2
Capture "screenshot_2_chi_tiet_du_an.png"

# 3. Chuyen sang Tab Trao doi trong Chi tiet du an (neu co) hoac mo tin nhan
& $adb shell input tap 700 480 # Tab Trao doi hoac Thao luan trong chi tiet du an
Start-Sleep -Seconds 2
Capture "screenshot_3_thao_luan_du_an.png"

# 4. Back ve va vao Tab Cong viec
& $adb shell input keyevent 4 # Back
Start-Sleep -Seconds 1
& $adb shell input tap 675 2280 # Tab Cong viec
Start-Sleep -Seconds 2
Capture "screenshot_4_danh_sach_cong_viec.png"

# 5. Vao Tab Cai dat -> Bam Dang ky nghi phep
& $adb shell input tap 945 2280 # Tab Cai dat
Start-Sleep -Seconds 1
& $adb shell input tap 400 660 # Dong "Dang ky nghi phep"
Start-Sleep -Seconds 2
Capture "screenshot_5_dang_ky_nghi_phep.png"

# 6. Back ve va Chup man hinh Cai dat
& $adb shell input keyevent 4 # Back
Start-Sleep -Seconds 1
Capture "screenshot_6_cai_dat_he_thong.png"

Write-Host "Hoan tat chup toan bo anh man hinh!"