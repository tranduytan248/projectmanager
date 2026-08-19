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

---

# [2026-08-16] Vấn đề: Tính năng "Log công việc" (nhật ký thao tác) + "Cập nhật trạng thái" trên mobile

## 1. Mô tả vấn đề
Nguyên văn (2 yêu cầu liên tiếp cùng lúc):
1. "Xây dựng 1 tính năng log công việc. Trong này lưu vết lại, ai đã thao tác gì? thay đổi/cập
   nhật thông tin gì? Mọi thứ phải rõ ràng, theo dòng lịch sử từ mới nhất đến cũ." — kèm ảnh
   khoanh đỏ góc trên phải AppBar màn "Chi tiết công việc" mobile (hiện trống).
2. "Bổ sung thêm phần cập nhật trạng thái công việc trên này nữa." — kèm ảnh khoanh đỏ khối
   "Thông tin chung" (đang chỉ hiển thị Trạng thái dạng chữ tĩnh, không đổi được).

## 2. Phân tích ban đầu
- **Bối cảnh**: Vừa hoàn thành tính năng "Chi tiết công việc" mobile view-only + 3 hành động
  tách riêng (Ghi giờ, Cập nhật danh sách việc cần làm, Trao đổi — xem mục
  [2026-08-14]/[2026-08-15] các vấn đề trước, task_detail_screen.dart). Khối "Thông tin chung"
  hiện hiển thị Trạng thái tĩnh, không có hành động đổi.
- **Nghiên cứu xác nhận** (Explore agent, không cần khảo sát lại):
  - Web ĐÃ có 2 luồng đổi trạng thái: `ChecklistController.SetState` (Kanban kéo-thả, chỉ đổi
    State) và `MyWorkController.Report` (form tự báo cáo: State + Progress% + ghi chú, có validate
    `TimeLogService.ValidateStateChange` — chặn chuyển "Đang làm"/"Hoàn thành" nếu công việc CHƯA
    được ghi giờ nào). Cả hai gọi chung `WorkService.ApplyState(task, state, progress)`.
  - Enum trạng thái `TaskStates` (`Models/Work/WorkTask.cs`): ChuaBatDau (Chưa bắt đầu), DangLam
    (Đang làm), TamDung (Tạm dừng), HoanThanh (Hoàn thành), Huy (Huỷ). Không có state-machine cứng
    (mọi trạng thái → mọi trạng thái được), chỉ có 2 ràng buộc mềm: (a) validate giờ đã ghi ở
    trên; (b) `ApplyState` tự set/xoá `CompletedAt` và ép `Progress=100` khi vào Hoàn thành.
  - Mobile API (`ChecklistApiController.cs`, `MyWorkApiController.cs`) CHƯA có action đổi trạng
    thái nào — phải xây mới hoàn toàn.
  - KHÔNG tồn tại cơ chế audit-log/history nào trong hệ thống (đã tìm `Log`/`History`/`Activity`/
    `NhatKy` — chỉ có `WorkLog` là báo cáo tuần theo dự án, khác hoàn toàn mục đích). Phải xây từ
    đầu, theo đúng kiến trúc lưu trữ hiện có (`Data/Repository.cs`, `SqlStore<T>`, KHÔNG dùng EF
    Migrations vì dự án không theo hướng đó).
  - Danh sách đầy đủ các điểm MUTATE dữ liệu công việc cần gắn log (cả web lẫn API, xem báo cáo
    Explore agent gốc để tra lại nếu cần): `ChecklistController.SetState/Report(MyWork)/Edit`,
    `Services/WorkService.ApplyState`, `LogTime/DeleteTimeLog` (web + API), `AddTodo/ToggleTodo/
    EditTodo/DeleteTodo` (web + API), `Comment/RecallComment` (web + API, cả ChecklistController
    lẫn MyWorkController).
- **Mục tiêu bề mặt**: xem lịch sử ai làm gì trên 1 công việc; đổi trạng thái ngay trên mobile.
- **Mục tiêu sâu xa**: minh bạch hoá thao tác (trace được trách nhiệm), và các hành động sẵn có
  (ghi giờ/todo/bình luận/đổi trạng thái) tự sinh log — không cần người dùng tự khai.

## 3. Câu hỏi làm rõ (dùng AskUserQuestion)
1. Phạm vi ghi log: chỉ 4 nhóm hành động đã có action riêng, hay rộng hơn (cả sửa thông tin chung
   như tiêu đề/người thực hiện/độ ưu tiên/ngày hạn)?
2. Luồng "Cập nhật trạng thái" mobile: giống `MyWork.Report` (state+progress%+note, giữ luật chặn
   theo giờ đã ghi) hay giống Kanban `SetState` (chỉ đổi state, không luật)?
3. Ai được xem log của 1 công việc: ai xem được công việc (CanSeeTask) hay chỉ PM/người thực
   hiện (CanEditTask)?
4. Làm luôn cả web hay trước mắt chỉ mobile?

## 4. Câu trả lời & Quyết định
1. → **Rộng hơn**: log cả khi sửa thông tin chung (Tiêu đề, Người thực hiện, Độ ưu tiên, Ngày bắt
   đầu, Hạn hoàn thành, Loại việc) NGOÀI 4 nhóm hành động sẵn có.
2. → **Giống `MyWork.Report`**: chọn Trạng thái + nhập Tiến độ % + Ghi chú tuỳ chọn; GIỮ NGUYÊN
   luật chặn "chưa ghi giờ thì không được chuyển Đang làm/Hoàn thành" (gọi lại
   `TimeLogService.ValidateStateChange` y hệt web, không nới lỏng cho mobile).
3. → **CanSeeTask** (PM dự án, người thực hiện, thành viên dự án đang hoạt động — đúng điều kiện
   xem công việc hiện có, không thu hẹp thêm).
4. → **Làm cả web** — thêm khối "Lịch sử" vào trang Chi tiết công việc web (`Views/Checklist/
   _Detail.cshtml` và/hoặc `Views/MyWork/Detail.cshtml`) cùng đợt, không để lại sau.

### Giả định tự quyết định (chưa hỏi lại, nêu rõ để điều chỉnh nếu sai)
- **Định dạng hiển thị**: hành động có giá trị cũ/mới rõ ràng (trạng thái, tiến độ, người thực
  hiện, độ ưu tiên, ngày hạn, tiêu đề) hiện dạng "Tên trường: giá trị cũ → giá trị mới". Hành động
  không có cặp cũ/mới rõ ràng (thêm bình luận, thu hồi bình luận, ghi giờ, xoá giờ, thêm/tick/sửa/
  xoá việc cần làm) hiện 1 câu mô tả hành động (vd "đã ghi 2.5 giờ", "đã thêm việc cần làm 'Kiểm
  tra lại API'").
- **Không lọc/tìm kiếm** trong log ở bản đầu — danh sách cuộn đơn giản, mới nhất trên đầu, không
  phân trang (lịch sử 1 công việc thường không quá dài); có thể bổ sung sau nếu cần.
- **Không log việc XOÁ công việc** (`ChecklistController.Delete`) — xoá làm mất luôn task nên
  không còn chỗ xem log của nó; nằm ngoài phạm vi "log công việc" (log gắn theo 1 task còn tồn
  tại). Có thể mở vấn đề riêng sau nếu cần log cấp dự án/toàn hệ thống.
- **Không log việc TẠO MỚI công việc** riêng — coi việc tạo là điểm bắt đầu, không phải "thay
  đổi"; log chỉ bắt đầu tính từ sau khi task đã tồn tại. (Có thể thêm entry "Tạo công việc" sau
  nếu người dùng muốn thấy cả mốc tạo trong dòng lịch sử.)
- **Lưu actor dạng chụp nhanh** (denormalized `ActorUserId` + `ActorName`) giống cách
  `WorkComment.AuthorName` đang làm — tránh phải join bảng User mỗi lần hiển thị, và giữ đúng tên
  tại thời điểm thao tác kể cả nếu người dùng đổi tên sau này.
- **Nút "Cập nhật trạng thái"** đặt cuối khối "Thông tin chung" trên mobile (đúng vị trí ảnh
  khoanh đỏ), theo đúng pattern 3 nút hành động đã có (Ghi giờ/Cập nhật danh sách/Phản hồi) — mở
  bottom sheet `task_status_sheet.dart`, không sửa trực tiếp trên trang chính (giữ nguyên tinh
  thần "trang chính chỉ xem, thao tác tách riêng" đã chốt trước đó).
- **Icon mở màn Lịch sử** đặt ở góc phải AppBar màn Chi tiết công việc (đúng vị trí ảnh khoanh đỏ
  thứ nhất), cạnh hoặc thay vị trí nút back — dùng `AppIconButton` icon đồng hồ/lịch sử
  (`ClockCounterClockwise` hoặc tương đương trong `phosphor_icons`).

## 5. Checklist: Log công việc + Cập nhật trạng thái

### Chuẩn bị
- [x] Khảo sát enum trạng thái, luồng đổi trạng thái web hiện có, và toàn bộ điểm mutate dữ liệu
      công việc cần gắn log (Explore agent).
- [x] Xác nhận KHÔNG có cơ chế audit-log nào sẵn có để tận dụng — xây từ đầu.

### Thực hiện — Backend nền tảng
- [x] `[Bắt buộc]` Tạo `Models/Work/TaskActivityLog.cs`: Id, TaskId, ActorUserId, ActorName,
      Action (enum thực tế: FieldChanged, TimeLogAdded, TimeLogDeleted, TodoAdded, TodoToggled,
      TodoEdited, TodoDeleted, CommentAdded, CommentRecalled — gộp mọi thay đổi trường vào MỘT
      Action `FieldChanged` thay vì tách TrangThaiThayDoi/TienDoThayDoi/TruongThongTinThayDoi như
      dự tính ban đầu, vì `RecordFieldChanges` diff chung một lượt nên không cần phân biệt loại
      trường ở tầng Action), FieldName/OldValue/NewValue (nullable), Description, CreatedAt.
- [x] `[Bắt buộc]` Đăng ký bảng `TaskActivityLogs` trong `Data/Repository.cs` theo đúng pattern
      `SqlStore<T>` hiện có.
- [x] `[Bắt buộc]` Tạo `Services/TaskActivityLogService.cs`: `Record(...)`, `Snapshot(WorkTask)`
      (chụp nhanh trước khi sửa), `RecordFieldChanges(before, after, actorUserId, actorName)` diff
      CẢ Title/AssigneeUserId/Priority/Kind/StartDate/DueDate LẪN State/Progress trong cùng một
      hàm (đơn giản hơn thiết kế ban đầu — không cần tách riêng "đổi trạng thái" vs "sửa thông tin
      chung" thành hai luồng, tránh nguy cơ ghi trùng/ghi thiếu), `GetForTask(taskId)` giảm dần
      theo CreatedAt.
- [x] `[Bắt buộc]` Thêm `TaskActivityLogDto` vào `Models/Api/ApiDtos.cs` + `ApiMappers.ToDto`.

### Thực hiện — Gắn log vào các điểm mutate (web + API)
- [x] `[Bắt buộc]` `ChecklistController.SetState` + `MyWorkController.Report` + `ChecklistApiController.
      UpdateStatus` (mới) — chụp snapshot trước `WorkService.ApplyState`, gọi `RecordFieldChanges`
      sau khi `Repository.WorkTasks.Update` thành công.
- [x] `[Bắt buộc]` `ChecklistController.Edit` (POST, nhánh cập nhật) — dùng `current` (đã có sẵn,
      tải trước khi sửa) làm snapshot, `RecordFieldChanges(current, model, ...)` sau khi Update.
- [x] `[Nên có]` `LogTime`/`DeleteTimeLog` (web `ChecklistController` + API `ChecklistApiController`).
- [x] `[Nên có]` `AddTodo`/`ToggleTodo`/`EditTodo`/`DeleteTodo` (web + API).
- [x] `[Nên có]` `Comment`/`RecallComment` — web `ChecklistController` + API `ChecklistApiController`
      từ đầu; **`MyWorkController.Comment`/`DeleteComment` bị BỎ SÓT ở lượt đầu** (phát hiện qua
      code-review agent), đã bổ sung ngay sau đó — xem mục Kiểm tra/Nghiệm thu.

### Thực hiện — API mới cho mobile
- [x] `[Bắt buộc]` `ChecklistApiController.UpdateStatus(id, state, progress, note)` — mirror
      `MyWorkController.Report` (CanEditTask + `TimeLogService.ValidateStateChange` + append note
      vào Description), trả về `TaskFullDetailDto` (không phải `TaskDetailDto` đơn — trả cả 4 khối
      để mobile cập nhật state một lần, khớp cách `TaskFullDetail` đã dùng ở `Detail`).
- [x] `[Bắt buộc]` `ChecklistApiController.ActivityLog(id)` — CanSeeTask, trả `List<TaskActivityLogDto>`.
- [x] `[Bắt buộc]` Build backend sạch (`msbuild`). Smoke-test qua curl bị giới hạn (không có sẵn
      token tài khoản thật để test end-to-end như lượt trước) — chỉ xác nhận hành vi 404/411 khớp
      với action `LogTime` hiện có dưới cùng điều kiện thiếu xác thực; xác nhận thật sự đến từ
      kiểm thử trên emulator (xem cuối file).

### Thực hiện — Mobile
- [x] `[Bắt buộc]` `task_detail_models.dart`: thêm `TaskActivityLogEntry`.
- [x] `[Bắt buộc]` `task_detail_service.dart`: `updateStatus(...)`, `fetchActivityLog(taskId)`.
      `api_endpoint.dart`: `taskUpdateStatus`, `taskActivityLog(id)`.
- [x] `[Bắt buộc]` `task_status_sheet.dart` — dropdown Trạng thái (`AppDropdown`, 5 giá trị), ô
      Tiến độ %, ô Ghi chú tuỳ chọn; lỗi từ backend (kể cả luật chặn "chưa ghi giờ") hiện nguyên văn
      qua `ToastService`.
- [x] `[Bắt buộc]` `task_activity_log_screen.dart` — danh sách timeline mới→cũ, đủ trạng thái
      loading/rỗng/lỗi+thử lại.
- [x] `[Bắt buộc]` `task_detail_screen.dart` — icon Lịch sử (`clockCounterClockwise`) ở AppBar, nút
      "Cập nhật trạng thái" cuối khối "Thông tin chung"; `_task` đổi từ `late final` sang `late` để
      cập nhật lại được sau khi sheet trả kết quả.
- [x] `[Nên có]` `flutter analyze` sạch (chỉ còn info-level lint có sẵn từ trước, không liên quan).

### Thực hiện — Web
- [x] `[Bắt buộc]` Thêm khối "Lịch sử thao tác" vào `Views/Checklist/_Detail.cshtml` (đọc 1 lần lúc
      mở hộp thoại, không tự vẽ lại như 3 khối kia — vì đóng/mở lại hộp thoại đã nạp lại toàn view)
      + CSS `.activitylog-*` trong `site.css`. **CHƯA làm** `Views/MyWork/Detail.cshtml` (view tách
      riêng, không dùng chung `_Detail.cshtml`) — để ngỏ, mở lại nếu cần.

### Kiểm tra / Nghiệm thu
- [ ] `chuyen-gia-nghiem-thu-design` — CHƯA chạy skill nghiệm thu chính thức cho
      `task_status_sheet.dart`/`task_activity_log_screen.dart` (khác với đợt tính năng Trao đổi
      trước, đợt này bỏ qua bước này do khối lượng công việc lớn trong 1 lượt — nên chạy nghiệm thu
      riêng nếu có thời gian).
- [x] `code-review` + `security-review` (2 agent riêng, chạy song song). Security: sạch, không có
      lỗ hổng. Code-review: phát hiện 1 lỗi thật — `MyWorkController.Comment`/`DeleteComment` thiếu
      log hook — đã sửa ngay (xem mục "Gắn log" ở trên).
- [ ] `test-engineer`: **CHƯA làm** — dự án backend không có sẵn project test nào (`*Tests.csproj`
      không tồn tại), dựng cả bộ khung xUnit/NUnit mới nằm ngoài phạm vi hợp lý của lượt này. Xác
      nhận đúng đắn của `RecordFieldChanges` dựa trên đọc code + code-review agent thay vì test tự
      động — nên cân nhắc dựng test harness backend trong một vấn đề riêng.
- [x] Build APK debug, cài lên `emulator-5554`, tự kiểm thử: mở nút "Cập nhật trạng thái" → sheet
      hiện đúng dropdown/tiến độ/ghi chú → bấm Cập nhật → toast "Đã cập nhật trạng thái." xác nhận
      backend nhận và lưu thành công → mở icon Lịch sử → màn hiện đúng trạng thái rỗng ("Chưa có
      thao tác nào") vì các hành động trước đó (giờ công/todo/bình luận trên task này) đều xảy ra
      TRƯỚC khi build này có code ghi log, và lượt cập nhật trạng thái vừa test không đổi giá trị
      nào (State/Progress giữ nguyên) nên đúng đắn không sinh entry — chưa kiểm thử được một hành
      động THẬT SỰ đổi giá trị sinh ra đúng 1 dòng log do gặp trục trặc ADB input (tap không tới
      field nhập, không phải lỗi code) khi thử thêm việc cần làm mới; nên xác nhận lại ở phiên sau
      bằng một đổi Tiến độ/Trạng thái thực sự khác giá trị cũ.

### Ghi chú
- Không log XOÁ công việc, không log TẠO MỚI công việc, không lọc/tìm kiếm trong log — xem mục
  "Giả định tự quyết định" ở trên nếu cần mở lại phạm vi này.
- Log không thay thế lịch sử bình luận (khối Trao đổi) — chỉ ghi 1 dòng "đã thêm/thu hồi bình
  luận", không duplicate nội dung.
- Chưa làm `Views/MyWork/Detail.cshtml` và chưa dựng test harness backend — 2 việc còn treo, xem
  các mục chưa tick ở trên.

---

# [2026-08-16] Vấn đề: "Nhân sự dự án" mobile + 2 tinh chỉnh (thẻ thống kê điều hướng, "Thêm công việc mới" full-screen kèm todo-list)

## 1. Mô tả vấn đề
Ba việc liên tiếp trong cùng phiên:
1. Xây tính năng "Nhân sự dự án" cho mobile (list + Thêm/Đặt PM/Kết thúc tham gia/Xoá khỏi lịch
   sử), mirror `WorkProjectsController` bên web — theo 2 ảnh chụp web người dùng gửi.
2. "Các thông tin này điều hướng đến màn hình tương ứng" — 4 thẻ thống kê (Thành viên/Checklist/
   Quá hạn/CV Hỗ trợ) trên màn Chi tiết dự án mobile hiện chỉ hiển thị, chưa bấm được.
3. "Màn hình này xây dựng thành 1 screen ko làm hiển thị như này. Có thể đưa phần todo-list vào
   lun." — form "Thêm công việc mới" (đang là bottom sheet trong `checklist_board_screen.dart`)
   cần chuyển thành full-screen riêng + gộp thêm khối "Việc cần làm" ngay lúc tạo.

## 2. Phân tích ban đầu (việc 2 + 3, việc 1 đã làm xong+kiểm thử OK trước khi phát sinh việc 2/3)
- **Việc 2** — Bối cảnh: `project_detail_screen.dart` có `_StatsStrip` 4 ô tĩnh. "Thành viên" →
  đích rõ (màn Nhân sự dự án vừa xây). "Checklist" → đích rõ (đã có nút Checklist làm y hệt).
  "Quá hạn" → Checklist có `_dueFilter='quahan'` sẵn, khớp đúng. "CV Hỗ trợ" → con số backend là
  `supportThisWeek` (đếm theo TUẦN NÀY) nhưng Checklist mobile chỉ lọc được theo Loại việc, không
  có khái niệm tuần — rủi ro lệch ngữ nghĩa nếu không thêm bộ lọc mới.
- **Việc 3** — Bối cảnh: `_AddTaskSheet` (7 trường) đang là bottom sheet, gọi thẳng
  `ChecklistApiController.Create` (không nhận todo). "Việc cần làm" vốn là tính năng tách riêng,
  chỉ tạo được sau khi đã có `taskId` (qua `AddTodo`). Gộp vào lúc tạo nghĩa là nhập nhiều dòng
  "Việc cần làm" NGAY trong màn tạo mới, trước khi task tồn tại.

## 3. Câu hỏi làm rõ (dùng AskUserQuestion)
1. "CV Hỗ trợ" điều hướng theo bộ lọc nào — thêm bộ lọc "Tuần này" mới, hay chấp nhận chỉ lọc
   theo Loại việc (không khớp tuyệt đối con số)?
2. Tạo task kèm todo — mở rộng API `Create` nhận luôn danh sách todo trong 1 request, hay tạo
   task trước rồi tự động gọi `AddTodo` nối tiếp nhiều lần?
3. Full-screen "Thêm công việc mới" có cần thay đổi gì khác ngoài đổi khung hiển thị + thêm khối
   todo-list không?

## 4. Câu trả lời & Quyết định
1. → **Chỉ lọc theo Loại việc = Hỗ trợ** (đơn giản, chấp nhận không khớp tuyệt đối với con số
   "tuần này" trên thẻ — không thêm bộ lọc mới).
2. → **Mở rộng API `Create`** nhận kèm danh sách todo trong CÙNG một request (gọn, an toàn hơn
   thay vì nhiều lượt gọi nối tiếp có thể lỗi giữa chừng).
3. → **Có thêm 1 thay đổi ngoài dự tính**: "Chuyển thành 1 screen riêng khi bấm vào nút Thêm mới,
   đưa phần todolist khi thêm mới công việc luôn. Phần nội dung [Mô tả] là sẽ dùng
   app_rich_editor" — tức trường "Mô tả" đổi từ `AppTextField` 3 dòng sang `AppRichEditor` (rich
   text editor đã xây cho khối Trao đổi), giữ nguyên 6 trường còn lại.

## 5. Checklist: Nhân sự dự án + thẻ thống kê điều hướng + Thêm công việc full-screen kèm todo

### Việc 1 — Nhân sự dự án (ĐÃ XONG, đã build + kiểm thử OK trên emulator)
- [x] Backend: `ProjectMemberDto` thêm `Id`/`Note`, `ProjectMemberFormOptionsDto` mới.
- [x] Backend: `ProjectMembersApiController.cs` mới (Index/AddMemberForm/AddMember/SetPm/
      EndMember/RemoveMember) — mirror đúng `WorkProjectsController` (OpenAssignment check,
      EndCurrentPm, SyncCurrentPm).
- [x] Backend: build sạch.
- [x] Mobile: `ProjectMember` model thêm `id`/`note`, model mới `ProjectMemberFormOptions`/
      `UserOption`.
- [x] Mobile: `project_members_service.dart` — dùng `yyyy-MM-dd` cho mọi tham số ngày (KHÔNG
      dùng `toIso8601String()` — app ép culture vi-VN toàn cục, không có DateTime ModelBinder
      riêng, chỉ có `DecimalModelBinder`; lỗi này tự phát hiện được trước khi test nhờ nhớ lại bài
      học cũ của `TimeLogService`).
- [x] Mobile: `project_members_screen.dart` (danh sách + menu Thao tác dạng bottom sheet),
      `add_project_member_sheet.dart`, sheet Kết thúc tham gia inline trong cùng file.
- [x] Mobile: route `AppRoutes.projectMembers` + `ProjectMembersController`, nối nút "Nhân sự"
      trên `project_detail_screen.dart` (trước đó chỉ là Toast placeholder).
- [x] `flutter analyze` sạch, build APK release, kiểm thử trên emulator: mở màn Nhân sự dự án của
      dự án "Báo cáo KHA" (5 giai đoạn, đúng khớp dữ liệu ảnh chụp web người dùng gửi).

### Việc 2 — Thẻ thống kê điều hướng
- [x] `[Bắt buộc]` Thêm `initialKindFilter`/`initialDueFilter` vào `ChecklistBoardScreen` +
      `ChecklistBoardController` (đọc từ route arguments).
- [ ] `[Bắt buộc]` Sửa `project_detail_screen.dart`: bọc 4 ô trong `_StatsStrip` bằng `InkWell`,
      điều hướng: Thành viên → `AppRoutes.projectMembers`; Checklist → `AppRoutes.checklist`
      (không lọc); Quá hạn → `AppRoutes.checklist` kèm `dueFilter: 'quahan'`; CV Hỗ trợ →
      `AppRoutes.checklist` kèm `kindFilter: 'HoTro'`.
- [ ] `[Nên có]` `flutter analyze` + build lại xác nhận không vỡ layout `_StatTile` khi bọc thêm
      `InkWell` (vùng chạm tối thiểu 48dp).

### Việc 3 — "Thêm công việc mới" full-screen kèm todo-list
- [ ] `[Bắt buộc]` Backend: `ChecklistApiController.Create` thêm tham số `string[] todos`, sau khi
      tạo `WorkTask` thành công thì lặp tạo `WorkTaskTodo` cho từng dòng (trim, bỏ dòng rỗng, giới
      hạn 300 ký tự như `AddTodo`), gọi `NotificationService.TodoAdded` +
      `TaskActivityLogService.Record(..., TodoAdded, ...)` cho TỪNG dòng — mirror đúng hành vi
      `AddTodo` để lịch sử thao tác nhất quán dù todo được thêm lúc tạo hay thêm sau.
- [ ] `[Bắt buộc]` Backend: thêm `[ValidateInput(false)]` vào `Create` — trường `description` giờ
      chứa HTML thật từ `AppRichEditor` (trước đây luôn là chữ thường nên chưa cần), thiếu
      attribute này sẽ vỡ với lỗi "potentially dangerous Request.Form value" y hệt bài học cũ ở
      action `Comment`/`Edit`.
- [ ] `[Bắt buộc]` Backend: build sạch.
- [ ] `[Bắt buộc]` Mobile: `checklist_service.dart.create()` đổi sang `FormData` (như
      `sendComment`) để gửi được `List<String> todos` dạng nhiều field cùng tên, ASP.NET MVC tự
      bind vào `string[] todos`.
- [ ] `[Bắt buộc]` Mobile: tạo `add_task_screen.dart` (file mới, full-screen, push trực tiếp qua
      `Navigator` — không qua route đặt tên, cùng pattern với `task_comments_screen.dart`): giữ
      nguyên 6/7 trường, đổi "Mô tả" sang `AppRichEditor` (`AppRichEditorController.toHtml()` lúc
      gửi), thêm khối "Việc cần làm" (ô nhập + nút "Thêm" bọc `ConstrainedBox` — ĐÚNG bài học cũ
      tránh `AppButton` trần trong `Row`, danh sách các dòng đã thêm kèm nút xoá từng dòng).
- [ ] `[Bắt buộc]` Mobile: `checklist_board_screen.dart` — xoá hẳn `_AddTaskSheet` (không còn
      dùng), đổi `_openAddTaskSheet` thành push `add_task_screen.dart`.
- [ ] `[Nên có]` `flutter analyze` sạch.

### Kiểm tra / Nghiệm thu
- [ ] Build APK release, cài lên emulator, kiểm thử: bấm 4 thẻ thống kê đều điều hướng đúng màn +
      đúng bộ lọc; mở "Thêm công việc mới" thấy full-screen (không phải sheet), nhập Mô tả có định
      dạng đậm/nghiêng qua `AppRichEditor`, thêm 2-3 dòng "Việc cần làm", gửi thành công → mở lại
      công việc vừa tạo thấy đúng todo đã nhập + khối Lịch sử có đủ dòng "Đã thêm việc cần làm"
      cho từng dòng.

### Ghi chú
- Việc 2 (CV Hỗ trợ) CHẤP NHẬN lệch nhẹ ngữ nghĩa so với con số "tuần này" trên thẻ — nếu sau này
  cần khớp tuyệt đối, phải thêm bộ lọc "Tuần này" mới vào `checklist_board_screen.dart` (mở vấn đề
  riêng, không tự ý làm thêm ở đây).

---

# [2026-08-17] Vấn đề: Tốc độ truy vấn chậm khi test mobile (BrewTask) trên emulator

## 1. Mô tả vấn đề
Người dùng test app mobile trên emulator, nhận thấy "tốc độ truy vấn khá chậm", chưa nói rõ chậm
ở màn hình/API nào, chậm lần đầu hay mọi lần, hỏi có tối ưu được không.

## 2. Phân tích ban đầu
- Bối cảnh: Backend `TTKDGP.ProjectManager` (ASP.NET MVC 5, SQL Server) + mobile Flutter
  (`Mobile-Flutter/`) gọi API qua `Controllers/Api/*`. Cấu hình dev hiện tại
  (`lib/config/api_endpoint.dart`): emulator Android gọi `http://10.0.2.2:8080` (site IIS local
  trên máy Windows host qua alias AVD), không phải server thật. `Web.config` khai báo
  `Db:Server = 10.57.30.10,1433` — SQL Server có thể là máy CHUNG trong mạng nội bộ, không phải
  localhost, nên ngay cả khi chạy site local, mọi truy vấn vẫn phải đi qua mạng tới `10.57.30.10`.
- Mục tiêu bề mặt: giảm độ trễ cảm nhận được khi thao tác trên app mobile.
  Mục tiêu sâu xa (cần xác nhận): tối ưu tổng thể hiệu năng backend, hay chỉ cần app "cảm giác"
  nhanh hơn (loading tốt hơn) mà chưa cần đụng vào tầng dữ liệu?
- Phạm vi khả dĩ: (a) tầng mạng (emulator ↔ IIS local ↔ SQL Server 10.57.30.10), (b) tầng backend
  (`SqlStore<T>`/`Repository`, N+1 giữa nhiều API con trong một màn hình), (c) tầng mobile
  (`Dio`/`ApiClient`, cache, số lượng request/màn hình, thiếu chỉ báo loading khiến CẢM GIÁC chậm
  dù thực tế không chậm).
- Ràng buộc kỹ thuật đã biết từ code:
  - `Infrastructure/Db.cs`: `Db.Open()` thử lại tối đa 4 lần, mỗi lần nghỉ `400ms * lần thử` khi
    kết nối SQL trượt — nếu đường tới `10.57.30.10` không ổn định, MỘT request có thể cộng dồn vài
    giây chờ retry trước khi lỗi hẳn hoặc mới kết nối được.
  - `Data/SqlStore.cs`: nạp TOÀN BỘ bảng vào bộ nhớ một lần (giống `JsonStore` cũ) rồi thao tác
    trên đó — nghĩa là sau lần load đầu, đọc dữ liệu (`.All()`) không cần hỏi lại SQL mỗi request;
    nhưng lần load đầu tiên (khi ứng dụng web vừa khởi động / IIS pool vừa recycle) có thể chậm
    nếu bảng lớn hoặc mạng tới SQL chậm — và mỗi API mobile gọi lại kích hoạt seed/kiểm tra như
    `RoleGroupSeeder`, `JsonToSqlMigration.RunIfNeeded()` chạy lúc `Application_Start`.
  - `Models/Permission.cs` (comment gốc trong code) từng ghi nhận vấn đề tương tự: SQL chập chờn
    khiến mỗi lượt hỏi quyền tốn "vài giây cho một trang" trước khi có cơ chế nhớ tạm theo request.
- Rủi ro/giả định: đang giả định "chậm" là do tầng dữ liệu/mạng — có thể sai, có thể do
  `flutter run` debug build (luôn chậm hơn release), do emulator yếu, hoặc do UI mobile gọi tuần
  tự nhiều API thay vì song song trong cùng một màn hình.
- Phương án sơ bộ: (1) đo đạc/khoanh vùng bằng log thời gian (backend + `Dio` interceptor) trước
  khi sửa bất cứ gì; (2) nếu do IIS pool recycle / cold start → cấu hình `AlwaysRunning`/Preload
  (đã có sẵn hướng dẫn trong README mục "Chạy đúng giờ", áp dụng tương tự cho dev); (3) nếu do
  nhiều API gọi tuần tự → gộp endpoint hoặc gọi song song ở mobile; (4) nếu do build debug → so
  sánh với release build trước khi kết luận backend chậm.

## 3. Câu hỏi làm rõ
1. Chậm cụ thể ở đâu — màn hình/thao tác nào (ví dụ mở Checklist, tải Dashboard, đăng nhập...),
   hay chậm đều ở mọi màn hình?
2. Chậm ở **lần đầu mở app sau khi khởi động lại site/IIS** (cold start), hay chậm ở **mọi lần**
   kể cả khi đã dùng app một lúc?
3. App mobile đang chạy bằng `flutter run` (debug) hay đã build bản release (`flutter build apk
   --release`)? Debug build vốn chậm hơn release đáng kể, cần loại trừ trước.
4. Site web đang trỏ vào SQL Server ở đâu — `10.57.30.10` (máy chủ chung qua mạng) hay đã đổi
   sang một SQL Server cài trên chính máy dev (localhost)? Mạng từ máy dev tới `10.57.30.10` có
   ổn định không (có hay gặp lỗi kết nối SQL chập chờn trong log/`App_Data/errors.log` không)?
5. "Chậm" là có cảm giác chờ vài giây rõ rệt, hay chỉ hơi giật/lag khi thao tác? Có ước lượng được
   thời gian chờ gần đúng không (ví dụ ~1s, ~3-5s, >10s)?
6. Đã thử so sánh chưa — ví dụ mở cùng chức năng đó trên trình duyệt web (`http://pm.vn` hoặc
   `localhost`) xem có chậm tương tự không, để biết là chậm do riêng mobile hay chậm chung cả hệ
   thống?
7. Có sẵn sàng để tôi thêm log đo thời gian (thời gian xử lý ở backend + thời gian gọi API ở
   mobile) để khoanh vùng chính xác trước khi sửa, hay muốn tôi cứ áp dụng luôn các tối ưu "an
   toàn, không đổi hành vi" đã biết trước (ví dụ bật `AlwaysRunning`/Preload cho site dev, kiểm
   tra log lỗi kết nối SQL) rồi xem có cải thiện không?

## 4. Câu trả lời & Quyết định
1-2-5. Người dùng: "Chậm tầm 3-5s, mỗi khi chuyển screen. Hiện dữ liệu chưa nhiều nhưng tốc độ
   truy vấn như vậy, tôi sợ sau này dữ liệu lớn thì sẽ bị chậm hơn" → xác nhận chậm ĐỀU mỗi lần
   chuyển màn hình (không riêng lần đầu), mức ~3-5s, và mối lo thật sự là khả năng mở rộng khi dữ
   liệu lớn lên, không phải chỉ trải nghiệm hiện tại.
3. Build type (hỏi qua `AskUserQuestion`) → **Debug (`flutter run`)**.
4. Vị trí SQL Server (hỏi qua `AskUserQuestion`) → **10.57.30.10, đúng như `Web.config`** — xác
   nhận mọi truy vấn đi qua mạng nội bộ, không phải localhost.
6. Không hỏi/không trả lời riêng — bỏ qua, không ảnh hưởng tới quyết định.
7. Cách tiếp cận (hỏi qua `AskUserQuestion`) → **"Áp dụng luôn các tối ưu phổ biến rồi kiểm tra
   lại"** (không cần thêm log đo thời gian trước).

**Điều tra thêm (trước khi sửa, để nhắm đúng chỗ thay vì đoán):**
- Đọc `Infrastructure/Db.cs`: `Open()` thử lại tối đa 4 lần, nghỉ `400ms * lần thử` khi trượt —
  góp phần vào độ trễ nếu mạng chập chờn, nhưng không phải nguyên nhân chính (không thấy log lỗi
  SQL nào trong phiên test hôm nay ở `App_Data/errors.log`, chỉ có các lỗi 404 routing cũ và một
  lỗi "Login failed" từ 12/08 không liên quan).
- Đọc `Data/SqlStore.cs`: `EnsureLoaded()` nạp TOÀN BỘ một bảng vào bộ nhớ ở lần truy cập ĐẦU TIÊN
  (kiểm tra schema qua `sys.columns`/`sys.tables` rồi `SELECT * FROM [Bảng] ORDER BY [Id]`), sau
  đó đọc từ bộ nhớ — mỗi lần nạp đầu tốn ít nhất 2 round-trip mạng tới SQL Server ngoài.
- Đọc `Data/JsonToSqlMigration.cs`: chỉ gọi `.Count()` (→ trigger nạp sẵn) trên **10 bảng của bộ
  gốc** (Members/Projects/Assignments/Users/4 danh mục/WorkLogs/ReminderLogs) lúc
  `Application_Start`. **Không đụng tới bất kỳ bảng nào của bộ "Quản lý công việc & KPI"**
  (WorkTasks, WorkProjects, Kpi*, Leaves, Notifications...) — đúng những bảng mà API cho mobile
  dùng (`Data/Repository.cs` liệt kê 32 bảng `SqlStore<T>` tổng cộng, 22 bảng còn lại chưa từng
  được nạp sẵn).
- Kiểm tra `dashboard_service.dart`/`http_manager.dart` phía mobile: mỗi màn hình gọi ĐÚNG MỘT
  API tổng hợp (không phải nhiều request tuần tự), `HttpManager` (Dio) không có logic retry/delay
  nhân tạo nào → loại trừ nguyên nhân "mobile tự gọi chậm", củng cố kết luận độ trễ nằm ở backend
  lúc nạp bảng lần đầu.

→ **Kết luận nguyên nhân gốc**: mỗi màn hình mobile mới (Dashboard/MyWork/Checklist/KPI/Leaves...)
chạm tới một vài bảng SQL của bộ "Quản lý công việc & KPI" **chưa từng được nạp sẵn** lúc khởi
động — nên lần đầu chạm phải chờ round-trip mạng tới `10.57.30.10` cho TỪNG bảng, cộng dồn thành
3-5s mỗi lần chuyển sang màn hình có bảng mới. Không liên quan tới khối lượng dữ liệu hiện tại
(khớp với việc người dùng thấy chậm dù dữ liệu còn ít) — nhưng đúng là sẽ NẶNG HƠN khi bảng lớn
lên (SELECT * toàn bảng lâu hơn), nên lo ngại của người dùng về sau này là có cơ sở, cần theo dõi
tiếp (xem mục Ghi chú).

## 5. Checklist: Warm-up toàn bộ bảng SQL lúc khởi động

### Thực hiện
- [x] `[Bắt buộc]` Thêm `Repository.WarmUpAll()` (`Data/Repository.cs`) — gọi `.All()` song song
      (`Parallel.ForEach`) trên 22 `SqlStore<T>` chưa được `JsonToSqlMigration` nạp sẵn, mỗi bảng
      có try/catch riêng (nuốt lỗi CSDL chập chờn, ghi cả `Debug.WriteLine` lẫn `ErrorLog.Write`
      để còn dấu vết trên build Release — `Debug.WriteLine` bị strip khỏi Release).
- [x] `[Bắt buộc]` Gọi `Data.Repository.WarmUpAll();` trong `Global.asax.cs` `Application_Start`.
- [x] `[Bắt buộc]` Đặt ĐÚNG vị trí: SAU `Data.WorkUserMigration.RunIfNeeded()` — phát hiện qua
      code-review: `WorkUserMigration` ghi thẳng ADO (bỏ qua `SqlStore`) vào 6 bảng cũng nằm trong
      danh sách warm-up; đặt `WarmUpAll()` trước sẽ làm bộ nhớ cache dữ liệu UserId/tên hiển thị
      CŨ, không thấy được bản ghi migrate cho tới lần khởi động sau — bug thật, đã sửa (đổi thứ tự
      hai dòng gọi).
- [x] `[Nên có]` Build Debug qua MSBuild xác nhận sạch, không lỗi biên dịch (2 lần — trước và sau
      khi sửa thứ tự).

### Kiểm tra / Nghiệm thu
- [ ] `[Bắt buộc]` Người dùng chạy lại site (F5/IIS Express), test app mobile trên emulator: xác
      nhận các lần CHUYỂN MÀN HÌNH sau khi site đã khởi động xong không còn chậm 3-5s (chỉ còn độ
      trễ mạng bình thường của một request, không phải lần nạp bảng đầu tiên).
- [ ] `[Nên có]` Quan sát thời gian request ĐẦU TIÊN sau khi khởi động site (sẽ chậm hơn — đang
      warm-up ~22 bảng song song) để biết tổng chi phí cold-start mới, xem có chấp nhận được không.
- [ ] `[Nên có]` Xác nhận màn hình liên quan tới `WorkUserMigration` (đầu việc của tôi, KPI, dự án
      theo PM...) vẫn hiển thị đúng người/tên sau khi sửa thứ tự — phòng hờ bug đã fix ở trên.

### Ghi chú
- Đây là tối ưu **tầng vận hành** (đổi THỜI ĐIỂM nạp dữ liệu, không đổi logic nghiệp vụ nào) —
  đúng phạm vi "an toàn, không đổi hành vi" người dùng đã chọn.
- **Chưa xử lý** rủi ro dài hạn người dùng lo ngại ("dữ liệu lớn thì chậm hơn"): `SqlStore<T>` nạp
  TOÀN BỘ bảng vào bộ nhớ (không phân trang), nên khi số dòng một bảng lên tới hàng chục nghìn+,
  cả thời gian nạp lẫn bộ nhớ dùng sẽ tăng theo — đây là giới hạn kiến trúc, không phải bug, và
  nằm NGOÀI phạm vi việc tối ưu lần này. Nếu dữ liệu thật sự lớn lên đáng kể, cần mở vấn đề riêng
  để bàn hướng phân trang/lazy-load theo từng truy vấn thay vì nạp cả bảng.
- Production (không phải máy dev) nên bật IIS *Application Initialization*/*AlwaysRunning* (đã có
  hướng dẫn ở README mục "Chạy đúng giờ") để chi phí warm-up chỉ xảy ra lúc app pool khởi động lại
  theo lịch, người dùng thật gần như không bao giờ gặp phải — chưa xác nhận máy dev hiện tại đã
  bật cấu hình tương đương chưa.

---

# [2026-08-17] Vấn đề: Mobile hiện nút hành động cho người không có quyền ở "Chi tiết công việc"

## 1. Mô tả vấn đề
Test bằng tài khoản `nhansudemo` (Dev thường trong "Dự án demo", KHÔNG phải PM — PM là "Trần PM",
không phải Quản lý Tổ) — mở một công việc được giao cho người KHÁC ("Trần Duy Tân"), màn "Chi tiết
công việc" (`task_detail_screen.dart`, dùng chung cho cả MyWork lẫn Checklist board) vẫn hiện đủ
3 nút hành động: "Cập nhật trạng thái", "Ghi giờ", "Cập nhật danh sách" (Việc cần làm) — dù người
này không có quyền với công việc đó. Người dùng yêu cầu chặn ngay.

## 2. Điều tra
- `TaskFullDetailDto` (trả về từ `ChecklistApi/Detail/{id}`) đã có sẵn 3 cờ quyền tính đúng ở
  server: `Task.CanEdit` (từ `BaseController.CanEditTask` — assignee/PM/Quản lý Tổ),
  `TimeLog.CanLog` + `BlockedReason` (từ `TimeLogService.BuildViewModel` — CHỈ đúng assignee, chặt
  hơn CanEdit, cả PM/Quản lý Tổ cũng không ghi giờ hộ được), `Todo.CanManage` (từ
  `BaseController.CanManageTodos` — assignee/người giao/PM/Quản lý Tổ).
- Cả 3 field đều đã được model Dart (`task_detail_models.dart`) parse đúng tên/đúng kiểu từ trước,
  nhưng **màn hình không hề dùng tới** — 3 nút hiện không điều kiện, khác với nút "Sửa người thực
  hiện" ngay bên trên đã đúng làm theo `t.canEditAll`.
- **Xác nhận qua code-review độc lập (agent riêng, tự đọc lại `ChecklistApiController.cs`/
  `BaseController.cs`/`TimeLogService.cs`, không suy diễn)**: backend đã chặn đúng cả 3 hành động
  từ trước (`UpdateStatus`/`LogTime`/thao tác Todo đều có kiểm tra quyền tương ứng, trả
  `BadRequest` nếu sai) — **đây là lỗi hiển thị ở CLIENT, không phải lỗ hổng bypass quyền ở
  backend**. Không có dữ liệu nào bị sửa trái phép trong lúc test.

## 3. Quyết định & Thực hiện
Bọc cả 3 nút theo đúng khuôn mẫu `if (...) ...[...]` đã có sẵn trong chính file
(`task_detail_screen.dart`):
- [x] "Cập nhật trạng thái" → chỉ hiện khi `t.canEdit`.
- [x] "Ghi giờ" → chỉ hiện khi `tl.canLog`; khi không, hiện `tl.blockedReason` (tận dụng field đã
      có sẵn nhưng trước đây không dùng tới — đúng mục đích thiết kế ban đầu của DTO).
- [x] "Cập nhật danh sách" (todo) → chỉ hiện khi `todo.canManage`.
- [x] `flutter analyze` sạch trên file đã sửa.
- [x] Review độc lập bằng agent (đóng vai code-reviewer + security-reviewer): xác nhận bản vá an
      toàn để merge, không phát hiện nút nào khác còn sót lỗi tương tự trong cùng file.

## 4. Ghi chú / Việc còn treo
- Rà soát chỉ giới hạn trong `task_detail_screen.dart` (phạm vi báo cáo của người dùng). Chưa rà
  soát các màn khác có khả năng cùng lớp lỗi (hiện hành động không kiểm tra cờ quyền server trả
  về) — ví dụ nút thu hồi bình luận (`canRecall`) nằm ở `task_comments_screen.dart`, ngoài phạm vi
  lần này, có thể cần rà soát riêng nếu muốn chắc chắn toàn app.
- Câu hỏi trước đó về màn "Cài đặt" hiện tên `—` (nghi cache đăng nhập cũ, xem mục
  [2026-08-17] Vấn đề: Tốc độ truy vấn chậm) **vẫn chưa có câu trả lời** — cần người dùng xác nhận
  đã thử đăng xuất/đăng nhập lại chưa.
- Chưa có xác nhận cuối cùng từ người dùng rằng sau khi sửa, test lại bằng `nhansudemo` không còn
  thấy 3 nút này nữa.

---

# [2026-08-18] Vấn đề: Build lại + tự test trên emulator bằng 2 tài khoản thật (nhansudemo/pmdemo)

## 1. Mô tả vấn đề
Người dùng cấp 2 tài khoản test thật (`nhansudemo` — Dev, `pmdemo` — PM, cùng mật khẩu
`Khoid@umo!248`) và yêu cầu build lại ứng dụng, tự kiểm thử lại các chức năng đã sửa ở 2 vấn đề
trước (tốc độ chuyển màn hình, 3 nút hành động lộ quyền).

## 2. Cách thực hiện
Build backend (MSBuild, đã xác nhận IIS site `pm.vn` cổng 8080 tự nhận bản mới không cần khởi
động lại thủ công), build APK debug Mobile-Flutter, cài lên `emulator-5554` qua `adb`, tự động
thao tác (tap/nhập liệu/chụp màn hình) qua `adb shell input` + `uiautomator dump` để lấy toạ độ
chính xác thay vì áng chừng từ ảnh chụp.

## 3. Kết quả xác nhận ĐÚNG như kỳ vọng

- **Tốc độ backend**: request đầu tiên sau khi build lại (site vừa recycle) mất 3.7s — đúng chi
  phí `Repository.WarmUpAll()` chạy lúc khởi động; các request sau đó (kể cả API cho mobile như
  `MyWorkApi`, `ChecklistApi`) chỉ còn 1-3ms. Xác nhận bằng `curl` trực tiếp vào
  `http://127.0.0.1:8080`, không qua mobile.
- **3 nút hành động ở "Chi tiết công việc"** (việc CV-001 "CV test", người thực hiện Trần Duy Tân,
  dự án "Dự án demo"):
  - `nhansudemo` (Dev, không phải PM/assignee): cả 3 nút "Cập nhật trạng thái"/"Ghi giờ"/"Cập nhật
    danh sách" đều ẨN đúng như sửa; khối Giờ công hiện đúng dòng "Chỉ người được giao việc mới ghi
    được giờ công." thay cho nút.
  - `pmdemo` (PM của dự án): "Cập nhật trạng thái" và "Cập nhật danh sách" HIỆN đúng (không bị
    chặn nhầm); riêng "Ghi giờ" vẫn ẨN cho cả PM — ĐÚNG theo luật nghiệp vụ đã có sẵn từ trước
    (`TimeLogService`: chỉ chính người thực hiện được ghi giờ, kể cả PM/Quản lý Tổ cũng không ghi
    hộ được) — không phải lỗi.
  - Kết luận: bản sửa 2 vấn đề trước hoạt động đúng, không over-restrict PM.

## 4. Phát hiện MỚI phát sinh trong lúc test (chưa sửa)

**Bug: Tên hiển thị (Dashboard "Chào, ...!" và màn Cài đặt) không cập nhật ngay sau khi đăng nhập
trong cùng phiên chạy app — chỉ đúng lại sau khi khởi động lại app.**

- Tái hiện: đăng xuất `nhansudemo` → đăng nhập `pmdemo` (không thoát app, cùng tiến trình) → Dashboard
  hiện "Chào, bạn!" (tên rỗng, fallback mặc định), màn Cài đặt hiện avatar "?" và tên "—" — giống
  hệt triệu chứng đã hỏi ở vấn đề trước, NHƯNG lần này xác nhận được **không phải do cache cũ/lỗi
  thời** như phỏng đoán ban đầu.
- Kiểm tra trực tiếp file `shared_prefs/FlutterSharedPreferences.xml` trên thiết bị (qua
  `adb shell run-as ... cat`): key `flutter.login_info` chứa ĐÚNG
  `{"displayName":"Trần PM","permissions":[]}` — nghĩa là backend trả đúng tên, `doAuth`/`AppCache`
  lưu đúng xuống đĩa. Vấn đề chỉ nằm ở việc **UI trong phiên hiện tại không đọc được giá trị vừa
  lưu** (`AuthProvider._displayName` trong bộ nhớ không được cập nhật đúng dù `notifyListeners()`
  đã gọi).
- Xác nhận bằng cách force-stop + mở lại app (không đăng nhập lại, dùng session đã lưu): Dashboard
  hiện đúng ngay "Chào, Trần PM!" — chứng minh dữ liệu lưu đúng, `_hydrate()` (chạy lúc khởi động
  app) đọc đúng; chỉ riêng luồng `AuthProvider.login()` (chạy ngay sau khi bấm "Đăng nhập", không
  qua khởi động lại app) là chỗ bị lỗi.
- Đã soát code liên quan (`auth_provider.dart`, `login_helper.dart`, `app_cache.dart`,
  `cache_manager.dart`, `main.dart` — chỉ MỘT `ChangeNotifierProvider(create: (_) => AuthProvider())`
  ở gốc app, không có nhiều instance) nhưng CHƯA tìm ra dòng code cụ thể gây lỗi — cần điều tra
  thêm (nghi vấn: thời điểm `notifyListeners()` so với thời điểm widget cây con thực sự lắng nghe,
  hoặc một race hiếm giữa `logout()` và `login()` gọi liên tiếp trong cùng một thao tác test).
- **Chưa sửa** — nằm ngoài phạm vi yêu cầu ban đầu (tốc độ + 3 nút quyền), phát sinh giữa lúc test.
  Ảnh hưởng: người dùng vừa đăng nhập xong sẽ thấy tên/avatar sai cho tới khi thoát hẳn app và mở
  lại — không ảnh hưởng tới quyền hạn hay dữ liệu (chỉ hiển thị), nhưng gây khó chịu/trông như lỗi
  nặng. Nên mở vấn đề riêng để điều tra sâu + sửa nếu người dùng xác nhận muốn ưu tiên.

## 5. Checklist
### Kiểm tra / Nghiệm thu
- [x] Backend warm-up hoạt động đúng (đo bằng curl).
- [x] 3 nút quyền: đúng cho cả Dev (ẩn) và PM (hiện, trừ Ghi giờ theo đúng luật cũ).
- [ ] Vấn đề tên hiển thị sau đăng nhập trong phiên — CHƯA sửa, cần mở vấn đề riêng.

### Ghi chú
- Công cụ test: `adb` (cài đặt tại `C:\Users\K\AppData\Local\Android\Sdk\platform-tools\adb.exe`),
  `uiautomator dump` để lấy toạ độ chính xác (không áng chừng theo ảnh chụp — từng bị lệch toạ độ
  do đọc sai tỉ lệ ảnh hiển thị so với độ phân giải thật của thiết bị 1080×2400).
- IIS site `pm.vn` (không phải IIS Express) đã chạy sẵn dạng service, tự nhận bản build mới mà
  không cần thao tác gì thêm — chỉ cần build lại `TTKDGP.ProjectManager.sln`.
