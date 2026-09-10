---
name: upcode-prod
description: Quy trình kiểm tra và cập nhật toàn diện môi trường Production Viễn thông (brewtask.vnptkhanhhoa.vn). LUÔN dùng skill này khi người dùng ra lệnh "upcode prod", "up code prod", "đẩy code prod", "deploy prod", hoặc bất kỳ câu lệnh nào ngụ ý muốn đồng bộ dữ liệu và source code từ hệ thống Trung tâm (pmncpt.cenit.vn) sang hệ thống Production Viễn thông (brewtask.vnptkhanhhoa.vn).
---

# Quy trình Upcode Production (BrewTask Viễn thông)

Hệ thống có 2 môi trường độc lập:
1. **Hệ thống 1 — Nội bộ Trung tâm**:
   - Website: `http://pmncpt.cenit.vn/`
   - CSDL: `10.57.30.10,1433` (DB: `pmncpt.cenit.vn`, User: `pmncpt.cenit.vn`)
   - Source code: Quản lý trên nhánh `main` và `upload-source`.

2. **Hệ thống 2 — Production Viễn thông**:
   - Website: `http://brewtask.vnptkhanhhoa.vn/`
   - CSDL: `10.57.47.2\MSSQL2012` (DB: `pmncpt.cenit.vn`, User: `tan.td`)
   - Web server & FTP: `10.57.47.3` (`/public_html`, User: `brewtask`)
   - Source code: Quản lý trên nhánh **`Prod`**.

---

## Các bước thực hiện khi người dùng yêu cầu "Upcode Prod"

Khi người dùng gõ lệnh **"upcode prod"**, **"up code prod"**, **"đẩy code prod"**, thực hiện tuần tự:

### Bước 1 — Kiểm tra & Báo cáo chênh lệch (Check Diff)
Chạy script kiểm tra tổng thể không sửa đổi dữ liệu:
```powershell
powershell -ExecutionPolicy Bypass -File "d:\SVN\projectmanager\build\upcode-prod.ps1"
```
Kết quả báo cáo cho người dùng gồm:
1. **Chênh lệch CSDL**:
   - Cấu trúc bảng/cột mới (Schema).
   - Số lượng bản ghi mới cần thêm (New Rows) và bản ghi có sửa đổi (`UpdatedAt` mới hơn) cho từng bảng.
2. **Chênh lệch Source Code & FTP**:
   - Danh sách commit mới trên nhánh `main` chưa có trên nhánh `Prod`.
   - Danh sách file thay đổi cần đóng gói và cập nhật lên FTP `10.57.47.3`.
3. **Trạng thái kết nối**: Tình trạng sẵn sàng của 2 CSDL và 2 Web server.

---

### Bước 2 — Thực thi Đồng bộ toàn diện (Execute Sync)
Thực hiện đồng bộ cả CSDL và Source Code FTP bằng lệnh:
```powershell
powershell -ExecutionPolicy Bypass -File "d:\SVN\projectmanager\build\upcode-prod.ps1" -Apply
```

Tiến trình tự động thực hiện:
1. **Đồng bộ CSDL (`build\sync-prod-db.ps1 -Apply`)**:
   - Thêm cột schema mới vào DB Target (nếu có).
   - Tạm tắt ràng buộc khóa ngoại (`NOCHECK CONSTRAINT ALL`).
   - Nạp các bản ghi mới bằng `SqlBulkCopy` kèm tùy chọn `KeepIdentity, KeepNulls` (giữ nguyên ID gốc).
   - Cập nhật các bản ghi có thay đổi mới hơn (`UpdatedAt`) qua bảng tạm.
   - Bật và kiểm tra lại toàn bộ ràng buộc khóa ngoại (`WITH CHECK CHECK CONSTRAINT ALL`).
2. **Đồng bộ Source Code & FTP (`build\sync-prod-ftp.ps1 -Apply`)**:
   - Checkout sang nhánh `Prod` và pull mới nhất.
   - Merge `main` vào `Prod` (nhờ cấu hình `merge=ours` trong `.gitattributes`, file cấu hình kết nối DB `10.57.47.2\MSSQL2012` trên `Prod` luôn được bảo toàn tuyệt đối, không bao giờ bị ghi đè).
   - Biên dịch và publish ứng dụng cấu hình `Release` vào `build\app`.
   - Tải lên FTP `10.57.47.3/public_html` các file mới hoặc có thay đổi (DLLs, Views, Scripts, Content), bỏ qua `secrets.config`.
   - Push nhánh `Prod` lên remote (`origin/Prod`).
   - Checkout quay về lại nhánh ban đầu (`main`).
3. **Kiểm tra sức khỏe website (Healthcheck)**:
   - Gửi yêu cầu HTTP GET tới `http://pmncpt.cenit.vn/` (xác nhận HTTP 200).
   - Gửi yêu cầu HTTP GET tới `http://brewtask.vnptkhanhhoa.vn/` (xác nhận HTTP 200).

---

### Bước 3 — Báo cáo kết quả hoàn tất
Báo cáo rõ ràng cho người dùng:
- Số bảng và số bản ghi CSDL đã thêm mới / cập nhật.
- Số commit đã merge và số file đã upload lên FTP.
- Kết quả kiểm tra sức khỏe của cả 2 website (HTTP 200 OK).

---

## Chạy độc lập từng phần (Khi cần)

- **Chỉ đồng bộ Database**:
  - Kiểm tra CSDL: `powershell -ExecutionPolicy Bypass -File "build\sync-prod-db.ps1"`
  - Thực thi đồng bộ CSDL: `powershell -ExecutionPolicy Bypass -File "build\sync-prod-db.ps1" -Apply`

- **Chỉ đồng bộ Source Code & FTP**:
  - Kiểm tra code/FTP: `powershell -ExecutionPolicy Bypass -File "build\sync-prod-ftp.ps1"`
  - Thực thi đồng bộ FTP: `powershell -ExecutionPolicy Bypass -File "build\sync-prod-ftp.ps1" -Apply`

---

## Điều cấm tuyệt đối
1. Cấm dùng sai thông tin CSDL hoặc nhầm lẫn giữa 2 máy chủ (`10.57.30.10` là Trung tâm, `10.57.47.2\MSSQL2012` là Prod).
2. Cấm upload nhầm code hoặc cấu hình của Prod lên máy chủ Trung tâm và ngược lại.
3. Cấm xóa hoặc ghi đè file `secrets.config` trên máy chủ FTP `10.57.47.3`.
4. Cấm phá vỡ `merge=ours` trên nhánh `Prod`.
