---
trigger: always_on
---

# 🧪 Strict Testing & Quality Assurance Rules

Bạn phải tuân thủ nghiêm ngặt quy trình kiểm thử và xác minh sau đây cho mọi thay đổi mã nguồn:

---

## 1. Nguyên tắc viết Test (Test Design)
- **Bao phủ 3 tầng kịch bản:** Mọi tính năng/hàm logic mới phải có test cho:
  1. *Happy Path:* Trường hợp đầu vào chuẩn, hoạt động đúng mong đợi.
  2. *Edge Cases:* Dữ liệu biên (null, undefined, rỗng, số âm, mảng rỗng, chuỗi quá dài).
  3. *Error Handling:* Trường hợp lỗi (sai mật khẩu, mất mạng, token hết hạn, dữ liệu không hợp lệ) và kiểm tra xem có throw đúng mã lỗi/thông báo không.
- **Assertion có ý nghĩa:** Tuyệt đối không viết test rỗng hoặc assert hình thức (ví dụ: `expect(res).toBeDefined()`). Phải assert chính xác dữ liệu trả về và trạng thái mong muốn.
- **Mocking chuẩn:** Mock toàn bộ các phụ thuộc ngoại vi (Database, Network API, Third-party service, File system) để bộ test có thể chạy độc lập, cô lập và ổn định.

---

## 2. Quy trình Thực thi & Sửa lỗi (Debug Loop)
- **Tự động chạy test:** Ngay sau khi viết hoặc sửa code, PHẢI chạy lệnh test của file đó qua terminal.
- **Quy tắc phân tích nguyên nhân gốc rễ (Root Cause Analysis):**
  - Khi test FAIL: Đọc kỹ stack trace, xác định chính xác dòng bị lỗi và lý do logic trước khi sửa.
  - **TUYỆT ĐỐI KHÔNG sửa file Test** để làm cho bài test PASS, trừ khi yêu cầu nghiệp vụ thực sự thay đổi. Trách nhiệm của bạn là sửa file Implementation (mã nguồn chính).
  - Không được đoán mò hoặc thử các cách sửa ngẫu nhiên lặp đi lặp lại.
- **Kiểm tra hồi quy (Regression Check):** Sau khi bài test của tính năng mới PASS, phải chạy lại toàn bộ test suite liên quan trong module để đảm bảo không làm hỏng tính năng cũ.

---

## 3. Giới hạn Vòng lặp & Báo cáo
- **Giới hạn 3 lần thử:** Nếu sau 3 lần tự sửa mà test vẫn FAIL:
  - DỪNG vòng lặp tự sửa lại.
  - Trích dẫn log lỗi và stack trace chính xác.
  - Giải thích cho người dùng: (1) Bạn đang muốn làm gì, (2) Lỗi thực sự nằm ở đâu (môi trường, logic, hay dependency), (3) Đề xuất hướng giải quyết.
- Chỉ xem nhiệm vụ là HOÀN THÀNH khi:
  - Tất cả các test đều PASS 100%.
  - Không còn bất kỳ cảnh báo Linting hay Type Error nào.