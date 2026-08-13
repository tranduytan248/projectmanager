# Checklist thiết kế giao diện Mobile — TTKDGP.ProjectManager

> Danh sách màn hình cần thiết kế nếu build bản mobile từ hệ thống quản lý dự án & nhân sự hiện tại (web ASP.NET MVC). Đánh dấu `[x]` khi đã có wireframe/mockup, và đánh dấu lần 2 khi đã dev xong nếu cần dùng chung checklist này cho cả giai đoạn triển khai.

## 1. Xác thực & chung
- [ ] Đăng nhập (username/password; cân nhắc thêm đăng nhập qua GoConnect/CAS VNPT)
- [ ] Splash / Trang chủ
- [ ] Hồ sơ cá nhân (xem/sửa thông tin, đổi mật khẩu)
- [ ] Danh sách thông báo (UserNotification)
- [ ] Chi tiết thông báo

## 2. Cá nhân (dùng chung mọi user) — ưu tiên cao nhất
- [ ] Dashboard tổng quan cá nhân (số việc, tiến độ, deadline gần)
- [ ] Công việc của tôi (MyWork — danh sách task được giao)
- [ ] Dự án của tôi (danh sách project đang tham gia)
- [ ] Chi tiết Task/Checklist item (mô tả, trạng thái, comment, timelog, đính kèm)
- [ ] Kanban board rút gọn (dạng list/swimlane, đổi trạng thái qua dropdown/swipe thay vì kéo-thả)
- [ ] Ghi timelog (form nhập giờ làm cho 1 task)
- [ ] Báo cáo tuần của tôi (MyReports — xem & nộp báo cáo)
- [ ] Đăng ký nghỉ phép (form tạo yêu cầu)
- [ ] Lịch sử nghỉ phép của tôi

## 3. Quản lý Tổ (role Manager / cờ `wteam.manage`)
- [ ] Bảng điều khiển Tổ (TeamDashboard — tổng hợp tiến độ cả tổ)
- [ ] Danh sách dự án của tổ
- [ ] Chi tiết dự án (thành viên, tiến độ, file đính kèm)
- [ ] Giao việc riêng (PrivateTasks — tạo/giao task ngoài dự án)
- [ ] Duyệt nghỉ phép (danh sách chờ duyệt + hành động duyệt/từ chối)
- [ ] Báo cáo của Tổ (TeamReports — tổng hợp báo cáo tuần nhân viên)
- [ ] Chấm/duyệt KPI tháng (danh sách nhân viên → chấm điểm → duyệt lần cuối)
- [ ] Workload / phân bổ nhân sự (ai đang rảnh/bận)

## 4. KPI (cá nhân)
- [ ] Xem KPI tháng của tôi (điểm số, chi tiết từng tiêu chí)
- [ ] Lịch sử KPI theo tháng

## 5. Quản trị hệ thống (ưu tiên thấp — khuyến nghị giữ trên web)
- [ ] Quản lý người dùng
- [ ] Nhóm quyền
- [ ] Danh mục dùng chung
- [ ] Chức năng hệ thống (bật/tắt)
- [ ] Tích hợp hệ thống (GoConnect, Telegram)

## Ghi chú kiến trúc điều hướng
- Bottom tab bar đề xuất: **Tổng quan · Việc của tôi · Dự án · Thông báo · Cá nhân**
  (Hồ sơ / Nghỉ phép / KPI gộp vào tab "Cá nhân" hoặc menu phụ)
- Vai trò Manager có thêm tab/menu **"Tổ"** dẫn vào nhóm màn hình mục 3.
- Hệ thống dùng `RoleGroup` cấu hình được theo mã `module.action` (không hard-code role) — app mobile nên ẩn/hiện tab theo quyền thực tế của user, tương tự cơ chế phân quyền hiện có ở web (`Models/Permission.cs`, `Models/RoleGroup.cs`).
