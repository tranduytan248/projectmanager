# Tự động đăng nhập cổng VNPT GoConnect (có OTP qua Telegram)

Ngày: 2026-07-23

## Mục tiêu

Mỗi sáng (mặc định 07:30) hệ thống tự mở trình duyệt nền, điền **số điện thoại của chính
chủ tài khoản**, bấm **Gửi mã OTP**; sau đó nhắn qua **Telegram** để chủ tài khoản trả lời
mã OTP. Nhận được OTP, hệ thống nhập OTP + tick **Đồng ý điều khoản** + bấm **Đăng nhập**,
vào tới `…/home/workspace` thì **lưu phiên đăng nhập** lại rồi báo "đăng nhập thành công".

Dùng cho trường hợp chủ tài khoản không ngồi máy, chỉ cần cầm điện thoại đọc OTP.

## Giới hạn an toàn đã cài sẵn

- Bot **chỉ nghe đúng một chat id** (`GoConnect:ChatId` — chat riêng của chủ tài khoản).
  Tin nhắn từ nơi khác bị bỏ qua.
- Lịch tự động **chỉ dùng đúng một số điện thoại** (`GoConnect:DefaultPhone`).
- OTP luôn về điện thoại của chủ số; hệ thống không đọc được OTP, phải do chủ tài khoản nhập.

## Các thành phần mới

| File | Vai trò |
|------|---------|
| `Infrastructure/GoConnectAutoLogin.cs` | Điều khiển Playwright/Chromium: điền số → gửi OTP → chờ OTP → nhập OTP → đăng nhập → lưu phiên. |
| `Infrastructure/GoConnectTelegramPoller.cs` | Vòng lặp nền đọc tin Telegram: xử lý `/sdt` và mã OTP. |
| `Infrastructure/GoConnectScheduler.cs` | Hẹn giờ 07:30 mỗi ngày để chạy đăng nhập tự động. |
| `Infrastructure/TelegramClient.cs` | Bổ sung `GetUpdates` để **nhận** tin (trước đây chỉ gửi). |
| `Infrastructure/AppSettings.cs` | Thêm nhóm cấu hình `GoConnect`. |
| `Web.config` | Thêm các khoá `GoConnect:*`. |
| `Global.asax.cs` | Khởi động/dừng bộ nhận tin và bộ lịch. |

## Điều khiển qua Telegram

- `/sdt` → bot hỏi số điện thoại, tin kế tiếp được coi là số điện thoại.
- `/sdt 09xxxxxxxx` → đăng nhập luôn với số kèm theo.
- Khi bot báo "Đã gửi mã OTP…", trả lời bằng **mã OTP** (chỉ chữ số) để đăng nhập.

## Cấu hình (Web.config + secrets.config)

```xml
<add key="GoConnect:Enabled" value="true" />
<add key="GoConnect:DefaultPhone" value="0942963127" />
<add key="GoConnect:ChatId" value="<chat id riêng của bạn>" />
<add key="GoConnect:AutoLoginHour" value="7" />
<add key="GoConnect:AutoLoginMinute" value="30" />
<add key="GoConnect:Headless" value="true" />
```

- Token bot dùng chung `Telegram:BotToken` trong `App_Config\secrets.config`.
- Lấy chat id riêng: nhắn cho bot một tin bất kỳ rồi mở
  `https://api.telegram.org/bot<token>/getUpdates`, lấy `message.chat.id`.

## Cài Chromium cho Playwright (bắt buộc, làm một lần trên mỗi máy chủ)

Playwright cần tải Chromium. Đặt vào thư mục ứng dụng đọc được (`App_Data\playwright-browsers`):

Web app không sinh ra `playwright.ps1`, nên dùng node đi kèm trong `bin\.playwright`
(đã kiểm chứng có sẵn sau khi build):

```powershell
cd f:\SanPhamKDGP-KhongXoa\TTKDGP_Project\Source\TTKDGP.ProjectManager
$env:PLAYWRIGHT_BROWSERS_PATH = "$PWD\App_Data\playwright-browsers"
& "bin\.playwright\node\win32_x64\node.exe" "bin\.playwright\package\cli.js" install chromium
```

Hoặc dùng CLI toàn cục (kết quả tương đương, nhớ đặt cùng `PLAYWRIGHT_BROWSERS_PATH`):

```powershell
dotnet tool install --global Microsoft.Playwright.CLI
playwright install chromium
```

Mã trong `GoConnectAutoLogin.ConfigureBrowsersPath()` tự trỏ
`PLAYWRIGHT_BROWSERS_PATH` về đúng thư mục trên khi chạy, nên chỉ cần cài đúng chỗ.

## Lưu ý IIS (khi chạy dưới pm.tdt.vn và server sau này)

- Bật **Always Running** cho application pool (Start Mode = AlwaysRunning) và bật
  **Application Initialization**, để vòng lặp nhận OTP và bộ lịch không bị IIS ngắt khi
  không có ai truy cập web.
- Danh tính application pool phải **ghi được** vào `App_Data` (nơi lưu phiên và Chromium).
- Lần chạy thật đầu tiên nên đặt tạm `GoConnect:Headless=false` để xem trình duyệt thao tác,
  kiểm tra các bộ chọn phần tử có khớp giao diện GoConnect không. Nếu giao diện đổi, chỉ cần
  sửa các mảng `*Selectors` trong `GoConnectAutoLogin.cs`.

## Còn phụ thuộc thực tế cần kiểm khi chạy thử

- Các **bộ chọn phần tử** (ô số điện thoại, nút Gửi OTP, ô OTP, checkbox, nút Đăng nhập)
  đang là suy đoán từ ảnh chụp màn hình; lần chạy đầu (headless=false) cần soi lại cho khớp.
- Việc cào dữ liệu sau đăng nhập **có thể vi phạm điều khoản VNPT** và làm tài khoản bị khoá —
  cân nhắc trước khi bật chạy đều.
