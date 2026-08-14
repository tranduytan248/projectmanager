using System;
using System.Linq;
using System.Web.Mvc;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;
using TTKDGP.ProjectManager.Models.Api;
using TTKDGP.ProjectManager.Services;

namespace TTKDGP.ProjectManager.Controllers.Api
{
    /// <summary>
    /// Danh sach + tao moi dau viec cua mot du an — man "Checklist" cua mobile, tuong duong
    /// ChecklistController.Index/Edit ben web (ban GRID, khong phai Kanban) nhung tra danh sach
    /// phang mot lan (khong PagedList, khong cay cha/con) — mobile tu loc/tim theo tu khoa o
    /// client. Form tao moi RUT GON so voi web: khong co Muc cha (luon tao muc goc), Ngay bat dau
    /// (tu gan hom nay), Tuan/Nam (tu suy tu Han hoan thanh), Trang thai (luon "Chua bat dau" luc
    /// tao) — quyet dinh nay ghi trong Memory.md o goc repo, muc "Nut Thêm mới trong Checklist mobile".
    /// </summary>
    [ApiAuthorize]
    public class ChecklistApiController : BaseController
    {
        [HttpGet]
        public ActionResult Index(int projectId)
        {
            var project = Repository.WorkProjects.Find(projectId);
            if (project == null || !CanViewProject(projectId)) return HttpNotFound();

            // Y het ChecklistController.Index: chi lay Checklist + HoTro, KHONG ke viec ngoai du
            // an (NgoaiDuAn khong bao gio gan ProjectId nen von da khong lot vao day).
            var tasks = WorkService.TasksOfProject(projectId)
                .Where(t => TaskKinds.InProject.Contains(t.Kind))
                .ToList();

            var dto = new ChecklistDto
            {
                ProjectId = project.Id,
                ProjectName = project.Name,
                PmName = project.PmName,
                CanEdit = CanEditProject(projectId),
                TotalCount = tasks.Count,
                DoneCount = tasks.Count(t => t.State == TaskStates.Done),
                OverdueCount = tasks.Count(t => t.IsOverdue),
                Tasks = tasks.Select(ApiMappers.ToDto).ToList(),
                Assignees = ActiveMemberIds(projectId)
                    .Select(id => Repository.Users.Find(id))
                    .Where(u => u != null)
                    .OrderBy(u => u.FullName, System.StringComparer.CurrentCulture)
                    .Select(u => new AssigneeOptionDto { UserId = u.Id, FullName = u.FullName })
                    .ToList()
            };
            dto.DonePercent = dto.TotalCount == 0 ? 0 : (int)System.Math.Round(dto.DoneCount * 100.0 / dto.TotalCount);

            return Json(dto, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Them mot dau viec moi — chi PM du an hoac Quan ly To (CanEditProject). Form rut gon:
        /// khong nhan ParentId/StartDate/Week/Year/State tu client, tu gan gia tri mac dinh y het
        /// quyet dinh trong Memory.md. Ma viec (Code) KHONG bat buoc va KHONG kiem trung — dung
        /// dung thuc te ben web (WorkTask.Code chi co [StringLength], khong co [Required] hay
        /// kiem trung, du _EditForm.cshtml co ghi chu "chi can duy nhat" nhu mot quy uoc, khong
        /// phai rang buoc thuc su).
        /// </summary>
        [HttpPost]
        public ActionResult Create(int projectId, string title, string code, string kind,
            string priority, int assigneeUserId, DateTime? dueDate, string description)
        {
            var project = Repository.WorkProjects.Find(projectId);
            if (project == null || !CanEditProject(projectId)) return HttpNotFound();

            if (string.IsNullOrWhiteSpace(title)) return BadRequest("Vui lòng nhập tên công việc.");
            if (!dueDate.HasValue) return BadRequest("Vui lòng nhập hạn hoàn thành.");

            var memberIds = ActiveMemberIds(projectId);
            if (assigneeUserId > 0 && !memberIds.Contains(assigneeUserId))
            {
                return BadRequest("Người thực hiện phải đang tham gia dự án này.");
            }

            var due = dueDate.Value;
            var task = new WorkTask
            {
                ProjectId = projectId,
                ParentId = 0,
                Kind = TaskKinds.InProject.Contains(kind) ? kind : TaskKinds.Checklist,
                Priority = TaskPriorities.Parse(priority),
                Title = title.Trim(),
                Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim(),
                AssigneeUserId = assigneeUserId,
                StartDate = DateTime.Today,
                DueDate = due,
                Week = WeekHelper.GetWeek(due),
                Year = WeekHelper.GetYear(due),
                State = TaskStates.NotStarted,
                Progress = 0,
                Description = HtmlSanitizer.Clean(description)
            };

            WorkService.FillNames(task);

            var siblings = Repository.WorkTasks.All()
                .Where(t => t.ProjectId == projectId && TaskKinds.InProject.Contains(t.Kind) && t.ParentId == 0)
                .ToList();
            task.SortOrder = siblings.Count == 0 ? 10 : siblings.Max(t => t.SortOrder) + 10;

            task.CreatedAt = DateTime.Now;
            task.CreatedBy = CurrentUser == null ? null : CurrentUser.FullName;
            task.AssignedByUserId = CurrentUserId;

            Repository.WorkTasks.Insert(task);

            // Y het ChecklistController.Edit: bao cho nguoi duoc giao (tru khi tu giao cho minh).
            if (task.AssigneeUserId > 0 && task.AssigneeUserId != CurrentUserId)
            {
                NotificationService.ProjectTaskAssigned(task, CurrentUser == null ? null : CurrentUser.FullName);
            }

            return Json(ApiMappers.ToDto(task));
        }

        /// <summary>Thanh vien DANG HOAT DONG cua du an (chua roi) — y het ProjectMembers ben web,
        /// nhung chi tra UserId (khong can gop nhieu dong assignment/nguoi nhu ben do).</summary>
        private static int[] ActiveMemberIds(int projectId)
        {
            return WorkService.AssignmentsOfProject(projectId)
                .Where(a => a.IsActive)
                .Select(a => a.UserId)
                .Distinct()
                .ToArray();
        }

        private ActionResult BadRequest(string message)
        {
            Response.StatusCode = 400;
            Response.TrySkipIisCustomErrors = true;
            return Json(new { error = message });
        }
    }
}
