using System.Collections.Generic;
using System.Linq;

namespace TTKDGP.ProjectManager.Models
{
    /// <summary>Hai kiểu nhìn cùng một danh sách công việc.</summary>
    public static class TaskViews
    {
        public const string Grid = "luoi";
        public const string Kanban = "kanban";

        public static string Parse(string raw)
        {
            return string.Equals(raw, Kanban, System.StringComparison.OrdinalIgnoreCase) ? Kanban : Grid;
        }
    }

    /// <summary>Một dòng trên màn checklist, đã dàn phẳng từ cây.</summary>
    public class ChecklistRow
    {
        public WorkTask Task { get; set; }

        /// <summary>Độ sâu trong cây, 0 là mục gốc — dùng để thụt lề.</summary>
        public int Depth { get; set; }

        public int CommentCount { get; set; }

        /// <summary>
        /// Người đang xem có sửa được ĐÚNG đầu việc này không. Tính theo từng dòng chứ không theo
        /// cả màn hình: thành viên sửa được việc của mình nhưng không đụng được việc người khác.
        /// </summary>
        public bool CanEdit { get; set; }

        /// <summary>Sửa được mọi trường (PM dự án hoặc Quản lý Tổ), không chỉ phần báo cáo.</summary>
        public bool CanEditAll { get; set; }
    }

    /// <summary>Một cột trên bảng Kanban — mỗi cột là một trạng thái công việc.</summary>
    public class KanbanColumn
    {
        public string State { get; set; }
        public string Title { get; set; }
        public List<ChecklistRow> Cards { get; set; }

        public KanbanColumn()
        {
            Cards = new List<ChecklistRow>();
        }
    }

    public class ChecklistViewModel
    {
        public WorkProject Project { get; set; }

        /// <summary>Sửa được cả dự án (PM hoặc Quản lý Tổ) — quyết định các nút thêm mới, import.</summary>
        public bool CanEdit { get; set; }

        /// <summary>Kiểu hiển thị đang chọn, xem <see cref="TaskViews"/>.</summary>
        public string View { get; set; }

        /// <summary>Chỉ các dòng của trang đang xem. Chỉ dùng ở kiểu lưới.</summary>
        public PagedList<ChecklistRow> Rows { get; set; }

        /// <summary>Toàn bộ cây — dùng cho các con số tổng, phải tính trên tất cả chứ không chỉ trang này.</summary>
        public List<ChecklistRow> AllRows { get; set; }

        /// <summary>
        /// Các cột Kanban. Bảng Kanban KHÔNG phân trang: cắt bảng làm nhiều trang thì việc kéo thả
        /// giữa hai trạng thái nằm khác trang trở thành bất khả thi.
        /// </summary>
        public List<KanbanColumn> Columns { get; set; }

        /// <summary>Người dùng đang hoạt động — nguồn của ô chọn người thực hiện.</summary>
        public List<User> Users { get; set; }

        public int TotalCount { get { return AllRows.Count; } }
        public int DoneCount { get { return AllRows.Count(r => r.Task.State == TaskStates.Done); } }
        public int OverdueCount { get { return AllRows.Count(r => r.Task.IsOverdue); } }

        public int DonePercent
        {
            get { return TotalCount == 0 ? 0 : (int)System.Math.Round(DoneCount * 100.0 / TotalCount); }
        }

        /// <summary>Có sửa được ít nhất một việc không — dùng để quyết định hiện cột thao tác.</summary>
        public bool CanEditAny { get { return AllRows.Any(r => r.CanEdit); } }

        // ---------- Thống kê khối lượng theo tháng (chỉ Quản lý Tổ) ----------

        /// <summary>
        /// Có dựng khối thống kê khối lượng không. Controller chỉ bật khi tài khoản có quyền
        /// workload.view — view KHÔNG được tự quyết định, để số liệu quản lý không lọt vào HTML
        /// của người thường.
        /// </summary>
        public bool ShowWorkload { get; set; }

        /// <summary>Khối thống kê có mở sẵn không (vừa đổi tháng thì mở, còn lại thì gập).</summary>
        public bool WorkloadOpen { get; set; }

        public int WorkloadYear { get; set; }
        public int WorkloadMonth { get; set; }

        public List<Services.WorkloadRow> Workload { get; set; }

        public ChecklistViewModel()
        {
            Rows = new PagedList<ChecklistRow>();
            AllRows = new List<ChecklistRow>();
            Columns = new List<KanbanColumn>();
            Users = new List<User>();
            Workload = new List<Services.WorkloadRow>();
            View = TaskViews.Grid;
        }
    }

    /// <summary>Một người nhắc được bằng @ trong khung trao đổi — người đang tham gia dự án.</summary>
    public class TaskMentionOption
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string UserName { get; set; }
    }

    /// <summary>
    /// Khung trao đổi của một đầu việc trong hộp thoại chi tiết. Dùng cho cả lượt vẽ đầu lẫn
    /// phần vẽ lại sau khi gửi/thu hồi — một partial duy nhất, khỏi lệch nhau.
    /// </summary>
    public class TaskCommentsViewModel
    {
        public int TaskId { get; set; }

        /// <summary>Tài khoản đang xem — quyết định nút thu hồi trên nội dung của chính mình.</summary>
        public int CurrentUserId { get; set; }

        /// <summary>PM dự án hoặc Quản lý Tổ — thu hồi được nội dung của người khác.</summary>
        public bool CanModerate { get; set; }

        public List<WorkComment> Comments { get; set; }

        /// <summary>Người đang tham gia dự án — nguồn của danh sách gợi ý khi gõ @.</summary>
        public List<TaskMentionOption> Participants { get; set; }

        public int VisibleCount
        {
            get { return Comments.Count(c => !c.IsDeleted); }
        }

        public TaskCommentsViewModel()
        {
            Comments = new List<WorkComment>();
            Participants = new List<TaskMentionOption>();
        }
    }

    /// <summary>Kết quả xử lý một dòng import.</summary>
    public static class ImportOutcomes
    {
        public const string Add = "Them";
        public const string Skip = "BoQua";
        public const string Error = "Loi";
    }

    /// <summary>Một dòng công việc trong màn xem trước import (sheet 1).</summary>
    public class ImportRow
    {
        public const string Add = ImportOutcomes.Add;
        public const string Skip = ImportOutcomes.Skip;
        public const string Error = ImportOutcomes.Error;

        public int LineNumber { get; set; }
        public string Code { get; set; }
        public string Title { get; set; }
        public string ParentCode { get; set; }

        /// <summary>Cột "Mô tả" trong file, đổ vào <see cref="WorkTask.Description"/>.</summary>
        public string Note { get; set; }

        /// <summary>Tài khoản người thực hiện, tham chiếu sang sheet nhân sự.</summary>
        public string AssigneeAccount { get; set; }

        /// <summary>Tên người đã ghép được, để người dùng đối chiếu trên màn xem trước.</summary>
        public string AssigneeName { get; set; }

        public string StartDateText { get; set; }
        public string DueDateText { get; set; }
        public string DoneDateText { get; set; }
        public string PriorityText { get; set; }
        public string WeekText { get; set; }
        public string YearText { get; set; }

        public string Outcome { get; set; }
        public string Message { get; set; }

        /// <summary>Bản ghi dựng sẵn, chờ ghi. Null khi dòng lỗi hoặc bị bỏ qua.</summary>
        public WorkTask Prepared { get; set; }
    }

    /// <summary>Một dòng nhân sự trong màn xem trước import (sheet 2).</summary>
    public class ImportMemberRow
    {
        public int LineNumber { get; set; }

        /// <summary>Tên đăng nhập ghi trong file.</summary>
        public string Account { get; set; }

        public string FullName { get; set; }
        public string Role { get; set; }

        public string Outcome { get; set; }
        public string Message { get; set; }

        /// <summary>Người dùng ghép được (bảng Users); 0 nghĩa là không tìm thấy.</summary>
        public int UserId { get; set; }

        /// <summary>Đã tham gia dự án từ trước nên không thêm phân công mới.</summary>
        public bool AlreadyInProject { get; set; }
    }

    public class ImportPreviewViewModel
    {
        public WorkProject Project { get; set; }
        public string FileName { get; set; }

        /// <summary>Sheet 1 — công việc.</summary>
        public List<ImportRow> Rows { get; set; }

        /// <summary>Sheet 2 — nhân sự tham gia.</summary>
        public List<ImportMemberRow> MemberRows { get; set; }

        /// <summary>Lỗi ở mức cả file (sai định dạng, thiếu cột). Có giá trị thì không hiện bảng.</summary>
        public string FileError { get; set; }

        /// <summary>Cảnh báo không chặn được import, ví dụ file chỉ có một sheet.</summary>
        public string FileWarning { get; set; }

        public int AddCount { get { return Rows.Count(r => r.Outcome == ImportRow.Add); } }
        public int SkipCount { get { return Rows.Count(r => r.Outcome == ImportRow.Skip); } }
        public int ErrorCount { get { return Rows.Count(r => r.Outcome == ImportRow.Error); } }

        public int MemberAddCount { get { return MemberRows.Count(r => r.Outcome == ImportOutcomes.Add); } }
        public int MemberErrorCount { get { return MemberRows.Count(r => r.Outcome == ImportOutcomes.Error); } }

        public bool CanConfirm
        {
            get { return string.IsNullOrEmpty(FileError) && (AddCount > 0 || MemberAddCount > 0); }
        }

        public ImportPreviewViewModel()
        {
            Rows = new List<ImportRow>();
            MemberRows = new List<ImportMemberRow>();
        }
    }
}
