using System.Linq;
using System.Web.Mvc;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;
using TTKDGP.ProjectManager.Services;

namespace TTKDGP.ProjectManager.Controllers.Api
{
    /// <summary>Toan bo cong viec cua mobile — man "Cong viec".</summary>
    [ApiAuthorize]
    public class MyWorkApiController : BaseController
    {
        /// <summary>
        /// scope="team": toan bo cong viec cua Ca To (dung cho the "Viec chua xong"/"Qua han"
        /// tren Dashboard) thay vi chi viec cua rieng minh — CHI hieu luc khi IsTeamManager,
        /// khong co quyen thi am tham coi nhu "mine" thay vi tra loi, khong lo du lieu toan To
        /// cho nguoi khong co quyen. filter="overdue"/"open" loc them tren nguon da chon.
        /// Giu nguyen hinh dang JSON tra ve (mang TaskDto phang) de khong pha hop dong cu.
        /// </summary>
        [HttpGet]
        public ActionResult Index(string scope = "mine", string filter = null)
        {
            var source = scope == "team" && IsTeamManager
                ? WorkService.AllTasks()
                : WorkService.TasksOfUser(CurrentUserId);

            if (filter == "overdue") source = source.Where(t => t.IsOverdue).ToList();
            else if (filter == "open") source = source.Where(t => !TaskStates.IsClosed(t.State)).ToList();

            var tasks = source.Select(ApiMappers.ToDto).ToList();

            return Json(tasks, JsonRequestBehavior.AllowGet);
        }
    }
}
