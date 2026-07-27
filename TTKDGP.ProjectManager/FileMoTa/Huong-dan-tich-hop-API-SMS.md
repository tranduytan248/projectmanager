# Hướng dẫn tích hợp API gửi tin nhắn SMS

API cho hệ thống bên ngoài nhờ phần mềm **gửi tin nhắn SMS** tới một hoặc nhiều số máy. Không đăng nhập; xác thực bằng cặp **Mã hệ thống** + **Khoá bí mật** đã khai ở màn hình **Hệ thống tích hợp** — dùng lại đúng khoá đó, không cấp khoá riêng cho SMS.

Phần mềm không nối thẳng vào nhà mạng: nhận bản tin, kiểm khoá, chuẩn hoá số máy rồi chuyển tiếp sang tổng đài SMS dùng chung (địa chỉ khai trong `Sms:GatewayUrl` của `Web.config`).

---

## 1. Chuẩn bị

1. Đăng nhập phần mềm bằng tài khoản **Quản trị** → menu **Quản trị → Hệ thống tích hợp**.
2. Bấm **Thêm hệ thống**, nhập **Mã** (ví dụ `CRM`) và **Tên**. Hệ thống tự sinh **Khoá bí mật**.
3. Ghi lại hai giá trị dùng để gọi API:
   - **Mã hệ thống** → tham số `partnerCode`.
   - **Khoá bí mật** → tham số `secretKey`; bấm **Sao chép** ở màn hình sửa để lấy đủ chuỗi.
4. Giữ khoá bí mật cẩn thận, chỉ cấp cho đúng hệ thống đối tác. Nếu lộ, mở màn hình sửa và bấm **Tạo lại khoá**.

> Hệ thống ở trạng thái **Ngừng** sẽ không gọi API được.
> Nếu đã có hệ thống dùng cho API HRM (`/api/hrm`) thì **dùng luôn mã và khoá đó**, không cần khai thêm.

---

## 2. Thông tin endpoint

| | |
|---|---|
| Phương thức | `GET` |
| Đường dẫn | `/api/Util/SendSms` |
| Địa chỉ đầy đủ | `http://pmncpt.cenit.vn/api/Util/SendSms` (hoặc tên miền nội bộ tương ứng) |
| Tham số | Query string (mục 3) |
| Kết quả | JSON, `Content-Type: application/json; charset=utf-8` |

---

## 3. Tham số đầu vào

| # | Tham số | Kiểu | Bắt buộc | Mô tả |
|---|---------|------|----------|-------|
| 1 | `partnerCode` | string | Có | Mã hệ thống tích hợp (cột **Mã**) |
| 2 | `phoneNums` | string | Có | Danh sách số nhận, ngăn nhau bởi dấu phẩy |
| 3 | `smsContents` | string | Có | Nội dung tin nhắn |
| 4 | `secretKey` | string | Có | Khoá bí mật của chính hệ thống đó (cột **Khoá bí mật**) |

Tất cả giá trị phải được **URL‑encode** khi ghép vào địa chỉ (nội dung tiếng Việt có dấu, dấu cách, dấu phẩy…).

`partnerCode` không phân biệt hoa thường; `secretKey` phải khớp **chính xác** với khoá đang lưu.

### Số điện thoại

Phần mềm tự chuẩn hoá trước khi chuyển cho tổng đài:

- Nhận cả `0942963127`, `84942963127`, `+84 942 963 127`, `0942.963.127` — đều thành `84942963127`.
- Số trùng nhau bị bỏ bớt, chỉ gửi một lần.
- Số sai định dạng bị loại và trả lại trong `invalidPhones`; các số còn lại vẫn được gửi.
- Không còn số nào hợp lệ → lỗi `03`.
- Một lần gọi tối đa **50 số** (đổi được bằng khoá `Sms:MaxPhones`); vượt trần → lỗi `03`.

---

## 4. Kết quả trả về

```json
{
  "code": "00",
  "message": "Gửi tin nhắn thành công",
  "data": ["84942963127", "84912345678"],
  "totalPhones": 2
}
```

Trường phản hồi:

| Trường | Mô tả |
|--------|-------|
| `code` | `00` là thành công; khác là lỗi (xem mục 5) |
| `message` | Thông báo của tổng đài, hoặc mô tả lỗi |
| `data` | Danh sách số đã chuyển cho tổng đài, đã chuẩn hoá |
| `totalPhones` | Số lượng máy nhận |
| `invalidPhones` | Các số bị loại vì sai định dạng — chỉ có khi thực sự có số sai |

---

## 5. Mã lỗi

| `code` | Ý nghĩa | Cách xử lý |
|--------|---------|------------|
| `00` | Thành công | — |
| `01` | Khoá bí mật không đúng | Sao chép lại khoá ở màn hình sửa hệ thống; chú ý không dính khoảng trắng thừa |
| `02` | Hệ thống không tồn tại hoặc đang bị khoá | Kiểm `partnerCode`; bật lại trạng thái hoạt động |
| `03` | Tham số sai (thiếu trường, số máy sai định dạng, vượt trần số máy) | Kiểm lại query string |
| `04` | Tổng đài từ chối hoặc tính năng SMS đang tắt | Xem `message`; báo quản trị kiểm `Sms:Enabled` và `Sms:GatewayUrl` |
| `99` | Lỗi hệ thống | Thử lại sau; báo quản trị nếu lặp lại |

---

## 6. Ví dụ đầy đủ

**Request**

```
GET /api/Util/SendSms
    ?partnerCode=CRM
    &phoneNums=0942963127%2C0912345678
    &smsContents=Xin%20chao!%20Toi%20la%20SMS
    &secretKey=15b8fe44b2e38c77bc594151ce1c113192c21cd922300736b1d1c...
```

**cURL**

```bash
curl -G "http://pmncpt.cenit.vn/api/Util/SendSms" \
  --data-urlencode "partnerCode=CRM" \
  --data-urlencode "phoneNums=0942963127,0912345678" \
  --data-urlencode "smsContents=Xin chao! Toi la SMS" \
  --data-urlencode "secretKey=DAN-KHOA-BI-MAT-VAO-DAY"
```

**C#**

```csharp
var url = "http://pmncpt.cenit.vn/api/Util/SendSms"
    + "?partnerCode=" + Uri.EscapeDataString("CRM")
    + "&phoneNums="   + Uri.EscapeDataString("0942963127,0912345678")
    + "&smsContents=" + Uri.EscapeDataString("Xin chao! Toi la SMS")
    + "&secretKey="   + Uri.EscapeDataString(secretKey);

using (var client = new WebClient { Encoding = Encoding.UTF8 })
{
    var json = client.DownloadString(url);
}
```

---

## 7. Dùng file Postman

Kèm theo file **`SMS-API.postman_collection.json`**.

1. Mở Postman → **Import** → chọn file này.
2. Chọn collection **SMS API** → tab **Variables**, điền:
   - `baseUrl` — ví dụ `http://pmncpt.cenit.vn`
   - `partnerCode` — mã hệ thống của bạn
   - `secretKey` — khoá bí mật của bạn
   - `phoneNums`, `smsContents` — số nhận và nội dung muốn thử
   Bấm **Save**.
3. Mở request **Gui SMS** → **Send**.

Collection có sẵn hai request thử lỗi (sai khoá → `01`, số máy sai → `03`) để đối chiếu.

---

## 8. Lưu ý bảo mật

- Khoá bí mật đi trên **query string**, nên sẽ bị ghi vào nhật ký của IIS, proxy và các thiết bị trung gian. Chỉ cấp khoá cho hệ thống trong **mạng nội bộ**.
- Mỗi hệ thống một khoá riêng. Nghi lộ thì **Tạo lại khoá** và cấp lại cho đối tác — khoá cũ mất hiệu lực ngay.
- Nên gọi qua HTTPS nếu hạ tầng có chứng chỉ.
- Nội dung tin nhắn cũng nằm trên query string nên **không gửi thông tin nhạy cảm** qua kênh HTTP thường.

> Nếu cần kênh chặt hơn, API HRM (`/api/hrm`) dùng cách ký checksum SHA‑256 — khoá bí mật không bao giờ rời khỏi hai đầu. Xem `Huong-dan-tich-hop-API-HRM.md`.

---

## 9. Cấu hình phía quản trị

Các khoá trong `Web.config` (mục `appSettings`):

| Khoá | Mặc định | Ý nghĩa |
|------|----------|---------|
| `Sms:Enabled` | `true` | Tắt thì endpoint trả lỗi `04` ngay, không gọi ra ngoài |
| `Sms:GatewayUrl` | `https://vnptkhanhhoa.cenit.vn/api/Util/SendSms` | Địa chỉ tổng đài, nhận GET với `phoneNums` và `smsContents` |
| `Sms:TimeoutSeconds` | `30` | Thời gian chờ tổng đài trả lời |
| `Sms:MaxPhones` | `50` | Trần số máy nhận trong một lần gọi |
