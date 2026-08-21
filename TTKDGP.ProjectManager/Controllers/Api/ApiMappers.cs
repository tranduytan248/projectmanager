using System;
using System.Collections.Generic;
using System.Linq;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Models;
using TTKDGP.ProjectManager.Models.Api;
using TTKDGP.ProjectManager.Services;

namespace TTKDGP.ProjectManager.Controllers.Api
{
    /// <summary>
    /// Doi tu model nghiep vu (WorkTask/WorkProject) sang DTO JSON — dung chung cho ca ba
    /// controller API (Dashboard/MyWork/MyProjects) de mot cho duy nhat quyet dinh cac truong
    /// nay tra ve nhu the nao, khong lap lai o tung noi.
    /// </summary>
    internal static class ApiMappers
    {
        /// <summary>
        /// Du an nguoi dung dang tham gia hoac lam PM — Y HET logic loc cua
        /// MyWorkController.Projects (PM hoac co WorkAssignment), viet lai o day vi khong co san
        /// mot service method dung chung.
        /// </summary>
        public static List<ProjectSummaryDto> MyProjects(int userId)
        {
            var projects = Repository.WorkProjects.All();
            var myAssignments = Repository.WorkAssignments.All().Where(a => a.UserId == userId).ToList();
            var stats = WorkService.StatsByProject(WorkService.AllTasks());
            var memberCounts = Repository.WorkAssignments.All()
                .Where(a => !a.LeftAt.HasValue)
                .GroupBy(a => a.ProjectId)
                .ToDictionary(g => g.Key, g => g.Select(a => a.UserId).Distinct().Count());

            var result = new List<ProjectSummaryDto>();
            foreach (var project in projects)
            {
                var isPm = project.PmUserId == userId;
                var isMember = myAssignments.Any(a => a.ProjectId == project.Id);
                if (!isPm && !isMember) continue;

                TaskStat stat;
                stats.TryGetValue(project.Id, out stat);
                int memberCount;
                memberCounts.TryGetValue(project.Id, out memberCount);

                result.Add(ToDto(project, stat, memberCount));
            }

            return result;
        }

        public static TaskDto ToDto(WorkTask task)
        {
            return new TaskDto
            {
                Id = task.Id,
                Code = task.Code,
                Title = task.Title,
                ProjectName = task.ProjectName,
                State = task.State,
                Priority = task.Priority,
                DueDate = task.DueDate,
                IsOverdue = task.IsOverdue,
                IsDueToday = task.DueDate.HasValue && task.DueDate.Value.Date == DateTime.Today,
                AssigneeName = task.AssigneeName,
                AssigneeUserId = task.AssigneeUserId,
                Progress = task.Progress,
                Kind = task.Kind,
                ParentId = task.ParentId,
                CreatedAt = task.CreatedAt
            };
        }

        /// <summary>Ban co kem CanEdit tung dong — dung cho danh sach nhieu nguoi cung xem chung
        /// (vi du Kanban Checklist mobile), noi man can biet ngay tung the co doi trang thai duoc
        /// hay khong ma khong phai goi rieng Detail cho tung task.</summary>
        public static TaskDto ToDto(WorkTask task, bool canEdit)
        {
            var dto = ToDto(task);
            dto.CanEdit = canEdit;
            return dto;
        }

        /// <summary>Chi tiet mot dau viec — TaskDetailDto ke thua het truong cua TaskDto, chi
        /// them Description (qua ToPlainText, cung cach ProjectDetailDto dang lam) + StartDate +
        /// CompletedAt.</summary>
        public static TaskDetailDto ToDetailDto(WorkTask task)
        {
            return new TaskDetailDto
            {
                Id = task.Id,
                Code = task.Code,
                Title = task.Title,
                ProjectName = task.ProjectName,
                State = task.State,
                Priority = task.Priority,
                DueDate = task.DueDate,
                IsOverdue = task.IsOverdue,
                IsDueToday = task.DueDate.HasValue && task.DueDate.Value.Date == DateTime.Today,
                AssigneeName = task.AssigneeName,
                AssigneeUserId = task.AssigneeUserId,
                Progress = task.Progress,
                Kind = task.Kind,
                ParentId = task.ParentId,
                CreatedAt = task.CreatedAt,
                Description = Infrastructure.HtmlSanitizer.ToPlainText(task.Description),
                StartDate = task.StartDate,
                CompletedAt = task.CompletedAt
            };
        }

        /// <summary>Chi tiet day du mot dau viec cho man "Chi tiet cong viec" tren mobile — them
        /// het cac truong tinh ma TaskDetailDto rut gon truoc day chua co, y het du lieu
        /// ChecklistController.Detail dang dua vao ViewBag/_Detail.cshtml.</summary>
        public static TaskDetailDto ToFullTaskDto(WorkTask task, string parentTitle, bool canEdit, bool canEditAll)
        {
            var dto = ToDetailDto(task);
            dto.ProjectId = task.ProjectId;
            dto.Week = task.Week;
            dto.Year = task.Year;
            dto.DaysLeft = task.DaysLeft;
            dto.IsOnTime = task.IsOnTime;
            dto.ParentTitle = parentTitle;
            dto.BonusPercent = task.BonusPercent;
            dto.HasAttachment = task.HasAttachment;
            dto.AttachmentName = task.AttachmentName;
            dto.CanEdit = canEdit;
            dto.CanEditAll = canEditAll;
            return dto;
        }

        /// <summary>Mot luot ghi gio cong — CanDelete tinh san o server: dung cua chinh nguoi dang
        /// xem VA viec chua dong, y het dieu kien ChecklistController.DeleteTimeLog dang gac.</summary>
        public static TimeLogEntryDto ToDto(WorkTimeLog log, int currentUserId, bool taskOpen)
        {
            return new TimeLogEntryDto
            {
                Id = log.Id,
                WorkDate = log.WorkDate,
                Hours = log.Hours,
                Note = log.Note,
                UserId = log.UserId,
                UserName = log.UserName,
                CanDelete = taskOpen && log.UserId == currentUserId
            };
        }

        /// <summary>Boc TaskTimeLogViewModel (dung chung voi web qua TimeLogService.BuildViewModel)
        /// sang DTO JSON cho mobile.</summary>
        public static TimeLogSummaryDto ToDto(TaskTimeLogViewModel model, bool taskOpen)
        {
            return new TimeLogSummaryDto
            {
                TaskCap = model.TaskCap,
                TaskTotal = model.TaskTotal,
                TaskRemaining = model.TaskRemaining,
                TodayTotal = model.TodayTotal,
                TodayRemaining = model.TodayRemaining,
                MaxPerDay = model.MaxPerDay,
                CanLog = model.CanLog,
                BlockedReason = model.BlockedReason,
                Logs = model.Logs.Select(l => ToDto(l, model.CurrentUserId, taskOpen)).ToList()
            };
        }

        public static TodoItemDto ToDto(WorkTaskTodo todo)
        {
            return new TodoItemDto
            {
                Id = todo.Id,
                Content = todo.Content,
                IsDone = todo.IsDone,
                CreatedByName = todo.CreatedByName,
                CreatedAt = todo.CreatedAt
            };
        }

        public static TodoSummaryDto ToDto(TaskTodoViewModel model)
        {
            return new TodoSummaryDto
            {
                CanManage = model.CanManage,
                DoneCount = model.DoneCount,
                TotalCount = model.TotalCount,
                Items = model.Items.Select(ToDto).ToList()
            };
        }

        /// <summary>Mot luot trao doi — Content tra HTML da qua HtmlSanitizer.Clean, mobile gio tu
        /// render dinh dang qua bo doc rieng thay vi phang ve chu thuong. Content da duoc loc luc
        /// luu, nhung o day Clean lai LAN NUA truoc khi tra ra API — cung nguyen tac "phong thu
        /// hai lop" ma web dang lam qua HtmlSanitizer.ToDisplay ngay truoc Html.Raw (_Comments.cshtml):
        /// neu mot luong ghi nao khac trong tuong lai lam sot buoc Clean() luc luu (import hang
        /// loat, cong cu quan tri...), API mobile van khong dua thang HTML chua loc ra ngoai.
        /// Noi dung da thu hoi thi khong tra Content.</summary>
        public static TaskCommentDto ToDto(WorkComment comment, int currentUserId, bool canModerate)
        {
            return new TaskCommentDto
            {
                Id = comment.Id,
                AuthorName = comment.AuthorName,
                Content = comment.IsDeleted ? null : Infrastructure.HtmlSanitizer.Clean(comment.Content),
                CreatedAt = comment.CreatedAt,
                IsDeleted = comment.IsDeleted,
                HasAttachment = !comment.IsDeleted && comment.HasAttachment,
                AttachmentName = comment.IsDeleted ? null : comment.AttachmentName,
                CanRecall = !comment.IsDeleted && (comment.UserId == currentUserId || canModerate)
            };
        }

        /// <summary>Nguoi co the bi @nhac — y het cach ChecklistController.BuildComments dung
        /// WorkService.TaskParticipants(task) (tra List&lt;User&gt;) roi tu rut gon sang
        /// TaskMentionOption ben web.</summary>
        public static TaskMentionOptionDto ToMentionOptionDto(User user)
        {
            return new TaskMentionOptionDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                UserName = user.UserName
            };
        }

        public static CommentsSummaryDto ToCommentsDto(System.Collections.Generic.List<WorkComment> comments,
            System.Collections.Generic.List<User> participants,
            int currentUserId, bool canModerate)
        {
            return new CommentsSummaryDto
            {
                Comments = comments.Select(c => ToDto(c, currentUserId, canModerate)).ToList(),
                Participants = participants.Select(ToMentionOptionDto).ToList()
            };
        }

        public static TaskActivityLogDto ToDto(TaskActivityLog log)
        {
            return new TaskActivityLogDto
            {
                Id = log.Id,
                ActorName = string.IsNullOrWhiteSpace(log.ActorName) ? "(không rõ)" : log.ActorName,
                Action = log.Action,
                Description = log.Description,
                CreatedAt = log.CreatedAt
            };
        }

        public static ProjectSummaryDto ToDto(WorkProject project, TaskStat stat, int memberCount)
        {
            return new ProjectSummaryDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                ProgressPercent = stat != null ? stat.Percent : 0,
                MemberCount = memberCount
            };
        }

        /// <summary>
        /// Rut gon KpiMonth (day du cot cho bang chi tiet ben web) thanh vai con so chinh cho man
        /// dien thoai. Cong thuc ScorePercent/HoursPercent/HoursShort lay Y HET
        /// DashboardViewModel ben web (Controllers/DashboardController.cs) de hai noi khong bao
        /// gio le%ch nhau ve cung mot thang diem.
        /// </summary>
        public static KpiSummaryDto ToDto(KpiMonth kpi)
        {
            if (kpi == null) return null;

            var scoreMax = KpiService.MaxQualityPoint > 0 ? KpiService.MaxQualityPoint : 100;
            var scorePercent = (int)Math.Round(kpi.FinalPoint * 100 / scoreMax);
            scorePercent = scorePercent < 0 ? 0 : (scorePercent > 100 ? 100 : scorePercent);

            var hoursPercent = 100;
            if (kpi.RequiredHours > 0)
            {
                var p = (int)Math.Round(kpi.WorkedHours * 100 / kpi.RequiredHours);
                hoursPercent = p > 100 ? 100 : p;
            }

            var missing = kpi.RequiredHours - kpi.WorkedHours;

            return new KpiSummaryDto
            {
                FinalPoint = kpi.FinalPoint,
                Rank = kpi.Rank,
                ScoreMax = scoreMax,
                ScorePercent = scorePercent,
                WorkedHours = kpi.WorkedHours,
                RequiredHours = kpi.RequiredHours,
                HoursPercent = hoursPercent,
                HoursShort = missing > 0 ? Math.Round(missing, 1) : 0
            };
        }

        public static NotificationDto ToDto(UserNotification notification)
        {
            return new NotificationDto
            {
                Id = notification.Id,
                Type = notification.Type,
                Message = notification.Message,
                ProjectId = notification.ProjectId,
                TaskId = notification.TaskId,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt
            };
        }

        public static UpcomingLeaveDto ToDto(LeaveRequest leave)
        {
            return new UpcomingLeaveDto
            {
                FromDate = leave.FromDate,
                ToDate = leave.ToDate,
                Days = leave.Days,
                Kind = leave.Kind
            };
        }

        public static ProjectAttentionDto ToAttentionDto(WorkProject project, TaskStat stat, int overdueCount)
        {
            return new ProjectAttentionDto
            {
                Id = project.Id,
                Name = project.Name,
                Customer = project.Customer,
                PmName = project.PmName,
                ProgressPercent = stat != null ? stat.Percent : 0,
                OverdueCount = overdueCount
            };
        }

        public static ProjectMemberDto ToDto(WorkAssignment assignment)
        {
            return new ProjectMemberDto
            {
                Id = assignment.Id,
                UserFullName = assignment.UserFullName,
                IsPm = assignment.IsPm,
                Role = assignment.Role,
                Phase = assignment.Phase,
                JoinedAt = assignment.JoinedAt,
                LeftAt = assignment.LeftAt,
                IsActive = assignment.IsActive,
                Note = assignment.Note
            };
        }

        public static WeekReportSummaryDto ToDto(WorkWeekReport report)
        {
            return new WeekReportSummaryDto
            {
                Year = report.Year,
                Week = report.Week,
                SubmittedAt = report.SubmittedAt,
                IsSubmitted = report.IsSubmitted,
                IsOnTime = report.IsOnTime
            };
        }

        /// <summary>Ban ghi tuan hien tai co the chua ton tai (chua ai nop) — van tra ve mot DTO
        /// "chua nop" thay vi null, de mobile khong phai tu suy dien nam/tuan dang xem.</summary>
        public static WeekReportSummaryDto ToDto(WorkWeekReport report, int year, int week)
        {
            if (report != null) return ToDto(report);

            return new WeekReportSummaryDto
            {
                Year = year,
                Week = week,
                SubmittedAt = null,
                IsSubmitted = false,
                IsOnTime = false
            };
        }

        public static LeaveRequestDto ToDetailDto(LeaveRequest l)
        {
            return new LeaveRequestDto
            {
                Id = l.Id,
                Kind = l.Kind,
                FromDate = l.FromDate,
                ToDate = l.ToDate,
                IsHalfDay = l.IsHalfDay,
                HalfDaySession = l.HalfDaySession,
                Days = l.Days,
                Reason = l.Reason,
                State = l.State,
                ApprovedByName = l.ApprovedByName,
                ApprovedAt = l.ApprovedAt,
                ApproverNote = l.ApproverNote,
                CreatedAt = l.CreatedAt
            };
        }
    }
}
