using System;
using System.Collections.Generic;
using System.Linq;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Services
{
    /// <summary>
    /// Ghi giờ công vào đầu việc (logtime) và kiểm các mốc chặn.
    ///
    /// Hai ràng buộc, độc lập nhau, đều phải qua:
    ///   1. TRẦN NGÀY  — tổng giờ một người khai trong một ngày, cộng trên MỌI đầu việc,
    ///                   không vượt 12 giờ.
    ///   2. TRẦN VIỆC  — tổng giờ khai vào một đầu việc không vượt
    ///                   (Hạn hoàn thành − Ngày bắt đầu) × 8.
    ///
    /// Mọi phép kiểm nằm ở đây chứ không rải ra controller: màn hình cần biết trước còn bao
    /// nhiêu giờ để hiện cho người dùng, còn máy chủ cần chặn lúc lưu — hai nơi phải dùng
    /// chung một luật, nếu không sẽ có lúc màn hình bảo được mà lưu lại báo lỗi.
    /// </summary>
    public static class TimeLogService
    {
        /// <summary>Các dòng giờ đã ghi của một đầu việc, mới nhất lên trước.</summary>
        public static List<WorkTimeLog> OfTask(int taskId)
        {
            return Repository.WorkTimeLogs.All()
                .Where(l => l.TaskId == taskId)
                .OrderByDescending(l => l.WorkDate)
                .ThenByDescending(l => l.Id)
                .ToList();
        }

        /// <summary>Tổng giờ đã ghi vào một đầu việc, của mọi người.</summary>
        public static decimal TotalOfTask(int taskId)
        {
            return Repository.WorkTimeLogs.All()
                .Where(l => l.TaskId == taskId)
                .Sum(l => (decimal?)l.Hours) ?? 0m;
        }

        /// <summary>
        /// Tổng giờ của NHIỀU đầu việc một lượt, trả map TaskId → số giờ.
        ///
        /// Bảng checklist cần con số này cho từng dòng; gọi <see cref="TotalOfTask"/> trong vòng
        /// lặp thì mỗi dòng lại quét lại cả bảng giờ công — cùng lối nghĩ với CommentCounts.
        /// Việc chưa ghi giờ nào KHÔNG có khoá trong map, nơi gọi tự hiểu là 0.
        /// </summary>
        public static Dictionary<int, decimal> TotalsByTask(IEnumerable<int> taskIds)
        {
            var ids = new HashSet<int>(taskIds ?? Enumerable.Empty<int>());
            if (ids.Count == 0) return new Dictionary<int, decimal>();

            return Repository.WorkTimeLogs.All()
                .Where(l => ids.Contains(l.TaskId))
                .GroupBy(l => l.TaskId)
                .ToDictionary(g => g.Key, g => g.Sum(l => l.Hours));
        }

        /// <summary>
        /// Tổng giờ một người đã khai trong một ngày, cộng trên mọi đầu việc.
        /// <paramref name="exceptLogId"/> để bỏ qua chính dòng đang sửa khi tính lại.
        /// </summary>
        public static decimal TotalOfDay(int userId, DateTime day, int exceptLogId = 0)
        {
            var date = day.Date;

            return Repository.WorkTimeLogs.All()
                .Where(l => l.UserId == userId && l.WorkDate.Date == date && l.Id != exceptLogId)
                .Sum(l => (decimal?)l.Hours) ?? 0m;
        }

        /// <summary>Giờ còn ghi được trong một ngày trước khi chạm trần 12 giờ.</summary>
        public static decimal RemainingOfDay(int userId, DateTime day, int exceptLogId = 0)
        {
            var left = TimeLogRules.MaxHoursPerDay - TotalOfDay(userId, day, exceptLogId);
            return left > 0 ? left : 0m;
        }

        /// <summary>
        /// Giờ còn ghi được vào một đầu việc trước khi chạm trần của việc. Trả null khi việc
        /// không có trần (thiếu ngày bắt đầu hoặc hạn) — nơi gọi phải tự xử lý trường hợp đó.
        /// </summary>
        public static decimal? RemainingOfTask(WorkTask task, int exceptLogId = 0)
        {
            if (task == null) return null;

            var cap = TimeLogRules.TaskCap(task.StartDate, task.DueDate);
            if (!cap.HasValue) return null;

            var used = Repository.WorkTimeLogs.All()
                .Where(l => l.TaskId == task.Id && l.Id != exceptLogId)
                .Sum(l => (decimal?)l.Hours) ?? 0m;

            var left = cap.Value - used;
            return left > 0 ? left : 0m;
        }

        /// <summary>
        /// Kiểm trước khi đổi trạng thái đầu việc sang Đang làm hoặc Hoàn thành: phải đã ghi giờ
        /// công ít nhất một lượt. Trả null nếu hợp lệ, ngược lại trả câu báo lỗi đã viết sẵn.
        ///
        /// Không xét state khác (Chưa bắt đầu, Tạm dừng, Huỷ) — những trạng thái đó không thể hiện
        /// đã có công sức bỏ ra nên không cần căn cứ giờ công.
        /// </summary>
        public static string ValidateStateChange(WorkTask task, string state)
        {
            if (task == null) return null;
            if (state != TaskStates.InProgress && state != TaskStates.Done) return null;
            if (TotalOfTask(task.Id) > 0) return null;

            return string.Format(
                "Phải ghi giờ công cho việc này trước khi chuyển sang trạng thái \"{0}\".",
                TaskStates.Display(state));
        }

        /// <summary>
        /// Kiểm một lượt ghi giờ trước khi lưu. Trả null nếu hợp lệ, ngược lại trả câu báo lỗi
        /// đã viết sẵn cho người dùng đọc.
        ///
        /// <paramref name="exceptLogId"/> khác 0 nghĩa là đang SỬA một dòng: dòng đó phải được
        /// trừ ra khỏi các phép cộng, nếu không nó tự chặn chính mình.
        /// </summary>
        public static string Validate(WorkTask task, int userId, DateTime workDate, decimal hours,
            int exceptLogId = 0)
        {
            if (task == null) return "Không tìm thấy công việc.";

            // ----- Số giờ của chính lượt này -----
            if (hours < TimeLogRules.MinHoursPerEntry)
            {
                return string.Format("Số giờ phải từ {0:0.##} trở lên.", TimeLogRules.MinHoursPerEntry);
            }

            if (hours > TimeLogRules.MaxHoursPerDay)
            {
                return string.Format("Một lượt ghi không quá {0:0.##} giờ.", TimeLogRules.MaxHoursPerDay);
            }

            // ----- Ngày làm -----
            if (workDate.Date > DateTime.Today)
            {
                return "Không ghi giờ cho ngày trong tương lai.";
            }

            // ----- Trần việc: (Hạn − Ngày bắt đầu) × 8 -----
            // Việc thiếu ngày thì KHÔNG cho ghi: không có căn cứ tính trần thì cũng không có
            // cách nào biết giờ khai đã vượt hay chưa.
            var cap = TimeLogRules.TaskCap(task.StartDate, task.DueDate);
            if (!cap.HasValue)
            {
                return "Công việc chưa có đủ ngày bắt đầu và hạn hoàn thành nên chưa ghi giờ được. "
                     + "Hãy nhờ người giao việc bổ sung.";
            }

            var taskLeft = RemainingOfTask(task, exceptLogId) ?? 0m;
            if (hours > taskLeft)
            {
                return string.Format(
                    "Vượt tổng thời gian của công việc. Trần là {0:0.##} giờ "
                    + "((hạn {1:dd/MM/yyyy} − bắt đầu {2:dd/MM/yyyy}) × {3:0.##} giờ), còn lại {4:0.##} giờ.",
                    cap.Value, task.DueDate, task.StartDate, TimeLogRules.HoursPerWorkDay, taskLeft);
            }

            // ----- Trần ngày: 12 giờ cho tất cả công việc cộng lại -----
            var dayLeft = RemainingOfDay(userId, workDate, exceptLogId);
            if (hours > dayLeft)
            {
                return string.Format(
                    "Vượt trần {0:0.##} giờ một ngày. Ngày {1:dd/MM/yyyy} bạn đã ghi {2:0.##} giờ "
                    + "ở các công việc khác, chỉ còn {3:0.##} giờ.",
                    TimeLogRules.MaxHoursPerDay, workDate,
                    TotalOfDay(userId, workDate, exceptLogId), dayLeft);
            }

            return null;
        }

        /// <summary>
        /// Ghi một lượt giờ. Trả null nếu lưu được, ngược lại trả câu báo lỗi.
        /// Người ghi LUÔN là tài khoản đang đăng nhập — không nhận từ form gửi lên.
        /// </summary>
        public static string Add(WorkTask task, int userId, string userName,
            DateTime workDate, decimal hours, string note)
        {
            var error = Validate(task, userId, workDate, hours);
            if (error != null) return error;

            Repository.WorkTimeLogs.Insert(new WorkTimeLog
            {
                TaskId = task.Id,
                UserId = userId,
                UserName = userName,
                WorkDate = workDate.Date,
                Hours = hours,
                Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
                CreatedAt = DateTime.Now
            });

            return null;
        }

        /// <summary>Tổng giờ một người đã khai trong một khoảng ngày — dùng cho thống kê và KPI.</summary>
        public static decimal HoursOfUser(int userId, DateTime from, DateTime to)
        {
            var start = from.Date;
            var end = to.Date;

            return Repository.WorkTimeLogs.All()
                .Where(l => l.UserId == userId && l.WorkDate.Date >= start && l.WorkDate.Date <= end)
                .Sum(l => (decimal?)l.Hours) ?? 0m;
        }

        /// <summary>
        /// Tổng giờ theo từng đầu việc trong một khoảng ngày, cho một người.
        /// Trả về map TaskId → số giờ, để nơi gọi khỏi phải quét lại bảng cho từng việc.
        /// </summary>
        public static Dictionary<int, decimal> HoursByTask(int userId, DateTime from, DateTime to)
        {
            var start = from.Date;
            var end = to.Date;

            return Repository.WorkTimeLogs.All()
                .Where(l => l.UserId == userId && l.WorkDate.Date >= start && l.WorkDate.Date <= end)
                .GroupBy(l => l.TaskId)
                .ToDictionary(g => g.Key, g => g.Sum(l => l.Hours));
        }

        /// <summary>Giờ đã khai của một người, gom theo NGÀY trong khoảng — để vẽ bảng theo ngày.</summary>
        public static Dictionary<DateTime, decimal> HoursByDay(int userId, DateTime from, DateTime to)
        {
            var start = from.Date;
            var end = to.Date;

            return Repository.WorkTimeLogs.All()
                .Where(l => l.UserId == userId && l.WorkDate.Date >= start && l.WorkDate.Date <= end)
                .GroupBy(l => l.WorkDate.Date)
                .ToDictionary(g => g.Key, g => g.Sum(l => l.Hours));
        }

        /// <summary>Xoá các dòng giờ của một đầu việc — gọi khi xoá hẳn đầu việc đó.</summary>
        public static void DeleteOfTask(int taskId)
        {
            Repository.WorkTimeLogs.DeleteWhere(l => l.TaskId == taskId);
        }

        /// <summary>
        /// Dựng khối "Giờ công" cho màn hình: danh sách lượt đã ghi, các mốc trần và phần còn
        /// lại. Đặt ở service để hộp thoại chi tiết và trang chi tiết dùng chung một cách tính —
        /// hai nơi tự tính riêng thì sớm muộn cũng lệch nhau.
        /// </summary>
        public static TaskTimeLogViewModel BuildViewModel(WorkTask task, int currentUserId)
        {
            var today = DateTime.Today;

            var model = new TaskTimeLogViewModel
            {
                TaskId = task.Id,
                CurrentUserId = currentUserId,
                Logs = OfTask(task.Id),
                TaskCap = TimeLogRules.TaskCap(task.StartDate, task.DueDate),
                TaskTotal = TotalOfTask(task.Id),
                TaskRemaining = RemainingOfTask(task),
                DefaultDate = today,
                TodayTotal = TotalOfDay(currentUserId, today),
                TodayRemaining = RemainingOfDay(currentUserId, today),
                MaxPerDay = TimeLogRules.MaxHoursPerDay
            };

            // Chỉ NGƯỜI THỰC HIỆN ghi giờ của chính mình — giờ công là căn cứ chấm KPI của họ,
            // nên không ai ghi hộ được, kể cả Quản lý Tổ.
            if (currentUserId <= 0 || task.AssigneeUserId != currentUserId)
            {
                model.BlockedReason = "Chỉ người được giao việc mới ghi được giờ công.";
            }
            else if (TaskStates.IsClosed(task.State))
            {
                model.BlockedReason = "Công việc đã đóng nên không ghi thêm giờ được.";
            }
            else if (!model.TaskCap.HasValue)
            {
                model.BlockedReason = "Công việc chưa có đủ ngày bắt đầu và hạn hoàn thành nên "
                                    + "chưa ghi giờ được. Hãy nhờ người giao việc bổ sung.";
            }
            else
            {
                model.CanLog = true;
            }

            return model;
        }
    }
}
