# TTKDGP Mobile (Flutter)

Khung (template) khởi đầu cho bản mobile của **TTKDGP.ProjectManager**, dựng theo checklist màn hình tại
[`../TTKDGP.ProjectManager/FileMoTa/Checklist-Thiet-ke-Giao-dien-Mobile.md`](../TTKDGP.ProjectManager/FileMoTa/Checklist-Thiet-ke-Giao-dien-Mobile.md).

> Máy dùng để tạo template này **chưa cài Flutter SDK**, nên thư mục này mới chỉ có phần mã nguồn Dart
> (`lib/`, `pubspec.yaml`...). Các thư mục nền tảng `android/`, `ios/`, `web/`... **chưa được sinh ra** —
> làm theo bước 1 bên dưới để sinh chúng.

## 1. Thiết lập lần đầu

1. Cài [Flutter SDK](https://docs.flutter.dev/get-started/install) (kênh stable), chạy `flutter doctor` để kiểm tra.
2. Tại thư mục này, chạy:
   ```
   flutter create --org com.ttkdgp --project-name ttkdgp_mobile .
   ```
   Lệnh này **an toàn** khi chạy trên thư mục đã có `pubspec.yaml` — nó chỉ sinh thêm các thư mục nền tảng
   còn thiếu (`android/`, `ios/`, `web/`, `windows/`, `linux/`, `macos/`), không ghi đè `lib/` đã viết sẵn.
3. Cài dependency:
   ```
   flutter pub get
   ```
4. Chạy thử:
   ```
   flutter run
   ```
   Màn hình đăng nhập sẽ hiện ra; do chưa nối API thật, nhập bất kỳ tài khoản/mật khẩu nào cũng đăng nhập
   được (xem TODO trong `lib/features/auth/auth_controller.dart`) để bạn xem trước toàn bộ điều hướng.

## 2. Vì sao cần lớp API trước khi code tiếp

Backend hiện tại (`TTKDGP.ProjectManager`) dùng **FormsAuthentication cookie**, các controller là MVC trả
HTML/JsonResult nội bộ — **chưa có REST API dùng token**. Trước khi nối dữ liệu thật, cần bổ sung ở backend:

- Endpoint đăng nhập trả **JWT (access + refresh token)**.
- Endpoint JSON tương ứng từng controller hiện có (xem `lib/core/network/api_endpoints.dart` — đã liệt kê
  sẵn danh sách endpoint dự kiến, đặt tên khớp với các `Controller` hiện có).
- Trả kèm danh sách quyền `module.action` của user (dựa theo `RoleGroup`/`Permission.cs`) để app tự
  ẩn/hiện màn hình, giống cơ chế phân quyền hiện có ở web.

## 3. Cấu trúc thư mục `lib/`

```
lib/
  main.dart              # entrypoint, khởi tạo Hive + ProviderScope
  app.dart                # MaterialApp.router + theme
  core/
    theme/                # màu sắc, ThemeData sáng/tối
    router/                # go_router: route + redirect theo trạng thái đăng nhập
    network/               # ApiClient (dio) + danh sách endpoint dự kiến
    storage/                # lưu access/refresh token (flutter_secure_storage)
    providers/              # Riverpod provider dùng chung (token storage, api client)
    auth/                   # PermissionGate — ẩn/hiện widget theo quyền module.action
  features/
    auth/                    # đăng nhập, AuthController (Riverpod StateNotifier)
    dashboard/                # Tổng quan cá nhân
    mywork/                    # Công việc của tôi + chi tiết task
    projects/                   # Dự án của tôi + chi tiết dự án
    checklist/                   # Kanban rút gọn theo dự án
    leaves/                       # Nghỉ phép của tôi + form đăng ký
    kpi/                           # KPI cá nhân
    notifications/                  # Thông báo
    profile/                         # Hồ sơ cá nhân + đăng xuất
    team/                             # Nhóm "Quản lý Tổ" (role wteam.manage)
  shared/
    widgets/
      app_scaffold.dart              # Bottom nav bar, tự thêm tab "Tổ" nếu có quyền
      placeholder_body.dart          # Body tạm cho màn chưa nối API
```

Mỗi màn hình trong `features/` hiện đang hiển thị `PlaceholderBody` kèm ghi chú `TODO` trỏ tới endpoint
API cần gọi — thay dần bằng danh sách/form thật khi backend đã sẵn sàng.

## 4. Package đã cấu hình sẵn trong `pubspec.yaml`

| Package | Dùng để |
|---|---|
| `flutter_riverpod` | Quản lý state |
| `go_router` | Điều hướng, shell route cho bottom nav, redirect theo đăng nhập |
| `dio` | Gọi API |
| `flutter_secure_storage` | Lưu token an toàn (Keychain/Keystore) |
| `hive` + `hive_flutter` | Cache offline nhẹ (vd. xem "Công việc của tôi" khi mất mạng) |
| `intl` | Định dạng ngày/giờ/số |
| `cached_network_image` | Cache ảnh avatar/file đính kèm |

## 5. Bước tiếp theo đề xuất

1. Thiết kế & code lớp API JWT ở backend (mục 2).
2. Nối `AuthController.login` và từng màn hình với `ApiClient` thật, bỏ dữ liệu giả trong
   `auth_controller.dart`.
3. Thay `ChecklistBoardScreen` bằng board kéo-thả thật, dùng package
   [`kanban_board`](https://pub.dev/packages/kanban_board).
4. Tích hợp Firebase Cloud Messaging cho `NotificationsScreen` (push thông báo giao việc/duyệt nghỉ phép).
5. Đối chiếu lại checklist màn hình gốc và tick `[x]` từng mục khi hoàn thành.
