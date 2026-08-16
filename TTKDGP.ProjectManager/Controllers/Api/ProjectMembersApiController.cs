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
    /// "Nhan su du an" cua mobile — tuong duong WorkProjectsController.Members/AddMember/SetPm/
    /// EndMember/RemoveMember ben web, dung lai NGUYEN VEN logic/luat da co (WorkService.
    /// OpenAssignment/SyncCurrentPm/CurrentPmAssignment), chi doi cach tra ket qua (JSON danh sach
    /// ProjectMemberDto moi nhat thay vi redirect lai trang). CanEditProject(id) la lop kiem quyen
    /// THAT cho moi thao tac sua — y het web, khong chi dua vao [ApiAuthorize] o muc lop.
    /// </summary>
    [ApiAuthorize]
    public class ProjectMembersApiController : BaseController
    {
        [HttpGet]
        public ActionResult Index(int id)
        {
            var project = Repository.WorkProjects.Find(id);
            if (project == null || !CanViewProject(id)) return HttpNotFound();

            return Json(BuildList(id), JsonRequestBehavior.AllowGet);
        }

        /// <summary>Danh sach nguoi dung dang hoat dong + vai tro trong danh muc — do form "Them
        /// nhan su" tren mobile can truoc khi mo, giong ViewBag.UserOptions/ViewBag.Roles cua
        /// WorkProjectsController.AddMemberForm.</summary>
        [HttpGet]
        public ActionResult AddMemberForm(int id)
        {
            var project = Repository.WorkProjects.Find(id);
            if (project == null || !CanEditProject(id)) return HttpNotFound();

            var options = new ProjectMemberFormOptionsDto
            {
                Users = WorkService.ActiveUsers()
                    .Select(u => new AssigneeOptionDto { UserId = u.Id, FullName = u.FullName })
                    .ToList(),
                Roles = Repository.ActiveNames(Repository.MemberRoles)
            };

            return Json(options, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AddMember(int id, int userId, string role, string phase,
            bool isPm, DateTime? joinedAt, string note)
        {
            if (!CanEditProject(id)) return HttpNotFound();

            var project = Repository.WorkProjects.Find(id);
            var person = Repository.Users.Find(userId);
            if (project == null || person == null) return HttpNotFound();

            var open = WorkService.OpenAssignment(id, userId);
            if (open != null)
            {
                return BadRequest(string.Format(
                    "\"{0}\" đang tham gia dự án từ {1}. Hãy kết thúc giai đoạn đó trước khi phân công lại.",
                    person.FullName, open.JoinedAt.ToString("dd/MM/yyyy")));
            }

            var from = joinedAt ?? DateTime.Today;
            if (isPm) EndCurrentPm(id, from);

            Repository.WorkAssignments.Insert(new WorkAssignment
            {
                ProjectId = id,
                ProjectName = project.Name,
                UserId = userId,
                UserFullName = person.FullName,
                Role = string.IsNullOrWhiteSpace(role) ? null : role.Trim(),
                IsPm = isPm,
                Phase = string.IsNullOrWhiteSpace(phase) ? AssignmentPhases.Both : phase,
                JoinedAt = from,
                Note = note,
                CreatedAt = DateTime.Now
            });

            WorkService.SyncCurrentPm(id);

            if (userId != CurrentUserId) NotificationService.ProjectAdded(userId, project);

            return Json(BuildList(id));
        }

        [HttpPost]
        public ActionResult SetPm(int id, int assignmentId, DateTime? from)
        {
            if (!CanEditProject(id)) return HttpNotFound();

            var target = Repository.WorkAssignments.Find(assignmentId);
            if (target == null || target.ProjectId != id) return HttpNotFound();

            if (target.LeftAt.HasValue)
            {
                return BadRequest("Người này đã rời dự án, không đặt làm PM được.");
            }

            var since = from ?? DateTime.Today;
            EndCurrentPm(id, since, assignmentId);

            target.IsPm = true;
            Repository.WorkAssignments.Update(target);
            WorkService.SyncCurrentPm(id);

            return Json(BuildList(id));
        }

        [HttpPost]
        public ActionResult EndMember(int id, int assignmentId, DateTime? leftAt)
        {
            if (!CanEditProject(id)) return HttpNotFound();

            var assignment = Repository.WorkAssignments.Find(assignmentId);
            if (assignment == null || assignment.ProjectId != id) return HttpNotFound();

            var date = leftAt ?? DateTime.Today;
            if (date.Date < assignment.JoinedAt.Date)
            {
                return BadRequest("Ngày rời dự án không được sớm hơn ngày tham gia.");
            }

            assignment.LeftAt = date;
            Repository.WorkAssignments.Update(assignment);

            // Nguoi vua roi co the dang la PM — phai tinh lai, giong het web.
            WorkService.SyncCurrentPm(id);

            if (assignment.UserId != CurrentUserId)
            {
                NotificationService.ProjectRemoved(assignment.UserId, assignment.ProjectName, id);
            }

            return Json(BuildList(id));
        }

        [HttpPost]
        public ActionResult RemoveMember(int id, int assignmentId)
        {
            if (!CanEditProject(id)) return HttpNotFound();

            var assignment = Repository.WorkAssignments.Find(assignmentId);
            if (assignment == null || assignment.ProjectId != id) return HttpNotFound();

            var wasActive = assignment.IsActive;

            Repository.WorkAssignments.Delete(assignmentId);
            WorkService.SyncCurrentPm(id);

            if (wasActive && assignment.UserId != CurrentUserId)
            {
                NotificationService.ProjectRemoved(assignment.UserId, assignment.ProjectName, id);
            }

            return Json(BuildList(id));
        }

        /// <summary>Chuyen vai tro PM sang mot nguoi khac — y het WorkProjectsController.EndCurrentPm,
        /// tra ve dong PM cu (khong dung ket qua o day, chi can tac dung phu la go co IsPm).</summary>
        private static void EndCurrentPm(int projectId, DateTime from, int excludeAssignmentId = 0)
        {
            var current = WorkService.CurrentPmAssignment(projectId);
            if (current == null || current.Id == excludeAssignmentId) return;

            current.IsPm = false;
            Repository.WorkAssignments.Update(current);
        }

        private static object BuildList(int projectId)
        {
            return WorkService.AssignmentsOfProject(projectId).Select(ApiMappers.ToDto).ToList();
        }
    }
}
