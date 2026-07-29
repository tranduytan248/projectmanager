# Đặc tả chức năng — Quản lý công việc & Chấm KPI

> **Tài liệu này dành cho AI đọc và xây dựng chức năng.** Viết cho phần mềm
> *Quản lý dự án & nhân sự* của Tổ Nghiên cứu Phát triển (NCPT).
> Mọi mục **[CHỐT?]** là chỗ đặc tả đang tự quyết một phương án hợp lý nhưng
> yêu cầu gốc chưa nói rõ — phải hỏi lại người dùng trước khi code.

---

## Mục lục

1. [Bối cảnh & phạm vi](#1-bối-cảnh--phạm-vi)
2. [Thuật ngữ](#2-thuật-ngữ)
3. [Vai trò](#3-vai-trò)
4. [Hiện trạng hệ thống](#4-hiện-trạng-hệ-thống)
5. [Mô hình dữ liệu](#5-mô-hình-dữ-liệu)
6. [Quy tắc nghiệp vụ](#6-quy-tắc-nghiệp-vụ)
7. [Công thức chấm chất lượng](#7-công-thức-chấm-chất-lượng)
8. [Danh sách Use Case](#8-danh-sách-use-case)
9. [Đặc tả chi tiết Use Case](#9-đặc-tả-chi-tiết-use-case)
10. [Màn hình & điều hướng](#10-màn-hình--điều-hướng)
11. [Quyền](#11-quyền)
12. [Ràng buộc kỹ thuật](#12-ràng-buộc-kỹ-thuật)
13. [Thứ tự triển khai](#13-thứ-tự-triển-khai)
14. [Những điểm cần chốt](#14-những-điểm-cần-chốt)

---

## 1. Bối cảnh & phạm vi

Tổ NCPT có một Quản lý Tổ, nhiều PM và nhiều nhân sự. Công việc chảy theo ba
nhánh:

```
Quản lý Tổ ──┬── lập dự án, chỉ định PM, phân công nhân sự
             ├── giao việc ngoài dự án (có hạn)
             └── duyệt KPI cuối cùng → gửi email kèm Excel
                                          ▲
PM ──────────┬── checklist công việc dự án (import Excel được)
             ├── phân công việc hỗ trợ đầu tuần                │
             ├── báo cáo tuần theo format chung                │
             └── đánh giá & duyệt KPI dự án mình quản lý ───────┘
                                          ▲
Nhân sự ─────┬── báo cáo tiến độ theo checklist                │
             ├── báo cáo việc hỗ trợ hàng tuần ────────────────┘
             └── trao đổi với PM trong từng mục checklist
```

**Trong phạm vi:** quản lý checklist công việc, phân công theo giai đoạn, báo
cáo tuần của PM, trao đổi trong công việc, việc ngoài dự án, và bộ chấm KPI
tháng có luồng duyệt hai cấp kèm kết xuất Excel gửi mail.

**Ngoài phạm vi:** chấm công, tính lương, quản lý hợp đồng/doanh thu, và cơ chế
đồng bộ nhân sự từ GoConnect (đã có sẵn, chỉ dùng lại).

---

## 2. Thuật ngữ

| Từ | Nghĩa trong tài liệu này |
|---|---|
| **Tổ** | Tổ Nghiên cứu Phát triển (NCPT) — đơn vị duy nhất trong phạm vi |
| **Quản lý Tổ** | Người đứng đầu Tổ. Một người. |
| **PM** | Người phụ trách một dự án cụ thể. Một người có thể là PM của nhiều dự án và đồng thời là nhân sự tham gia dự án khác. |
| **Nhân sự** | Thành viên của Tổ, bản ghi trong bảng `Members` |
| **Đầu việc** | Một đơn vị công việc có thể giao và có thể chấm — dùng chung cho cả ba loại dưới |
| **Việc checklist** | Đầu việc thuộc checklist của một dự án, có hạn hoàn thành |
| **Việc hỗ trợ** | Đầu việc hỗ trợ kỹ thuật cho khách hàng, phân theo tuần |
| **Việc ngoài dự án** | Đầu việc do Quản lý Tổ giao riêng, không thuộc dự án nào |
| **Tuần** | Tuần ISO. Hệ thống đã có `WeekHelper` để quy đổi năm+tuần ↔ khoảng ngày. |
| **Kỳ KPI** | Một tháng dương lịch, khoá theo `(Năm, Tháng)` |
| **Điểm chất lượng** | Số phần trăm chất lượng công việc của một nhân sự trong một kỳ KPI |

---

## 3. Vai trò

Hệ thống hiện có ba **nhóm quyền** cấu hình được: `Admin`, `Manager`,
`Reporter`. Ba vai trò nghiệp vụ trong tài liệu này ánh xạ như sau:

| Vai trò nghiệp vụ | Nhóm quyền | Cách xác định |
|---|---|---|
| Quản lý Tổ | `Manager` (hoặc `Admin`) | Theo nhóm quyền của tài khoản |
| PM | `Reporter` + là PM của dự án | **Theo ngữ cảnh**: `Project.PmMemberId == tài khoản đang đăng nhập` |
| Nhân sự | `Reporter` | Theo nhóm quyền |

> **Điểm cốt lõi:** "PM" **không phải** một nhóm quyền toàn cục. Cùng một tài
> khoản là PM ở dự án A nhưng chỉ là nhân sự ở dự án B. Vì vậy mọi kiểm tra
> quyền của PM đều phải là **hai lớp**:
>
> 1. Lớp 1 — `[AppAuthorize(Permission = "...")]` chặn ở mức chức năng.
> 2. Lớp 2 — trong action, kiểm `IsPmOf(projectId, currentMemberId)`; không
>    phải PM thì trả `HttpNotFound()` (không phải `Forbidden`, để không lộ sự
>    tồn tại của dự án).
>
> Bỏ lớp 2 là lỗ hổng: PM dự án A sửa được checklist dự án B chỉ bằng cách đổi
> `id` trên thanh địa chỉ.

Ánh xạ tài khoản ↔ nhân sự: bảng `Users` và `Members` hiện là **hai bảng rời**.
Phải có đường nối để biết "người đang đăng nhập là nhân sự nào".
→ Xem [§5.1](#51-thay-đổi-trên-bảng-hiện-có).

---

## 4. Hiện trạng hệ thống

Nền tảng: **ASP.NET MVC 5, .NET Framework 4.8, SQL Server**. Không dùng Entity
Framework — tầng dữ liệu là `SqlStore<T>` tự viết.

### 4.1. Đã có, dùng lại được

| Thành phần | Đường dẫn | Ghi chú |
|---|---|---|
| `Project` | `Models\Project.cs` | Tên, PM, khách hàng, loại, trạng thái, link Redmine/Git/SVN |
| `Member` | `Models\Member.cs` | Chỉ có `FullName`, `Email`, `IsActive` |
| `ProjectMember` | `Models\ProjectMember.cs` | Phân công; **chỉ có `IsActive`, chưa có mốc thời gian** |
| `WorkLog` | `Models\WorkLog.cs` | Nhật ký tuần theo *phân công* (không theo đầu việc) |
| `User`, `RoleGroup` | `Models\User.cs`, `RoleGroup.cs` | Đăng nhập + nhóm quyền cấu hình được |
| Danh mục | `Models\CatalogItem.cs` | `ProjectTypes`, `ProjectStatuses`, `WorkStatuses`, `MemberRoles` |
| Phân quyền | `Models\Permission.cs` | Mã dạng `module.action`, menu động theo quyền |
| `SqlStore<T>` | `Data\SqlStore.cs` | Tự tạo bảng, suy cột từ property |
| `WeekHelper` | `Infrastructure\WeekHelper.cs` | Quy đổi năm+tuần ↔ ngày |
| `EmailClient` | `Infrastructure\EmailClient.cs` | Gửi HTML — **chưa đính kèm được file** |
| `TelegramClient` | `Infrastructure\TelegramClient.cs` | Gửi tin nhóm |
| `ReminderScheduler` | `Infrastructure\ReminderScheduler.cs` | Lịch chạy nền trong tiến trình IIS |
| HRM | `Data\HrEmployeeStore.cs`, `HrOrgStore.cs` | Nhân sự đồng bộ từ GoConnect |

### 4.2. Chưa có, phải làm mới

- Checklist công việc trong dự án, và mọi khái niệm "đầu việc có hạn".
- Trao đổi kiểu diễn đàn gắn với công việc.
- Việc ngoài dự án.
- Phân công theo khoảng thời gian (vào/ra giữa chừng).
- Báo cáo tuần của PM theo format ba phần.
- Phân công việc hỗ trợ theo tuần.
- Toàn bộ bộ chấm KPI, luồng duyệt hai cấp, kết xuất Excel, gửi mail đính kèm.
- Import Excel và tải file mẫu.

### 4.3. Khả năng và giới hạn của `SqlStore<T>`

Đây là ràng buộc quan trọng nhất khi thiết kế bảng mới.

**Kiểu dữ liệu được hỗ trợ** (`Data\SqlStore.cs:65-92`):

| Kiểu C# | Cột SQL |
|---|---|
| `int`, `short`, `byte` | `INT` |
| `long` | `BIGINT` |
| `bool` | `BIT` |
| `DateTime` | `DATETIME2` |
| `decimal` | `DECIMAL(18,2)` |
| `double`, `float` | `FLOAT` |
| `Guid` | `UNIQUEIDENTIFIER` |
| `string` | `NVARCHAR(MAX)` |
| `List<int>` | `NVARCHAR(MAX)` (chuỗi JSON) |
| Kiểu `Nullable<>` của các kiểu trên | như trên, cho phép NULL |

**Giới hạn — phải thiết kế quanh những điều này:**

- **Không có khoá ngoại, không có join.** Quan hệ chỉ là cột `int` trỏ tới `Id`
  bảng khác. Muốn ghép dữ liệu thì đọc cả hai bảng lên bộ nhớ rồi ghép bằng
  LINQ, như `Repository.BuildSummaryRows()` đang làm.
- **Không phân trang ở tầng SQL.** `All()` nạp toàn bộ bảng. Với bảng dự kiến
  lớn (bình luận, dòng chi tiết KPI) phải tự chặn khối lượng — xem
  [§12.2](#122-hiệu-năng).
- **Không có transaction nhiều bảng.** Thao tác ghi nhiều bảng phải viết sao
  cho chạy lại được (idempotent) và tự dọn nếu đứt giữa chừng.
- **Bảng tự tạo khi khởi động, cột mới tự thêm.** Thêm property vào entity là
  đủ; **không** cần script migration. Nhưng **đổi tên hoặc xoá property thì cột
  cũ vẫn nằm lại** — không tự dọn.

---

## 5. Mô hình dữ liệu

### 5.1. Thay đổi trên bảng hiện có

#### `Member` — nối với tài khoản đăng nhập

```csharp
/// <summary>Tài khoản đăng nhập tương ứng. 0 nghĩa là chưa nối.</summary>
public int UserId { get; set; }
```

> **[CHỐT?]** Có thể nối theo `Email` thay vì thêm cột (`Member.Email` ==
> `User.Email`). Nối theo Id chắc chắn hơn vì email đổi được. Đặc tả này chọn
> **thêm cột `UserId`**, kèm một màn hình để Quản lý Tổ ghép thủ công những
> bản ghi chưa nối.

#### `ProjectMember` — phân công theo giai đoạn

```csharp
/// <summary>Ngày bắt đầu tham gia. Bắt buộc.</summary>
public DateTime JoinedAt { get; set; }

/// <summary>Ngày rời dự án. NULL nghĩa là vẫn đang tham gia.</summary>
public DateTime? LeftAt { get; set; }
```

Quy tắc: `IsActive` trở thành **giá trị suy ra** — `LeftAt == null`. Giữ lại cột
cũ để màn hình hiện tại không vỡ, nhưng mỗi lần ghi phải đồng bộ hai bên.

Một người rời rồi quay lại dự án → **thêm bản ghi `ProjectMember` mới**, không
sửa bản ghi cũ. Nhờ vậy lịch sử tham gia còn nguyên và KPI tra được "tháng đó ai
đang ở trong dự án".

#### `Project` — phân loại công việc

```csharp
/// <summary>
/// Loại công việc chính của dự án: "HoTro" (hỗ trợ kỹ thuật cho khách hàng)
/// hoặc "Checklist" (thực hiện chức năng theo checklist).
/// Chỉ là giá trị mặc định khi tạo đầu việc — đầu việc vẫn tự mang loại riêng.
/// </summary>
public string WorkKind { get; set; }
```

> **[CHỐT?]** Yêu cầu gốc viết *"Mỗi dự án sẽ có 2 loại là Hỗ trợ kỹ thuật cho
> khách hàng và thực hiện các chức năng trên checklist"*. Hiểu theo hai cách:
> (a) mỗi dự án **thuộc** một trong hai loại; (b) mỗi dự án **chứa cả hai** loại
> việc. Đặc tả chọn cách an toàn cho cả hai: phân loại đặt ở **đầu việc**, còn
> `Project.WorkKind` chỉ là giá trị mặc định gợi ý. Dự án lai vẫn chạy đúng.

### 5.2. Bảng mới

#### `WorkItem` — đầu việc (bảng `WorkItems`)

Một bảng dùng chung cho cả ba loại việc. Gộp lại thay vì tách ba bảng vì màn
"việc của tôi" và bộ chấm KPI đều phải duyệt qua cả ba.

```csharp
public class WorkItem : IEntity
{
    public int Id { get; set; }

    /// <summary>Loại việc: "Checklist" | "HoTro" | "NgoaiDuAn". Quyết định cách chấm điểm.</summary>
    public string Kind { get; set; }

    /// <summary>Dự án chứa đầu việc. 0 với việc ngoài dự án.</summary>
    public int ProjectId { get; set; }

    /// <summary>Mã việc do người dùng đặt, ví dụ "CL-01". Không bắt buộc, không cần duy nhất toàn hệ thống.</summary>
    public string Code { get; set; }

    /// <summary>Tên đầu việc. Bắt buộc, tối đa 250 ký tự.</summary>
    public string Title { get; set; }

    /// <summary>Mô tả chi tiết. Không bắt buộc.</summary>
    public string Description { get; set; }

    /// <summary>Mục cha để gom nhóm checklist nhiều cấp. 0 là mục gốc.</summary>
    public int ParentId { get; set; }

    /// <summary>Thứ tự hiển thị trong cùng một cấp.</summary>
    public int SortOrder { get; set; }

    /// <summary>Nhân sự thực hiện. 0 nghĩa là chưa giao.</summary>
    public int AssigneeMemberId { get; set; }

    /// <summary>Người giao việc.</summary>
    public int AssignedByMemberId { get; set; }

    public DateTime? StartDate { get; set; }

    /// <summary>Hạn hoàn thành. Bắt buộc với việc checklist và việc ngoài dự án.</summary>
    public DateTime? DueDate { get; set; }

    /// <summary>Thời điểm được đánh dấu hoàn thành. NULL nghĩa là chưa xong.</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Trạng thái: "ChuaBatDau" | "DangLam" | "TamDung" | "HoanThanh" | "Huy".</summary>
    public string Status { get; set; }

    /// <summary>Phần trăm hoàn thành, 0-100.</summary>
    public int Progress { get; set; }

    // ----- Chỉ dùng cho việc hỗ trợ (Kind = "HoTro") -----

    /// <summary>Năm của tuần được phân việc hỗ trợ. 0 với loại việc khác.</summary>
    public int Year { get; set; }

    /// <summary>Tuần ISO được phân việc hỗ trợ. 0 với loại việc khác.</summary>
    public int Week { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string UpdatedBy { get; set; }
}
```

#### `WorkItemComment` — trao đổi trong đầu việc (bảng `WorkItemComments`)

```csharp
public class WorkItemComment : IEntity
{
    public int Id { get; set; }
    public int WorkItemId { get; set; }

    /// <summary>Người viết.</summary>
    public int MemberId { get; set; }

    /// <summary>Tên người viết tại thời điểm viết — giữ lại phòng khi nhân sự bị xoá.</summary>
    public string MemberName { get; set; }

    /// <summary>Nội dung. Lưu văn bản thuần, hiển thị phải HTML-encode.</summary>
    public string Content { get; set; }

    /// <summary>Bình luận cha để trả lời lồng nhau. 0 là bình luận gốc.</summary>
    public int ParentId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Đã thu hồi. Giữ dòng để mạch hội thoại không đứt.</summary>
    public bool IsDeleted { get; set; }
}
```

#### `WeeklyReport` — báo cáo tuần của PM (bảng `WeeklyReports`)

```csharp
public class WeeklyReport : IEntity
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int Year { get; set; }
    public int Week { get; set; }

    /// <summary>Phần 1 — Công việc đang thực hiện.</summary>
    public string CurrentWork { get; set; }

    /// <summary>Phần 2 — Khó khăn, vướng mắc.</summary>
    public string Difficulties { get; set; }

    /// <summary>
    /// Phần 3 — Tiến độ tuần tiếp theo, dạng văn bản tự do.
    /// Danh sách mục checklist được chọn nằm ở bảng WeeklyReportPlanItems.
    /// </summary>
    public string NextWeekNote { get; set; }

    /// <summary>Thời điểm nộp. NULL nghĩa là còn là bản nháp.</summary>
    public DateTime? SubmittedAt { get; set; }
    public int SubmittedByMemberId { get; set; }

    /// <summary>Nộp đúng hạn hay không — tính một lần lúc nộp rồi chốt lại.</summary>
    public bool IsOnTime { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

Khoá nghiệp vụ: `(ProjectId, Year, Week)` — mỗi dự án mỗi tuần đúng một báo cáo.
`SqlStore` không tạo unique index, nên **phải tự kiểm trùng trước khi ghi**.

#### `WeeklyReportPlanItem` — kế hoạch tuần tới (bảng `WeeklyReportPlanItems`)

```csharp
public class WeeklyReportPlanItem : IEntity
{
    public int Id { get; set; }
    public int WeeklyReportId { get; set; }

    /// <summary>Mục checklist được chọn đưa vào kế hoạch tuần tới.</summary>
    public int WorkItemId { get; set; }

    /// <summary>Hạn hoàn thành PM cam kết cho mục này.</summary>
    public DateTime CommittedDueDate { get; set; }

    public string Note { get; set; }
}
```

Khi PM chọn một mục vào kế hoạch, hệ thống **ghi đè `WorkItem.DueDate` bằng
`CommittedDueDate`** để chỉ có một nguồn sự thật về hạn khi chấm KPI.

#### `KpiEvaluation` — phiếu chấm một người một tháng (bảng `KpiEvaluations`)

```csharp
public class KpiEvaluation : IEntity
{
    public int Id { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public int MemberId { get; set; }

    // ----- Điểm hệ thống tự tính -----
    public decimal ChecklistScore { get; set; }   // 0-100, điểm thành phần checklist
    public decimal SupportScore { get; set; }     // 0-100, điểm thành phần hỗ trợ
    public decimal BaseScore { get; set; }        // sau khi áp trọng số
    public decimal ExtraTaskBonus { get; set; }   // cộng từ việc ngoài dự án
    public decimal PmBonus { get; set; }          // cộng 2% cho PM làm đúng hạn
    public decimal SystemTotal { get; set; }      // BaseScore + ExtraTaskBonus + PmBonus

    // ----- Điểm sau điều chỉnh -----
    /// <summary>Điểm cuối cùng. Khởi tạo bằng SystemTotal, người duyệt sửa được.</summary>
    public decimal FinalTotal { get; set; }

    /// <summary>Lý do điều chỉnh. Bắt buộc khi FinalTotal khác SystemTotal.</summary>
    public string AdjustReason { get; set; }

    // ----- Luồng duyệt -----
    /// <summary>"Nhap" | "ChoPmDuyet" | "PmDaDuyet" | "ToTruongDaDuyet" | "DaGui".</summary>
    public string Status { get; set; }

    public string PmComment { get; set; }
    public int PmApprovedByMemberId { get; set; }
    public DateTime? PmApprovedAt { get; set; }

    public string ManagerComment { get; set; }
    public int ManagerApprovedByMemberId { get; set; }
    public DateTime? ManagerApprovedAt { get; set; }

    public DateTime? SentAt { get; set; }
    public string SentTo { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

#### `KpiEvaluationLine` — dòng chi tiết (bảng `KpiEvaluationLines`)

Chụp lại từng đầu việc tại thời điểm chốt kỳ. **Sao chép giá trị, không trỏ
động** — sửa đầu việc sau khi chốt không được làm đổi bảng KPI đã duyệt.

```csharp
public class KpiEvaluationLine : IEntity
{
    public int Id { get; set; }
    public int KpiEvaluationId { get; set; }

    public int WorkItemId { get; set; }
    public string Kind { get; set; }           // Checklist | HoTro | NgoaiDuAn
    public int ProjectId { get; set; }
    public string ProjectName { get; set; }    // chụp lại tên
    public string Title { get; set; }          // chụp lại tên việc

    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }

    /// <summary>Số ngày thực hiện, dùng để xếp bậc điểm cộng.</summary>
    public int DurationDays { get; set; }

    public bool IsOnTime { get; set; }

    /// <summary>Điểm dòng này đóng góp vào tổng.</summary>
    public decimal Score { get; set; }

    /// <summary>Nội dung người duyệt sửa lại. Rỗng nghĩa là giữ nguyên Title.</summary>
    public string AdjustedTitle { get; set; }

    public string Note { get; set; }
}
```

### 5.3. Sơ đồ quan hệ

```
Members ──1:n── ProjectMembers ──n:1── Projects ──1:n── WorkItems ──1:n── WorkItemComments
   │                                       │                 │
   │                                       │                 └── (Kind, DueDate, CompletedAt)
   │                                       │                            │
   │                                       └── WeeklyReports ──1:n── WeeklyReportPlanItems
   │                                                                    │
   └──1:n── KpiEvaluations ──1:n── KpiEvaluationLines ◄─── chụp từ ─────┘

Users ──1:1── Members        (qua Member.UserId)
```

---

## 6. Quy tắc nghiệp vụ

Đánh mã `BR-xx` để Use Case tham chiếu tới.

| Mã | Quy tắc |
|---|---|
| **BR-01** | Chỉ Quản lý Tổ được Thêm/Sửa/Xoá dự án và chỉ định PM. |
| **BR-02** | Chỉ PM của chính dự án đó (hoặc Quản lý Tổ) được sửa checklist, phân công hỗ trợ, và nộp báo cáo tuần của dự án đó. |
| **BR-03** | Nhân sự chỉ thấy và cập nhật đầu việc được giao cho mình; chỉ thấy dự án mình đang hoặc đã từng tham gia. |
| **BR-04** | Việc checklist và việc ngoài dự án **bắt buộc có `DueDate`**. Việc hỗ trợ bắt buộc có `(Year, Week)`. |
| **BR-05** | Một đầu việc **đúng hạn** khi `CompletedAt <= DueDate` (so theo ngày, bỏ qua giờ). Chưa hoàn thành mà đã quá hạn thì tính là **trễ**. |
| **BR-06** | Báo cáo tuần của PM **đúng hạn** khi `SubmittedAt` nằm trước mốc hạn tuần đó — mặc định **17:00 thứ Sáu** của chính tuần báo cáo. [CHỐT?] |
| **BR-07** | Việc hỗ trợ được phân **từ đầu tuần**; hệ thống chặn tạo mới việc hỗ trợ cho một tuần sau khi tuần đó đã kết thúc, trừ khi Quản lý Tổ tự mở. |
| **BR-08** | Xoá dự án **không xoá cứng** đầu việc, bình luận, báo cáo tuần. Đánh dấu dự án ngừng và giữ nguyên dữ liệu để KPI kỳ cũ vẫn tra được. |
| **BR-09** | Kỳ KPI đã ở trạng thái `ToTruongDaDuyet` hoặc `DaGui` thì **khoá**, không sinh lại và không sửa được. Muốn sửa phải mở khoá, và thao tác mở khoá phải ghi lại. |
| **BR-10** | Một nhân sự trong một kỳ KPI có **đúng một** `KpiEvaluation`. Người tham gia nhiều dự án vẫn chỉ một phiếu, gộp điểm mọi dự án. |
| **BR-11** | Nhân sự không có đầu việc nào đến hạn trong kỳ thì **không chấm** (`SystemTotal = null`, ghi rõ "Không có việc trong kỳ"), không phải chấm 0 điểm. |
| **BR-12** | Chỉ đầu việc có **hạn rơi vào trong kỳ** mới được tính vào kỳ đó. Việc kéo dài nhiều tháng chỉ tính ở tháng chứa hạn. |
| **BR-13** | Bình luận đã đăng không xoá cứng; đánh dấu `IsDeleted` và hiện "Nội dung đã thu hồi". |
| **BR-14** | Import checklist là **thêm mới**, không ghi đè. Dòng có `Code` trùng với mục đã có trong cùng dự án thì bỏ qua và báo lại cho người dùng. [CHỐT?] |

---

## 7. Công thức chấm chất lượng

Đây là phần dễ hiểu sai nhất — đặc tả kỹ và có ví dụ kiểm chứng.

### 7.1. Ba thành phần

Với nhân sự **M** trong kỳ **(Năm Y, Tháng T)**:

```
C = { đầu việc Kind="Checklist",  AssigneeMemberId = M, DueDate thuộc tháng T }
S = { đầu việc Kind="HoTro",      AssigneeMemberId = M, (Year,Week) thuộc tháng T }
A = { đầu việc Kind="NgoaiDuAn",  AssigneeMemberId = M, DueDate thuộc tháng T }
```

**Điểm thành phần checklist**

```
P_c = 100 × (số việc trong C đúng hạn) / |C|          — nếu |C| > 0
P_c = không xác định                                   — nếu |C| = 0
```

**Điểm thành phần hỗ trợ**

```
P_s = 100 × (số việc trong S đúng hạn) / |S|          — nếu |S| > 0
P_s = không xác định                                   — nếu |S| = 0
```

Việc hỗ trợ tính "đúng hạn" theo **báo cáo tuần**: nhân sự có ghi nhận tiến độ
cho tuần đó trước khi tuần kết thúc.

### 7.2. Áp trọng số

Yêu cầu gốc: *checklist 80%, hỗ trợ 20%, báo cáo đúng tiến độ thì đủ 100%*.

```
Nếu có cả C và S:   BaseScore = 0.8 × P_c + 0.2 × P_s
Nếu chỉ có C:       BaseScore = P_c
Nếu chỉ có S:       BaseScore = P_s
Nếu không có gì:    Không chấm (BR-11)
```

> **[CHỐT?]** Cách co giãn trọng số ở trên là suy luận, không có trong yêu cầu
> gốc. Cách khác là **giữ cứng 80/20**, khi đó người cả tháng chỉ làm việc hỗ
> trợ chỉ đạt tối đa 20 điểm. Cách cứng gần như chắc chắn không phải ý người
> dùng, nhưng phải hỏi lại vì nó đổi hẳn kết quả chấm.

### 7.3. Điểm cộng việc ngoài dự án

Chỉ tính việc **hoàn thành đúng hạn** trong `A`. Với mỗi việc:

```
DurationDays = (DueDate - StartDate).Days + 1     — tính cả ngày đầu và ngày cuối

DurationDays <= 3   →  +1%
DurationDays 4..7   →  +2%
DurationDays >= 8   →  +3%

ExtraTaskBonus = tổng điểm cộng của mọi việc trong A đúng hạn
```

> **[CHỐT?]** Yêu cầu gốc viết *"dưới 3 ngày là 1%, từ 4-7 ngày là 2%, trên 7
> ngày là 3%"* — **hở đúng hai chỗ**: việc dài **đúng 3 ngày** và việc dài
> **đúng 7 ngày** không thuộc bậc nào. Đặc tả vá lại thành `≤3`, `4–7`, `≥8`.
> Cần xác nhận đây đúng là ý người dùng.

> **[CHỐT?]** Chưa rõ có **trần** cho `ExtraTaskBonus` không. Giao 20 việc ngắn
> thì cộng thêm 20%. Đặc tả tạm **không đặt trần**, nhưng nêu rõ ở đây vì đây là
> chỗ dễ bị lạm dụng nhất của cả bộ chấm.

### 7.4. Điểm cộng PM

```
PmBonus = 2   nếu M là PM của ít nhất một dự án trong kỳ
              VÀ với MỌI dự án M quản lý trong kỳ, cả ba việc dưới đều đúng hạn:
                 (a) cập nhật checklist
                 (b) lập kế hoạch tuần (WeeklyReportPlanItems có dòng)
                 (c) nộp báo cáo tuần (BR-06)
PmBonus = 0   trong các trường hợp còn lại
```

> **[CHỐT?]** Hai điều chưa rõ: (1) 2% là **một lần** hay **mỗi dự án**? Đặc tả
> chọn **một lần**. (2) Thế nào là "cập nhật checklist đúng hạn"? Đặc tả tạm
> hiểu là *trong tuần có ít nhất một lần sửa checklist của dự án*, nhưng đây là
> định nghĩa yếu, cần người dùng mô tả rõ hơn.

### 7.5. Tổng

```
SystemTotal = BaseScore + ExtraTaskBonus + PmBonus
FinalTotal  = SystemTotal, cho tới khi người duyệt sửa
```

`SystemTotal` **có thể vượt 100**. [CHỐT?] Có chặn trần 100 không?

### 7.6. Ví dụ kiểm chứng

Dùng đúng bộ số này làm ca kiểm thử khi code.

**Ví dụ 1 — nhân sự làm cả hai loại việc**

| Dữ liệu | Giá trị |
|---|---|
| Việc checklist đến hạn trong tháng | 10, trong đó 9 đúng hạn |
| Tuần được phân việc hỗ trợ | 4, trong đó 4 báo cáo đúng hạn |
| Việc ngoài dự án | 2 việc: một việc 2 ngày, một việc 10 ngày, cả hai đúng hạn |
| Có phải PM không | Không |

```
P_c = 100 × 9/10 = 90
P_s = 100 × 4/4  = 100
BaseScore      = 0.8 × 90 + 0.2 × 100 = 72 + 20 = 92
ExtraTaskBonus = 1 (việc 2 ngày) + 3 (việc 10 ngày) = 4
PmBonus        = 0
SystemTotal    = 96
```

**Ví dụ 2 — PM chỉ làm việc checklist**

| Dữ liệu | Giá trị |
|---|---|
| Việc checklist đến hạn | 5, cả 5 đúng hạn |
| Việc hỗ trợ | Không có |
| Việc ngoài dự án | Không có |
| Là PM 2 dự án, cả hai đều đúng hạn cả ba mục | Có |

```
P_c = 100, P_s = không xác định
BaseScore   = 100        (chỉ có C nên lấy nguyên P_c)
PmBonus     = 2          (một lần, không nhân đôi theo số dự án)
SystemTotal = 102
```

**Ví dụ 3 — nhân sự chỉ làm việc hỗ trợ**

| Dữ liệu | Giá trị |
|---|---|
| Việc checklist | Không có |
| Tuần hỗ trợ | 4, trong đó 3 đúng hạn |

```
P_s = 75
BaseScore   = 75         (chỉ có S — KHÔNG phải 0.2 × 75 = 15)
SystemTotal = 75
```

Ví dụ 3 chính là chỗ hai cách hiểu ở [§7.2](#72-áp-trọng-số) cho kết quả khác
nhau: **75** theo cách co giãn, **15** theo cách giữ cứng 80/20.

---

## 8. Danh sách Use Case

| Mã | Tên | Vai trò chính | Ưu tiên |
|---|---|---|---|
| **UC-01** | Xem bảng điều khiển Tổ | Quản lý Tổ | Cao |
| **UC-02** | Quản lý dự án (Thêm/Sửa/Xoá) | Quản lý Tổ | Đã có, cần bổ sung |
| **UC-03** | Chỉ định PM cho dự án | Quản lý Tổ | Đã có, cần bổ sung |
| **UC-04** | Phân công nhân sự theo giai đoạn | Quản lý Tổ, PM | Cao |
| **UC-05** | Xem tổng hợp tiến độ dự án & công việc tuần | Quản lý Tổ | Cao |
| **UC-06** | Giao việc ngoài dự án | Quản lý Tổ | Cao |
| **UC-07** | Tạo đầu việc có hạn trong dự án | Quản lý Tổ, PM | Cao |
| **UC-08** | Theo dõi tiến độ checklist | Quản lý Tổ | Cao |
| **UC-09** | Duyệt KPI lần cuối & gửi email | Quản lý Tổ | Cao |
| **UC-10** | Cập nhật tiến độ dự án | PM | Trung bình |
| **UC-11** | Quản lý checklist công việc | PM | Cao |
| **UC-12** | Import checklist từ Excel | PM | Cao |
| **UC-13** | Tải file mẫu import | PM | Cao |
| **UC-14** | Cập nhật nhân sự tham gia theo giai đoạn | PM | Cao |
| **UC-15** | Lập báo cáo tuần dự án | PM | Cao |
| **UC-16** | Chọn mục checklist cho kế hoạch tuần tới | PM | Cao |
| **UC-17** | Trao đổi trong đầu việc | PM, Nhân sự | Trung bình |
| **UC-18** | Phân công việc hỗ trợ đầu tuần | PM | Cao |
| **UC-19** | PM đánh giá & duyệt KPI dự án mình | PM | Cao |
| **UC-20** | Xem dự án đang tham gia | Nhân sự | Trung bình |
| **UC-21** | Xem danh sách việc được giao | Nhân sự | Cao |
| **UC-22** | Báo cáo tiến độ việc checklist | Nhân sự | Cao |
| **UC-23** | Báo cáo việc hỗ trợ hàng tuần | Nhân sự | Cao |
| **UC-24** | Xem điểm chất lượng của mình | Nhân sự | Thấp |
| **UC-25** | Sinh bảng KPI tháng | Hệ thống | Cao |
| **UC-26** | Điều chỉnh nội dung công việc trong KPI | PM, Quản lý Tổ | Cao |
| **UC-27** | Kết xuất KPI ra Excel | Quản lý Tổ | Cao |
| **UC-28** | Nhắc hạn tự động | Hệ thống | Thấp |

---

## 9. Đặc tả chi tiết Use Case

Định dạng thống nhất: **Tác nhân → Tiền điều kiện → Luồng chính → Luồng phụ →
Hậu điều kiện → Quy tắc áp dụng**.

### UC-01 — Xem bảng điều khiển Tổ

**Tác nhân:** Quản lý Tổ
**Tiền điều kiện:** Đã đăng nhập, có quyền `team.view`.

**Luồng chính**
1. Người dùng mở màn hình *Bảng điều khiển Tổ*.
2. Hệ thống hiện, tính cho **tuần hiện tại**:
   - Bảng **Ai đang làm gì**: mỗi dòng một nhân sự đang hoạt động, kèm danh sách
     dự án đang tham gia, PM của từng dự án, số đầu việc đang mở, số đầu việc
     quá hạn.
   - Bảng **Dự án**: tên dự án, PM, trạng thái, tiến độ checklist (số việc xong
     / tổng), báo cáo tuần này đã nộp chưa.
   - Ô cảnh báo: đầu việc quá hạn, báo cáo tuần chưa nộp, nhân sự chưa được giao
     việc nào.
3. Người dùng bấm vào một nhân sự → sang UC-21 lọc theo người đó; bấm vào một dự
   án → sang màn chi tiết dự án.

**Luồng phụ**
- 2a. Không có dữ liệu tuần này → hiện trạng thái rỗng kèm nút chuyển tuần khác.

**Hậu điều kiện:** Không đổi dữ liệu.
**Quy tắc:** BR-01.

---

### UC-04 — Phân công nhân sự theo giai đoạn

**Tác nhân:** Quản lý Tổ, PM (chỉ dự án mình)
**Tiền điều kiện:** Dự án tồn tại; người dùng qua kiểm tra hai lớp ở [§3](#3-vai-trò).

**Luồng chính**
1. Mở màn *Nhân sự dự án*. Hệ thống hiện danh sách `ProjectMember` của dự án,
   kèm cột **Từ ngày** và **Đến ngày**, sắp theo `JoinedAt` giảm dần.
2. Bấm **Thêm nhân sự**. Chọn nhân sự, vai trò, **Từ ngày** (mặc định hôm nay).
3. Hệ thống kiểm: nhân sự này đã có bản ghi nào **đang mở** (`LeftAt == null`)
   trong dự án chưa.
4. Lưu bản ghi mới với `LeftAt = null`, `IsActive = true`.

**Luồng phụ**
- 3a. Đã có bản ghi đang mở → báo lỗi *"Nhân sự này đang tham gia dự án từ
  {JoinedAt}. Muốn phân công lại thì kết thúc giai đoạn cũ trước."* và dừng.
- **Rút nhân sự ra:** bấm **Kết thúc tham gia**, nhập **Đến ngày**
  (phải `>= JoinedAt`). Hệ thống đặt `LeftAt`, `IsActive = false`.
- **Bổ sung lại:** thêm mới như bước 2, tạo bản ghi thứ hai. Lịch sử giữ nguyên.
- 2a. Đầu việc đang giao cho người sắp bị rút ra → hiện cảnh báo kèm số lượng và
  hỏi có chuyển sang người khác không. Không chặn.

**Hậu điều kiện:** Lịch sử tham gia đủ để KPI truy vấn "tháng T ai ở trong dự án".
**Quy tắc:** BR-02.

---

### UC-06 — Giao việc ngoài dự án

**Tác nhân:** Quản lý Tổ
**Tiền điều kiện:** Có quyền `workitems.create`.

**Luồng chính**
1. Mở *Việc ngoài dự án* → **Giao việc mới**.
2. Nhập: tên việc (bắt buộc), mô tả, nhân sự thực hiện (bắt buộc), ngày bắt đầu
   (mặc định hôm nay), **hạn hoàn thành** (bắt buộc).
3. Hệ thống tính trước và **hiện ngay** bậc điểm cộng theo [§7.3](#73-điểm-cộng-việc-ngoài-dự-án),
   ví dụ *"Việc 5 ngày — cộng 2% nếu hoàn thành đúng hạn"*.
4. Lưu với `Kind = "NgoaiDuAn"`, `ProjectId = 0`, `Status = "ChuaBatDau"`.
5. Gửi thông báo cho nhân sự được giao.

**Luồng phụ**
- 2a. Hạn trước ngày bắt đầu → báo lỗi, không lưu.
- 2b. Nhân sự đã nghỉ (`Member.IsActive == false`) → không cho chọn.

**Hậu điều kiện:** Đầu việc hiện trong màn *Việc của tôi* của nhân sự (UC-21) và
được tính vào `ExtraTaskBonus` kỳ chứa hạn.
**Quy tắc:** BR-04, BR-12.

---

### UC-11 — Quản lý checklist công việc

**Tác nhân:** PM (chỉ dự án mình), Quản lý Tổ
**Tiền điều kiện:** Dự án tồn tại; qua kiểm tra hai lớp.

**Luồng chính**
1. Mở dự án → thẻ **Checklist**. Hệ thống hiện cây đầu việc theo `ParentId` và
   `SortOrder`, mỗi dòng: mã, tên, người thực hiện, hạn, trạng thái, tiến độ, số
   bình luận.
2. **Thêm mục**: nhập tên (bắt buộc), mã, mô tả, mục cha, người thực hiện, ngày
   bắt đầu, **hạn (bắt buộc)**.
3. **Sửa mục**: sửa mọi trường trên. Đổi người thực hiện thì thông báo cho cả
   người cũ và người mới.
4. **Xoá mục**: chỉ xoá được khi **chưa có bình luận và chưa từng vào bảng KPI
   đã duyệt**. Ngược lại chỉ cho chuyển `Status = "Huy"`.
5. **Sắp xếp**: kéo thả đổi `SortOrder`.

**Luồng phụ**
- 2a. Không nhập hạn → báo lỗi (BR-04).
- 4a. Mục có mục con → hỏi xác nhận, xoá kèm toàn bộ con.
- 4b. Mục đã nằm trong `KpiEvaluationLine` của kỳ đã duyệt → chặn xoá, giải
  thích rõ lý do.

**Hậu điều kiện:** Checklist phản ánh đúng khối lượng dự án; là đầu vào của
UC-08, UC-16, UC-25.
**Quy tắc:** BR-02, BR-04, BR-09.

---

### UC-12 — Import checklist từ Excel

**Tác nhân:** PM
**Tiền điều kiện:** Đã tải file mẫu (UC-13) và điền xong.

**Luồng chính**
1. Mở thẻ **Checklist** → **Import từ Excel** → chọn file.
2. Hệ thống đọc file, đối chiếu với cấu trúc mẫu ở [§9-UC-13](#uc-13--tải-file-mẫu-import).
3. Hiện **màn xem trước**: mỗi dòng kèm trạng thái *Sẽ thêm* / *Bỏ qua (trùng
   mã)* / *Lỗi (kèm mô tả)*. Chưa ghi gì vào CSDL.
4. Người dùng xem, bấm **Xác nhận import**.
5. Hệ thống chỉ thêm những dòng *Sẽ thêm*, báo lại: đã thêm bao nhiêu, bỏ qua
   bao nhiêu, lỗi bao nhiêu.

**Luồng phụ**
- 2a. File sai định dạng hoặc thiếu cột bắt buộc → báo lỗi rõ tên cột thiếu,
  không sang bước 3.
- 3a. Mọi dòng đều lỗi → chặn nút Xác nhận.
- 3b. Dòng có `Code` trùng mục đã có trong dự án → đánh dấu *Bỏ qua* (BR-14).
- 3c. Tên người thực hiện không khớp nhân sự nào → không chặn; import với
  `AssigneeMemberId = 0` và cảnh báo *"Chưa giao người"*.
- 3d. Ngày sai định dạng → đánh dấu dòng đó *Lỗi*.

**Hậu điều kiện:** Các mục hợp lệ vào checklist. Import **không bao giờ ghi đè**
mục đã có.
**Quy tắc:** BR-14.

---

### UC-13 — Tải file mẫu import

**Tác nhân:** PM
**Luồng chính**
1. Bấm **Tải file mẫu** ở thẻ Checklist.
2. Hệ thống trả file mẫu có đúng các cột sau, kèm một dòng ví dụ:

| Cột | Bắt buộc | Định dạng | Ví dụ |
|---|---|---|---|
| `Ma` | Không | Chuỗi | `CL-01` |
| `TenCongViec` | **Có** | Chuỗi, ≤250 ký tự | `Dựng màn hình đăng nhập` |
| `MoTa` | Không | Chuỗi | `Gồm cả quên mật khẩu` |
| `MaCha` | Không | Chuỗi, khớp `Ma` của dòng khác | `CL-00` |
| `NguoiThucHien` | Không | Họ tên đúng như trong Thành viên | `Nguyễn Văn A` |
| `NgayBatDau` | Không | `dd/MM/yyyy` | `01/08/2026` |
| `HanHoanThanh` | **Có** | `dd/MM/yyyy` | `15/08/2026` |
| `GhiChu` | Không | Chuỗi | |

**Hậu điều kiện:** Không đổi dữ liệu.

> **Định dạng file:** xem [§12.3](#123-đọc-và-ghi-excel-không-thêm-thư-viện) —
> dự án không có thư viện Excel nên phải chọn cách xử lý phù hợp.

---

### UC-15 — Lập báo cáo tuần dự án

**Tác nhân:** PM
**Tiền điều kiện:** Là PM của dự án; tuần báo cáo hợp lệ.

**Luồng chính**
1. Mở dự án → thẻ **Báo cáo tuần** → chọn tuần (mặc định tuần hiện tại).
2. Hệ thống nạp bản nháp đã có, hoặc tạo form trống theo **format ba phần**:
   - **Công việc đang thực hiện** — hệ thống *gợi ý sẵn* từ các mục checklist
     đang `DangLam`, PM sửa lại được.
   - **Khó khăn** — nhập tay.
   - **Tiến độ tuần tiếp theo** — chọn mục checklist (UC-16) + ghi chú.
3. **Lưu nháp** bất cứ lúc nào (`SubmittedAt` vẫn null).
4. **Nộp báo cáo**: hệ thống kiểm ba phần đều đã có nội dung.
5. Đặt `SubmittedAt = giờ hiện tại`, tính `IsOnTime` theo BR-06 rồi **chốt lại**
   (nộp bù sau này không đổi được cờ này).

**Luồng phụ**
- 4a. Thiếu phần bắt buộc → báo lỗi, không nộp.
- 4b. Tuần đó đã nộp rồi → cho sửa và nộp lại, nhưng **`IsOnTime` giữ nguyên giá
  trị lần nộp đầu**. Nếu không, PM nộp trễ rồi sửa lại sẽ tự "chữa" được điểm.
- 2a. Chọn tuần tương lai → chặn.

**Hậu điều kiện:** `WeeklyReport` có `SubmittedAt` và `IsOnTime`; là đầu vào tính
`PmBonus` ([§7.4](#74-điểm-cộng-pm)).
**Quy tắc:** BR-02, BR-06.

---

### UC-18 — Phân công việc hỗ trợ đầu tuần

**Tác nhân:** PM
**Luồng chính**
1. Mở dự án → thẻ **Việc hỗ trợ** → chọn tuần (mặc định tuần hiện tại).
2. Hệ thống hiện nhân sự đang tham gia dự án tại tuần đó (theo `JoinedAt`/`LeftAt`).
3. Với mỗi người, PM nhập nội dung hỗ trợ được phân trong tuần.
4. Lưu thành các `WorkItem` với `Kind = "HoTro"`, `Year`, `Week` tương ứng.

**Luồng phụ**
- 1a. Tuần đã kết thúc → chặn tạo mới, trừ khi người dùng là Quản lý Tổ (BR-07).
- 2a. Không có ai đang tham gia dự án tuần đó → hiện trạng thái rỗng.

**Hậu điều kiện:** Nhân sự thấy việc hỗ trợ trong màn *Việc của tôi*, và đến cuối
tuần phải báo cáo (UC-23).
**Quy tắc:** BR-02, BR-04, BR-07.

---

### UC-21 — Xem danh sách việc được giao

**Tác nhân:** Nhân sự
**Luồng chính**
1. Mở *Việc của tôi*.
2. Hệ thống hiện **ba khối tách biệt**:
   - **Việc checklist** — gom theo dự án, kèm hạn và tiến độ.
   - **Việc hỗ trợ tuần này** — kèm ô báo cáo nhanh.
   - **Việc ngoài dự án** — kèm hạn và bậc điểm cộng.
3. Bộ lọc: trạng thái, dự án, khoảng hạn. Mặc định ẩn việc đã `HoanThanh`/`Huy`.
4. Việc quá hạn hiện nổi bật ở đầu danh sách.

**Hậu điều kiện:** Không đổi dữ liệu.
**Quy tắc:** BR-03.

---

### UC-22 — Báo cáo tiến độ việc checklist

**Tác nhân:** Nhân sự
**Tiền điều kiện:** Đầu việc được giao cho chính người này.

**Luồng chính**
1. Từ UC-21 bấm vào một đầu việc.
2. Cập nhật **tiến độ (0–100)** và **trạng thái**; ghi chú nếu cần.
3. Chọn trạng thái `HoanThanh` → hệ thống đặt `CompletedAt = giờ hiện tại`.
4. Lưu, ghi `UpdatedBy`/`UpdatedAt`.
5. Hệ thống hiện ngay *"Hoàn thành đúng hạn"* hoặc *"Trễ hạn N ngày"* để người
   dùng biết ảnh hưởng tới điểm.

**Luồng phụ**
- 3a. Bỏ trạng thái `HoanThanh` → xoá `CompletedAt`.
- 2a. Người dùng không phải người được giao → `HttpNotFound()`.

**Hậu điều kiện:** `CompletedAt` là căn cứ chấm đúng/trễ hạn (BR-05).
**Quy tắc:** BR-03, BR-05.

---

### UC-25 — Sinh bảng KPI tháng

**Tác nhân:** Hệ thống, khởi động bởi Quản lý Tổ
**Tiền điều kiện:** Chọn kỳ `(Năm, Tháng)`; kỳ chưa bị khoá (BR-09).

**Luồng chính**
1. Quản lý Tổ mở *Chấm KPI* → chọn kỳ → **Sinh bảng chấm**.
2. Hệ thống, với mỗi nhân sự đang hoạt động:
   - Gom `C`, `S`, `A` theo [§7.1](#71-ba-thành-phần).
   - Tính `P_c`, `P_s`, `BaseScore`, `ExtraTaskBonus`, `PmBonus`, `SystemTotal`.
   - Tạo `KpiEvaluation` (`Status = "ChoPmDuyet"`, `FinalTotal = SystemTotal`).
   - Tạo `KpiEvaluationLine` cho **từng đầu việc**, chụp lại tên dự án, tên việc,
     hạn, ngày hoàn thành, số ngày, đúng/trễ hạn, điểm đóng góp.
3. Hiện bảng tổng hợp: mỗi dòng một nhân sự, kèm ba điểm thành phần và tổng.

**Luồng phụ**
- 2a. Nhân sự không có việc nào trong kỳ → vẫn tạo phiếu nhưng đánh dấu *Không
  có việc trong kỳ*, `SystemTotal` để trống (BR-11).
- 1a. Kỳ đã sinh rồi và **chưa duyệt** → hỏi *"Sinh lại sẽ ghi đè mọi điều chỉnh
  thủ công. Tiếp tục?"*.
- 1b. Kỳ đã ở `ToTruongDaDuyet` hoặc `DaGui` → chặn, buộc mở khoá trước (BR-09).

**Hậu điều kiện:** Kỳ ở trạng thái `ChoPmDuyet`, sẵn sàng cho UC-19.
**Quy tắc:** BR-09, BR-10, BR-11, BR-12.

---

### UC-19 — PM đánh giá & duyệt KPI dự án mình

**Tác nhân:** PM
**Tiền điều kiện:** Kỳ ở `ChoPmDuyet`.

**Luồng chính**
1. PM mở *Chấm KPI* → thấy **chỉ những nhân sự có việc trong dự án mình quản lý**.
2. Với mỗi người: xem các dòng chi tiết, sửa `AdjustedTitle` và `Note` (UC-26),
   ghi `PmComment`.
3. Bấm **Duyệt**. Hệ thống ghi `PmApprovedByMemberId`, `PmApprovedAt`.
4. Khi **mọi PM liên quan** đã duyệt xong → kỳ chuyển sang `PmDaDuyet`.

**Luồng phụ**
- 2a. PM sửa `FinalTotal` mà bỏ trống `AdjustReason` → báo lỗi, không lưu.
- 3a. PM đã duyệt rồi → nút đổi thành **Bỏ duyệt**, dùng được chừng nào kỳ chưa
  sang `ToTruongDaDuyet`.
- 1a. Một nhân sự tham gia dự án của nhiều PM → **mỗi PM chỉ thấy và sửa những
  dòng thuộc dự án mình**. Điểm tổng chỉ Quản lý Tổ sửa được. [CHỐT?]

**Hậu điều kiện:** Kỳ sẵn sàng cho UC-09.
**Quy tắc:** BR-02, BR-09.

---

### UC-09 — Duyệt KPI lần cuối & gửi email

**Tác nhân:** Quản lý Tổ
**Tiền điều kiện:** Kỳ ở `PmDaDuyet`.

**Luồng chính**
1. Mở *Chấm KPI* → xem toàn bộ nhân sự, kèm nhận xét của từng PM.
2. Điều chỉnh lần cuối nếu cần (UC-26), ghi `ManagerComment`.
3. Bấm **Duyệt lần cuối** → kỳ sang `ToTruongDaDuyet` và **khoá** (BR-09).
4. Bấm **Gửi email**: nhập **địa chỉ nhận** (một hoặc nhiều, ngăn bởi dấu phẩy).
5. Hệ thống dựng file Excel gồm **hai sheet**:
   - *Tổng hợp* — mỗi dòng một nhân sự: họ tên, điểm checklist, điểm hỗ trợ,
     điểm cộng, tổng, nhận xét.
   - *Chi tiết* — mỗi dòng một đầu việc: nhân sự, dự án, tên việc, loại, hạn,
     ngày hoàn thành, đúng/trễ, điểm.
6. Gửi mail kèm file, ghi `SentAt`, `SentTo`, chuyển kỳ sang `DaGui`.
7. Báo kết quả gửi ngay trên màn hình.

**Luồng phụ**
- 4a. Email sai định dạng → báo lỗi, không gửi.
- 6a. **Gửi mail thất bại** → giữ trạng thái `ToTruongDaDuyet` (KHÔNG chuyển
  `DaGui`), hiện lỗi kèm nút **Gửi lại**. Không được coi như đã gửi.
- 3a. Còn phiếu nào PM chưa duyệt → cảnh báo, cho phép bỏ qua nhưng phải xác nhận.

**Hậu điều kiện:** Kỳ khoá, file đã gửi, lịch sử gửi lưu lại.
**Quy tắc:** BR-09.

> **Lưu ý kỹ thuật:** `EmailClient.Send` hiện **chưa nhận đính kèm**
> (`Infrastructure\EmailClient.cs:41`). Phải bổ sung một nạp chồng nhận danh
> sách file. Xem [§12.4](#124-mở-rộng-emailclient).

---

### UC-26 — Điều chỉnh nội dung công việc trong KPI

**Tác nhân:** PM (dự án mình), Quản lý Tổ (toàn bộ)
**Tiền điều kiện:** Kỳ chưa khoá.

**Luồng chính**
1. Trong bảng chấm, bấm vào một dòng chi tiết.
2. Sửa `AdjustedTitle` (nội dung công việc ghi trong báo cáo) và `Note`.
3. Sửa `Score` của dòng nếu cần → hệ thống **tính lại** `FinalTotal` của phiếu.
4. Nếu `FinalTotal != SystemTotal` thì **bắt buộc** nhập `AdjustReason`.
5. Lưu.

**Hậu điều kiện:** Bảng KPI phản ánh đánh giá của con người; `SystemTotal` giữ
nguyên để đối chiếu về sau.
**Quy tắc:** BR-09.

---

### UC-28 — Nhắc hạn tự động

**Tác nhân:** Hệ thống (`ReminderScheduler`)
**Luồng chính**
1. Mỗi ngày vào giờ đã hẹn, hệ thống quét:
   - Đầu việc đến hạn trong **2 ngày tới** và chưa hoàn thành → nhắc người thực hiện.
   - Đầu việc **đã quá hạn** chưa hoàn thành → nhắc người thực hiện và PM.
   - **Chiều thứ Sáu**: dự án chưa có báo cáo tuần → nhắc PM.
   - **Sáng thứ Hai**: dự án chưa phân việc hỗ trợ tuần này → nhắc PM.
2. Gửi qua Telegram (nhóm) và/hoặc email (cá nhân), dùng lại hạ tầng sẵn có.

**Quy tắc:** Tái dùng `ReminderScheduler`, `TelegramClient`, `EmailClient`.

---

## 10. Màn hình & điều hướng

Menu bổ sung, chèn vào `Permissions.Menu` trong `Models\Permission.cs`:

```
(khối chính)
  Việc cần xử lý              — đã có
  Tổng hợp                    — đã có
  Bảng điều khiển Tổ          ★ mới   UC-01, UC-05, UC-08
  Việc của tôi                ★ mới   UC-21, UC-22, UC-23
  Báo cáo của tôi             — đã có
  Dự án                       — đã có, thêm các thẻ bên trong
  Việc ngoài dự án            ★ mới   UC-06
  Thành viên                  — đã có
  Phân công                   — đã có, bổ sung mốc thời gian (UC-04)
  Báo cáo nhân sự             — đã có
  Danh mục ▾                  — đã có
(khối mới: Chất lượng công việc)
  Chấm KPI                    ★ mới   UC-25, UC-19, UC-09, UC-26, UC-27
  Điểm của tôi                ★ mới   UC-24
```

**Màn chi tiết dự án** chuyển thành dạng thẻ:

| Thẻ | Use Case | Ai thấy |
|---|---|---|
| Thông tin chung | UC-02, UC-10 | Mọi người tham gia |
| Nhân sự | UC-04, UC-14 | Mọi người tham gia; sửa: PM + Quản lý Tổ |
| Checklist | UC-07, UC-08, UC-11, UC-12, UC-13 | Mọi người tham gia; sửa: PM + Quản lý Tổ |
| Việc hỗ trợ | UC-18 | Mọi người tham gia; sửa: PM + Quản lý Tổ |
| Báo cáo tuần | UC-15, UC-16 | Mọi người tham gia; sửa: PM |

**Màn chi tiết đầu việc** — thông tin + tiến độ + khu trao đổi (UC-17), là nơi
nhân sự và PM nói chuyện với nhau.

---

## 11. Quyền

Thêm vào `Models\Permission.cs`, theo đúng nếp `module.action` đang dùng:

```csharp
public static readonly PermModule Team =
    new PermModule("team", "Bảng điều khiển Tổ", "Chính",
        new[] { A(View, "Xem") });

public static readonly PermModule WorkItems =
    new PermModule("workitems", "Công việc", "Chính",
        new[] { A(View, "Xem"), A(Create, "Thêm"), A(Edit, "Sửa"), A(Delete, "Xóa"),
                A("assign", "Giao việc"), A("import", "Import Excel") });

public static readonly PermModule Checklists =
    new PermModule("checklists", "Checklist dự án", "Chính",
        new[] { A(View, "Xem"), A(Edit, "Sửa") });

public static readonly PermModule WeeklyReports =
    new PermModule("weeklyreports", "Báo cáo tuần dự án", "Chính",
        new[] { A(View, "Xem"), A(Edit, "Lập báo cáo") });

public static readonly PermModule Kpi =
    new PermModule("kpi", "Chấm KPI", "Chất lượng",
        new[] { A(View, "Xem"), A("generate", "Sinh bảng chấm"),
                A("pmapprove", "PM duyệt"), A("approve", "Duyệt lần cuối"),
                A("export", "Kết xuất"), A("send", "Gửi email") });
```

Gán cho ba nhóm mặc định (sửa `Data\RoleGroupSeeder.cs`):

| Nhóm | Quyền thêm |
|---|---|
| **Admin** | Toàn bộ (`*`, không cần sửa) |
| **Manager** (Quản lý Tổ) | Toàn bộ quyền mới ở trên |
| **Reporter** (PM & nhân sự) | `team.view`, `workitems.view`, `workitems.edit`, `checklists.view`, `checklists.edit`, `weeklyreports.view`, `weeklyreports.edit`, `kpi.view`, `kpi.pmapprove` |

> Nhóm `Reporter` được cấp cả quyền của PM vì **không phân biệt được PM ở mức
> nhóm quyền**. Việc chặn thật nằm ở **lớp 2** — kiểm `PmMemberId` trong từng
> action ([§3](#3-vai-trò)). Bỏ lớp 2 thì mọi nhân sự sửa được checklist mọi dự
> án. Đây là rủi ro lớn nhất của thiết kế phân quyền này, phải kiểm thử riêng.

---

## 12. Ràng buộc kỹ thuật

### 12.1. Theo đúng nếp dự án

- **Tiếng Việt** trong toàn bộ giao diện, thông báo lỗi, và **chú thích mã nguồn**.
- Chú thích giải thích **vì sao**, không mô tả lại code đang làm gì.
- Controller kế thừa `BaseController`, dùng `Notify()` / `NotifyError()`.
- Đánh dấu quyền bằng `[AppAuthorize(Permission = "...")]`.
- View dùng lại class CSS sẵn có: `card`, `page-head`, `data`, `btn`,
  `badge`, `filters`, `empty-state`, `table-wrap`, `_Pager.cshtml`.
- **File `.cshtml` phải có BOM** — thiếu BOM thì chữ tiếng Việt tĩnh trong view
  hiện thành ký tự lỗi.
- Entity mới phải implement `IEntity` và khai vào `Data\Repository.cs`.
- **Không thêm thư viện ngoài** trừ khi thật sự không có đường khác.

### 12.2. Hiệu năng

`SqlStore.All()` nạp cả bảng lên bộ nhớ. Ba bảng sẽ lớn nhanh:
`WorkItems`, `WorkItemComments`, `KpiEvaluationLines`.

Cách xử lý:
- Bình luận: **luôn lọc theo `WorkItemId`** ngay sau khi đọc, không bao giờ
  hiện danh sách bình luận toàn hệ thống.
- Dòng KPI: đọc theo từng kỳ.
- Bảng điều khiển Tổ và bảng chấm KPI: đọc **một lần** các bảng cần dùng rồi
  ghép trong bộ nhớ bằng `Dictionary`, **không** gọi `Find()` trong vòng lặp —
  đây là lỗi hay gặp nhất, biến một màn hình thành hàng nghìn lượt truy vấn.
- Nếu `WorkItems` vượt vài chục nghìn dòng, viết `HrEmployeeStore`-style store
  riêng có `OFFSET/FETCH` thay vì dùng `SqlStore`.

### 12.3. Đọc và ghi Excel (không thêm thư viện)

Dự án **không có thư viện Excel** và có nếp không thêm phụ thuộc mới.

**Kết xuất (UC-27, UC-09)** — dùng **SpreadsheetML 2003** (`.xls`): một file XML
thuần, Excel mở được, hỗ trợ nhiều sheet và định dạng cơ bản. Tự sinh bằng
`XmlWriter`, không cần thư viện. Đây là cách phù hợp nhất với yêu cầu "hai sheet
Tổng hợp và Chi tiết".

**Import (UC-12)** — khó hơn: `.xlsx` là file nén, đọc bằng tay rất phiền.
Ba lựa chọn:

| Cách | Ưu | Nhược |
|---|---|---|
| **A. Mẫu CSV** | Không cần thư viện; đọc đơn giản | Người dùng phải lưu thành CSV; dễ vỡ dấu tiếng Việt nếu sai mã hoá |
| **B. Mẫu SpreadsheetML 2003** | Không cần thư viện; đọc bằng `XDocument`; giữ nguyên tiếng Việt; mở bằng Excel như file thường | Excel hiện cảnh báo định dạng khi lưu |
| **C. Thêm ClosedXML/EPPlus** | Đọc `.xlsx` thật, trải nghiệm tốt nhất | Phá nếp "không thêm thư viện" |

> **[CHỐT?]** Đặc tả **đề xuất cách B** — cân bằng nhất và giữ đúng nếp dự án.
> Nhưng người dùng nói *"Có mẫu import lên"*, có thể đang hình dung file `.xlsx`
> quen thuộc. **Phải hỏi trước khi code**, vì chọn sai thì làm lại cả UC-12 và
> UC-13.

### 12.4. Mở rộng `EmailClient`

Hiện tại chỉ có `Send(string toAddress, string subject, string htmlBody)`
(`Infrastructure\EmailClient.cs:41`). Cần thêm nạp chồng:

```csharp
/// <summary>
/// Gửi mail kèm tệp đính kèm. Nội dung tệp truyền bằng mảng byte thay vì đường
/// dẫn để không phải ghi file tạm ra đĩa — bảng KPI sinh thẳng trong bộ nhớ.
/// </summary>
public static EmailResult Send(
    string toAddress, string subject, string htmlBody,
    string attachmentName, byte[] attachmentContent, string attachmentContentType)
```

Lưu ý: `MailMessage` và `Attachment` phải nằm trong `using` để giải phóng luồng;
`toAddress` phải tách được nhiều địa chỉ ngăn bởi dấu phẩy.

### 12.5. Bảo mật

- **Bình luận và mọi nội dung người dùng nhập phải HTML-encode khi hiển thị.**
  Khu trao đổi là nơi lưu văn bản tự do rồi hiện lại cho người khác — đúng chỗ
  dễ dính XSS nhất trong toàn bộ đặc tả này.
- File import: chặn theo phần mở rộng **và** kiểm nội dung; giới hạn dung lượng.
- Kiểm quyền **hai lớp** với mọi action của PM ([§3](#3-vai-trò)).
- Mọi thao tác duyệt/mở khoá KPI phải ghi lại ai làm, lúc nào.

---

## 13. Thứ tự triển khai

Mỗi giai đoạn chạy được và kiểm thử được độc lập.

| GĐ | Nội dung | UC | Vì sao ở đây |
|---|---|---|---|
| **1** | Nối `Member` ↔ `User`; mốc thời gian cho `ProjectMember` | UC-04, UC-14 | Mọi thứ sau đều cần biết "tôi là nhân sự nào" và "tháng đó ai ở trong dự án" |
| **2** | `WorkItem` + CRUD + màn *Việc của tôi* | UC-06, UC-07, UC-11, UC-21, UC-22 | Xương sống của cả hệ thống |
| **3** | Checklist trong dự án + tải mẫu + import | UC-12, UC-13 | Cần `WorkItem` đã xong |
| **4** | Việc hỗ trợ theo tuần | UC-18, UC-23 | Cần `WorkItem`; là thành phần 20% của KPI |
| **5** | Báo cáo tuần của PM + kế hoạch tuần tới | UC-15, UC-16 | Cần checklist để chọn mục |
| **6** | Trao đổi trong đầu việc | UC-17 | Độc lập, làm lúc nào cũng được |
| **7** | Bảng điều khiển Tổ | UC-01, UC-05, UC-08 | Cần dữ liệu từ GĐ 2–5 mới có gì để hiện |
| **8** | Sinh bảng KPI + luồng duyệt hai cấp | UC-19, UC-24, UC-25, UC-26 | Cần **toàn bộ** GĐ 1–5 |
| **9** | Kết xuất Excel + gửi mail đính kèm | UC-09, UC-27 | Cần bảng KPI đã có |
| **10** | Nhắc hạn tự động | UC-28 | Tiện ích, để cuối |

**Không nên đảo thứ tự GĐ 1.** Nếu làm sau, mọi màn hình đã viết đều phải sửa
lại chỗ xác định người dùng hiện tại.

---

## 14. Những điểm cần chốt

Tập hợp lại toàn bộ mục **[CHỐT?]**. **Trả lời hết trước khi bắt đầu GĐ 8.**

| # | Câu hỏi | Ảnh hưởng | Đặc tả đang tạm chọn |
|---|---|---|---|
| 1 | Người chỉ làm việc hỗ trợ, cả tháng đúng hạn — được **75–100 điểm** (trọng số co giãn) hay tối đa **20 điểm** (giữ cứng 80/20)? | **Rất lớn** — đổi hẳn kết quả chấm, xem Ví dụ 3 §7.6 | Co giãn |
| 2 | Việc dài **đúng 3 ngày** và **đúng 7 ngày** thuộc bậc nào? | Trung bình | `≤3` = 1%, `4–7` = 2%, `≥8` = 3% |
| 3 | `ExtraTaskBonus` có **trần** không? | Trung bình — dễ bị lạm dụng | Không trần |
| 4 | Tổng điểm có chặn **tối đa 100** không? | Trung bình | Không chặn |
| 5 | PM được +2% **một lần** hay **mỗi dự án**? | Trung bình | Một lần |
| 6 | Thế nào là "cập nhật checklist đúng hạn"? | Lớn — hiện chưa định nghĩa được rõ | Có ít nhất một lần sửa checklist trong tuần |
| 7 | Hạn nộp báo cáo tuần chính xác là lúc nào? | Trung bình | 17:00 thứ Sáu cùng tuần |
| 8 | File import là `.xlsx` thật hay chấp nhận SpreadsheetML/CSV? | **Lớn** — quyết định có phải thêm thư viện | SpreadsheetML 2003 |
| 9 | Dự án có **thuộc** một trong hai loại, hay **chứa cả hai** loại việc? | Trung bình | Phân loại đặt ở đầu việc |
| 10 | Nối `Member` ↔ `User` bằng **cột `UserId`** hay khớp theo **email**? | Trung bình | Cột `UserId` |
| 11 | Nhân sự ở dự án của nhiều PM thì **ai** chốt điểm tổng? | Trung bình | Quản lý Tổ |
| 12 | Import trùng `Code` thì **bỏ qua** hay **ghi đè**? | Nhỏ | Bỏ qua |
| 13 | Email nhận bảng KPI là **nhập tay mỗi lần** hay **cấu hình sẵn**? | Nhỏ | Nhập tay, gợi ý lần gửi trước |

---

## Phụ lục A — Đối chiếu yêu cầu gốc với Use Case

Bảng này để kiểm không sót ý nào.

| Yêu cầu gốc | UC |
|---|---|
| **1. Quản lý Tổ** | |
| Nắm thành viên đang làm gì, tham gia dự án nào, ai là PM | UC-01 |
| Thêm/Xoá/Sửa dự án | UC-02 |
| Phân công PM và nhân sự tham gia | UC-03, UC-04 |
| Nắm tiến độ dự án và công việc tuần, có nơi tổng hợp nhanh | UC-05 |
| Giao việc ngoài dự án, có thời hạn | UC-06 |
| Tạo công việc có thời hạn trong dự án | UC-07 |
| Xem tiến độ checklist do PM đưa lên | UC-08 |
| **2. PM** | |
| Cập nhật tiến độ dự án | UC-10 |
| Cập nhật checklist, có mẫu import | UC-11, UC-12, UC-13 |
| Cập nhật nhân sự theo khoảng thời gian, rút ra / bổ sung vào | UC-14 |
| Báo cáo tuần theo format ba phần | UC-15, UC-16 |
| Làm đúng hạn được cộng 2% | §7.4 |
| Trao đổi kiểu forum trong checklist | UC-17 |
| Hai loại dự án: hỗ trợ kỹ thuật và checklist | §5.1 `Project.WorkKind` |
| Việc hỗ trợ phân từ đầu tuần | UC-18 |
| **3. Nhân sự** | |
| Thấy dự án đang tham gia | UC-20 |
| Thấy việc Quản lý Tổ giao | UC-21 |
| Báo cáo tiến độ theo checklist | UC-22 |
| Đúng tiến độ thì đủ 100% | §7.2 |
| Việc hỗ trợ đúng hạn đủ 20% | §7.2 |
| Việc checklist đúng hạn đủ 80% | §7.2 |
| Việc riêng ngoài dự án được cộng % theo độ dài | §7.3 |
| **4. KPI** | |
| Liệt kê công việc và chất lượng theo từng cá nhân | UC-25 |
| Cho phép kết xuất và điều chỉnh nội dung | UC-26, UC-27 |
| PM đánh giá và duyệt dự án mình | UC-19 |
| Quản lý Tổ duyệt lần cuối, gửi email kèm Excel tổng hợp + chi tiết | UC-09 |
