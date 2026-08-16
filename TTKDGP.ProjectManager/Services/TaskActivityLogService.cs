using System;
using System.Collections.Generic;
using System.Linq;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Services
{
    /// <summary>
    /// Ghi và đọc nhật ký thao tác của từng đầu việc (ai làm gì, lúc nào, giá trị cũ/mới nếu có).
    ///
    /// Một điểm ghi DUY NHẤT cho mọi loại thao tác (<see cref="Record"/>) và một hàm so sánh dùng
    /// chung cho mọi nơi sửa thông tin chung/trạng thái/tiến độ (<see cref="RecordFieldChanges"/>)
    /// — controller chỉ việc chụp ảnh trước khi sửa rồi gọi lại sau khi lưu, không tự ghép câu.
    /// </summary>
    public static class TaskActivityLogService
    {
        /// <summary>Ghi một dòng nhật ký. Dùng cho các thao tác không có cặp giá trị cũ/mới rõ ràng
        /// (ghi giờ, việc cần làm, bình luận) — <paramref name="description"/> là câu hiển thị sẵn.</summary>
        public static void Record(int taskId, int actorUserId, string actorName, string action,
            string description, string fieldName = null, string oldValue = null, string newValue = null)
        {
            Repository.TaskActivityLogs.Insert(new TaskActivityLog
            {
                TaskId = taskId,
                ActorUserId = actorUserId,
                ActorName = string.IsNullOrWhiteSpace(actorName) ? "(không rõ)" : actorName,
                Action = action,
                FieldName = fieldName,
                OldValue = oldValue,
                NewValue = newValue,
                Description = description,
                CreatedAt = DateTime.Now
            });
        }

        /// <summary>
        /// Chụp lại các trường cần so sánh của một đầu việc TRƯỚC khi sửa. Gọi trước mọi thao tác
        /// có thể đổi thông tin chung/trạng thái/tiến độ, giữ lại để gọi <see cref="RecordFieldChanges"/>
        /// sau khi lưu xong.
        /// </summary>
        public static WorkTask Snapshot(WorkTask task)
        {
            return new WorkTask
            {
                Id = task.Id,
                Title = task.Title,
                AssigneeUserId = task.AssigneeUserId,
                AssigneeName = task.AssigneeName,
                Priority = task.Priority,
                Kind = task.Kind,
                StartDate = task.StartDate,
                DueDate = task.DueDate,
                State = task.State,
                Progress = task.Progress
            };
        }

        /// <summary>
        /// So sánh ảnh chụp TRƯỚC (<paramref name="before"/>, từ <see cref="Snapshot"/> hoặc bản ghi
        /// gốc chưa sửa) với đầu việc SAU khi đã lưu, ghi MỘT dòng log cho MỖI trường có giá trị
        /// khác nhau. Không phân biệt "đổi trạng thái" hay "sửa thông tin chung" thành hai luồng
        /// riêng — driven hoàn toàn bởi so sánh thật, nên gọi ở bất kỳ điểm sửa nào (SetState, Report,
        /// Edit...) đều không bao giờ ghi trùng hay ghi thiếu.
        /// </summary>
        public static void RecordFieldChanges(WorkTask before, WorkTask after, int actorUserId, string actorName)
        {
            if (before == null || after == null) return;

            var pairs = new[]
            {
                new { Field = "Tên công việc", Old = before.Title ?? "", New = after.Title ?? "" },
                new { Field = "Người thực hiện", Old = AssigneeLabel(before), New = AssigneeLabel(after) },
                new { Field = "Độ ưu tiên", Old = TaskPriorities.Display(before.Priority), New = TaskPriorities.Display(after.Priority) },
                new { Field = "Loại công việc", Old = TaskKinds.Display(before.Kind), New = TaskKinds.Display(after.Kind) },
                new { Field = "Ngày bắt đầu", Old = DateLabel(before.StartDate), New = DateLabel(after.StartDate) },
                new { Field = "Hạn hoàn thành", Old = DateLabel(before.DueDate), New = DateLabel(after.DueDate) },
                new { Field = "Trạng thái", Old = TaskStates.Display(before.State), New = TaskStates.Display(after.State) },
                new { Field = "Tiến độ", Old = before.Progress + "%", New = after.Progress + "%" }
            };

            foreach (var p in pairs)
            {
                if (string.Equals(p.Old, p.New, StringComparison.Ordinal)) continue;

                Record(after.Id, actorUserId, actorName, TaskActivityActions.FieldChanged,
                    string.Format("Đổi {0}: \"{1}\" → \"{2}\"", p.Field, p.Old, p.New),
                    p.Field, p.Old, p.New);
            }
        }

        private static string AssigneeLabel(WorkTask task)
        {
            if (task.AssigneeUserId <= 0) return "(chưa giao)";
            return string.IsNullOrWhiteSpace(task.AssigneeName) ? "?" : task.AssigneeName;
        }

        private static string DateLabel(DateTime? date)
        {
            return date.HasValue ? date.Value.ToString("dd/MM/yyyy") : "(chưa có)";
        }

        /// <summary>Toàn bộ nhật ký của một đầu việc, mới nhất lên trước.</summary>
        public static List<TaskActivityLog> GetForTask(int taskId)
        {
            return Repository.TaskActivityLogs.All()
                .Where(l => l.TaskId == taskId)
                .OrderByDescending(l => l.CreatedAt)
                .ThenByDescending(l => l.Id)
                .ToList();
        }
    }
}
