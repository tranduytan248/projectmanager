---
name: wcag-audit-patterns
description: Kiểm toán khả năng tiếp cận (Accessibility Audit) theo chuẩn WCAG 2.2 AA/AAA và tiêu chuẩn quốc tế. Chuyên sâu về: tỷ lệ tương phản màu sắc (Contrast Ratio >= 4.5:1), kích thước vùng bấm cảm ứng (Touch Target >= 48dp), thứ tự điều hướng bàn phím (Focus Order), nhãn nhạy cảm ngữ cảnh và hỗ trợ thiết bị trợ năng / Screen Reader. Áp dụng cho cả Flutter BrewTask và Web ASP.NET.
---

# ♿ Skill: WCAG Audit Patterns — Kiểm Toán Khả Năng Tiếp Cận & Tiêu Chuẩn Trực Quan

## 🎯 Mục đích

Khả năng tiếp cận (Accessibility - a11y) không phải là tính năng phụ thêm, mà là tiêu chuẩn cơ bản của một sản phẩm phần mềm chuyên nghiệp. Skill này hướng dẫn cách kiểm toán, phát hiện và khắc phục triệt để các rào cản tiếp cận theo chuẩn **WCAG 2.2 cấp độ AA**, đảm bảo ứng dụng **BrewTask** và **Web Quản lý dự án** sử dụng thuận tiện cho mọi người dùng, kể cả người lớn tuổi hoặc người có khiếm khuyết thị giác/vận động.

---

## 🏛️ 4 Trụ Cột WCAG (Nguyên Tắc POUR)

1. **Perceivable (Dễ Cảm Nhận)**: Thông tin và giao diện hiển thị sao cho người dùng có thể nhận biết được (tương phản màu sắc rõ nét, chữ không quá nhỏ, icon có nhãn ngữ nghĩa).
2. **Operable (Dễ Thao Tác)**: Mọi tương tác đều điều khiển được bằng cảm ứng hoặc bàn phím (vùng bấm $\ge 48\text{dp}$, không bẫy focus, đủ thời gian thao tác).
3. **Understandable (Dễ Hiểu)**: Ngôn ngữ rõ ràng, phân cấp thị giác mạch lạc, thông báo lỗi cụ thể, hành động phá hủy phải có xác nhận.
4. **Robust (Bền Vững & Tương Thích)**: Mã nguồn tuân thủ ngữ nghĩa chuẩn (Semantic Tree), tương thích tốt với các công cụ đọc màn hình (TalkBack trên Android, VoiceOver trên iOS).

---

## 🔍 Bộ Quy Tắc Kiểm Toán Cốt Lõi Dự Án

### 1. Độ Tương Phản Màu Sắc (Color Contrast - WCAG 1.4.3 & 1.4.11)
- **Văn bản thường (< 18pt / 24px hoặc < 14pt đậm)**: Tỷ lệ tương phản giữa màu chữ và màu nền tối thiểu **4.5:1**.
- **Văn bản lớn ($\ge 18\text{pt}$ hoặc $\ge 14\text{pt}$ đậm)**: Tỷ lệ tương phản tối thiểu **3:1**.
- **Thành phần giao diện & Icon đồ họa**: Tỷ lệ tương phản với nền tối thiểu **3:1**.
- **Dark Theme (BrewTask)**:
  - Nền tối `#1E1E1E` $\rightarrow$ Chữ chính phải đạt từ `#CCCCCC` đến `#FFFFFF` (tỷ lệ > 7:1).
  - Nghiêm cấm dùng chữ xám mờ `#666666` hoặc `#777777` trên nền tối cho thông tin quan trọng.

### 2. Kích Thước Vùng Bấm Cảm Ứng (Touch Target Size - WCAG 2.5.8)
- Kích thước vùng bấm tối thiểu: **$48 \times 48\text{dp}$** (tương đương ngón tay người trưởng thành).
- Khoảng cách an toàn giữa các nút bấm liền kề tối thiểu **8dp** để tránh hiện tượng bấm nhầm (Fat Finger problem).
- Trên Flutter: Bọc các icon nhỏ hoặc gesture detector bằng `AppButton` hoặc cấu hình `minTargetSize` đạt chuẩn.

### 3. Điều Hướng Bàn Phím & Thứ Tự Focus (Focus Order - WCAG 2.4.3)
- Thứ tự chuyển Tab trên Web phải theo logic đọc tự nhiên: Từ trên xuống dưới, từ trái sang phải.
- Con trỏ Focus phải luôn nhìn thấy rõ ràng (không dùng `outline: none` trên CSS mà không có hiệu ứng thay thế).
- Khi mở Modal Dialog: Phải khóa focus bên trong modal (Focus Trap) và phím `Esc` phải đóng được modal.

### 4. Nhãn Ngữ Nghĩa & Hỗ Trợ Đọc Màn Hình (Screen Reader - WCAG 4.1.2)
- Mọi `IconButton` trong Flutter phải có `tooltip` hoặc `Semantics(label: '...')`.
- Các ô nhập form bắt buộc có nhãn hiển thị rõ ràng, không dùng placeholder để thay thế nhãn.
- Hình ảnh chức năng phải có văn bản thay thế mô tả ý nghĩa; hình trang trí phải đánh dấu `excludeFromSemantics: true`.

---

## 📋 Bảng Đối Chiếu Vi Phạm Phổ Biến & Cách Sửa Chữa

| Loại Vi Phạm | Mức Độ | Biểu Hiện Trong Code | Cách Sửa Chuẩn |
|---|---|---|---|
| **Tương phản kém** | Nghiêm trọng | Dùng chữ `#888888` trên nền tối `#1E1E1E` (tỷ lệ ~ 2.8:1) | Chuyển sang `AppColors.textSecondary` (`#A0A0A0` - tỷ lệ 5.2:1) |
| **Vùng bấm quá nhỏ** | Nghiêm trọng | Icon button $24 \times 24\text{dp}$ không có padding | Dùng `AppFab` hoặc bọc `SizedBox(width: 48, height: 48)` |
| **Thiếu nhãn Icon** | Trung bình | `IconButton(icon: Icon(Icons.delete), onPressed: ...)` | Thêm `tooltip: 'Xóa công việc'` |
| **Chữ quá nhỏ** | Trung bình | `fontSize: 11` hoặc `12` cho nội dung chính | Nâng lên tối thiểu `14` theo `AppTextStyles.bodyMedium` |
| **Không có trạng thái lỗi** | Nghiêm trọng | Bấm nút gọi API thất bại nhưng im lặng | Hiển thị `AppErrorState` hoặc `AppSnackBar` tiếng Việt |

---

## 🛠️ Hướng Dẫn Thực Hành Chi Tiết

Xem tài liệu tham khảo bổ trợ tại: [`resources/implementation-playbook.md`](file:///d:/MyProject/projectmanager/.agents/skills/wcag-audit-patterns/resources/implementation-playbook.md)
