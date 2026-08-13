using System.Linq;
using System.Web.Mvc;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Services;

namespace TTKDGP.ProjectManager.Controllers.Api
{
    /// <summary>Toan bo cong viec cua mobile — man "Cong viec".</summary>
    [ApiAuthorize]
    public class MyWorkApiController : BaseController
    {
        [HttpGet]
        public ActionResult Index()
        {
            var tasks = WorkService.TasksOfUser(CurrentUserId)
                .Select(ApiMappers.ToDto)
                .ToList();

            return Json(tasks, JsonRequestBehavior.AllowGet);
        }
    }
}
