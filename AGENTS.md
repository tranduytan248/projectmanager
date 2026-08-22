# Dự án: Hệ thống quản lý dự án & nhân sự — Tổ NCPT (web) + BrewTask (mobile)

Web ASP.NET MVC 5 (.NET Framework 4.8) + SQL Server quản lý dự án, nhân sự, giao việc, chấm KPI,
nghỉ phép của Tổ NCPT, kèm ứng dụng mobile Flutter (**BrewTask**, thư mục `Mobile-Flutter/`) gọi
thẳng API của web. Chi tiết chức năng xem `README.md` ở gốc dự án.

Các quy tắc trong `.agents/rules/` (hoặc `.claude/rules/`) áp dụng **bắt buộc và tuyệt đối cho toàn bộ dự án**:

## 1. Ngôn ngữ giao tiếp & Hiển thị
- Luôn trả lời, viết tài liệu, comment, và mọi chuỗi hiển thị UI bằng **tiếng Việt có dấu chuẩn xác 100%**.

## 2. Quy tắc bắt buộc theo loại file (KHÔNG CÓ NGOẠI LỆ)

| Đang sửa | Quy tắc áp dụng | Yêu cầu cốt lõi |
|---|---|---|
| **Mọi file** | `CODING_RULES.md` | Giữ nguyên kiến trúc, không đổi business logic ngoài phạm vi yêu cầu, không rename namespace/class/project bừa, không đổi framework/phiên bản .NET, UTF-8 có BOM cho file `.cshtml`/`.cs`, không SELECT * tùy tiện. |
| **`.dart` (Flutter)** | `FLUTTER_RULES.md` | **CẤM DÙNG TRỰC TIẾP WIDGET GỐC FLUTTER**: Tuyệt đối không dùng `CircularProgressIndicator`, `TextField`, `TextFormField`, `Text`, `DropdownButton`, `Checkbox`, `FloatingActionButton`, `Card`, `ElevatedButton`. **BẮT BUỘC DÙNG 100% CUSTOM WIDGETS `App*`**: `AppLoading` (ly cà phê bốc khói), `AppTextField`, `AppDateField`, `AppRichEditor`, `AppText`, `AppDropdown`, `AppCheckbox`, `AppFab`, `AppCard`, `AppButton`, `AppErrorState`, `AppColors`, `AppDimens` (bội số 4, touch target ≥ 48dp). |
| **`.cs` / `.cshtml`** | `C_SHARP_RULES.md` | Naming convention chuẩn C#, luồng Controller → Service → Repository, không viết SQL/business trong Controller, method Async có hậu tố `Async`. |
| **`.sql` / Query** | `SQL_SERVER_RULES.md` | Không `SELECT *`, luôn tham số hóa (không nối chuỗi), `DELETE`/`UPDATE` luôn có `WHERE`, transaction cho nhiều update, `NVARCHAR`/`N'...'` cho dữ liệu tiếng Việt. |

## 3. Quy trình Skill bắt buộc
- **Khi đẩy code / push code**: LUÔN LUÔN dùng skill `git-push-merge` (Commit → Push nhánh hiện tại → Merge `main` → Push `main` → Merge `upload-source` → Push `upload-source` → Checkout về lại nhánh ban đầu).
- **Khi xong màn hình Mobile**: LUÔN LUÔN tự nghiệm thu theo skill `chuyen-gia-nghiem-thu-design` (7 tiêu chí WCAG AA, Typography, Spacing bội số 4, Touch target ≥ 48dp, Custom widgets `App*`, 5 trạng thái: loading / data / empty / error / offline).

## 4. Điều CẤM TUYỆT ĐỐI
- Cấm tự ý dùng `CircularProgressIndicator` (phải dùng `AppLoading`).
- Cấm tự ý sửa đổi code để ép test xanh mà vi phạm nghiệp vụ.
- Cấm push code thiếu bước lên `upload-source`.
- Cấm hard-code màu sắc hex hoặc số lẻ khoảng cách (phải dùng `AppColors` và `AppDimens`).
