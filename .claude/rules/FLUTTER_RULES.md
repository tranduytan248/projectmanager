---
name: quy-tac-code-flutter
description: Quy tắc bắt buộc khi viết, sửa, hoặc review bất kỳ đoạn code Flutter/Dart nào trong dự án. Luôn áp dụng skill này khi người dùng yêu cầu tạo màn hình, tạo giao diện, sửa UI, viết widget, thêm tính năng, hoặc bất kỳ tác vụ nào sinh ra code Flutter — kể cả khi họ không nhắc đến "quy tắc". Hai quy tắc cốt lõi: (1) mọi chuỗi tiếng Việt phải có dấu đầy đủ, (2) giao diện phải code theo kiến trúc custom widget — không dùng trực tiếp widget của Flutter hay thư viện UI trong màn hình, để khi đổi thư viện giao diện chỉ cần sửa tầng widget, không phải code lại toàn bộ dự án.
---

# Quy tắc code Flutter

## Quy tắc 1 — Tiếng Việt PHẢI có dấu đầy đủ

**Mọi chuỗi tiếng Việt trong dự án đều phải viết có dấu, đúng chính tả.** Áp dụng cho:

- Text hiển thị trên giao diện (label, button, title, hint, placeholder…)
- Thông báo lỗi, thông báo xác nhận, validation message
- Nội dung dialog, snackbar, toast, tooltip
- Comment và documentation trong code

```dart
// ❌ SAI
Text('Dang nhap');
Text('Vui long nhap so dien thoai');
// Ham xu ly dang nhap

// ✅ ĐÚNG
Text('Đăng nhập');
Text('Vui lòng nhập số điện thoại');
// Hàm xử lý đăng nhập
```

### Tập trung chuỗi vào một nơi

Không viết chuỗi cứng rải rác trong màn hình. Gom toàn bộ chuỗi hiển thị vào file hằng số (hoặc hệ thống l10n nếu dự án đa ngôn ngữ):

```dart
// lib/core/constants/app_strings.dart
class AppStrings {
  AppStrings._();

  static const String dangNhap = 'Đăng nhập';
  static const String dangXuat = 'Đăng xuất';
  static const String vuiLongNhapSoDienThoai = 'Vui lòng nhập số điện thoại';
  static const String luuThanhCong = 'Lưu dữ liệu thành công';
}

// Trong màn hình
AppText(AppStrings.dangNhap);
```

Lợi ích: sửa chính tả/dấu một chỗ, dễ rà soát chuỗi không dấu, sẵn sàng chuyển sang đa ngôn ngữ sau này.

### Những gì vẫn dùng tiếng Anh không dấu

- Tên biến, tên hàm, tên class, tên file (theo chuẩn Dart: `camelCase`, `PascalCase`, `snake_case.dart`)
- Key của JSON/API, tên route, tên bảng/cột database
- Commit message có thể tiếng Việt có dấu hoặc tiếng Anh — nhưng KHÔNG tiếng Việt không dấu

---

## Quy tắc 2 — Giao diện code theo kiến trúc Custom Widget

### Nguyên tắc

**Màn hình (screen/page) KHÔNG được dùng trực tiếp widget giao diện của Flutter hay của thư viện UI bên thứ ba.** Mọi thành phần giao diện phải đi qua tầng widget riêng của dự án (prefix `App*`). Tầng này là nơi DUY NHẤT được phép gọi widget gốc.

```
┌─────────────────────────────┐
│  Screens / Pages            │  ← chỉ dùng AppButton, AppTextField…
├─────────────────────────────┤
│  Tầng Custom Widget (App*)  │  ← nơi DUY NHẤT gọi widget gốc
├─────────────────────────────┤
│  Flutter Material / Cupertino│
│  hoặc thư viện UI bên ngoài  │  ← đổi thư viện = chỉ sửa tầng trên
└─────────────────────────────┘
```

Khi đổi thư viện giao diện (ví dụ từ Material sang một UI kit khác), chỉ cần sửa phần thân các widget `App*` — toàn bộ màn hình giữ nguyên.

### Cấu trúc thư mục

```
lib/
├── core/
│   ├── constants/
│   │   └── app_strings.dart
│   ├── theme/
│   │   ├── app_colors.dart      # màu sắc
│   │   ├── app_text_styles.dart # kiểu chữ
│   │   └── app_dimens.dart      # khoảng cách, bo góc, kích thước
│   └── widgets/                 # TẦNG CUSTOM WIDGET
│       ├── app_button.dart
│       ├── app_text.dart
│       ├── app_text_field.dart
│       ├── app_card.dart
│       ├── app_dialog.dart
│       ├── app_dropdown.dart
│       ├── app_checkbox.dart
│       ├── app_loading.dart
│       ├── app_app_bar.dart
│       └── app_scaffold.dart
└── features/
    └── <ten_chuc_nang>/
        └── screens/             # chỉ import từ core/widgets
```

### Mẫu viết một custom widget

Widget `App*` phải:
1. **Che giấu hoàn toàn widget gốc** — không lộ tham số/kiểu dữ liệu của thư viện ra ngoài API của widget.
2. **Nhận tham số theo ngữ nghĩa của dự án** (loại nút, kích cỡ…), không theo API thư viện.
3. **Lấy màu/chữ/kích thước từ theme tập trung**, không hard-code.

```dart
// lib/core/widgets/app_button.dart
import 'package:flutter/material.dart';
import '../theme/app_colors.dart';
import '../theme/app_dimens.dart';

enum AppButtonType { primary, secondary, danger, outline }

/// Nút bấm chuẩn của dự án.
/// Mọi màn hình đều dùng AppButton, không dùng ElevatedButton/TextButton trực tiếp.
class AppButton extends StatelessWidget {
  final String label;
  final VoidCallback? onPressed;
  final AppButtonType type;
  final bool isLoading;
  final bool fullWidth;

  const AppButton({
    super.key,
    required this.label,
    required this.onPressed,
    this.type = AppButtonType.primary,
    this.isLoading = false,
    this.fullWidth = false,
  });

  @override
  Widget build(BuildContext context) {
    // Phần thân này là nơi DUY NHẤT gắn với Material.
    // Nếu đổi thư viện UI, chỉ sửa ở đây.
    return SizedBox(
      width: fullWidth ? double.infinity : null,
      child: ElevatedButton(
        style: ElevatedButton.styleFrom(
          backgroundColor: _mauNen(),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(AppDimens.radiusMd),
          ),
        ),
        onPressed: isLoading ? null : onPressed,
        child: isLoading
            ? const AppLoading(size: AppDimens.iconSm)
            : Text(label),
      ),
    );
  }

  Color _mauNen() {
    switch (type) {
      case AppButtonType.primary:
        return AppColors.primary;
      case AppButtonType.secondary:
        return AppColors.secondary;
      case AppButtonType.danger:
        return AppColors.danger;
      case AppButtonType.outline:
        return AppColors.transparent;
    }
  }
}
```

Màn hình chỉ được viết như sau:

```dart
// ✅ ĐÚNG — màn hình chỉ dùng widget App*
AppButton(
  label: AppStrings.dangNhap,
  type: AppButtonType.primary,
  fullWidth: true,
  onPressed: _xuLyDangNhap,
);

// ❌ SAI — dùng trực tiếp widget Material trong màn hình
ElevatedButton(
  onPressed: _xuLyDangNhap,
  child: const Text('Dang nhap'),
);
```

### Danh sách widget CẤM dùng trực tiếp trong màn hình

| Widget gốc (Flutter/thư viện) | Thay bằng |
|---|---|
| `ElevatedButton`, `TextButton`, `OutlinedButton`, `IconButton` | `AppButton`, `AppIconButton` |
| `Text` | `AppText` |
| `TextField`, `TextFormField` | `AppTextField` |
| `Card` | `AppCard` |
| `AlertDialog`, `showDialog` trực tiếp | `AppDialog.show(...)` |
| `SnackBar` trực tiếp | `AppSnackbar.show(...)` |
| `DropdownButton` | `AppDropdown` |
| `Checkbox`, `Switch`, `Radio` | `AppCheckbox`, `AppSwitch`, `AppRadio` |
| `CircularProgressIndicator` | `AppLoading` |
| `AppBar` | `AppAppBar` |
| `Scaffold` | `AppScaffold` |
| Mọi widget của thư viện UI bên thứ ba | Bọc trong widget `App*` tương ứng |

**Ngoại lệ** (được dùng trực tiếp vì thuần bố cục, không phụ thuộc thư viện giao diện): `Column`, `Row`, `Stack`, `Expanded`, `Flexible`, `SizedBox`, `Padding`, `Container`, `ListView`, `GridView`, `SingleChildScrollView`, `GestureDetector`, `Visibility`.

### Quy tắc khi thêm thư viện UI mới

1. KHÔNG import thư viện UI trực tiếp trong bất kỳ file nào thuộc `features/`.
2. Chỉ import trong `core/widgets/` (và `core/theme/` nếu cần).
3. Nếu cần một thành phần chưa có widget `App*` tương ứng → **tạo widget `App*` mới trước**, rồi mới dùng trong màn hình.
4. Không lộ enum/class/kiểu dữ liệu của thư viện ra chữ ký (constructor, tham số) của widget `App*`.

---

## Checklist tự kiểm tra trước khi hoàn thành code

- [ ] Không còn chuỗi tiếng Việt không dấu nào (kể cả trong comment)
- [ ] Chuỗi hiển thị lấy từ `AppStrings`, không hard-code trong màn hình
- [ ] Màn hình không import trực tiếp widget nằm trong danh sách cấm
- [ ] Màn hình không import thư viện UI bên thứ ba
- [ ] Widget `App*` mới (nếu có) đặt trong `core/widgets/`, lấy style từ `core/theme/`
- [ ] Không lộ kiểu dữ liệu của thư viện gốc ra API của widget `App*`

## Khi review / sửa code cũ

- Gặp chuỗi không dấu → sửa thành có dấu ngay, kể cả khi không phải nội dung đang làm.
- Gặp màn hình dùng widget gốc trực tiếp → nếu widget `App*` tương ứng đã có, thay ngay; nếu chưa có, đề xuất tạo và ghi chú lại để xử lý.
- Không tạo widget `App*` trùng chức năng — kiểm tra `core/widgets/` trước khi tạo mới.
