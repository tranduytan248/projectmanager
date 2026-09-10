---
name: flutter-expert
description: Chuyên gia Flutter & Dart chuyên sâu cho ứng dụng mobile BrewTask. Làm chủ kiến trúc Clean Architecture, quản lý State Management, tối ưu hiệu năng render 60/120fps, chống jank frame, quản lý vòng đời Widget, phòng chống Memory Leak, viết Widget Test / Golden Test, và TUÂN THỦ 100% việc dùng bộ Custom Widgets App* của dự án.
---

# 🚀 Skill: Flutter Expert — Chuyên Gia Kiến Trúc & Hiệu Năng Flutter BrewTask

## 🎯 Mục đích

Skill này cung cấp các hướng dẫn kỹ thuật chuyên sâu nhất về Flutter & Dart cho ứng dụng di động **BrewTask**, giúp xây dựng ứng dụng mượt mà, đạt tốc độ khung hình 60/120fps ổn định, không lỗi rò rỉ bộ nhớ, kiến trúc sạch sẽ và tuân thủ tuyệt đối các quy tắc bắt buộc của dự án.

---

## ⛔ QUY TẮC SỐNG CÒN CỦA DỰ ÁN: 100% CUSTOM WIDGETS `App*`

Trong dự án BrewTask, **TUYỆT ĐỐI CẤM DÙNG TRỰC TIẾP CÁC WIDGET GỐC FLUTTER**. Bắt buộc phải thay thế 100% bằng bộ custom widget thuộc Design System của dự án:

| Widget Gốc Bị Cấm | BẮT BUỘC Thay Thế Bằng | Lý Do & Điểm Khác Biệt |
|---|---|---|
| `CircularProgressIndicator` | **`AppLoading`** | Hiệu ứng ly cà phê bốc khói đặc trưng thương hiệu BrewTask, tự thích ứng màu dark theme. |
| `TextField`, `TextFormField` | **`AppTextField`** | Tích hợp sẵn bo góc chuẩn, viền focus VS Code, xử lý label, hint text và thông báo lỗi tiếng Việt. |
| `Text` | **`AppText`** (hoặc `AppTextStyles`) | Kiểm soát chặt chẽ cỡ chữ theo thang chuẩn, line-height an toàn cho tiếng Việt có dấu. |
| `ElevatedButton`, `TextButton` | **`AppButton`** | Đảm bảo kích thước bấm $\ge 48\text{dp}$, tích hợp trạng thái loading bên trong và chống double-tap. |
| `FloatingActionButton` | **`AppFab`** | Chuẩn kích thước, màu sắc và hiệu ứng bóng đổ của hệ thống. |
| `Card` | **`AppCard`** | Tích hợp sẵn nền `AppColors.cardDark`, viền `AppColors.borderDark`, padding chuẩn `AppDimens`. |
| `Checkbox` | **`AppCheckbox`** | Đảm bảo touch target $\ge 48\text{dp}$, bo góc nhẹ nhàng, màu tick chuẩn. |
| `DropdownButton`, `DropdownFormField` | **`AppDropdown`** | Menu sổ xuống tùy biến phong cách dark theme, không bị tràn màn hình. |
| Date Picker mặc định | **`AppDateField`** | Lịch chọn ngày tiếng Việt chuẩn, format `dd/MM/yyyy`. |
| Trình soạn thảo thô | **`AppRichEditor`** | Hỗ trợ định dạng văn bản ghi chú công việc trực quan. |
| Màn hình lỗi sơ sài | **`AppErrorState`** | Giao diện báo lỗi chuẩn kèm icon cảnh báo và nút bấm "Thử lại". |

---

## ⚡ Tối Ưu Hóa Hiệu Năng & Tránh Rơi Khung Hình (Performance & Jank Prevention)

### 1. Giảm Thiểu Rebuild Cây Widget Thừa Thãi
- **Luôn tận dụng từ khóa `const`**: Giúp Flutter tái sử dụng instance của widget và bỏ qua hoàn toàn bước rebuild khi widget cha render lại.
- **Tách nhỏ các Widget (Extract to separate Widget)**:
  - KHÔNG viết các hàm trả về widget kiểu `Widget _buildHeader()`.
  - HÃY tách thành class riêng `class HeaderWidget extends StatelessWidget` để Flutter tối ưu hóa cây phần tử (Element Tree).
- **Phạm vi State nhỏ gọn**: Sử dụng `ValueNotifier`, `Selector` hoặc `BlocSelector` để chỉ rebuild đúng vùng giao diện cần cập nhật thay vì toàn bộ màn hình.

### 2. Tối Ưu Hóa Danh Sách Dữ Liệu Lớn (ListView Optimization)
- Luôn sử dụng `ListView.builder` hoặc `CustomScrollView` với `SliverList` cho danh sách có nhiều phần tử.
- Cung cấp `itemExtent` hoặc `prototypeItem` nếu các item có chiều cao cố định để Flutter tính toán vị trí cuộn ngay lập tức mà không cần đo đạc từng phần tử.
- Thêm `addAutomaticKeepAlives: false` và `addRepaintBoundaries: true` khi cuộn các danh sách phức tạp.

### 3. Phòng Ngừa Rò Rỉ Bộ Nhớ (Memory Leak Prevention)
- Mọi `AnimationController`, `TextEditingController`, `ScrollController`, `StreamSubscription` khởi tạo trong `StatefulWidget` **BẮT BUỘC phải được gọi `.dispose()`** trong phương thức `dispose()`.
- Luôn kiểm tra tính hợp lệ của State trước khi cập nhật sau một tác vụ bất đồng bộ:
  ```dart
  final result = await apiService.fetchData();
  if (!mounted) return; // BẮT BUỘC để tránh setState sau khi màn hình đã dispose
  setState(() {
    _data = result;
  });
  ```

---

## 🧪 Viết Widget Test & Kiểm Thử Tương Tác

```dart
// test/widgets/task_card_test.dart
import 'package:flutter_test/flutter_test.dart';
import 'package:brew_task/shared/widgets/app_card.dart';
import 'package:brew_task/shared/widgets/app_text.dart';
import 'package:brew_task/core/theme/app_colors.dart';

void main() {
  testWidgets('AppCard render đúng màu nền và chứa tiêu đề công việc', (WidgetTester tester) async {
    // Arrange: Bơm widget vào môi trường test
    await tester.pumpWidget(
      const Directionality(
        textDirection: TextDirection.ltr,
        child: AppCard(
          child: AppText.titleMedium('Hoàn thành báo cáo tiến độ'),
        ),
      ),
    );

    // Assert: Tìm kiếm widget hiển thị
    final textFinder = find.text('Hoàn thành báo cáo tiến độ');
    expect(textFinder, findsOneWidget);

    // Kiểm tra không dùng widget gốc cấm
    expect(find.byType(CircularProgressIndicator), findsNothing);
  });
}
```

---

## 📋 Checklist Kiểm Soát Chất Lượng Flutter (Flutter Quality Gate)

- [ ] Không có bất kỳ widget gốc nào bị dùng trực tiếp (`CircularProgressIndicator`, `Card`, `TextField`,...)?
- [ ] Mọi màu sắc được trích xuất từ `AppColors`, không hard-code hex?
- [ ] Mọi khoảng cách padding/margin tuân thủ hệ số 4 của `AppDimens`?
- [ ] Tất cả các Controller và Stream đã được `dispose()` sạch sẽ?
- [ ] Mọi lời gọi `setState()` sau `await` đều có chốt chặn `if (!mounted) return`?
- [ ] Chạy `flutter analyze` trong terminal đạt **0 Errors, 0 Warnings**?
- [ ] Chạy `flutter test` đạt **100% PASS**?
