# Hướng dẫn tích hợp API lấy dữ liệu CAS VNPT (HRM)

API cho hệ thống bên ngoài lấy dữ liệu danh bạ lấy từ cổng CAS VNPT (id.vnpt.com.vn → hrm.vnpt.vn) qua lệnh `/signin` trên bot Telegram: **nhân sự**, **đơn vị**, **chức danh**. Không đăng nhập; xác thực bằng **checksum SHA‑256** tính từ nội dung bản tin cộng khoá bí mật của hệ thống.

> **Lưu ý:** API này **hoàn toàn khác** API `/api/hrm` (xem *Hướng dẫn tích hợp API lấy dữ liệu HRM*) — đó là dữ liệu đồng bộ từ GoConnect, còn API dưới đây lấy từ cổng CAS VNPT. Hai nguồn độc lập, không dùng chung bảng dữ liệu. Cả hai API dùng **chung** một cặp mã hệ thống (`partnerCode`) và khoá bí mật (`secretKey`).

---

## 1. Chuẩn bị

1. Đăng nhập phần mềm bằng tài khoản **Quản trị** → menu **Quản trị → Hệ thống tích hợp**.
2. Bấm **Thêm hệ thống**, nhập **Mã** (ví dụ `CRM`) và **Tên**. Hệ thống tự sinh **Khoá bí mật**. Nếu hệ thống đối tác đã tích hợp API HRM (GoConnect) từ trước thì dùng lại đúng mã và khoá đó, không cần tạo mới.
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
| Đường dẫn | `/api/hrmcas` |
| Địa chỉ đầy đủ | `http://pmncpt.cenit.vn/api/hrmcas` (hoặc tên miền nội bộ tương ứng) |
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
| 4 | `resource` | string | Có | Loại dữ liệu: `employees` \| `departments` \| `jobs` |
| 5 | `page` | number | Không | Trang, đếm từ 1 (mặc định 1) |
| 6 | `pageSize` | number | Không | Số dòng mỗi trang (mặc định 100, tối đa 500) |
| 7 | `keyword` | string | Không | Từ khoá lọc — chỉ áp dụng cho `employees` (họ tên, mã nhân viên, điện thoại, email, tên đơn vị/chức danh). `departments` và `jobs` bỏ qua trường này (luôn trả về cả danh mục theo trang). |
| — | `checksum` | string | Có | Chuỗi SHA‑256, xem mục 4 |

---

## 4. Cách tạo checksum

```
checksum = SHA256( partnerCode | requestId | requestTime | resource | page | pageSize | keyword | secretKey )
```

Quy tắc — **giống hệt** API `/api/hrm`:

1. Ghép các trường **1 → 7 theo đúng thứ tự**, ngăn nhau bởi ký tự `|`.
2. Trường **không bắt buộc** mà bỏ trống thì để **chuỗi rỗng**, nhưng **vẫn giữ dấu `|`**.
3. **Khoá bí mật đứng cuối cùng**.
4. Băm SHA‑256, lấy kết quả dạng **hex chữ thường** (64 ký tự).

### Ví dụ tính (có thể kiểm lại)

Giả sử:

- `secretKey` = `3f8a1c9e2b7d4056a1e3c5b7d9f0246813579bdf02468ace13579bdf02468ac0`
- `partnerCode` = `CRM`, `requestId` = `REQ-20260813-0001`, `requestTime` = `20260813090000`
- `resource` = `employees`, `page` = `1`, `pageSize` = `100`, `keyword` = *(rỗng)*

Chuỗi đầu vào (chú ý hai dấu `||` ở chỗ `keyword` rỗng):

```
CRM|REQ-20260813-0001|20260813090000|employees|1|100||3f8a1c9e2b7d4056a1e3c5b7d9f0246813579bdf02468ace13579bdf02468ac0
```

Kết quả:

```
checksum = 8b1a99533ff5c0313f14f5e9a51ec0e05ac1c2c62b5ad38b8cb9c1e0d2f1e4a5
```

*(chuỗi trên chỉ minh hoạ cách ghép — hãy tự băm lại với `secretKey` thật của bạn để có checksum đúng.)*

---

## 5. Kết quả trả về

```json
{
  "code": "00",
  "message": "Thành công.",
  "requestId": "REQ-20260813-0001",
  "resource": "employees",
  "page": 1,
  "pageSize": 100,
  "totalItems": 773,
  "totalPages": 8,
  "data": [ /* danh sách bản ghi của trang */ ],
  "checksum": "..."
}
```

Trường phản hồi và checksum của phản hồi: **giống hệt** API `/api/hrm` (xem *Hướng dẫn tích hợp API lấy dữ liệu HRM*, mục 5).

```
checksum = SHA256( code | message | requestId | resource | page | pageSize | totalItems | secretKey )
```

> **Phản hồi lỗi không kèm `checksum`**, các số phân trang đều bằng `0`. **Mọi trường hợp đều trả HTTP 200** — phân biệt thành công hay lỗi bằng `code`.

### Cấu trúc `data` theo từng `resource`

**employees**
```json
{ "code": "VNPT005292", "fullName": "Bùi Công Khoa",
  "phoneNumber": "0914161333", "email": "khoabc.kha@vnpt.vn",
  "gender": "nam", "birthday": "1995-05-20",
  "departmentId": 18942, "departmentCode": "239.601.700",
  "department": "Viễn thông Khánh Hòa / Trung tâm ABC / Phòng Nhân sự - Tổng hợp",
  "jobCode": "CV", "job": "Chuyên viên",
  "workPositionCode": "CV_DTTD", "workPosition": "Chuyên viên Đào tạo - Tuyển dụng",
  "isCollaborator": false }
```
> `department` là **đường dẫn đầy đủ** từ đơn vị gốc ("Viễn thông Khánh Hòa") xuống tới đơn vị sâu nhất của người đó, ghép bằng ` / `. `departmentCode`/`jobCode` khớp với trường `code` của `departments`/`jobs` khi khớp được với đơn vị/chức danh tương ứng bên GoConnect — có thể **vắng mặt** nếu đơn vị/chức danh đó chưa khớp được mã (xem cảnh báo `departments` bên dưới). `job` (chức danh, `hr.job`) và `workPosition` (vị trí công việc, `vitri_congviec`) là **hai khái niệm khác nhau** trong dữ liệu CAS, không phải trùng lặp.

**departments**
```json
{ "id": 100001, "code": "239", "name": "Trung tâm ABC",
  "parentId": 0, "parentCode": "239", "employeeCount": 25 }
```
> `id` là id nội bộ của hệ thống (có thể là id giả từ 100001 trở lên đối với những đơn vị cấp trung gian không có id thật trong API gốc — xem tài liệu kỹ thuật `HrmDirectorySync`). `code`/`parentCode` là mã chuẩn khớp từ GoConnect theo **tên đơn vị**; **vắng mặt** khi chưa tìm được đơn vị cùng tên bên GoConnect. `parentId` là `0` ở đơn vị gốc ("Viễn thông Khánh Hòa"), vắng mặt (`null` bị lược bỏ) nếu chưa suy được đơn vị cha.

**jobs**
```json
{ "id": 12, "code": "CD_VNPT_08", "name": "Giám đốc", "employeeCount": 5 }
```

> **Trường rỗng bị lược bỏ khỏi bản ghi.** Nhân sự không có email thì phản hồi **không có khoá** `email` chứ không phải `"email": null`. Bên nhận nên coi khoá vắng mặt là giá trị rỗng — đọc kiểu `record.email ?? ""` thay vì giả định khoá luôn tồn tại.

---

## 6. Mã lỗi

Giống hệt API `/api/hrm`:

| `code` | Ý nghĩa | Cách xử lý |
|--------|---------|------------|
| `00` | Thành công | — |
| `01` | Checksum không hợp lệ | Kiểm lại thứ tự trường, dấu `\|`, khoá bí mật |
| `02` | Hệ thống không tồn tại hoặc đang bị khoá | Kiểm `partnerCode`; bật lại trạng thái hoạt động |
| `03` | Tham số sai (thiếu trường bắt buộc hoặc `resource` không đúng — chỉ nhận `employees`/`departments`/`jobs`) | Kiểm body |
| `99` | Lỗi hệ thống — **kể cả khi thân JSON sai cú pháp** | Kiểm lại body có đúng JSON không; nếu body đúng thì thử lại sau và báo quản trị |

---

## 7. Ví dụ đầy đủ

**Request** — `POST /api/hrmcas`

```json
{
  "partnerCode": "CRM",
  "requestId": "REQ-20260813-0001",
  "requestTime": "20260813090000",
  "resource": "employees",
  "page": 1,
  "pageSize": 100,
  "keyword": "",
  "checksum": "<chuỗi SHA-256 tính theo secretKey thật của bạn>"
}
```

---

## 8. Dùng file Postman

Kèm theo file **`CAS-HRM-API.postman_collection.json`**.

1. Mở Postman → **Import** → chọn file này.
2. Chọn collection **CAS HRM API** → tab **Variables**, điền:
   - `baseUrl` — ví dụ `http://pmncpt.cenit.vn`
   - `partnerCode` — mã hệ thống của bạn
   - `secretKey` — khoá bí mật của bạn
   Bấm **Save**.
3. Mở request bất kỳ (Nhân sự / Đơn vị / Chức danh) → **Send**.

File Postman **tự sinh `requestId`, `requestTime` và tính `checksum`** trước mỗi lần gửi (trong tab *Pre‑request Script*), nên bạn không phải tính tay. Tab *Tests* sẽ tự kiểm `code = 00` và xác thực lại checksum phản hồi.

---

## 9. Lưu ý bảo mật

- **Khoá bí mật không bao giờ gửi trong bản tin** — chỉ dùng để tính checksum ở mỗi bên.
- Mỗi hệ thống một khoá riêng, dùng chung cho cả API HRM (GoConnect) lẫn API CAS HRM này. Nghi lộ thì **Tạo lại khoá** và cấp lại cho đối tác — khoá mới áp dụng cho cả hai API cùng lúc.
- Nên gọi qua HTTPS nếu hạ tầng có chứng chỉ.
- `requestId` nên duy nhất mỗi lần gọi để tiện đối soát nhật ký.
