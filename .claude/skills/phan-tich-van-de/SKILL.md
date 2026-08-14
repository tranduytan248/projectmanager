---
name: phan-tich-van-de
description: Quy trình bắt buộc khi người dùng đặt ra một vấn đề, yêu cầu, bài toán, hoặc ý tưởng mới trong dự án. Luôn dùng skill này khi người dùng mô tả một nhu cầu, một tính năng cần làm, một lỗi cần xử lý, hoặc một quyết định cần đưa ra — kể cả khi họ không nói rõ "hãy phân tích". Skill hướng dẫn: (1) phân tích rõ các nội dung xung quanh vấn đề, (2) đặt câu hỏi làm rõ lại cho người dùng, (3) khi nhận được câu trả lời thì xây dựng thành checklist hành động, và (4) ghi toàn bộ nội dung vào file Memory.md của dự án.
---

# Skill: Phân tích vấn đề → Hỏi lại → Checklist → Memory.md

## Nguyên tắc cốt lõi

**KHÔNG bắt tay vào giải quyết ngay.** Khi người dùng đặt ra một vấn đề, phải đi qua đủ 4 giai đoạn theo thứ tự. Mọi nội dung phát sinh ở từng giai đoạn đều phải được ghi vào `Memory.md` tại thư mục gốc của dự án.

---

## Giai đoạn 1 — Phân tích vấn đề

Khi tiếp nhận vấn đề, phân tích rõ các nội dung xung quanh trước khi hỏi:

1. **Bối cảnh**: Vấn đề nằm ở đâu trong dự án? Liên quan module/nghiệp vụ nào?
2. **Mục tiêu**: Người dùng thực sự muốn đạt được điều gì? (mục tiêu bề mặt vs mục tiêu sâu xa)
3. **Phạm vi**: Cái gì nằm trong phạm vi, cái gì nằm ngoài?
4. **Các bên liên quan**: Ai bị ảnh hưởng? (người dùng cuối, đội phát triển, khách hàng, hệ thống khác…)
5. **Ràng buộc**: Kỹ thuật (tech stack, dữ liệu hiện có), thời gian, nguồn lực, quy định.
6. **Rủi ro & giả định**: Những gì đang được ngầm giả định? Rủi ro nếu giả định sai?
7. **Phương án khả dĩ**: Sơ bộ 2–3 hướng tiếp cận có thể có.

Trình bày phần phân tích này **ngắn gọn, có cấu trúc** để người dùng thấy vấn đề đã được hiểu đúng.

➡️ **Ghi vào Memory.md**: mục "Phân tích ban đầu" (theo mẫu ở cuối file).

## Giai đoạn 2 — Đặt câu hỏi làm rõ

Từ phần phân tích, đặt lại cho người dùng **3–7 câu hỏi**, tuân thủ:

- Mỗi câu hỏi phải **gắn với một điểm chưa rõ cụ thể** trong phân tích (không hỏi chung chung).
- Đánh số câu hỏi để người dùng trả lời từng câu dễ dàng.
- Ưu tiên câu hỏi ảnh hưởng lớn đến quyết định thiết kế/giải pháp.
- Nếu có phương án gợi ý, đưa kèm dạng lựa chọn: "Anh/chị muốn theo hướng A hay B? (A: …, B: …)"
- **Dừng lại chờ trả lời.** Không tự trả lời thay, không đi tiếp sang giai đoạn 3.

➡️ **Ghi vào Memory.md**: mục "Câu hỏi làm rõ" (liệt kê đầy đủ các câu đã hỏi).

## Giai đoạn 3 — Xây dựng Checklist từ câu trả lời

Khi nhận được câu trả lời:

1. **Tóm tắt lại** câu trả lời để xác nhận hiểu đúng (1–2 dòng mỗi câu).
2. Nếu còn điểm mâu thuẫn hoặc chưa rõ → quay lại Giai đoạn 2 (hỏi bổ sung, tối đa 1–2 vòng).
3. Khi đã đủ thông tin, xây dựng **checklist hành động** theo cấu trúc:

```markdown
## Checklist: [Tên vấn đề]

### Chuẩn bị
- [ ] Việc cần làm trước (dữ liệu, môi trường, phê duyệt…)

### Thực hiện
- [ ] Các bước chính, theo thứ tự thực hiện
- [ ] Mỗi mục là một việc cụ thể, kiểm chứng được (làm xong biết là xong)

### Kiểm tra / Nghiệm thu
- [ ] Tiêu chí xác nhận hoàn thành
- [ ] Trường hợp biên cần test

### Ghi chú
- Ràng buộc, quyết định quan trọng, việc để lại giai đoạn sau
```

Yêu cầu với checklist:
- Mỗi mục bắt đầu bằng **động từ** (Tạo, Kiểm tra, Cập nhật, Xác nhận…).
- Sắp xếp theo **thứ tự thực hiện thực tế**.
- Gắn nhãn ưu tiên nếu cần: `[Bắt buộc]` / `[Nên có]` / `[Tùy chọn]`.

➡️ **Ghi vào Memory.md**: mục "Câu trả lời & Quyết định" + toàn bộ "Checklist".

## Giai đoạn 4 — Ghi vào Memory.md (bắt buộc, xuyên suốt)

`Memory.md` là **nhật ký tri thức của dự án**. Quy tắc:

- Nếu file chưa tồn tại → tạo mới tại thư mục gốc dự án.
- Nếu đã tồn tại → **ghi nối tiếp (append)**, không xóa/sửa nội dung cũ.
- Mỗi vấn đề là **một mục riêng**, có ngày tháng, theo mẫu dưới đây.
- Khi checklist được thực hiện ở các phiên sau → cập nhật trạng thái tick `[x]` ngay trong Memory.md.

### Mẫu ghi Memory.md

```markdown
---

# [YYYY-MM-DD] Vấn đề: <Tên ngắn gọn>

## 1. Mô tả vấn đề
<Nguyên văn hoặc tóm tắt trung thực yêu cầu của người dùng>

## 2. Phân tích ban đầu
- Bối cảnh: …
- Mục tiêu: …
- Phạm vi: …
- Ràng buộc: …
- Rủi ro / Giả định: …
- Phương án sơ bộ: …

## 3. Câu hỏi làm rõ
1. <Câu hỏi 1>
2. <Câu hỏi 2>

## 4. Câu trả lời & Quyết định
1. <Trả lời câu 1> → Quyết định: …
2. <Trả lời câu 2> → Quyết định: …

## 5. Checklist
### Chuẩn bị
- [ ] …
### Thực hiện
- [ ] …
### Kiểm tra / Nghiệm thu
- [ ] …
### Ghi chú
- …
```

---

## Tình huống đặc biệt

- **Người dùng muốn làm ngay, không muốn hỏi đáp**: Vẫn phân tích nhanh (rút gọn), nêu tối đa 1–2 câu hỏi then chốt kèm giả định mặc định ("Nếu không có ý kiến khác, tôi giả định X"), rồi làm checklist. Vẫn ghi Memory.md đầy đủ.
- **Vấn đề quá nhỏ** (sửa 1 dòng, câu hỏi tra cứu): Không cần đủ 4 giai đoạn, nhưng nếu phát sinh quyết định đáng nhớ → vẫn ghi 1 dòng vào Memory.md.
- **Vấn đề liên quan mục cũ trong Memory.md**: Đọc lại mục cũ trước khi phân tích, tham chiếu đến nó trong mục mới ("Liên quan: [YYYY-MM-DD] Vấn đề X").
- **Trả lời làm thay đổi phạm vi lớn**: Cập nhật lại phần Phân tích trong Memory.md (ghi chú "Cập nhật ngày …"), không âm thầm bỏ qua.

## Điều KHÔNG được làm

- Không nhảy thẳng vào code/giải pháp khi chưa qua Giai đoạn 1–2.
- Không tự bịa câu trả lời thay cho người dùng.
- Không tạo checklist mơ hồ kiểu "Hoàn thiện tính năng" — phải cụ thể, kiểm chứng được.
- Không quên ghi Memory.md — đây là yêu cầu bắt buộc ở mọi giai đoạn.
