# Coding Rules

Version: 1.0

## Mục tiêu

Mọi source code được AI hoặc lập trình viên tạo ra phải:

- Tuân thủ kiến trúc hiện có.
- Không làm thay đổi business logic nếu không được yêu cầu.
- Không thay đổi coding style của dự án.
- Chỉ sửa đúng phạm vi yêu cầu.
- Không gây lỗi Unicode hoặc lỗi font tiếng Việt.

---

# Nguyên tắc ưu tiên

Ưu tiên theo thứ tự:

1. Giữ nguyên kiến trúc.
2. Không ảnh hưởng chức năng.
3. Đồng nhất coding style.
4. Dễ đọc.
5. Dễ bảo trì.

---

# Không được phép

❌ Không rename namespace.

❌ Không rename project.

❌ Không rename class nếu không được yêu cầu.

❌ Không thay đổi folder structure.

❌ Không tự tạo package mới.

❌ Không đổi framework.

❌ Không đổi phiên bản .NET.

❌ Không tự động refactor toàn bộ file.

❌ Không format toàn bộ project.

❌ Không sửa các file không liên quan.

---

# Quy tắc Encoding

Toàn bộ source phải sử dụng UTF-8.

Không được:

- xuất hiện ký tự �
- xuất hiện ký tự ?
- lỗi tiếng Việt
- ANSI
- UTF-16 nếu project không dùng

Không copy code từ Word.

---

# Quy tắc khi AI sinh code

AI phải:

- Đọc coding style hiện tại trước khi viết.
- Viết giống style hiện có.
- Không tự ý tối ưu khi chưa được yêu cầu.
- Không đổi business logic.
- Không sinh code dư thừa.
- Không tạo helper mới nếu project đã có helper tương tự.
- Không duplicate code.

---

# Source Control

Không commit:

- bin
- obj
- publish
- temp
- backup
- package

Không commit:

- Password
- Token
- Connection String Production

---

# Logging

Được phép log:

- Error
- Warning
- Information

Không log:

- Password
- JWT
- Access Token
- Refresh Token
- Connection String

---

# Checklist trước khi Commit

- Build thành công.
- Không warning mới.
- Không còn TODO.
- Không còn Console.WriteLine.
- Không còn dữ liệu test.
- Không lỗi Unicode.
- Không lỗi Encoding.
- Không sửa file ngoài phạm vi.