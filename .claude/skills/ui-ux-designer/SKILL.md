---
name: ui-ux-designer
description: Chuyên gia thiết kế UI/UX hiện đại, xây dựng hệ thống Design System (Design Tokens, Component Library, Atomic Design), phân cấp thị giác (Visual Hierarchy), bố cục màn hình trực quan, vi tương tác (micro-interactions) và responsive đa nền tảng. Áp dụng chuẩn bảng màu VS Code Dark Theme cho BrewTask và thiết kế Web quản trị dự án chuyên nghiệp.
---

# 🎨 Skill: UI/UX Designer — Thiết Kế Hệ Thống Giao Diện & Trải Nghiệm Người Dùng

## 🎯 Mục đích & Tầm Nhìn Thiết Kế

Skill này định hình tiêu chuẩn thiết kế mỹ thuật và trải nghiệm người dùng cao cấp cho toàn bộ hệ sinh thái dự án: từ ứng dụng di động **BrewTask** (VS Code Dark Theme, huyền bí, tập trung cao độ) đến ứng dụng Web **Quản lý dự án & nhân sự** (hiện đại, trực quan, sạch sẽ, chuẩn chỉ).

---

## 💎 Nguyên Tắc Thiết Kế Cốt Lõi

1. **Phân Cấp Thị Giác Rõ Ràng (Visual Hierarchy)**:
   - Mắt người dùng phải quét được ngay thông tin quan trọng nhất trong 3 giây đầu tiên (Tên dự án, trạng thái KPI, hạn chót công việc).
   - Sử dụng độ đậm nhạt (Font Weight) và kích thước (Scale) để dẫn dắt hành trình thị giác, không lạm dụng quá nhiều màu sắc nổi bật cạnh nhau.
2. **Hệ Thống Thiết Kế Nguyên Tử (Atomic Design & Design Tokens)**:
   - Mọi thành phần giao diện bắt nguồn từ các đơn vị cơ bản: Màu sắc (`AppColors`), Khoảng cách (`AppDimens`), Kiểu chữ (`AppTextStyles`).
   - Tuyệt đối không hard-code mã màu hex, số lẻ khoảng cách trong code giao diện.
3. **5 Trạng Thái Giao Diện Bắt Buộc (5 Screen States)**:
   - **Loading**: Ly cà phê bốc khói `AppLoading` mang tính biểu tượng của BrewTask.
   - **Data**: Hiển thị thẻ gọn gàng, bố cục cân đối, dữ liệu số được format tiếng Việt.
   - **Empty**: Hình minh họa thân thiện, thông điệp rõ ràng, kèm nút hành động kích hoạt.
   - **Error**: Thông báo lỗi tiếng Việt dễ hiểu kèm nút "Thử lại" (`AppErrorState`).
   - **Offline**: Xử lý mất mạng mềm mại, giữ lại dữ liệu cache cục bộ.
4. **Vùng Bấm An Toàn & Khoảng Thở (Touch Target & Breathing Room)**:
   - Touch target $\ge 48\text{dp}$ cho mọi tương tác chạm trên di động.
   - Khoảng cách bội số của 4: 4px, 8px, 12px, 16px, 24px, 32px.
   - Tạo đủ khoảng thở giữa các card và khối nội dung để giảm tải nhận thức (Cognitive Load).

---

## 🎨 Bảng Mã Thiết Kế VS Code Dark Theme (BrewTask)

| Vai Trò Thiết Kế | Mã Màu Chuẩn | Token Code | Ý Nghĩa Sử Dụng |
|---|---|---|---|
| **Nền Tổng Thể** | `#1E1E1E` | `AppColors.backgroundDark` | Nền chính của toàn app, sâu thẳm, dịu mắt. |
| **Bề Mặt Card / Dialog** | `#252526` | `AppColors.cardDark` | Nền các thẻ công việc, popup, thanh điều hướng. |
| **Bề Mặt Nổi Bật Hơn** | `#2D2D30` | `AppColors.surfaceDark` | Thẻ đang được chọn, thanh tiêu đề phụ. |
| **Viền Phân Cách** | `#3E3E42` | `AppColors.borderDark` | Viền nhẹ nhàng giữa các thành phần, tinh tế, mờ ảo. |
| **Màu Điểm Nhấn (Accent)** | `#0E639C` / `#007ACC` | `AppColors.primary` | Nút chính, tab đang chọn, icon trọng tâm. |
| **Thành Công (Success)** | `#4EC9B0` / `#89D185` | `AppColors.success` | Đã hoàn thành, KPI đạt chuẩn, phê duyệt. |
| **Cảnh Báo (Warning)** | `#CE9178` / `#CCA700` | `AppColors.warning` | Gần đến hạn, đang chờ duyệt. |
| **Nguy Hiểm (Danger)** | `#F48771` / `#F14C4C` | `AppColors.danger` | Quá hạn, lỗi, hủy bỏ, thao tác xóa. |
| **Chữ Chính** | `#D4D4D4` | `AppColors.textPrimary` | Nội dung văn bản chính, rõ ràng, tương phản cao. |
| **Chữ Phụ** | `#858585` | `AppColors.textSecondary` | Nhãn phụ, ngày giờ, người phụ trách, ghi chú. |

---

## 📐 Bố Cục Thẻ Công Việc Mẫu Chuẩn (Card Anatomy)

```
+-------------------------------------------------------------+
| [Tag Dự án]                            [Icon Ưu tiên: Cao] |
|                                                             |
| Tiêu đề công việc in đậm (16sp - AppColors.textPrimary)    |
| Mô tả tóm tắt nội dung 1-2 dòng (14sp - AppColors.textSec) |
|                                                             |
| [Avatar Người làm] [Hạn chót: 15:30 05/10]  [Badge: 80%]   |
+-------------------------------------------------------------+
```
- **Padding nội bộ thẻ**: 16dp (`AppDimens.paddingLarge`).
- **Bo góc**: 12dp (`AppDimens.radiusMedium`).
- **Viền thẻ**: 1dp solid `AppColors.borderDark`.
- **Hiệu ứng chạm**: Khi người dùng chạm giữ, thẻ có hiệu ứng phản hồi nhẹ (ripple animation).

---

## ⚡ Vi Tương Tác & Phản Hồi Thị Giác (Micro-Interactions)

1. **Hiệu Ứng Bấm Nút**:
   - Khi bấm, nút hơi thu nhỏ nhẹ (scale 0.98) hoặc sáng nhẹ rồi trả về bình thường.
   - Khi gọi API, nút hiển thị spinner nhỏ gọn bên trong chữ và tự động khóa bấm (disable) để ngăn chặn double-tap.
2. **Chuyển Cảnh Mượt Mà (Transitions)**:
   - Mở màn hình chi tiết công việc dùng hiệu ứng Shared Element Transition (Hero animation) giữa tiêu đề hoặc icon.
   - Danh sách công việc khi tải xong xuất hiện với hiệu ứng trượt lên nhẹ nhàng (Fade-in + Slide-up 200ms).
3. **Kéo Xuống Để Tải Lại (Pull to Refresh)**:
   - Hiển thị hoạt ảnh ly cà phê bốc khói xoay nhẹ mượt mà ở đầu danh sách.

---

## 📋 Checklist Nghiệm Thu UI/UX (Designer Review Checklist)

- [ ] Phân cấp thị giác đã rõ ràng chưa? (Tiêu đề nổi bật, nội dung phụ dịu mắt)
- [ ] Mọi màu sắc có lấy từ `AppColors` không? Có màu nào bị chói mắt hoặc lạc tông không?
- [ ] Mọi khoảng cách padding/margin có tuân theo hệ số 4 (`AppDimens`) không?
- [ ] Vùng bấm của tất cả nút, icon, ô chọn có đạt kích thước tối thiểu **$48 \times 48\text{dp}$**?
- [ ] Đã chuẩn bị đầy đủ thiết kế cho 5 trạng thái: Loading, Data, Empty, Error, Offline chưa?
- [ ] Font chữ hiển thị tiếng Việt có dấu chuẩn đẹp, không bị lỗi dấu hoặc lỗi xuống dòng vô lý?
