# Hướng dẫn tích hợp API lấy dữ liệu HRM

API cho hệ thống bên ngoài lấy dữ liệu nhân sự đã đồng bộ từ GoConnect: **nhân sự**, **đơn vị**, **chức danh**. Không đăng nhập; xác thực bằng **checksum SHA‑256** tính từ nội dung bản tin cộng khoá bí mật của hệ thống.

---

## 1. Chuẩn bị

1. Đăng nhập phần mềm bằng tài khoản **Quản trị** → menu **Quản trị → Hệ thống tích hợp**.
2. Bấm **Thêm hệ thống**, nhập **Mã** (ví dụ `CRM`) và **Tên**. Hệ thống tự sinh **Khoá bí mật**.
3. Ghi lại hai giá trị dùng để gọi API:
   - **Mã hệ thống** (`partnerCode`).
   - **Khoá bí mật** (`secretKey`) — bấm **Sao chép** ở màn hình sửa.
4. Giữ khoá bí mật cẩn thận, chỉ cấp cho đúng hệ thống đối tác. Nếu lộ, mở màn hình sửa và bấm **Tạo lại khoá**.

> Hệ thống ở trạng thái **Ngừng** sẽ không gọi API được.

---

## 2. Thông tin endpoint

| | |
|---|---|
| Phương thức | `POST` |
| Đường dẫn | `/api/hrm` |
| Địa chỉ đầy đủ | `http://pmncpt.cenit.vn/api/hrm` (hoặc tên miền nội bộ tương ứng) |
| Header | `Content-Type: application/json` |
| Thân (body) | JSON (mục 3) |

---

## 3. Tham số đầu vào

Thứ tự các trường dưới đây **chính là thứ tự** dùng để tạo chuỗi tính checksum.

| # | Trường | Kiểu | Bắt buộc | Mô tả |
|---|--------|------|----------|-------|
| 1 | `partnerCode` | string | Có | Mã hệ thống tích hợp |
| 2 | `requestId` | string | Có | Mã yêu cầu, duy nhất mỗi lần gọi (để đối soát) |
| 3 | `requestTime` | string | Có | Thời gian gửi, định dạng `yyyyMMddHHmmss` |
| 4 | `resource` | string | Có | Loại dữ liệu: `employees` \| `workplaces` \| `positions` |
| 5 | `page` | number | Không | Trang, đếm từ 1 (mặc định 1) |
| 6 | `pageSize` | number | Không | Số dòng mỗi trang (mặc định 100, tối đa 500) |
| 7 | `keyword` | string | Không | Từ khoá lọc (họ tên, mã, email, tên đơn vị/chức danh…) |
| — | `checksum` | string | Có | Chuỗi SHA‑256, xem mục 4 |

---

## 4. Cách tạo checksum

```
checksum = SHA256( partnerCode | requestId | requestTime | resource | page | pageSize | keyword | secretKey )
```

Quy tắc:

1. Ghép các trường **1 → 7 theo đúng thứ tự**, ngăn nhau bởi ký tự `|`.
2. Trường **không bắt buộc** mà bỏ trống thì để **chuỗi rỗng**, nhưng **vẫn giữ dấu `|`**.
3. **Khoá bí mật đứng cuối cùng**.
4. Băm SHA‑256, lấy kết quả dạng **hex chữ thường** (64 ký tự).

### Ví dụ tính (có thể kiểm lại)

Giả sử:

- `secretKey` = `3f8a1c9e2b7d4056a1e3c5b7d9f0246813579bdf02468ace13579bdf02468ac0`
- `partnerCode` = `CRM`, `requestId` = `REQ-20260724-0001`, `requestTime` = `20260724013000`
- `resource` = `employees`, `page` = `1`, `pageSize` = `100`, `keyword` = *(rỗng)*

Chuỗi đầu vào (chú ý hai dấu `||` ở chỗ `keyword` rỗng):

```
CRM|REQ-20260724-0001|20260724013000|employees|1|100||3f8a1c9e2b7d4056a1e3c5b7d9f0246813579bdf02468ace13579bdf02468ac0
```

Kết quả:

```
checksum = 4e68f0f8da65ae0779230f8e79fe05722c2f13a25c877bb1d78fd727101b8ce5
```

---

## 5. Kết quả trả về

```json
{
  "code": "00",
  "message": "Thành công.",
  "requestId": "REQ-20260724-0001",
  "resource": "employees",
  "page": 1,
  "pageSize": 100,
  "totalItems": 780,
  "totalPages": 8,
  "data": [ /* danh sách bản ghi của trang */ ],
  "checksum": "..."
}
```

Trường phản hồi:

| Trường | Mô tả |
|--------|-------|
| `code` | `00` là thành công; khác là lỗi (xem mục 6) |
| `message` | Mô tả kết quả |
| `requestId` | Trả lại đúng `requestId` đã gửi |
| `resource` | Loại dữ liệu đã lấy |
| `page`, `pageSize`, `totalItems`, `totalPages` | Thông tin phân trang |
| `data` | Mảng bản ghi của trang hiện tại |
| `checksum` | Checksum của phản hồi (xem dưới) |

### Checksum của phản hồi

Để kiểm phản hồi đúng nguồn và không bị sửa, tính lại và đối chiếu với `checksum`:

```
checksum = SHA256( code | message | requestId | resource | page | pageSize | totalItems | secretKey )
```

Lấy giá trị từ **chính phản hồi**, không phải từ request đã gửi: `page` và `pageSize` có thể khác lúc gửi — trang vượt quá tổng số trang bị kéo về trang cuối, còn `pageSize` lớn hơn 500 bị chặn ở 500.

> **Phản hồi lỗi không kèm `checksum`** — chưa xác thực được bên gọi thì hệ thống không ký. Khi đó `page`, `pageSize`, `totalItems`, `totalPages` đều bằng `0` và `data` là mảng rỗng.

> **Mọi trường hợp đều trả HTTP 200.** Phân biệt thành công hay lỗi bằng `code`, không bằng mã HTTP.

### Cấu trúc `data` theo từng `resource`

**employees**
```json
{ "code": "CTV074188", "fullName": "Nguyễn Văn A", "email": "a@vnpt.vn",
  "phoneNumber": "0912345678", "gender": "1", "department": "Tổ NCPT", "position": "Chuyên viên" }
```

**workplaces**
```json
{ "wpId": "0199...", "code": "239.307.603", "name": "Tổ NCPT",
  "parentId": "0199...", "level": 0, "employeeCount": 12 }
```
> `level`: 0 là cấp thấp nhất (phòng/tổ), số càng lớn càng lên cao.

**positions**
```json
{ "posId": "0199...", "code": "CV", "name": "Chuyên viên", "employeeCount": 210 }
```

> **Trường rỗng bị lược bỏ khỏi bản ghi.** Nhân sự không có email thì phản hồi **không có khoá** `email` chứ không phải `"email": null`. Tương tự với `phoneNumber`, `department`, `position` và `parentId`, `level` của đơn vị. Bên nhận nên coi khoá vắng mặt là giá trị rỗng — đọc kiểu `record.email ?? ""` thay vì giả định khoá luôn tồn tại.

---

## 6. Mã lỗi

| `code` | Ý nghĩa | Cách xử lý |
|--------|---------|------------|
| `00` | Thành công | — |
| `01` | Checksum không hợp lệ | Kiểm lại thứ tự trường, dấu `\|`, khoá bí mật |
| `02` | Hệ thống không tồn tại hoặc đang bị khoá | Kiểm `partnerCode`; bật lại trạng thái hoạt động |
| `03` | Tham số sai (thiếu trường bắt buộc hoặc `resource` không đúng) | Kiểm body |
| `99` | Lỗi hệ thống — **kể cả khi thân JSON sai cú pháp** | Kiểm lại body có đúng JSON không; nếu body đúng thì thử lại sau và báo quản trị |

---

## 7. Ví dụ đầy đủ

**Request** — `POST /api/hrm`

```json
{
  "partnerCode": "CRM",
  "requestId": "REQ-20260724-0001",
  "requestTime": "20260724013000",
  "resource": "employees",
  "page": 1,
  "pageSize": 100,
  "keyword": "",
  "checksum": "4e68f0f8da65ae0779230f8e79fe05722c2f13a25c877bb1d78fd727101b8ce5"
}
```

*(checksum trên tính với `secretKey` mẫu ở mục 4 — thay bằng khoá thật của bạn.)*

---

## 8. Dùng file Postman

Kèm theo file **`HRM-API.postman_collection.json`**.

1. Mở Postman → **Import** → chọn file này.
2. Chọn collection **HRM API** → tab **Variables**, điền:
   - `baseUrl` — ví dụ `http://pmncpt.cenit.vn`
   - `partnerCode` — mã hệ thống của bạn
   - `secretKey` — khoá bí mật của bạn
   Bấm **Save**.
3. Mở request bất kỳ (Nhân sự / Đơn vị / Chức danh) → **Send**.

File Postman **tự sinh `requestId`, `requestTime` và tính `checksum`** trước mỗi lần gửi (trong tab *Pre‑request Script*), nên bạn không phải tính tay. Tab *Tests* sẽ tự kiểm `code = 00` và xác thực lại checksum phản hồi.

---

## 9. Lưu ý bảo mật

- **Khoá bí mật không bao giờ gửi trong bản tin** — chỉ dùng để tính checksum ở mỗi bên.
- Mỗi hệ thống một khoá riêng. Nghi lộ thì **Tạo lại khoá** và cấp lại cho đối tác.
- Nên gọi qua HTTPS nếu hạ tầng có chứng chỉ.
- `requestId` nên duy nhất mỗi lần gọi để tiện đối soát nhật ký.
