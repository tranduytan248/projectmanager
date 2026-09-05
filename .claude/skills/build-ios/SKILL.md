---
name: build-ios
description: Kích hoạt quy trình đóng gói và ký số ứng dụng iOS (BrewTask) bằng GitHub Actions, xuất file .ipa chuẩn App Store Connect / TestFlight. Chỉ kích hoạt khi người dùng gõ lệnh rõ ràng như build-ios, build ios, đóng gói ios, tạo ipa ios.
---

# Build iOS App (BrewTask) Workflow & Skill

Skill này chịu trách nhiệm điều phối việc đóng gói ứng dụng Flutter iOS (BrewTask) thành file .ipa ký số chuẩn phát hành lên Apple App Store / TestFlight thông qua máy chủ ảo GitHub Actions macOS.

## Nguyên tắc cốt lõi
- **Không tự động kích hoạt**: Job build iOS chạy trên máy chủ ảo macOS của GitHub rất tốn tài nguyên và thời gian (10-15 phút), do đó file workflow .github/workflows/build-ios.yml được cấu hình **CHỈ CHẠY QUA workflow_dispatch** (không tự động kích hoạt khi push code thông thường).
- **Chỉ kích hoạt khi người dùng yêu cầu**: Khi người dùng gõ lệnh `build-ios`, `build ios`, `đóng gói ios`, agent mới thực thi quy trình theo skill này.

## Quy trình Thực hiện khi có lệnh "build-ios"

### Bước 1 — Kiểm tra tính toàn vẹn của mã nguồn Mobile
1. Chạy linter và kiểm thử trong thư mục `Mobile-Flutter/`:
   ```bash
   flutter analyze
   flutter test
   ```
2. Đảm bảo toàn bộ test case đều **PASS 100%** và không còn lỗi linting.

### Bước 2 — Kiểm tra cấu hình ký số iOS
Đảm bảo các file cấu hình sau đã sẵn sàng:
- `Mobile-Flutter/ios/ExportOptions.plist` (method `app-store`, `teamID: L4GY27T5X6`, profile `BrewTask_AppStore`).
- `Mobile-Flutter/ios/Runner.xcodeproj/project.pbxproj` (Release config dùng `Apple Distribution`, `DEVELOPMENT_TEAM: L4GY27T5X6`).
- Các secret đã được cấu hình trên GitHub Repo:
  - `BUILD_CERTIFICATE_BASE64`
  - `P12_PASSWORD`
  - `BUILD_PROVISION_PROFILE_BASE64`
  - `KEYCHAIN_PASSWORD`

### Bước 3 — Đẩy code mới nhất (nếu có thay đổi)
Nếu có thay đổi mã nguồn chưa đẩy lên GitHub:
- Sử dụng quy trình của `.agents/skills/git-push-merge/SKILL.md` để đồng bộ nhánh `mobile` -> `main` -> `upload-source`.

### Bước 4 — Hướng dẫn / Kích hoạt Workflow
Thông báo cho người dùng hoặc kích hoạt workflow `build-ios.yml`:
- Đường dẫn trực tiếp: `https://github.com/tranduytan248/projectmanager/actions/workflows/build-ios.yml`
- Bấm nút **Run workflow** (chọn nhánh `main` hoặc `mobile`).

### Bước 5 — Thu hoạch file .ipa và đưa lên TestFlight
1. Sau khi job chạy xong (khoảng 10-12 phút), tải file `BrewTask-AppStore-Signed-IPA` trong mục **Artifacts**.
2. Giải nén được file `brewtask.ipa`.
3. Mở phần mềm **Transporter** trên máy Mac, kéo thả file `.ipa` vào và bấm **Deliver (Giao hàng)**.
4. Kiểm tra bản build xuất hiện trên TestFlight của App Store Connect sau 10-15 phút.
