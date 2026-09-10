# Sổ Tay Thực Hành Kiểm Toán WCAG 2.2 (Implementation Playbook)

Tài liệu này tổng hợp toàn bộ các mẫu kiểm tra, checklist chi tiết và đoạn mã sửa lỗi theo tiêu chuẩn **WCAG 2.2 cấp độ AA** cho cả hệ thống Web ASP.NET MVC 5 và ứng dụng di động Flutter BrewTask.

---

## 1. 4 Cấp Độ & Nhóm Lỗi Trọng Yếu

| Mức Độ Lỗi | Tác Động Thực Tế | Ví Dụ Điển Hình | Hướng Xử Lý Bắt Buộc |
|---|---|---|---|
| **Chặn hoàn toàn (Critical)** | Người dùng không thể hoàn thành thao tác. | Nút bấm không thể focus bằng bàn phím; không có nhãn form; ảnh chụp nút bấm không có alt text. | Khắc phục ngay lập tức trước mọi tính năng khác. |
| **Nghiêm trọng (Serious)** | Gây khó khăn lớn hoặc nhầm lẫn. | Tương phản màu < 3:1; modal mở ra không trap focus; thiếu tiêu đề trang. | Khắc phục trong cùng sprint. |
| **Vừa phải (Moderate)** | Ảnh hưởng đến tính mượt mà. | Thiếu thuộc tính `lang="vi"`; phân cấp thẻ `h1-h6` lộn xộn; link không rõ ràng ("bấm vào đây"). | Chuẩn hóa theo Design System. |

---

## 2. Checklist Chi Tiết Theo 4 Nguyên Tắc POUR

### Nguyên Tắc 1: Dễ Cảm Nhận (Perceivable)

#### 1.1 Văn Bản Thay Thế (Text Alternatives - Level A)
- [ ] Mọi hình ảnh mang ý nghĩa nội dung đều có `alt` (Web) hoặc `Semantics.label` (Flutter).
- [ ] Hình ảnh trang trí thuần túy phải đặt `alt=""` (Web) hoặc `excludeFromSemantics: true` (Flutter).
- [ ] Các biểu tượng chức năng (icon buttons) bắt buộc có nhãn mô tả hành động (ví dụ: `tooltip: 'Chỉnh sửa dự án'`).

#### 1.2 Cấu Trúc & Phân Cấp (Adaptable - Level A)
- [ ] Web: Sử dụng đúng thứ tự thẻ tiêu đề `<h1>` (tên trang duy nhất) $\rightarrow$ `<h2>` (các mục lớn) $\rightarrow$ `<h3>` (tiểu mục).
- [ ] Mobile: Phân cấp kích thước font rõ ràng: `headlineLarge` $\rightarrow$ `titleMedium` $\rightarrow$ `bodyMedium`.
- [ ] Bảng dữ liệu (Data Table) phải có `<th>` với `scope="col"` hoặc `scope="row"`.

#### 1.3 Khả Năng Phân Biệt & Màu Sắc (Distinguishable - Level AA)
- [ ] Không bao giờ dùng màu sắc là phương tiện DUY NHẤT để truyền tải trạng thái:
  - Trạng thái công việc: Kèm icon hoặc nhãn chữ ("Đang thực hiện", "Hoàn thành") thay vì chỉ đổi màu chấm tròn.
- [ ] Tỷ lệ tương phản chữ trên nền:
  - Chữ thường: $\ge 4.5:1$.
  - Chữ lớn ($\ge 18\text{pt}$ hoặc $\ge 14\text{pt}$ đậm): $\ge 3:1$.
  - Viền ô nhập, icon trạng thái: $\ge 3:1$.
- [ ] Khả năng thu phóng (Resize): Giao diện Web có thể zoom 200% mà không bị mất chữ hoặc gãy bố cục.

---

### Nguyên Tắc 2: Dễ Thao Tác (Operable)

#### 2.1 Điều Khiển Bằng Bàn Phím & Cảm Ứng (Keyboard & Touch - Level A & AA)
- [ ] Mọi tương tác (bấm, chọn, kéo) đều thực hiện được bằng bàn phím (phím `Tab`, `Enter`, `Space`, `Arrow`).
- [ ] Không có bẫy bàn phím (Keyboard Trap): Người dùng luôn có thể tab vào và tab ra khỏi mọi thành phần.
- [ ] Vùng bấm cảm ứng (Touch Target): Mọi nút bấm, checkbox, item danh sách trên BrewTask $\ge 48 \times 48\text{dp}$.
- [ ] Khoảng cách an toàn giữa 2 nút bấm liền kề $\ge 8\text{dp}$.

#### 2.2 Thời Gian Đầy Đủ (Enough Time - Level A)
- [ ] Thông báo tạm thời (Toast, SnackBar): Thời gian hiển thị tối thiểu 4-5 giây để người dùng kịp đọc.
- [ ] Cảnh báo hết phiên đăng nhập (Session Timeout): Phải có hộp thoại cảnh báo trước khi đăng xuất tự động.

#### 2.3 Điều Hướng (Navigable - Level AA)
- [ ] Có liên kết bỏ qua khối nội dung lặp lại (Skip to Main Content) trên Web.
- [ ] Vùng focus đang hoạt động phải hiển thị viền rõ ràng (Focus Ring), không xóa `outline` khi chưa có kiểu thay thế.

---

### Nguyên Tắc 3: Dễ Hiểu (Understandable)

#### 3.1 Dễ Đọc (Readable - Level A)
- [ ] Ngôn ngữ giao diện 100% tiếng Việt có dấu chuẩn xác.
- [ ] Web HTML khai báo đúng `<html lang="vi">`.
- [ ] Tránh dùng từ ngữ quá kỹ thuật; thông báo lỗi giải thích nguyên nhân và cách xử lý thay vì hiện mã lỗi hệ thống (`NullReferenceException`).

#### 3.2 Dự Đoán Được (Predictable - Level A)
- [ ] Thay đổi trạng thái nhập liệu (Dropdown, Checkbox) không được tự ý chuyển hướng trang mà không có sự chủ động của người dùng.
- [ ] Thanh điều hướng (Navigation Bar, Sidebar) phải nằm ở vị trí nhất quán trên tất cả màn hình.

#### 3.3 Hỗ Trợ Nhập Liệu & Tránh Lỗi (Input Assistance - Level AA)
- [ ] Mọi trường bắt buộc phải có dấu sao `*` hoặc chữ ghi rõ "(bắt buộc)".
- [ ] Lỗi validate hiển thị ngay bên dưới ô nhập kèm màu sắc và icon cảnh báo.
- [ ] Các thao tác xóa vĩnh viễn (xóa dự án, hủy tài khoản) bắt buộc phải có hộp thoại xác nhận thứ hai (Confirmation Dialog).

---

### Nguyên Tắc 4: Bền Vững & Tương Thích (Robust)

- [ ] Web: Sử dụng mã HTML5 chuẩn cú pháp, không dùng các thẻ đã lỗi thời (`<center>`, `<font>`).
- [ ] Mobile: Cung cấp đầy đủ nhãn ngữ nghĩa cho TalkBack / VoiceOver qua widget `Semantics`.

---

## 3. Đoạn Mã Mẫu Chuẩn WCAG (Code Snippets)

### Flutter: Nút Bấm Đạt Chuẩn Kích Thước & Khả Năng Tiếp Cận
```dart
// ĐÚNG: Đạt kích thước tối thiểu 48dp, có tooltip và phản hồi thị giác
Widget buildAccessibleButton({
  required VoidCallback onPressed,
  required String label,
  required IconData icon,
}) {
  return Semantics(
    button: true,
    label: label,
    child: InkWell(
      onTap: onPressed,
      borderRadius: BorderRadius.circular(AppDimens.radiusMedium),
      child: Container(
        constraints: const BoxConstraints(
          minWidth: 48,
          minHeight: 48,
        ),
        padding: const EdgeInsets.symmetric(
          horizontal: AppDimens.paddingMedium,
          vertical: AppDimens.paddingSmall,
        ),
        decoration: BoxDecoration(
          color: AppColors.primary,
          borderRadius: BorderRadius.circular(AppDimens.radiusMedium),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, color: AppColors.textLight, size: 20),
            const SizedBox(width: AppDimens.spacingSmall),
            AppText.labelLarge(label, color: AppColors.textLight),
          ],
        ),
      ),
    ),
  );
}
```

### Web: Form Nhập Liệu Đạt Chuẩn WCAG 2.2
```html
<div class="form-group">
  <label for="projectName" class="control-label">
    Tên Dự Án <span class="text-danger" aria-hidden="true">*</span>
    <span class="sr-only">(bắt buộc)</span>
  </label>
  <input type="text" 
         id="projectName" 
         name="ProjectName" 
         class="form-control" 
         required 
         aria-required="true"
         aria-describedby="projectNameHelp projectNameError" />
  <small id="projectNameHelp" class="form-text text-muted">
    Nhập tên dự án rõ ràng, tối đa 150 ký tự.
  </small>
  <span id="projectNameError" class="text-danger field-validation-error" role="alert"></span>
</div>
```
