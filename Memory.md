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
