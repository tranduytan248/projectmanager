---
trigger: always_on
---

# Testing & Verification Rules

- Sau khi tạo mới hoặc chỉnh sửa bất kỳ logic code nào, bạn LUÔN PHẢI tự động chạy bộ test tương ứng qua terminal (ví dụ: `npm test`, `pytest`, `go test ./...`).
- Nếu test thất bại (FAIL) hoặc gặp lỗi compile/linter, hãy tự động sửa code và chạy lại test.
- Chỉ xem nhiệm vụ hoàn thành khi toàn bộ test đã vượt qua (PASS) và không còn lỗi linting.