# Hệ thống quản lý dự án & nhân sự — Tổ NCPT

Ứng dụng web ASP.NET MVC 5 (.NET Framework 4.8) quản lý dự án, nhân sự và việc phân công
tham gia dự án. Dữ liệu lưu dưới dạng file JSON trong `App_Data`, không cần cài đặt CSDL.

## Chạy ứng dụng

Mở `TTKDGP.ProjectManager.sln` bằng Visual Studio 2022 rồi nhấn F5.

Hoặc chạy bằng dòng lệnh:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" TTKDGP.ProjectManager.sln /p:Configuration=Debug
& "C:\Program Files (x86)\IIS Express\iisexpress.exe" /path:"<đường dẫn tuyệt đối>\TTKDGP.ProjectManager" /port:52341
```

Truy cập http://localhost:52341

Lần đầu chạy trên máy mới, `App_Data` chưa có dữ liệu nên hệ thống tự chép từ
`App_Data/seed` sang — chạy được ngay, không cần thiết lập gì thêm.

## Tài khoản

Tài khoản **mẫu** đi kèm mã nguồn:

| Tên đăng nhập | Mật khẩu      | Quyền   |
|---------------|---------------|---------|
| `admin`       | `Admin@123`   | Admin   |
| `manager`     | `Manager@123` | Manager |

- **Admin** — toàn quyền, bao gồm tạo/sửa/xoá người dùng.
- **Manager** — cập nhật dự án, danh mục, thành viên, phân công; không vào được mục Người dùng.

> **Đổi mật khẩu hai tài khoản này ngay khi triển khai thật.** Đây là mật khẩu công khai
> trong mã nguồn, ai đọc repo cũng biết.

Mật khẩu được băm bằng PBKDF2-SHA256 (100.000 vòng, muối riêng từng tài khoản), không lưu
dạng văn bản thường. Đổi mật khẩu tại menu **Thông tin cá nhân**; Admin đặt lại hộ người khác
tại mục **Người dùng**.

## Chức năng

**Công khai — không cần đăng nhập**
- `/` Màn hình tổng hợp. Phần *Chi tiết tham gia* gom theo **từng thành viên**: mỗi khối là một
  người, bên trong liệt kê dự án / khách hàng / vai trò / ghi chú công việc / trạng thái.
  Dự án ở trạng thái ưu tiên cao nhất (mặc định *Đang thực hiện*) hiện ngay, phần còn lại
  gập sau nút **Xem thêm**. Có lọc theo từ khoá / thành viên / dự án / trạng thái, kèm số liệu
  thống kê và biểu đồ khối lượng.
- `/Home/Project/{id}` Chi tiết một dự án kèm danh sách thành viên.

**Cần đăng nhập**
- **Dự án** — khai báo dự án (khách hàng, PM, loại, trạng thái, hợp đồng, Redmine/SVN/Github…).
- **Thành viên** — nhân sự của tổ.
- **Phân công** — gán thành viên vào dự án kèm vai trò, nội dung công việc, trạng thái tham gia.
  Đây là nguồn dữ liệu cho màn hình tổng hợp. Mỗi phân công có nút **Tiến trình** dẫn tới timeline.
- **Tiến trình thực hiện** (`/WorkLogs/Timeline/{assignmentId}`) — nhật ký công việc **theo tuần**
  của một thành viên trên một dự án, dựng thành timeline. Xem công khai, ghi thì phải đăng nhập.
- **Danh mục** — bốn danh mục dùng chung, cùng một khung màn hình:
  *Loại dự án*, *Trạng thái dự án*, *Trạng thái tham gia*, *Vai trò*.
- **Thông tin cá nhân** — xem tài khoản, sửa họ tên, đổi mật khẩu (bấm vào tên mình ở góc phải).
- **Người dùng** (chỉ Admin) — tạo/sửa/xoá tài khoản, đặt lại mật khẩu, khoá tài khoản.

### Cách danh mục hoạt động

Bản ghi nghiệp vụ tham chiếu tới danh mục bằng **tên** chứ không bằng khoá. Do đó:

- **Đổi tên** một mục sẽ tự động cập nhật mọi bản ghi đang dùng tên cũ.
- **Không cho xoá** mục còn bản ghi sử dụng — nút Xoá bị khoá và có cảnh báo kèm số lượng.
- **Bỏ đánh dấu "Đang sử dụng"** để ẩn mục khỏi danh sách chọn mà vẫn giữ nguyên bản ghi cũ.
- Bản ghi đang mang giá trị đã bị ẩn/xoá vẫn giữ được giá trị đó khi mở ra sửa.

Toàn bộ danh sách chọn đều đọc từ danh mục, không có danh sách cứng nào trong mã nguồn.

Riêng danh mục **Trạng thái dự án** có thêm cột **Độ ưu tiên** (số nhỏ = ưu tiên cao). Giá trị này
quyết định thứ tự ở màn hình tổng hợp và dự án nào được hiện sẵn trước khi bấm *Xem thêm*.

### Nhật ký công việc theo tuần

Mỗi phân công có một chuỗi nhật ký, mỗi dòng ứng với **một tuần trong năm**: tuần đó làm gì,
trạng thái ra sao. Các dòng nối lại thành timeline theo dõi tiến trình.

- Tuần tính theo chuẩn **ISO 8601** (tuần bắt đầu Thứ Hai, tuần 1 là tuần chứa ngày 4/1).
  .NET Framework 4.8 không có sẵn lớp `ISOWeek` nên phần này tự cài đặt trong `WeekHelper`.
  Năm có 52 hoặc 53 tuần tuỳ năm — ví dụ 2026 có 53 tuần, 2025 và 2027 có 52.
- Dropdown chọn tuần hiển thị kèm khoảng ngày, ví dụ *Tuần 29 (13/07 – 19/07)*.
- Mỗi tuần chỉ được một dòng cho mỗi phân công; ghi trùng tuần sẽ bị từ chối.
- **Trạng thái và nội dung công việc của phân công luôn lấy theo dòng nhật ký mới nhất.**
  Thêm, sửa hay xoá nhật ký đều đồng bộ lại giá trị này, nên màn hình tổng hợp luôn phản ánh
  tình hình cập nhật nhất mà không phải nhập hai lần.
- Xoá một phân công hoặc một dự án sẽ xoá luôn nhật ký liên quan.

### Nhân sự luôn tham chiếu bằng Id

Mọi chỗ liên quan đến con người đều lưu **Id thành viên**, không lưu tên dạng chuỗi:

| Nơi dùng | Trường |
|----------|--------|
| PM phụ trách dự án | `Project.PmMemberId` — combobox |
| Nhân sự tham gia dự án | `Project.ParticipantIds` — chọn nhiều |
| Phân công | `ProjectMember.MemberId` — combobox |

Đổi tên một thành viên chỉ sửa ở một chỗ và hiện đúng ở mọi màn hình. Id không tồn tại bị loại
bỏ khi lưu. Thành viên đã nghỉ vẫn xuất hiện trong danh sách chọn (có ghi chú) để bản ghi cũ
không bị mất người phụ trách.

### Quản trị tài khoản

Họ tên và phân quyền được đọc lại từ dữ liệu ở mỗi request, không lấy trong cookie. Nhờ vậy
việc khoá tài khoản, đổi quyền hay đổi họ tên **có hiệu lực ngay**, kể cả với phiên đang mở.

## Dữ liệu

Mỗi loại bản ghi là một file JSON trong `App_Data`: `projects`, `members`, `projectMembers`,
`workLogs`, `users` và bốn danh mục `projectTypes` / `projectStatuses` / `workStatuses` /
`memberRoles`.

### Dữ liệu thật không nằm trong kho mã nguồn

`App_Data/*.json` được `.gitignore` loại trừ, vì đó là dữ liệu vận hành thật (tên dự án, khách
hàng, nhân sự, tài khoản). Kho mã chỉ chứa **dữ liệu mẫu** trong `App_Data/seed/`.

Khi `JsonStore` nạp một file mà file đó chưa tồn tại, nó tự chép bản mẫu cùng tên từ `seed/`
sang. Nhờ vậy máy mới clone về là chạy được ngay, còn dữ liệu thật thì không bao giờ bị đẩy
lên GitHub.

Muốn dựng lại dữ liệu mẫu sạch: xoá các file `.json` trong `App_Data` (giữ nguyên thư mục
`seed`) rồi chạy lại ứng dụng.

Ứng dụng **không** kết nối tới nguồn dữ liệu bên ngoài lúc chạy — mọi thao tác đọc/ghi đều
trên các file JSON cục bộ.

### Lưu ý khi thao tác dữ liệu

- Mỗi lần ghi, `JsonStore` ghi ra file tạm rồi thay thế nguyên tử và giữ lại bản `.bak`,
  nên file không bị hỏng nếu tiến trình dừng giữa chừng.
- Nên sao lưu thư mục `App_Data` định kỳ.
- Mô hình file JSON phù hợp ở quy mô vài trăm bản ghi như hiện tại. Nếu dữ liệu lớn lên đáng kể
  hoặc cần nhiều người ghi đồng thời, nên chuyển sang SQL Server.

## Dropdown có tìm kiếm (Select2)

Mọi `<select>` trong hệ thống được `Scripts/app.js` tự khởi tạo Select2, không phải khai báo
thủ công ở từng view. Quy tắc:

- Dropdown từ **8 lựa chọn trở lên** mới hiện ô tìm kiếm (dự án 51 mục, tuần 53 mục).
  Ít hơn thì bỏ ô tìm kiếm cho gọn.
- Lựa chọn đầu tiên có value rỗng (*— Tất cả —*, *— Chọn ... —*) được dùng làm gợi ý,
  kèm nút **×** để bỏ chọn nhanh.
- Thông báo hiển thị bằng tiếng Việt (*Không tìm thấy kết quả*, *Đang tìm…*).
- Muốn một ô nào đó giữ nguyên dropdown thường thì thêm thuộc tính `data-no-select2`.

**jQuery và Select2 được đóng gói sẵn trong project, không gọi CDN** — app chạy được cả khi
server không có internet. Nếu view nào cần script riêng chạy sau jQuery, đặt trong
`@section Scripts { ... }` chứ đừng viết thẳng vào thân view (thân view render trước thẻ script).

## Favicon

Ảnh gốc `FileContents/favicon.png` (1254×1254) được thu nhỏ thành các cỡ chuẩn trong
`Content/img/`, cộng thêm `favicon.ico` đa kích thước (16/32/48) đặt ở gốc site để trình duyệt
tự tìm thấy. Các thẻ khai báo nằm trong `_Layout.cshtml` nên mọi trang đều dùng chung.

Muốn đổi icon: thay `FileContents/favicon.png` rồi chạy lại script xuất ảnh, hoặc tự thay
trực tiếp các file trong `Content/img/` và `favicon.ico`.

Đuôi `.webmanifest` phải khai báo MIME trong `Web.config` (`application/manifest+json`),
nếu không IIS trả về 404.

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
   (ứng dụng ghi dữ liệu trực tiếp vào đây).
3. Sinh `machineKey` riêng: IIS Manager → chọn site → **Machine Key** → *Generate Keys* → Apply.
   Không có bước này thì mỗi lần khởi động lại ứng dụng, mọi người bị đăng xuất.
4. Đăng nhập bằng `admin` / `Admin@123` rồi **đổi mật khẩu ngay**.

## Lưu ý cho người phát triển

**File nguồn `.cs` và `.cshtml` phải lưu dạng UTF-8 có BOM.** Nếu thiếu BOM, trình biên dịch C#
và Razor sẽ đọc theo codepage ANSI của Windows và mọi chữ tiếng Việt viết thẳng trong mã nguồn
sẽ hiển thị sai (kiểu `LÆ°á»£t phÃ¢n cÃ´ng`). Visual Studio mặc định giữ BOM khi lưu; chỉ cần chú ý
nếu sửa file bằng công cụ khác.

Khi triển khai thật, hãy sinh `machineKey` mới trong `Web.config`
(IIS Manager → Machine Key → Generate Keys) thay cho giá trị mẫu đang có.

## Cấu trúc

```
TTKDGP.ProjectManager/
├── App_Data/          Dữ liệu JSON
├── App_Start/         Cấu hình route, filter
├── Content/           site.css (CSS tự viết)
│   ├── img/           Các cỡ favicon đã xuất
│   └── lib/           select2.min.css
├── Scripts/           app.js (khởi tạo Select2)
│   └── lib/           jquery.min.js, select2.min.js, select2.vi.js
├── FileContents/      Ảnh gốc do người dùng cung cấp (favicon.png 1254×1254)
├── favicon.ico        Icon đa kích thước 16/32/48 ở gốc site
├── site.webmanifest   Khai báo icon cho trình duyệt di động
├── Controllers/       Home, Account, Projects, Members, Assignments, Users
│                      CatalogControllerBase<T> + 4 danh mục kế thừa (CatalogControllers.cs)
├── Data/              JsonStore<T> (kho JSON) + Repository (điểm truy cập chung)
├── Infrastructure/    Xác thực, băm mật khẩu, helper cho view
├── Models/            Thực thể + ViewModel
└── Views/
```
