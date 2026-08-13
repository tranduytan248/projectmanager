using System.Web.Mvc;
using TTKDGP.ProjectManager.Infrastructure;

namespace TTKDGP.ProjectManager.Controllers.Api
{
    /// <summary>Du an nguoi dung dang tham gia hoac lam PM — man "Du an" cua mobile.</summary>
    [ApiAuthorize]
    public class MyProjectsApiController : BaseController
    {
        [HttpGet]
        public ActionResult Index()
        {
            var projects = ApiMappers.MyProjects(CurrentUserId);
            return Json(projects, JsonRequestBehavior.AllowGet);
        }
    }
}
