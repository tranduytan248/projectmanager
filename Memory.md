# Memory.md — Nhật ký tri thức của dự án

---

# [2026-08-14] Vấn đề: Fix text màu xám toàn ứng dụng mobile — phát hiện lệch kiến trúc với FLUTTER_RULES.md

## 1. Mô tả vấn đề
Nguyên văn: "Hãy fix hết text màu xám trên toàn bộ ứng dụng. Thực hiện theo agents, rules, skills
của .claude" — yêu cầu sửa toàn bộ chữ màu xám khó đọc, VÀ làm theo đúng agent/rule/skill định
nghĩa trong `.claude/`.

## 2. Phân tích ban đầu
- **Bối cảnh**: Trước đó đã sửa màu xám cho riêng màn Chi tiết dự án (đổi `Colors.black45/54`
  rải rác sang `AppTheme.textMuted`). Người dùng muốn áp dụng cho TOÀN app, và lần này chỉ rõ
  phải theo `.claude/agents/designer-mobile-pro.md` + `.claude/rules/FLUTTER_RULES.md` +
  `.claude/skills/phan-tich-van-de/SKILL.md`.
- **Phát hiện xung đột lớn**: `FLUTTER_RULES.md` bắt buộc kiến trúc widget `App*`
  (`AppText`/`AppButton`/`AppTextField`/`AppCard`/`AppDialog`...) + theme tập trung 3 file
  `core/theme/app_colors.dart`/`app_text_styles.dart`/`app_dimens.dart` — cấm dùng thẳng `Text`,
  `Container` kiểu widget gốc trong màn hình. TOÀN BỘ 20 file Flutter đã viết trong các phiên
  trước (Dashboard, Dự án, Checklist, Cài đặt, Chi tiết dự án, Đăng nhập...) đều dùng `Text(...)`
  trực tiếp với style rải rác — CHƯA có `core/theme/` hay `core/widgets/` nào tồn tại. Cũng có
  120 lượt gọi `AppTheme.<màu>` (class `config/app_theme.dart`, không phải tên `AppColors` rule
  yêu cầu) trải trên 11 file.
- **Mục tiêu**: (a) Sửa đúng lỗi chữ xám khó đọc trên nền trắng; (b) tuân thủ kiến trúc App*.
- **Phạm vi quyết định**: Sửa nhanh (đổi màu tại chỗ, không tạo widget mới) HAY dựng nền tảng
  `core/theme/` + `AppText` trước rồi migrate toàn bộ `Text` sang `AppText`.
- **Rủi ro**: Dựng nền tảng rồi migrate cả 20 file là khối lượng lớn hơn hẳn "chỉ sửa màu xám" —
  cần người dùng xác nhận trước khi làm, đúng tinh thần "không bắt tay giải quyết ngay" của
  skill `phan-tich-van-de` và bước "Đề xuất phương án (trước khi code)" của
  `designer-mobile-pro`.

## 3. Câu hỏi làm rõ (dùng AskUserQuestion thay vì hỏi tự do, do phạm vi đã rõ 2 lựa chọn)
1. Sửa mau xám theo hướng nào: (A) chỉ đổi màu tại chỗ (`Colors.black45/54` → token màu đậm hơn,
   không tạo widget mới), hay (B) dựng nền tảng `core/theme/` + widget `AppText` theo đúng
   `FLUTTER_RULES.md` rồi migrate toàn bộ `Text` hiện có sang `AppText`?

## 4. Câu trả lời & Quyết định
1. → **(B) Dựng nền tảng App\* trước.** Người dùng chọn tuân thủ đúng kiến trúc rule yêu cầu
   thay vì vá nhanh.
   - **Quyết định phạm vi con** (tự quyết định dựa trên nguyên tắc "không làm việc thừa" +
     hướng dẫn "rồi mới sửa từng màn hình" của designer-mobile-pro — sẽ xác nhận lại nếu người
     dùng muốn khác):
     - Tạo `core/theme/app_colors.dart` với vai trò `textPrimary`/`textSecondary` MỚI (đây là
       phần thật sự thiếu, gây ra lỗi chữ xám) + các màu đã có sẵn trong `AppTheme` (giữ đúng giá
       trị, không đổi màu thương hiệu).
     - Tạo `core/theme/app_text_styles.dart`, `core/theme/app_dimens.dart` theo đúng thang trong
       rule (chữ 20-22/16-18/14-16/12-13, khoảng cách bội số 4).
     - Tạo `core/widgets/app_text.dart` — widget bắt buộc theo rule, migrate TOÀN BỘ 20 file
       `Text(...)` hiện có sang `AppText`.
     - KHÔNG đổi tên `AppTheme` → `AppColors` ở 120 chỗ gọi màu đã có (`AppTheme.brandBlue`...)
       — đó là đổi tên thuần tuý không mang lại giá trị, khác với việc thêm vai trò
       textPrimary/textSecondary còn thiếu. `AppColors` mới sẽ à alias/tham chiếu cùng giá trị.
     - CHƯA tạo `AppButton`/`AppTextField`/`AppCard`/`AppDialog`/... (bảng cấm còn lại trong
       rule) — phạm vi lần này là CHỮ (đúng yêu cầu "fix text màu xám"), các widget khác để lần
       sau, ghi rõ trong mục Ghi chú để không quên.

## 5. Checklist: Dựng core/theme + AppText, migrate toàn bộ Text sang AppText

### Chuẩn bị
- [x] Đọc `.claude/agents/designer-mobile-pro.md`, `.claude/rules/FLUTTER_RULES.md`.
- [x] Khảo sát toàn bộ: 20 file dùng `Text(`, 8 file dùng `Colors.black45/54` (chữ xám thật sự),
      11 file / 120 lượt dùng `AppTheme.<màu>`.

### Thực hiện
- [x] Tạo `lib/core/theme/app_colors.dart` (thêm `textPrimary`/`textSecondary`/`textFaint` +
      alias các màu đã có trong `AppTheme`).
- [x] Tạo `lib/core/theme/app_text_styles.dart` (title/heading/body/caption theo đúng thang cỡ
      chữ trong rule).
- [x] Tạo `lib/core/theme/app_dimens.dart` (khoảng cách 4/8/12/16/24/32, bo góc).
- [x] Tạo `lib/core/widgets/app_text.dart`.
- [x] Migrate lần lượt 20 file — TICK từng file khi xong (danh sách ở dưới).
- [x] `flutter analyze` sạch sau khi xong toàn bộ (chỉ còn 2 info-level lint có sẵn từ trước,
      không liên quan, ở `display_manager.dart`).

#### Danh sách 20 file cần migrate Text → AppText
- [x] `core/utils/dialog_service.dart`
- [x] `core/utils/toast_service.dart`
- [x] `features/auth/login_screen.dart`
- [x] `features/checklist/checklist_board_screen.dart`
- [x] `features/dashboard/dashboard_screen.dart`
- [x] `features/kpi/kpi_screen.dart`
- [x] `features/leaves/leave_list_screen.dart`
- [x] `features/leaves/leave_request_form_screen.dart`
- [x] `features/mywork/my_work_screen.dart`
- [x] `features/mywork/task_detail_screen.dart`
- [x] `features/notifications/notifications_screen.dart`
- [x] `features/profile/policy_screen.dart`
- [x] `features/profile/profile_screen.dart`
- [x] `features/projects/my_projects_screen.dart`
- [x] `features/projects/project_detail_screen.dart`
- [x] `features/team/leave_approval_screen.dart`
- [x] `features/team/private_tasks_screen.dart`
- [x] `features/team/team_dashboard_screen.dart`
- [x] `shared/widgets/app_bottom_nav.dart`
- [x] `shared/widgets/placeholder_body.dart`

### Kiểm tra / Nghiệm thu
- [x] `grep` không còn `Text(` trực tiếp nào trong `lib/features/`, `lib/shared/`, `lib/core/utils/`.
- [x] Không còn `Colors.black45`/`Colors.black54` dùng cho chữ phụ ở bất kỳ đâu — các chỗ còn lại
      chỉ là icon/viền (`caretRight`, `Border.all`) hoặc chú thích code, không phải chữ.
- [x] Build emulator thật, xem lại ít nhất Dashboard + Chi tiết dự án + Checklist — đã build
      `--release` lên `emulator-5554`, chụp màn hình xác nhận: Dashboard, Dự án của tôi, Chi tiết
      dự án, Checklist, Chính sách bảo mật đều hiển thị chữ phụ (nhãn, ghi chú, ngày) rõ ràng,
      không còn xám mờ khó đọc.

### Ghi chú
- **CHƯA làm** (phạm vi ngoài yêu cầu lần này, cần mở vấn đề riêng nếu muốn tuân thủ đầy đủ
  `FLUTTER_RULES.md`): `AppButton` (thay `FilledButton`/`OutlinedButton`/`IconButton`),
  `AppTextField` (thay `TextField`/`TextFormField`), `AppCard`, `AppDialog` (thay
  `showModalBottomSheet`/`showDialog` trực tiếp đang dùng rất nhiều), `AppDropdown` (thay
  `DropdownButtonFormField`), `AppCheckbox`, `AppLoading` (thay `CircularProgressIndicator`),
  `AppAppBar`, `AppScaffold`. Toàn bộ các file trong `lib/features/` hiện vẫn import
  `package:flutter/material.dart` trực tiếp — vi phạm "Màn hình không import thư viện UI bên thứ
  ba" của rule cho tới khi có đủ bộ widget `App*` để bọc hết.
- `AppStrings` (gom chuỗi hiển thị vào một nơi) cũng CHƯA làm — chuỗi tiếng Việt vẫn viết thẳng
  trong từng màn hình. Đã có dấu đầy đủ (đúng Quy tắc 1) nhưng chưa tập trung vào một file.

---

# [2026-08-14] Vấn đề: Nút "Thêm mới" trong Checklist mobile cho PM/Quản lý Tổ

## 1. Mô tả vấn đề
Nguyên văn yêu cầu: "Bây giờ đối với bên trong checklist thì PM hoặc là Quản trị Tổ thì sẽ có
nút Thêm mới. Hãy cập nhật trong màn hình checklist" — thêm chức năng tạo đầu việc mới trong màn
Checklist của app mobile, chỉ hiện cho người có quyền (PM dự án hoặc Quản lý Tổ).

## 2. Phân tích ban đầu
- **Bối cảnh**: Màn Checklist mobile (`Mobile-Flutter/lib/features/checklist/checklist_board_screen.dart`,
  xây dựng cùng ngày) hiện CHỈ ĐỌC — danh sách đầu việc phẳng, lọc theo từ khoá/trạng thái/loại
  việc/người thực hiện/hạn, chưa có thêm/sửa/xoá. Backend `ChecklistApi/Index` (`Controllers/Api/ChecklistApiController.cs`)
  cũng chỉ có action đọc.
- Bên web, "Thêm mục" nằm trong `ChecklistController.Edit` (GET dựng form trống, POST lưu) +
  view `Views/Checklist/_EditForm.cshtml` — quyền `CanEditProject(projectId)` (PM dự án HOẶC
  Quản lý Tổ), đúng khớp yêu cầu.
- **Mục tiêu**: Cho PM/Quản lý Tổ tạo đầu việc mới ngay trên mobile, không phải mở trình duyệt.
- **Phạm vi**: Chỉ "Thêm mới" — CHƯA bao gồm Sửa/Xoá mục có sẵn (nằm ngoài yêu cầu lần này).
- **Ràng buộc**: Form web đầy đủ có 10 trường khi tạo mới: Loại việc (Checklist/Hỗ trợ), Ưu
  tiên, Tên công việc, Mã (duy nhất trong dự án), Mục cha (tạo việc con), Người thực hiện (chỉ
  gồm thành viên dự án), Ngày bắt đầu (mặc định hôm nay), Hạn hoàn thành (**bắt buộc theo
  nghiệp vụ**, backend tự kiểm tay vì `WorkTask` dùng chung cho nhiều luồng nên không gắn
  `[Required]` được), Tuần/Năm (tự suy từ Hạn hoàn thành, đổi tay được), Trạng thái (mặc định
  "Chưa bắt đầu"), Mô tả (rich text). Trường "Tiến độ" KHÔNG hiện khi tạo mới.
- **Rủi ro/giả định**: 10 trường trên một form điện thoại sẽ khá dài — các màn trước trong phiên
  này (Dashboard, Chi tiết dự án) đều được RÚT GỌN có chủ đích so với web (bỏ biểu đồ, bỏ
  credential nhạy cảm...), nên cần hỏi lại có áp dụng tinh thần đó ở đây không, tránh tự ý cắt
  bớt trường nghiệp vụ quan trọng.
- **Phương án sơ bộ**:
  - A. Form đầy đủ 10 trường như web.
  - B. Form rút gọn: Tên, Mã, Loại việc, Ưu tiên, Người thực hiện, Hạn hoàn thành, Mô tả — bỏ
    Mục cha (luôn tạo mục gốc), Ngày bắt đầu (tự lấy hôm nay), Tuần/Năm (tự suy từ Hạn hoàn
    thành ở backend, không cho sửa tay trên mobile), Trạng thái (mặc định "Chưa bắt đầu").
  - C. Chỉ thêm nút mở trình duyệt tới form web — không hợp vì yêu cầu rõ là chức năng thật
    trong app.

## 3. Câu hỏi làm rõ
1. Form rút gọn (Phương án B) hay đầy đủ 10 trường như web (Phương án A)?
2. "Mục cha" (tạo việc con trong một mục có sẵn) — có cần trên mobile không, hay bản đầu chỉ
   tạo mục gốc?
3. Danh sách "Người thực hiện" khi tạo mới có cần giới hạn đúng bằng thành viên đang tham gia dự
   án đó (giống web) không?
4. Sau khi thêm xong, quay lại danh sách Checklist (làm mới danh sách) là đủ, hay cần mở luôn
   màn Chi tiết công việc vừa tạo?

## 4. Câu trả lời & Quyết định
1. Form rút gọn hay đầy đủ? → **Rút gọn**. Giữ: Tên, Mã, Loại việc, Ưu tiên, Người thực hiện,
   Hạn hoàn thành, Mô tả. Bỏ: Mục cha (luôn tạo mục gốc, `ParentId = 0`), Ngày bắt đầu (backend
   tự gán hôm nay), Tuần/Năm (backend tự suy từ Hạn hoàn thành, không cho sửa tay), Trạng thái
   (mặc định "Chưa bắt đầu", không hiện trên form vì luôn giống nhau lúc tạo mới).
2. Người thực hiện có giới hạn theo thành viên dự án không? → **Có** — dropdown chỉ gồm thành
   viên đang tham gia đúng dự án đó (`WorkService.AssignmentsOfProject`), khớp web.
3. Sau khi thêm xong làm gì? → **Quay lại danh sách Checklist**, làm mới (`_reload()`) để thấy
   việc vừa thêm. Không mở màn Chi tiết công việc (màn đó còn là placeholder).

## 5. Checklist: Nút "Thêm mới" trong Checklist mobile

### Chuẩn bị
- [x] Đọc `ChecklistController.Edit` (GET/POST) + `_EditForm.cshtml` để biết đúng field/quyền/
      validation cần khớp.
- [x] Xác nhận quyền: `CanEditProject(projectId)` = PM dự án HOẶC Quản lý Tổ — khớp
      `ChecklistDto.CanEdit` đã có sẵn từ `ChecklistApi/Index`.

### Thực hiện
- [x] Thêm `POST ChecklistApi/Create` (`Controllers/Api/ChecklistApiController.cs`): nhận
      projectId, title, code, kind, priority, assigneeUserId, dueDate, description; tự gán
      ParentId=0, StartDate=hôm nay, State=ChuaBatDau, tự suy Week/Year từ dueDate
      (`WeekHelper.GetWeek/GetYear`); kiểm `CanEditProject` + người thực hiện phải đang tham gia
      dự án; gọi `NotificationService.ProjectTaskAssigned` khi có giao việc — khớp
      `ChecklistController.Edit` bên web. **Sửa lại quyết định nhỏ**: kiểm tra kỹ thì Mã việc
      (Code) bên web KHÔNG có `[Required]` và KHÔNG kiểm trùng thật sự (chỉ có chú thích quy ước
      trong `_EditForm.cshtml`) — nên API cũng để Mã là tuỳ chọn, không chặn trùng, đúng thực tế
      hệ thống thay vì theo giả định ban đầu trong mục 5 (Kiểm tra/Nghiệm thu bên dưới đã sửa lại).
- [x] Thêm `AssigneeOptionDto` (UserId, FullName) — `ChecklistDto.Assignees`, lấy từ
      `WorkService.AssignmentsOfProject(projectId).Where(IsActive)`, luôn trả kèm trong
      `ChecklistApi/Index` (không cần gọi riêng khi mở form).
- [x] Build backend (`msbuild`) + test `curl` với token thật: `Index` trả `Assignees`, `Create`
      tạo thành công (Id 321, "Test tạo từ API"), gọi lại `Index` thấy `TotalCount` tăng đúng.
- [x] Flutter: FloatingActionButton "Thêm mới" trong `checklist_board_screen.dart`, CHỈ hiện khi
      `data.canEdit == true` (dùng FutureBuilder riêng tren cung `_future` de khong goi lai API).
- [x] Flutter: form thêm mới dạng bottom sheet (`_AddTaskSheet`) — 7 trường theo quyết định mục
      4.1 (Tên*, Mã, Loại việc, Ưu tiên, Người thực hiện, Hạn hoàn thành*, Mô tả), validate Tên +
      Hạn hoàn thành bắt buộc.
- [x] Gọi API tạo xong → đóng sheet (`Navigator.pop(true)`), `_reload()` danh sách Checklist,
      hiện toast xác nhận.
- [x] `flutter analyze` sạch.

### Kiểm tra / Nghiệm thu
- [ ] Tài khoản PM/Quản lý Tổ thấy nút "Thêm mới"; tài khoản thành viên thường KHÔNG thấy.
- [x] Tạo việc mới thành công qua API, xuất hiện đúng trong danh sách Checklist sau khi gọi lại
      (đã test bằng curl — TotalCount tăng từ 1 lên 2).
- [ ] Không nhập Tên hoặc Hạn hoàn thành thì bị chặn (bắt buộc theo nghiệp vụ) — Mã việc KHÔNG
      bắt buộc và không kiểm trùng (xác minh đúng thực tế web, xem mục "Thực hiện").
- [ ] Build emulator thật, tự tay tạo 1 việc để xác nhận luồng đầu-cuối.

### Ghi chú
- Không làm Sửa/Xoá mục có sẵn trong lần này — chỉ Thêm mới, theo đúng phạm vi yêu cầu.
- Nếu sau này cần "Mục cha"/đổi Trạng thái lúc tạo/chọn Tuần-Năm tay, quay lại mục này bổ sung
  thay vì mở vấn đề mới.
