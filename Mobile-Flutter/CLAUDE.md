# Dự án: BrewTask - Quản Lý Công Việc

<Mô tả 1–2 dòng: dự án làm gì, cho ai. Ví dụ: Ứng dụng thu phí cho Urenco Nha Trang — mobile Flutter + API ASP.NET Core + SQL Server.>

## Ngôn ngữ giao tiếp

- Luôn trả lời và viết tài liệu bằng **tiếng Việt có dấu**.

## Quy tắc bắt buộc (không có ngoại lệ)

1. **Tiếng Việt phải có dấu đầy đủ** trong mọi chuỗi hiển thị, thông báo, comment, tài liệu. Chuỗi không dấu = lỗi phải sửa ngay.
2. **UI Flutter theo kiến trúc custom widget**: màn hình CHỈ dùng widget `App*` trong `lib/core/widgets/`; tuyệt đối không dùng trực tiếp widget của Flutter/thư viện UI trong màn hình (trừ widget thuần bố cục: Column, Row, Padding, SizedBox…).
3. **Không hard-code**: chuỗi lấy từ `AppStrings`, màu từ `AppColors`, cỡ chữ từ `AppTextStyles`, khoảng cách từ `AppDimens`, icon từ `AppIcons`.
4. **SQL luôn tham số hóa** — cấm nối chuỗi tạo câu lệnh. Không commit secret/connection string vào git (file `Database.md` phải nằm trong `.gitignore`).
5. Quy tắc chi tiết theo ngôn ngữ nằm trong `.claude/rules/` (tự nạp theo loại file đang làm) — phải tuân thủ như quy tắc trong file này.

## Đội hình agent & skill của dự án

| Tên | Loại | Vai trò |
|---|---|---|
| `phan-tich-van-de` | Skill | Phân tích vấn đề → hỏi làm rõ → checklist → ghi Memory.md |
| `designer-mobile-pro` | Agent | Chuyên gia thiết kế UI/UX mobile — đề xuất phương án, dựng giao diện theo kiến trúc `App*` |
| `chuyen-gia-nghiem-thu-design` | Skill | Chuyên gia nghiệm thu design khó tính — chấm ĐẠT/KHÔNG ĐẠT, bắt sửa màu xấu, layout cẩu thả |
| `code-reviewer` | Agent | Review code sau mỗi lần viết/sửa |
| `test-engineer` | Agent | Viết và chạy test đến khi xanh |
| `security-auditor` | Agent | Kiểm tra bảo mật trước khi bàn giao |

## Quy trình làm việc

### Khi nhận vấn đề / yêu cầu / tính năng mới
- Áp dụng skill **phan-tich-van-de**: phân tích → đặt câu hỏi làm rõ → nhận trả lời → xây checklist → ghi toàn bộ vào `Memory.md` ở gốc dự án.
- KHÔNG code ngay khi chưa qua bước phân tích và làm rõ (trừ việc quá nhỏ).

### Khi code
- Trước khi viết, đọc lại các mục liên quan trong `Memory.md` để nắm quyết định cũ.
- Code xong một phần việc → gọi agent **code-reviewer** kiểm tra, sửa hết lỗi 🔴 trước khi tiếp tục.

### Khi làm giao diện mobile
- Thiết kế/dựng màn hình → dùng agent **designer-mobile-pro** (đề xuất phương án trước, code sau; đủ 5 trạng thái: loading / có dữ liệu / rỗng / lỗi / mất mạng).
- Màn hình hoàn thành → nghiệm thu bằng skill **chuyen-gia-nghiem-thu-design**. Kết luận KHÔNG ĐẠT thì phải sửa và nghiệm thu lại, không cho qua.

### Khi hoàn thành tính năng
- Gọi agent **test-engineer** viết và chạy test cho logic nghiệp vụ quan trọng; test phải xanh mới coi là xong.
- Tính năng nhạy cảm (đăng nhập, phân quyền, upload, API công khai, dữ liệu cá nhân) → gọi agent **security-auditor** trước khi bàn giao.

## Memory.md

- `Memory.md` ở gốc dự án là nhật ký tri thức: mọi phân tích, hỏi–đáp, quyết định, checklist đều ghi vào đây (ghi nối tiếp, không xóa nội dung cũ).
- Checklist được thực hiện ở phiên sau → cập nhật tick `[x]` trong `Memory.md`.

## Lệnh thường dùng

<Sửa theo dự án thực tế, ví dụ:>
- Chạy phân tích tĩnh Flutter: `flutter analyze`
- Chạy test Flutter: `flutter test`
- Chạy test backend: `dotnet test`

## Điều KHÔNG được làm

- Không tự bịa câu trả lời thay người dùng khi thiếu thông tin — phải hỏi.
- Không sửa code nghiệp vụ để ép test xanh; phát hiện bug thì báo cáo.
- Không hạ tiêu chuẩn nghiệm thu design ở vòng sau.
- Không dùng thư viện UI/icon trực tiếp trong màn hình — chỉ qua tầng `App*`/`AppIcons`.