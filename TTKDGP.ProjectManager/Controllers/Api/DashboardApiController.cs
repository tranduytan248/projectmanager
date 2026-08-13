using System;
using System.Linq;
using System.Web.Mvc;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;
using TTKDGP.ProjectManager.Models.Api;
using TTKDGP.ProjectManager.Services;

namespace TTKDGP.ProjectManager.Controllers.Api
{
    /// <summary>Tong quan cho man Home cua mobile: so du an + cac viec dang can lam.</summary>
    [ApiAuthorize]
    public class DashboardApiController : BaseController
    {
        /// <summary>Trung voi DashboardController.DueSoonDays — cung mot dinh nghia "sap den han".</summary>
        private const int DueSoonDays = 7;

        private const int TaskListLimit = 20;

        [HttpGet]
        public ActionResult Index()
        {
            var userId = CurrentUserId;
            var today = DateTime.Today;

            var mine = WorkService.TasksOfUser(userId);
            var open = mine.Where(t => !TaskStates.IsClosed(t.State)).ToList();
            var dueLimit = today.AddDays(DueSoonDays);

            // "Dang can lam": qua han hoac sap den han, viec tam dung khong tinh (dong ho da
            // dung) — y het DashboardController.BuildMyTasks, nhung khong bo theo thang dang
            // xem vi mobile khong co o chon thang.
            var tasks = open
                .Where(t => !TaskStates.IsClockStopped(t.State))
                .Where(t => t.IsOverdue || (t.DueDate.HasValue && t.DueDate.Value.Date <= dueLimit))
                .OrderByDescending(t => t.IsOverdue)
                .ThenBy(t => t.DueDate ?? DateTime.MaxValue)
                .Take(TaskListLimit)
                .Select(ApiMappers.ToDto)
                .ToList();

            var todayCount = open.Count(t => t.DueDate.HasValue && t.DueDate.Value.Date == today);
            var projects = ApiMappers.MyProjects(userId);

            var dto = new DashboardDto
            {
                ProjectCount = projects.Count,
                TodayTaskCount = todayCount,
                Projects = projects,
                Tasks = tasks
            };

            return Json(dto, JsonRequestBehavior.AllowGet);
        }
    }
}
