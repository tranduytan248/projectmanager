# Hệ thống quản lý dự án & nhân sự — Tổ NCPT

Ứng dụng web ASP.NET MVC 5 (.NET Framework 4.8) quản lý dự án, nhân sự, giao việc, chấm KPI,
nghỉ phép và tích hợp dữ liệu nhân sự với các hệ thống ngoài. Dữ liệu lưu trong **SQL Server**.
Repo còn kèm bản **mobile Flutter (BrewTask)** gọi thẳng API của ứng dụng web, xem mục
[Ứng dụng di động](#ứng-dụng-di-động-brewtask).

## Chạy ứng dụng

Mở `TTKDGP.ProjectManager.sln` bằng Visual Studio 2022 rồi nhấn F5.

Hoặc chạy bằng dòng lệnh:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" TTKDGP.ProjectManager.sln /p:Configuration=Debug
& "C:\Program Files (x86)\IIS Express\iisexpress.exe" /path:"<đường dẫn tuyệt đối>\TTKDGP.ProjectManager" /port:52341
```

Truy cập http://localhost:52341

Ứng dụng cần kết nối được tới **SQL Server** — khai báo tại `Web.config` (`appSettings`):
`Db:Server`, `Db:Name`, `Db:User`, `Db:Password`, `Db:IntegratedSecurity`. Lúc khởi động,
`JsonToSqlMigration` tự tạo bảng nếu chưa có và nạp dữ liệu mẫu từ `App_Data/seed/*.json` sang
SQL nếu bảng còn trống — máy mới trỏ được vào một SQL Server rỗng là chạy ngay, không cần tự tay
tạo schema hay import dữ liệu.

## Tài khoản

Tài khoản **mẫu** đi kèm mã nguồn:

| Tên đăng nhập | Mật khẩu      | Nhóm quyền |
|---------------|---------------|------------|
| `admin`       | `Admin@123`   | Quản trị (toàn quyền) |
| `manager`     | `Manager@123` | Quản lý |

- **Quản trị** — toàn quyền hệ thống (mang mã `*`), gồm cả cấu hình nhóm quyền, người dùng, tích hợp.
- **Quản lý** — dự án, thành viên, phân công, báo cáo nhân sự, danh mục, và toàn bộ bộ *Quản lý
  công việc & KPI* (xem [Phân quyền theo nhóm](#phân-quyền-theo-nhóm)).

> **Đổi mật khẩu hai tài khoản này ngay khi triển khai thật.** Đây là mật khẩu công khai
> trong mã nguồn, ai đọc repo cũng biết.

Mật khẩu được băm bằng PBKDF2-SHA256 (100.000 vòng, muối riêng từng tài khoản), không lưu
dạng văn bản thường. Đổi mật khẩu tại menu **Thông tin cá nhân**; Admin đặt lại hộ người khác
tại mục **Người dùng**.

## Phân quyền theo nhóm

Hệ thống dùng **nhóm quyền động** (`RoleGroup`), không còn cố định hai vai Admin/Manager. Mỗi
chức năng mang một mã `module.action` (ví dụ `wprojects.edit`, `kpi.approve`) được định nghĩa cố
định trong mã nguồn (`Permission.cs`, 23 module: Tổng hợp, Báo cáo của tôi, Dự án, Thành viên,
Phân công, Báo cáo nhân sự, Tiến trình công việc, Danh mục, Bảng điều khiển Tổ, Dự án (bộ mới),
Công việc, Báo cáo tuần dự án, Chấm KPI, Nghỉ phép, Thống kê khối lượng, HRM, CAS VNPT, Thông báo,
GoConnect, Hệ thống tích hợp, Người dùng, Nhóm quyền, Chức năng hệ thống). Việc **gán** mã chức
năng cho từng nhóm là dữ liệu cấu hình, sửa tại màn **Nhóm quyền**.

- Cài đặt sẵn ba nhóm gốc: **Quản trị** (`*` — toàn quyền), **Quản lý**, **Báo cáo công việc**
  (chỉ xem/báo cáo phần việc của chính mình). Có thể tạo thêm nhóm tuỳ ý.
- Một tài khoản có thể mang **nhiều nhóm** cùng lúc (`User.Role` phân tách bằng dấu phẩy).
- **Quản lý Tổ** (`wteam.manage`) không phải quyền theo nhóm — đó là ô tích riêng trên từng tài
  khoản (màn **Người dùng**), vì cấp theo nhóm sẽ biến mọi PM mang nhóm Quản lý thành "Quản lý Tổ"
  dù họ chỉ tham gia dự án như nhân sự bình thường.
- Menu trái tự ẩn/hiện theo quyền "xem" của từng mục — không có danh sách menu cứng theo vai trò.
- Màn **Chức năng hệ thống** cho phép **tắt hẳn** một chức năng khỏi toàn hệ thống, kể cả với
  tài khoản toàn quyền — khác với việc chỉ gỡ chức năng đó khỏi một nhóm.

## Chức năng

**Công khai — không cần đăng nhập**
- `/` Màn hình tổng hợp. Phần *Chi tiết tham gia* gom theo **từng thành viên**: mỗi khối là một
  người, bên trong liệt kê dự án / khách hàng / vai trò / ghi chú công việc / trạng thái.
  Dự án ở trạng thái ưu tiên cao nhất (mặc định *Đang thực hiện*) hiện ngay, phần còn lại
  gập sau nút **Xem thêm**. Có lọc theo từ khoá / thành viên / dự án / trạng thái, kèm số liệu
  thống kê và biểu đồ khối lượng.
- `/Home/Project/{id}` Chi tiết một dự án kèm danh sách thành viên.

**Quản lý công việc & KPI — bộ chính đang dùng, cần đăng nhập**

Bảng dữ liệu riêng (`WorkProject`/`WorkTask`...), người tham gia là tài khoản `User` của hệ thống
thay vì hồ sơ Thành viên. Đây là các màn hiện có trên menu:

- **Tổng quan / Công việc của tôi / Dự án của tôi / Nghỉ phép của tôi** — ai đăng nhập cũng thấy,
  xem việc được giao, dự án tham gia, tự đăng ký nghỉ phép.
- **Checklist dự án** — Kanban rút gọn theo hai loại việc *Triển khai* / *Hỗ trợ*, có trao đổi
  (rich text), lịch sử thao tác, todo-list, chấm ngày công theo dòng nhật ký; PM tự import
  checklist cho dự án mình phụ trách.
- **Bảng điều khiển Tổ** (`wteam.manage`) — hôm nay ai làm gì, KPI tạm tính, tải việc mỗi người
  trong tổ.
- **Dự án** (bộ mới) — quản lý dự án, phân công theo giai đoạn.
- **Giao việc riêng** — giao việc ngoài phạm vi dự án, kèm **điểm cộng KPI 0,2 – 1,5%** khi hoàn
  thành đúng hạn và có thể đính kèm file.
- **Duyệt nghỉ phép** — Quản lý Tổ duyệt đơn của nhân sự trong tổ.
- **KPI theo tháng** — sinh bảng chấm, PM duyệt, duyệt lần cuối, kết xuất, gửi email kết quả.
  **Cấu hình KPI** (mục Quản trị) chỉnh công thức chấm: trọng số, mức trừ, ngày công, ngưỡng
  xếp loại.
- **Báo cáo tuần dự án / Báo cáo nhân sự** — PM lập báo cáo tuần cho dự án mình phụ trách, tổng
  hợp theo nhân sự.

**Bộ gốc (Dự án / Thành viên / Phân công / Danh mục) — vẫn chạy, đã ẩn khỏi menu**

Các controller này vẫn hoạt động đầy đủ và là nguồn dữ liệu cho trang chủ công khai, nhưng
không còn xuất hiện trên menu sau đăng nhập (bộ *Quản lý công việc & KPI* đã thay thế cho việc
quản lý hằng ngày). Truy cập trực tiếp bằng đường dẫn khi cần:

- **Dự án** — khai báo dự án (khách hàng, PM, loại, trạng thái, hợp đồng, Redmine/SVN/Github…).
- **Thành viên** — nhân sự của tổ (hồ sơ riêng, khác tài khoản `User` đăng nhập).
- **Phân công** — gán thành viên vào dự án kèm vai trò, nội dung công việc, trạng thái tham gia.
  Mỗi phân công có nút **Tiến trình** dẫn tới timeline nhật ký tuần (xem mục kế tiếp).
- **Danh mục** — bốn danh mục dùng chung: *Loại dự án*, *Trạng thái dự án*, *Trạng thái tham gia*,
  *Vai trò*.

**Nhân sự (HRM) — chỉ Admin, hai nguồn độc lập nhau**
- **HRM** — tra cứu nhân sự/đơn vị/chức danh đồng bộ **một chiều, chỉ đọc** từ GoConnect.
- **CAS VNPT (HRM)** — danh bạ lấy qua cổng CAS VNPT, kích hoạt bằng lệnh `/signin` gõ trong bot
  Telegram. Hoàn toàn tách biệt với HRM/GoConnect ở trên, không dùng chung dữ liệu.

**Tích hợp / đối tác**
- **GoConnect** — tự động đăng nhập hệ thống GoConnect bằng trình duyệt điều khiển từ xa, xác
  thực OTP qua bot Telegram; là nguồn đồng bộ cho màn HRM ở trên.
- **Hệ thống tích hợp** — quản trị các hệ thống đối tác được phép gọi API vào hệ thống (mã, tên,
  khoá bí mật riêng từng hệ thống).
- **API cho đối tác ngoài** (không cần đăng nhập, xác thực bằng checksum + khoá bí mật) —
  `POST /api/hrm` trả dữ liệu nhân sự, `GET /api/Util/SendSms` gửi SMS qua tổng đài dùng chung.
  Hướng dẫn tích hợp và Postman collection nằm trong `App_Data/docs/` (bản trong git, không phải
  dữ liệu vận hành) và `FileMoTa/`.

**Quản trị**
- **Thông tin cá nhân** — xem tài khoản, sửa họ tên, đổi mật khẩu (bấm vào tên mình ở góc phải).
- **Người dùng** — tạo/sửa/xoá tài khoản, đặt lại mật khẩu, khoá tài khoản, gán nhóm quyền và cờ
  Quản lý Tổ.
- **Nhóm quyền** — tạo/sửa nhóm, gán mã chức năng (xem [Phân quyền theo nhóm](#phân-quyền-theo-nhóm)).
- **Chức năng hệ thống** — đổi tên hiển thị và bật/tắt từng chức năng.
- **Thông báo** — cấu hình/theo dõi nhắc báo cáo (xem mục riêng bên dưới) và gửi mail thủ công.
- **Mẫu email** — quản trị nội dung mẫu email hệ thống gửi đi.
- **Tình trạng hệ thống** — việc tồn đọng, trạng thái các luồng chạy nền tự động.

### Cách danh mục hoạt động

Bản ghi nghiệp vụ tham chiếu tới danh mục bằng **tên** chứ không bằng khoá. Do đó:

- **Đổi tên** một mục sẽ tự động cập nhật mọi bản ghi đang dùng tên cũ.
- **Không cho xoá** mục còn bản ghi sử dụng — nút Xoá bị khoá và có cảnh báo kèm số lượng.
- **Bỏ đánh dấu "Đang sử dụng"** để ẩn mục khỏi danh sách chọn mà vẫn giữ nguyên bản ghi cũ.
- Bản ghi đang mang giá trị đã bị ẩn/xoá vẫn giữ được giá trị đó khi mở ra sửa.

Toàn bộ danh sách chọn của bộ gốc đều đọc từ danh mục, không có danh sách cứng nào trong mã nguồn.

Riêng danh mục **Trạng thái dự án** có thêm cột **Độ ưu tiên** (số nhỏ = ưu tiên cao). Giá trị này
quyết định thứ tự ở màn hình tổng hợp và dự án nào được hiện sẵn trước khi bấm *Xem thêm*.

### Nhật ký công việc theo tuần (bộ gốc)

Mỗi phân công (bộ gốc) có một chuỗi nhật ký, mỗi dòng ứng với **một tuần trong năm**: tuần đó làm
gì, trạng thái ra sao. Các dòng nối lại thành timeline theo dõi tiến trình.

- Tuần tính theo chuẩn **ISO 8601** (tuần bắt đầu Thứ Hai, tuần 1 là tuần chứa ngày 4/1).
  .NET Framework 4.8 không có sẵn lớp `ISOWeek` nên phần này tự cài đặt trong `WeekHelper`.
  Năm có 52 hoặc 53 tuần tuỳ năm.
- Dropdown chọn tuần hiển thị kèm khoảng ngày, ví dụ *Tuần 29 (13/07 – 19/07)*.
- Mỗi tuần chỉ được một dòng cho mỗi phân công; ghi trùng tuần sẽ bị từ chối.
- **Trạng thái và nội dung công việc của phân công luôn lấy theo dòng nhật ký mới nhất.**
- Xoá một phân công hoặc một dự án sẽ xoá luôn nhật ký liên quan.

> Bộ *Quản lý công việc & KPI* có cơ chế báo cáo tuần **riêng** (Checklist theo dòng ngày công,
> Báo cáo tuần dự án) — không dùng chung bảng với nhật ký tuần của bộ gốc ở trên.

### Nhân sự luôn tham chiếu bằng Id

Mọi chỗ liên quan đến con người ở bộ gốc đều lưu **Id thành viên**, không lưu tên dạng chuỗi:

| Nơi dùng | Trường |
|----------|--------|
| PM phụ trách dự án | `Project.PmMemberId` — combobox |
| Nhân sự tham gia dự án | `Project.ParticipantIds` — chọn nhiều |
| Phân công | `ProjectMember.MemberId` — combobox |

Đổi tên một thành viên chỉ sửa ở một chỗ và hiện đúng ở mọi màn hình. Id không tồn tại bị loại
bỏ khi lưu. Thành viên đã nghỉ vẫn xuất hiện trong danh sách chọn (có ghi chú) để bản ghi cũ
không bị mất người phụ trách.

### Quản trị tài khoản

Nhóm quyền và họ tên được đọc lại từ dữ liệu ở mỗi request, không lấy trong cookie. Nhờ vậy
việc khoá tài khoản, đổi quyền hay đổi họ tên **có hiệu lực ngay**, kể cả với phiên đang mở (kết
quả phân quyền được nhớ tạm trong phạm vi một request để không quét lại bảng nhóm quyền hàng
chục lần trên cùng một trang).

## Ứng dụng di động (BrewTask)

Thư mục `Mobile-Flutter/` là ứng dụng mobile Flutter, gọi thẳng vào các controller API của web
app tại `Controllers/Api/` (quy ước route `{controller}/{action}` của MVC, không có tiền tố
`/api`). **Đã nối API thật**, không còn là bản khung/placeholder.

- Đăng nhập qua `AuthApiController` — cấp bearer token lưu ở bảng `ApiTokens` (GUID, hạn 365
  ngày, không phải JWT ký số), kèm quên mật khẩu bằng OTP gửi SMS.
- Các API còn lại (`AccountApi`, `DashboardApi`, `MyWorkApi`, `MyProjectsApi`, `ChecklistApi`,
  `ProjectMembersApi`, `NotificationsApi`…) đều gắn `[ApiAuthorize]`, dùng lại đúng logic nghiệp
  vụ của controller web tương ứng qua `ApiMappers`.
- Tính năng đã có trên mobile: đăng nhập, tổng quan cá nhân, công việc của tôi, dự án của tôi,
  checklist (Kanban rút gọn, trao đổi rich text, log giờ công, lịch sử thao tác), nhân sự dự án
  (thêm/đặt PM/kết thúc tham gia), nghỉ phép, KPI, thông báo, hồ sơ cá nhân.

> `Mobile-Flutter/README.md` hiện mô tả **trạng thái cũ** (chưa nối API, dùng Riverpod/go_router)
> — không còn đúng với mã nguồn thật, xem `lib/config/api_endpoint.dart` và `pubspec.yaml` để có
> thông tin chính xác nếu cần chỉnh sửa tài liệu đó.

## Dữ liệu

Dữ liệu nghiệp vụ chính lưu ở **SQL Server**. `Data/Repository.cs` dùng `SqlStore<T>` cho toàn
bộ entity: Dự án, Thành viên, Phân công, Tài khoản, bốn danh mục bộ gốc, nhật ký tuần, nhật ký
nhắc báo cáo, hệ thống tích hợp, nhóm quyền, và toàn bộ bảng của bộ *Quản lý công việc & KPI*.
Kết nối khai báo qua `appSettings` trong `Web.config` (`Db:Server`, `Db:Name`, `Db:User`,
`Db:Password`, `Db:IntegratedSecurity`), không dùng section `<connectionStrings>` cổ điển.

`JsonStore<T>` (file JSON trong `App_Data`) vẫn còn dùng cho:
- **Dữ liệu mẫu** (`App_Data/seed/*.json`) — `JsonToSqlMigration` nạp sang SQL một lần lúc khởi
  động nếu bảng còn trống, để máy mới trỏ vào SQL Server rỗng vẫn chạy được ngay.
- Vài kho dữ liệu nhỏ độc lập với SQL: `HrEmployeeStore` (cache dữ liệu GoConnect), thông tin
  đăng nhập/danh bạ CAS VNPT, `GoConnectTokenStore`.

`App_Data/*.json` (ngoài `seed/`) bị `.gitignore` loại trừ vì là dữ liệu vận hành thật; kho mã chỉ
chứa dữ liệu mẫu. Muốn dựng lại dữ liệu mẫu sạch: xoá bảng tương ứng trong SQL Server (hoặc xoá
toàn bộ CSDL) rồi chạy lại ứng dụng để `JsonToSqlMigration` nạp lại từ `seed/`.

Ứng dụng **không** kết nối tới nguồn dữ liệu bên ngoài nào khác ngoài SQL Server đã cấu hình,
Telegram (nhắc báo cáo, HRM, GoConnect) và tổng đài SMS dùng chung — không có kết nối CSDL bên
thứ ba nào khác lúc chạy.

## Dropdown có tìm kiếm (Select2)

Mọi `<select>` trong hệ thống được `Scripts/app.js` tự khởi tạo Select2, không phải khai báo
thủ công ở từng view. Quy tắc:

- Dropdown từ **8 lựa chọn trở lên** mới hiện ô tìm kiếm. Ít hơn thì bỏ ô tìm kiếm cho gọn.
- Lựa chọn đầu tiên có value rỗng (*— Tất cả —*, *— Chọn ... —*) được dùng làm gợi ý,
  kèm nút **×** để bỏ chọn nhanh.
- Thông báo hiển thị bằng tiếng Việt (*Không tìm thấy kết quả*, *Đang tìm…*).
- Muốn một ô nào đó giữ nguyên dropdown thường thì thêm thuộc tính `data-no-select2`.

**jQuery và Select2 được đóng gói sẵn trong project, không gọi CDN** — app chạy được cả khi
server không có internet. Nếu view nào cần script riêng chạy sau jQuery, đặt trong
`@section Scripts { ... }` chứ đừng viết thẳng vào thân view (thân view render trước thẻ script).

## Favicon

Ảnh gốc `FileContents/favicon.png` được thu nhỏ thành các cỡ chuẩn trong `Content/img/`, cộng
thêm `favicon.ico` đa kích thước đặt ở gốc site để trình duyệt tự tìm thấy. Các thẻ khai báo nằm
trong `_Layout.cshtml` nên mọi trang đều dùng chung.

Muốn đổi icon: thay `FileContents/favicon.png` rồi chạy lại script xuất ảnh, hoặc tự thay
trực tiếp các file trong `Content/img/` và `favicon.ico`.

Đuôi `.webmanifest` phải khai báo MIME trong `Web.config` (`application/manifest+json`),
nếu không IIS trả về 404.

## Nhắc báo cáo

Hệ thống tự rà soát và nhắc khi có dự án/việc chưa được báo cáo, qua ba kênh cấu hình độc lập
trong `Web.config`: **Telegram** (`Telegram:*`), **Email** (`Email:*`), và **SMS** cho việc/báo
cáo trễ (`Reminder:TaskSmsEnabled`, giờ nhắc sáng/chiều riêng). Với nhắc báo cáo tuần qua
Telegram (bộ gốc):

| Thời điểm | Rà soát | Nội dung |
|-----------|---------|----------|
| Sáng thứ Hai (mặc định 8h) | Tuần **trước** | Các dự án chưa dừng lại mà PM chưa báo cáo |
| Chiều thứ Sáu (mặc định 15h) | Tuần **này** | Nhắc PM chưa thực hiện báo cáo trong tuần |

Tin nhắn gom theo từng PM phụ trách:

```
Nội dung: Các dự án chưa dừng lại mà bạn chưa báo cáo

Trần Thiên Long
1. Phần mềm CRM
2. Hóa đơn điện tử

Trịnh Minh Hậu
1. Website Làng gốm
```

- Một dự án được coi là **đã báo cáo** nếu có ít nhất một dòng nhật ký tuần trong tuần đó.
- Dự án **không bị nhắc** khi trạng thái được đánh dấu *Coi như đã dừng* trong danh mục
  **Trạng thái dự án**. Mặc định là *Đang chờ*, *Tạm dừng*, *Hoàn thành* — sửa được trong giao diện.
- Tin dài quá 4096 ký tự được tự cắt thành nhiều tin, cắt ở ranh giới dòng.

### Thiết lập Telegram

1. Nhắn **@BotFather** trên Telegram, lấy token bot.
2. **Thêm bot vào nhóm** rồi gửi một tin bất kỳ trong nhóm.
   Bot không thể tự vào nhóm bằng link mời — Telegram không cho phép.
3. Chép `App_Config\secrets.example.config` thành `App_Config\secrets.config`, điền token.
4. Vào màn hình **Thông báo** (chỉ Admin thấy), bấm **Dò chat id**, chép id vào `secrets.config`.
5. Vẫn ở màn hình đó, xem trước nội dung và bấm **Gửi ngay lên nhóm** để thử.

Màn hình Thông báo cũng hiển thị lịch sử các lần gửi, kèm lý do khi gửi hỏng.

### Chạy đúng giờ

Bộ lịch chạy sẵn trong ứng dụng. Nhưng IIS sẽ tắt ứng dụng khi không có ai truy cập, lúc đó bộ
lịch cũng dừng. Chọn một trong hai cách:

**Cách 1 — giữ ứng dụng luôn chạy.** Trong IIS Manager đặt application pool
*Start Mode* = `AlwaysRunning` và site *Preload Enabled* = `True`.

**Cách 2 — dùng Task Scheduler (chắc chắn hơn).** Tạo tác vụ chạy định kỳ:

```powershell
Invoke-WebRequest "http://localhost/Notifications/Trigger?key=<Reminder:TriggerKey>" -UseBasicParsing
```

Endpoint này không cần đăng nhập nhưng phải đúng khoá trong `secrets.config`. Nó chỉ gửi khi
đã tới giờ hẹn và kỳ đó chưa gửi, nên gọi thừa cũng không sinh tin trùng — an toàn khi chạy
song song với cách 1.

## Đóng gói (publish)

```powershell
.\publish.ps1                    # Release, mặc định
.\publish.ps1 -Configuration Debug
```

Mỗi lần chạy sẽ:

1. Biên dịch và publish vào `build\app\` (thư mục này sẵn sàng chép thẳng lên IIS)
2. Băm SHA256 toàn bộ file kết quả
3. So với lần publish trước để tìm file **thêm / sửa / xoá**
4. Ghi báo cáo vào `build\changes\changes-<ngày giờ>.md`
5. Cập nhật `build\manifest.json` làm mốc cho lần sau

Thư mục `build\app` và `manifest.json` không đưa vào git (là kết quả sinh ra), riêng các báo
cáo trong `build\changes` thì giữ lại để đối chiếu giữa các lần phát hành.

Publish cũng dùng được từ Visual Studio: chuột phải project → **Publish** → chọn hồ sơ
`FolderProfile`. Nhưng chạy qua `publish.ps1` mới sinh được báo cáo file thay đổi.

## Triển khai lên máy chủ

1. Chép toàn bộ `build\app\` vào thư mục site trên IIS.
2. Cấp quyền **Modify** cho tài khoản application pool trên thư mục `App_Data`
   (một số kho dữ liệu nhỏ, file đính kèm và `errors.log` vẫn ghi trực tiếp vào đây).
3. Đảm bảo IIS/server truy cập được SQL Server đã khai báo ở `Db:*` trong `Web.config`.
4. Sinh `machineKey` riêng: IIS Manager → chọn site → **Machine Key** → *Generate Keys* → Apply.
   Không có bước này thì mỗi lần khởi động lại ứng dụng, mọi người bị đăng xuất.
5. Đăng nhập bằng `admin` / `Admin@123` rồi **đổi mật khẩu ngay**.

## Lưu ý cho người phát triển

**File nguồn `.cs` và `.cshtml` phải lưu dạng UTF-8 có BOM.** Nếu thiếu BOM, trình biên dịch C#
và Razor sẽ đọc theo codepage ANSI của Windows và mọi chữ tiếng Việt viết thẳng trong mã nguồn
sẽ hiển thị sai (kiểu `LÆ°á»£t phÃ¢n cÃ´ng`). Visual Studio mặc định giữ BOM khi lưu; chỉ cần chú ý
nếu sửa file bằng công cụ khác.

Ứng dụng cố định culture `vi-VN` cho mọi request (`Global.asax.cs`), nhưng `<input type="number">`
của trình duyệt luôn gửi số dạng dấu chấm — `DecimalModelBinder` xử lý riêng để không bị hiểu
sai thành 0.

Khi triển khai thật, hãy sinh `machineKey` mới trong `Web.config`
(IIS Manager → Machine Key → Generate Keys) thay cho giá trị mẫu đang có.

## Cấu trúc

```
projectmanager/
├── TTKDGP.ProjectManager/    Web app ASP.NET MVC 5
│   ├── App_Data/             seed/ (dữ liệu mẫu), attachments/, docs/ (hướng dẫn tích hợp API)
│   ├── App_Start/            Cấu hình route, filter
│   ├── Content/               site.css, img/ (favicon), lib/ (select2, swagger-ui)
│   ├── Scripts/                app.js (khởi tạo Select2), lib/ (jquery, select2, chart.js)
│   ├── FileMoTa/                Đặc tả nghiệp vụ, hướng dẫn tích hợp API (Markdown + Postman)
│   ├── Controllers/              Home, Account, Projects, Members, Assignments, Users…
│   │   └── Api/                    Auth/Account/Dashboard/MyWork/MyProjects/Checklist/
│   │                                ProjectMembers/Notifications — API cho mobile
│   ├── Data/                        Repository (điểm truy cập chung) + SqlStore<T> (chính) +
│   │                                  JsonStore<T> (seed/kho nhỏ) + JsonToSqlMigration
│   ├── Infrastructure/                Xác thực, băm mật khẩu, Telegram, scheduler, helper view
│   ├── Models/                         Thực thể bộ gốc + ViewModel
│   │   └── Work/                        Entity bộ Quản lý công việc & KPI (WorkProject, WorkTask,
│   │                                      KpiConfig, LeaveRequest, ApiToken…)
│   └── Views/
└── Mobile-Flutter/           App mobile Flutter (BrewTask) — gọi API tại Controllers/Api/
    └── lib/
        ├── config/            api_endpoint.dart
        ├── core/                network, storage, theme dùng chung
        ├── features/             auth, dashboard, mywork, projects, checklist, leaves, kpi,
        │                          notifications, profile, team
        └── shared/                 widget App* dùng chung toàn app
```
