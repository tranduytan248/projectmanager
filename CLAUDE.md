# Dự án: Hệ thống quản lý dự án & nhân sự — Tổ NCPT (web) + BrewTask (mobile)

Web ASP.NET MVC 5 (.NET Framework 4.8) + SQL Server quản lý dự án, nhân sự, giao việc, chấm KPI,
nghỉ phép của Tổ NCPT, kèm ứng dụng mobile Flutter (**BrewTask**, thư mục `Mobile-Flutter/`) gọi
thẳng API của web. Chi tiết chức năng xem `README.md` ở gốc dự án.

`.claude/` (rules, agents, skills) dùng **chung cho toàn repo** — áp dụng như nhau cho cả phần
web (C#/SQL Server) lẫn phần mobile (Flutter), không phân biệt đang làm ở thư mục nào.

## Ngôn ngữ giao tiếp

- Luôn trả lời, viết tài liệu, comment, và mọi chuỗi hiển thị bằng **tiếng Việt có dấu**.

## Quy tắc bắt buộc — nạp theo loại file đang sửa (không có ngoại lệ)

Chi tiết đầy đủ nằm trong `.claude/rules/`, phải tuân thủ như quy tắc viết trong chính file này:

| Đang sửa | Nạp thêm |
|---|---|
| Mọi file | `.claude/rules/CODING_RULES.md` — nguyên tắc chung: giữ nguyên kiến trúc, không đổi business logic ngoài phạm vi yêu cầu, không rename namespace/class/project bừa, không đổi framework/phiên bản .NET, không SELECT * tùy tiện, UTF-8 không lỗi font tiếng Việt |
| `.cs` / `.cshtml` | `.claude/rules/C_SHARP_RULES.md` — naming convention, luồng Controller → Service → Repository, không viết SQL/business trong Controller, `catch (Exception ex)` phải log rồi `throw`, method Async có hậu tố `Async` |
| `.sql` / stored procedure / câu lệnh truy vấn | `.claude/rules/SQL_SERVER_RULES.md` — không `SELECT *`, luôn tham số hóa (không nối chuỗi), `DELETE`/`UPDATE` luôn có `WHERE`, transaction cho nhiều update, `NVARCHAR`/`N'...'` cho dữ liệu tiếng Việt |
| `.dart` | `.claude/rules/FLUTTER_RULES.md` — tiếng Việt có dấu đầy đủ, kiến trúc custom widget `App*` (màn hình không dùng trực tiếp widget gốc Flutter/thư viện UI ngoài) |

## Đội hình agent & skill của dự án (trong `.claude/`)

| Tên | Loại | Khi dùng |
|---|---|---|
| `phan-tich-van-de` | Skill | Có vấn đề/yêu cầu/tính năng mới → phân tích → hỏi làm rõ → nhận trả lời → xây checklist → ghi `Memory.md` |
| `designer-mobile-pro` | Agent | Thiết kế/dựng giao diện mobile Flutter — đề xuất phương án trước, code sau, theo kiến trúc `App*` |
| `chuyen-gia-nghiem-thu-design` | Skill | Nghiệm thu giao diện mobile vừa dựng/sửa — chấm ĐẠT/KHÔNG ĐẠT, không dễ dãi |
| `code-reviewer` | Agent | Review code (C#, Dart, SQL...) ngay sau khi viết hoặc sửa |
| `security-auditor` | Agent | Kiểm tra bảo mật cho chức năng nhạy cảm trước khi bàn giao |
| `test-engineer` | Agent | Viết/chạy/sửa test (xUnit/NUnit cho web, `flutter_test` cho mobile) đến khi xanh |
| `git-push-merge` | Skill | Khi được lệnh đẩy/push code — commit → push nhánh hiện tại → merge `main` → merge `upload-source` → quay lại nhánh ban đầu |

## Quy trình làm việc bắt buộc

### 1. Khi nhận vấn đề / yêu cầu / tính năng mới
- Áp dụng skill **phan-tich-van-de**: phân tích bối cảnh/mục tiêu/phạm vi → đặt câu hỏi làm rõ →
  nhận trả lời → xây checklist → ghi toàn bộ vào `Memory.md` ở gốc dự án.
- KHÔNG code ngay khi chưa qua bước phân tích và làm rõ, trừ việc quá nhỏ (sửa một dòng, tra cứu
  đơn giản).

### 2. Khi code (web hoặc mobile)
- Trước khi viết, đọc lại các mục liên quan trong `Memory.md` để nắm quyết định cũ — tránh làm
  lại hoặc đi ngược quyết định đã chốt ở phiên trước.
- Đọc quy tắc coding tương ứng loại file (bảng ở trên) trước khi viết; viết giống coding style
  hiện có trong file/module đang sửa.
- Code xong một phần việc → gọi agent **code-reviewer** kiểm tra; sửa hết lỗi 🔴 (Nghiêm trọng)
  trước khi tiếp tục, cân nhắc 🟡/🟢 theo phạm vi việc đang làm.

### 3. Khi làm giao diện mobile (Flutter)
- Thiết kế/dựng màn hình → dùng agent **designer-mobile-pro** (đề xuất phương án trước, code sau;
  thiết kế đủ 5 trạng thái: loading / có dữ liệu / rỗng / lỗi / mất mạng).
- Màn hình hoàn thành hoặc sửa xong → nghiệm thu bằng skill **chuyen-gia-nghiem-thu-design**.
  Kết luận KHÔNG ĐẠT thì phải sửa và nghiệm thu lại — không cho qua, không hạ chuẩn ở vòng sau.

### 4. Khi hoàn thành một tính năng
- Gọi agent **test-engineer** viết và chạy test cho logic nghiệp vụ quan trọng; test phải xanh
  mới coi là xong. (Lưu ý: phần web hiện **chưa có** project test nào sẵn — dựng cả bộ khung
  test mới nằm ngoài phạm vi hợp lý của một tính năng đơn lẻ, cần thống nhất riêng với người
  dùng trước khi làm.)
- Tính năng nhạy cảm (đăng nhập, phân quyền, upload file, API công khai/API cho đối tác ngoài,
  dữ liệu cá nhân) → gọi agent **security-auditor** trước khi bàn giao.

### 5. Khi đẩy code lên remote
- Dùng skill **git-push-merge** khi người dùng ra lệnh đẩy/push/merge code. Không tự ý chạy các
  lệnh git push/merge rời rạc ngoài quy trình của skill này trừ khi người dùng yêu cầu khác.

## Memory.md

- `Memory.md` ở gốc dự án là **nhật ký tri thức chung** cho cả web lẫn mobile: mọi phân tích,
  hỏi–đáp, quyết định, checklist đều ghi vào đây (ghi nối tiếp — append, không xóa/sửa nội dung
  cũ).
- Checklist được thực hiện ở phiên sau → cập nhật tick `[x]` ngay trong `Memory.md`.

## Lệnh thường dùng

- Chạy/build web: xem `README.md` mục "Chạy ứng dụng" (MSBuild + IIS Express, cần SQL Server
  cấu hình ở `Web.config`).
- Đóng gói web: `.\publish.ps1`
- Test web: `dotnet test` (hiện dự án chưa có sẵn project test).
- Phân tích tĩnh Flutter: `flutter analyze`
- Test Flutter: `flutter test`

## Điều KHÔNG được làm

- Không tự bịa câu trả lời thay người dùng khi thiếu thông tin — phải hỏi (skill
  `phan-tich-van-de`).
- Không sửa code nghiệp vụ để ép test xanh; phát hiện bug thì báo cáo, việc sửa là quyết định
  của người dùng.
- Không hạ tiêu chuẩn nghiệm thu design ở vòng sau.
- Không dùng widget UI/icon gốc trực tiếp trong màn hình Flutter — chỉ qua tầng `App*`/`AppIcons`
  trong `core/widgets/`.
- Không nối chuỗi tạo câu lệnh SQL, không hard-code secret/connection string/token trong mã nguồn.
- Không rename namespace/class/project, không đổi kiến trúc, không đổi framework hay phiên bản
  .NET nếu chưa được yêu cầu.
- Không tự ý push/merge git ngoài quy trình skill `git-push-merge` khi người dùng yêu cầu đẩy code.
