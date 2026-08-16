---
name: git-push-merge
description: Quy trình đẩy code chuẩn của Tân. LUÔN dùng skill này khi người dùng ra lệnh đẩy/push code, ví dụ "push code", "đẩy code", "up code", "đẩy code lên", "push và merge", "merge lên main", "hoàn tất và đẩy code", hoặc bất kỳ câu lệnh nào ngụ ý muốn đưa code hiện tại lên remote — kể cả khi họ không nhắc đến main hay upload-source. Skill thực hiện tuần tự - commit thay đổi (nếu có), push nhánh hiện tại, merge nhánh hiện tại vào main, rồi merge main vào upload-source, và quay về nhánh ban đầu.
---

# Git Push & Merge Workflow

Khi được kích hoạt, thực hiện quy trình sau **theo đúng thứ tự**, dùng các lệnh `git` qua bash. Không hỏi lại người dùng trừ khi gặp tình huống bất thường được nêu ở mục "Xử lý sự cố".

## Quy trình chuẩn

### Bước 0 — Kiểm tra trạng thái

```bash
git status
git branch --show-current
```

- Ghi nhớ tên nhánh hiện tại (gọi là `<current>`), sẽ dùng xuyên suốt và quay về ở bước cuối.
- Nếu `<current>` chính là `main` hoặc `upload-source`: báo người dùng và hỏi xác nhận trước khi tiếp tục (bình thường quy trình chạy từ một nhánh tính năng).

### Bước 1 — Commit thay đổi chưa lưu (nếu có)

Nếu `git status` cho thấy còn thay đổi chưa commit:

```bash
git add -A
git commit -m "<thông điệp ngắn gọn mô tả nội dung thay đổi>"
```

- Viết commit message dựa trên nội dung công việc trong cuộc hội thoại (tiếng Việt hoặc tiếng Anh tùy theo convention của repo — xem `git log --oneline -5` để bắt chước style).
- Nếu không có thay đổi nào, bỏ qua bước này.

### Bước 2 — Push nhánh hiện tại

```bash
git push origin <current>
```

Nếu nhánh chưa có upstream: `git push -u origin <current>`.

### Bước 3 — Merge vào main

```bash
git checkout main
git pull origin main
git merge <current>
git push origin main
```

### Bước 4 — Merge main vào upload-source

```bash
git checkout upload-source
git pull origin upload-source
git merge main
git push origin upload-source
```

### Bước 5 — Quay về nhánh ban đầu và báo cáo

```bash
git checkout <current>
```

Báo cáo ngắn gọn cho người dùng:
- Commit đã tạo (nếu có) và message
- Các nhánh đã push thành công: `<current>`, `main`, `upload-source`

## Xử lý sự cố

- **Xung đột (merge conflict)**: DỪNG NGAY, không tự ý resolve. Chạy `git merge --abort`, liệt kê các file bị xung đột (`git diff --name-only --diff-filter=U` trước khi abort), báo người dùng và hỏi hướng xử lý.
- **Push bị từ chối (rejected / non-fast-forward)**: pull lại (`git pull --rebase` trên nhánh tính năng, `git pull` trên main/upload-source) rồi thử push lại một lần. Nếu vẫn lỗi, dừng và báo người dùng. TUYỆT ĐỐI không dùng `git push --force`.
- **Nhánh `upload-source` chưa tồn tại trên remote**: hỏi người dùng có muốn tạo mới từ main không, chỉ tạo khi được đồng ý (`git checkout -b upload-source main && git push -u origin upload-source`).
- **Repo có hook / CI chặn push**: báo lại nguyên văn lỗi cho người dùng, không tìm cách bypass (`--no-verify` bị cấm).

## Nguyên tắc

- Luôn giữ nguyên lịch sử: không force push, không rebase main/upload-source, không xóa nhánh.
- Mỗi bước chạy xong phải kiểm tra kết quả lệnh trước khi sang bước tiếp theo.
- Kết thúc quy trình, working tree phải sạch và đang đứng ở nhánh `<current>` ban đầu.
